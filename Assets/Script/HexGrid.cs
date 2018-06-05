using UnityEngine;
using System.Collections;
using System.Collections.Generic ;
using UnityEngine.UI ;

public class HexGrid : MonoBehaviour {

    public int cellCountX = 20;
    public int cellCountZ = 15;
    

    public HexCell cellPrefab ;
    public HexGridChunk chunkPrefab ;
    public Text cellLabelPrefab ;
    public Texture2D noiseSource ;
    public int hashSeed ;

    private int chunkCountX ;
    private int chunkCountZ ;


    private HexCell[] cells ;
    private HexGridChunk[] chunks ;
    private HexCellPriorityQueue searchFrontier ;

    private void Awake() {
        HexMetrics.noiseSource = noiseSource ;
        HexMetrics.InitTialzeHashGrid( hashSeed ) ;

        CreateMap( cellCountX , cellCountZ);
    }

    public void CreateMap( int x, int z ) {
        if ( x <= 0 || x % HexMetrics.chunkSizeX != 0 ) {
            Debug.LogError( "Error : Unsupported map X size :" + x );
            return;
        }
        if ( z <= 0 || z % HexMetrics.chunkSizeZ != 0 ) {
            Debug.LogError( "Error : Unsupported map Z size" + x );
            return;
        }

        if ( chunks != null ) {
            for ( int i = 0 ; i < chunks.Length ; i++ ) {
                Destroy( chunks[i].gameObject );
            }
        }

        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();

        HexMapCamera.ValidatePosition();
    }

    private void OnEnable() {
        if ( !HexMetrics.noiseSource ) {
            HexMetrics.noiseSource = noiseSource ;
            HexMetrics.InitTialzeHashGrid(hashSeed);
        }
    }

    // Use this for initialization
    private void Start() {
    }


    public void Save( System.IO.BinaryWriter writer ) {
        writer.Write( cellCountX );
        writer.Write( cellCountZ );
        for ( int i = 0 ; i < cells.Length ; i++ ) {
            cells[i].Save( writer );
        }
    }

    public void Load( System.IO.BinaryReader reader ) {
        Clean();

        CreateMap(reader.ReadInt32(), reader.ReadInt32() );
        for (int i = 0; i < cells.Length; i++) {
            cells[ i ].Load( reader ) ;
        }
        for (int i = 0; i < cells.Length; i++) {
            cells[i].Refresh();
        }
    }


    private void CreateChunks() {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for ( int z = 0, i = 0 ; z < chunkCountZ ; z++ ) {
            for ( int x = 0 ; x < chunkCountX ; x++ ) {
                HexGridChunk chunk = chunks[ i++ ] = Instantiate( chunkPrefab ) ;
                chunk.transform.SetParent( transform );
                chunk.name = chunkPrefab.name + "_" + i ;
            }
        }
    }

    private void CreateCells() {
        cells = new HexCell[cellCountZ * cellCountX] ;
        for ( int z = 0 , i = 0 ; z < cellCountZ ; z++ ) {
            for ( int x = 0 ; x < cellCountX ; x++ ) {
                CreateCell( x , z , i++ ) ;
            }
        }
        
    }

    //初始化六边形基础信息
    private void CreateCell( int x , int z , int i ) {
        Vector3 position ;
        position.x = (x + (z & 1) * 0.5f) * (HexMetrics.innerRadius * 2f) ;
        position.y = 0f ;
        position.z = z * (HexMetrics.outerRadius * 1.5f) ;

        HexCell cell = cells[ i ] = Instantiate<HexCell>( cellPrefab ) ;
        //cell.transform.SetParent( transform,true );
        cell.transform.localPosition = position ;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates( x,z );
        cell.transform.name = "cell_" + i ;

        //添加相邻的HexCell
        if ( x > 0 ) {
            cell.SetNeighbor( HexDirectionEnum.Left , cells[ i - 1 ] ) ;
        }
        if ( z > 0 ) {
            if ( (z & 1) == 0 ) {
                cell.SetNeighbor( HexDirectionEnum.BottomRight , cells[ i - cellCountX] ) ;
                if ( x > 0 ) cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - cellCountX - 1 ] ) ;
            }
            else {
                cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - cellCountX] ) ;
                if ( x < cellCountX - 1 ) cell.SetNeighbor( HexDirectionEnum.BottomRight , cells[ i - cellCountX + 1 ] ) ;
            }
        }
        Text label = Instantiate<Text>( cellLabelPrefab ) ;
        label.rectTransform.anchoredPosition3D = new Vector2(position.x,position.z);
        label.rectTransform.Rotate( Vector3.zero );

        cell.uiRect = label.rectTransform;
        cell.Elevation = 0 ;

        AddCellToChunk( x , z , cell ) ;
    }

    

    private void AddCellToChunk( int x , int z , HexCell cell ) {
        int chunkX = x / HexMetrics.chunkSizeX ;
        int chunkZ = z / HexMetrics.chunkSizeZ ;
        int index = chunkX + chunkZ * chunkCountX ;
        HexGridChunk chunk = chunks[ index ] ;

        int localX = x - chunkX * HexMetrics.chunkSizeX ;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ ;
        chunk.AddCell( localX + localZ * HexMetrics.chunkSizeX , cell ) ;
    }
	
	// Update is called once per frame
	void Update () {
	}

    public void ShowUI( bool visible ) {
        for ( int i = 0; i < chunks.Length; i++ ) {
            chunks[i].ShowUI( visible );
        }
    }
    

    public HexCell GetCell( Vector3 position ) {
        position = transform.InverseTransformPoint( position );
        HexCoordinates coordinates = HexCoordinates.FromPositon( position );
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        if ( index < 0 ) return null ;
        return cells[ index ];
    }

    public HexCell GetCell( HexCoordinates coordinates ) {
        int z = coordinates.Z;
        if ( z < 0 || z >= cellCountZ ) return null;
        int x = coordinates.X + z / 2;
        if ( x < 0 || x >= cellCountX ) return null;
        return cells[ x + z * cellCountX ];
    }


    public void FindDistancesTo( HexCell cell ) {
        Clean() ;
        StartCoroutine( Search( cell ) ) ;
    }

    public void FindPath( HexCell fromCell , HexCell toCell ) {
        Clean();
        StartCoroutine( Search( fromCell , toCell ) ) ;
    }

    private IEnumerator Search( HexCell cell ) {
        
        WaitForSeconds delay = new WaitForSeconds( 1/60f );

        List<HexCell> fromtier = new List<HexCell>();
        cell.Distance = 0 ;
        fromtier.Add( cell );

        while ( fromtier.Count>0 ) {
            yield return delay ;
            HexCell current = fromtier[0] ;
            fromtier.RemoveAt( 0 );
            for (HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;

                if ( Obstacle( current ,neighbor ) ) continue ;
                int distance = current.Distance ;

                if ( current.HasRoadThroughEdge( d ) ) distance += 1 ;
                else {
                    if ( current.GetEdgeType( neighbor ) == HexEdgeType.Flat ) distance += 5 ;
                    else distance += 10 ;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel ;
                }

                if ( neighbor.Distance == int.MaxValue ) {
                    neighbor.Distance = distance;
                    fromtier.Add(neighbor);
                }
                else if(distance <neighbor.Distance){
                    neighbor.Distance = distance;

                }
                fromtier.Sort( ( x , y ) => x.Distance.CompareTo( y.Distance ) ) ;
            }
        }

        /*for ( int i = 0 ; i < cells.Length ; i++ ) {
            yield return delay ;
            cells[ i ].Distance = cell.coordinates.DistancesTo( cells[ i ].coordinates ) ;
        }*/
    }

    private IEnumerator Search(HexCell fromCell, HexCell toCell) {

        if ( searchFrontier == null ) searchFrontier = new HexCellPriorityQueue() ;
        else searchFrontier.Clear();

        fromCell.EnableHighlight(Color.blue);
        WaitForSeconds delay = new WaitForSeconds( 1 / 60f ) ;
        toCell.EnableHighlight(Color.red);

        fromCell.Distance = 0 ;
        searchFrontier.Enqueue( fromCell );

        while (searchFrontier.Count > 0 ) {
            yield return delay ;
            HexCell current = searchFrontier.Dequeue();

            if ( current == toCell ) {
                current = current.pathFrom ;
                while ( current != fromCell ) {
                    current.EnableHighlight();
                    current = current.pathFrom ;
                }
                break ;
            }

            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;

                if ( Obstacle( current , neighbor ) ) continue ;
                int distance = current.Distance ;

                if ( current.HasRoadThroughEdge( d ) ) distance += 1 ;
                else {
                    if ( current.GetEdgeType( neighbor ) == HexEdgeType.Flat ) distance += 5 ;
                    else distance += 10 ;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel ;
                }

                if ( neighbor.Distance == int.MaxValue ) {
                    neighbor.Distance = distance;
                    neighbor.pathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistancesTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else  if ( distance < neighbor.Distance ) {
                    int oldPriority = neighbor.SearchPriority ;
                    neighbor.Distance = distance;
                    neighbor.pathFrom = current;
                    searchFrontier.Change( neighbor, oldPriority);
                }
            }
        }
    }

    public void Clean() {
        StopAllCoroutines();
        for (int i = 0; i < cells.Length; i++) {
            cells[i].Distance = int.MaxValue;
            cells[i].DisableHighlight();
        }
    }

    private bool Obstacle( HexCell cell , HexCell neighbor ) {
        if ( neighbor == null ) return true ;
        if ( cell.HasRiver ) return true ;
        if ( neighbor.IsUnderWater ) return true ;
        if ( cell.Walled != neighbor.Walled ) return true;
        if ( cell.GetEdgeType( neighbor ) == HexEdgeType.Cliff ) return true;
        return false ;
    }
}
