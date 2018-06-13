using UnityEngine;
using System.Collections;
using System.Collections.Generic ;
using UnityEditorInternal ;
using UnityEngine.Experimental.Rendering ;
using UnityEngine.UI ;

public class HexGrid : MonoBehaviour {

    public int cellCountX = 20;
    public int cellCountZ = 15;
    

    public HexCell cellPrefab ;
    public HexGridChunk chunkPrefab ;
    public Text cellLabelPrefab ;
    public Texture2D noiseSource ;
    public int hashSeed ;
    public HexUnit unitPrefab;

    private int chunkCountX ;
    private int chunkCountZ ;


    private HexCell[] cells ;
    private HexGridChunk[] chunks ;
    private HexCellPriorityQueue searchFrontier ;
    private int searchFrontierPhase ;

    public bool HasPath { get { return pathList.Count > 0 ; } }
    private List<HexCell> pathList = new List<HexCell>() ;//保存寻路结果

    private List<HexUnit> unitList = new List<HexUnit>() ; //单位保存

    private HexCellShaderData cellShaderData ;

    private void Awake() {
        HexMetrics.noiseSource = noiseSource ;
        HexMetrics.InitTialzeHashGrid( hashSeed ) ;
        HexUnit.unitPrefab = unitPrefab ;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>() ;

        CreateMap( cellCountX , cellCountZ);
    }

    private void OnEnable() {
        if ( !HexMetrics.noiseSource ) {
            HexMetrics.noiseSource = noiseSource ;
            HexMetrics.InitTialzeHashGrid(hashSeed);
            HexUnit.unitPrefab = unitPrefab;
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

        writer.Write( unitList.Count );
        for ( int i = 0 ; i < unitList.Count ; i++ ) {
            unitList[i].Save( writer );
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
        int unitCount = reader.ReadInt32() ;
        for ( int i = 0 ; i < unitCount; i++ ) {
            LoadUnit( reader );
        }
    }

    #region 初始化地图

    public void CreateMap( int x , int z ) {
        if ( x <= 0 || x % HexMetrics.chunkSizeX != 0 ) {
            Debug.LogError( "Error : Unsupported map X size :" + x ) ;
            return ;
        }
        if ( z <= 0 || z % HexMetrics.chunkSizeZ != 0 ) {
            Debug.LogError( "Error : Unsupported map Z size" + x ) ;
            return ;
        }

        Clean();

        if ( chunks != null ) {
            for ( int i = 0 ; i < chunks.Length ; i++ ) {
                Destroy( chunks[ i ].gameObject ) ;
            }
        }

        cellCountX = x ;
        cellCountZ = z ;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX ;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ ;

        cellShaderData.Initialize( cellCountX,cellCountZ );

        CreateChunks() ;
        CreateCells() ;

        HexMapCamera.ValidatePosition() ;
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
        cell.Index = i ; 
        cell.ShaderData = cellShaderData ;
        cell.gameObject.name = "cell_" + i ;

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


    public HexCell GetCell( Vector3 position ) {
        position = transform.InverseTransformPoint( position ) ;
        HexCoordinates coordinates = HexCoordinates.FromPositon( position ) ;
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2 ;
        if ( index < 0 || index >= cells.Length ) return null ;
        return cells[ index ] ;
    }

    public HexCell GetCell( HexCoordinates coordinates ) {
        int z = coordinates.Z ;
        if ( z < 0 || z >= cellCountZ ) return null ;
        int x = coordinates.X + z / 2 ;
        if ( x < 0 || x >= cellCountX ) return null ;
        return cells[ x + z * cellCountX ] ;
    }

    public HexCell GetCell( Ray ray ) {
        RaycastHit hit ;
        if ( Physics.Raycast( ray , out hit ) ) {
            return GetCell( hit.point ) ;
        }
        return null ;
    }

    #endregion

    // Update is called once per frame
    void Update () {
	}

    public void ShowUI( bool visible ) {
        ClearPath();
        for ( int i = 0; i < chunks.Length; i++ ) {
            chunks[i].ShowUI( visible );
        }
    }


    #region 单位

    private void LoadUnit( System.IO.BinaryReader reader ) {
        HexUnit unit = Instantiate( unitPrefab ) ;
        unit.Load( reader , this ) ;
        unitList.Add( unit ) ;
    }

    public void AddUnit( HexCell location , float orientation ) {
        HexUnit unit = Instantiate( unitPrefab ) ;
        unit.transform.SetParent( transform , false ) ;
        unit.hexGrid = this ;
        unit.Location = location ;
        unit.Orientation = orientation ;
        unitList.Add( unit ) ;
    }

    public void RemoveUnit( HexUnit unit ) {
        unitList.Remove( unit ) ;
        unit.Die() ;
    }

    private void CleanUnits() {
        for ( int i = 0 ; i < unitList.Count ; i++ ) {
            unitList[ i ].Die() ;
        }
        unitList.Clear() ;
    }

    #endregion

    #region 寻路

    public void SetBeginCell( HexCell cell ) {
        ClearPath();
        cell.EnableHighlight(Color.blue);
    }

    public void FindDistancesTo( HexCell cell ) {
        Clean() ;
        StartCoroutine( Search( cell ) ) ;
    }

    public void FindPath( HexCell fromCell , HexCell toCell ) {
        Clean();
        StartCoroutine( Search( fromCell , toCell ) ) ;
    }

    public void FindPath( HexCell fromCell , HexCell toCell , int speed ) {
        ClearPath();

        bool pathExists = Search( fromCell , toCell , speed ) ;
        if ( pathExists ) ShowPath( fromCell , toCell , speed ) ;

        fromCell.EnableHighlight( Color.blue ) ;
        toCell.EnableHighlight( Color.red ) ;
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

    private bool Search( HexCell fromCell , HexCell toCell , int speed ) {
        searchFrontierPhase += 2 ;

        if ( searchFrontier == null ) searchFrontier = new HexCellPriorityQueue() ;
        else searchFrontier.Clear() ;

        //fromCell.EnableHighlight( Color.blue ) ;

        fromCell.SearchPhase = searchFrontierPhase ;
        fromCell.Distance = 0 ;
        searchFrontier.Enqueue( fromCell ) ;

        while ( searchFrontier.Count > 0 ) {
            HexCell current = searchFrontier.Dequeue() ;
            current.SearchPhase += 1 ;

            if ( current == toCell ) return true ;

            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;

                if ( Obstacle( current , neighbor ) ) continue ;
                if ( neighbor.SearchPhase > searchFrontierPhase ) continue ;

                int currentTurn = (current.Distance - 1) / speed ;
                int moveCost ;

                if ( current.HasRoadThroughEdge( d ) ) moveCost = 1 ;
                else {
                    if ( current.GetEdgeType( neighbor ) == HexEdgeType.Flat ) moveCost = 5 ;
                    else moveCost = 10 ;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel ;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed ;
                if ( turn > currentTurn ) distance = turn * speed + moveCost ;

                if ( neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase ;
                    neighbor.Distance = distance ;
                    neighbor.pathFrom = current ;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistancesTo( toCell.coordinates ) ;
                    searchFrontier.Enqueue( neighbor ) ;
                }
                else if ( distance < neighbor.Distance ) {
                    int oldPriority = neighbor.SearchPriority ;
                    neighbor.Distance = distance ;
                    neighbor.pathFrom = current ;
                    searchFrontier.Change( neighbor , oldPriority ) ;
                }
            }
        }
        return false ;
    }

    

    public void ClearPath() {
        for ( int i = 0 ; i < pathList.Count ; i++ ) {
            pathList[ i ].SetLabel( null ) ;
            pathList[i].DisableHighlight();
        }
        pathList.Clear();
    }

    private void ShowPath(HexCell fromCell, HexCell toCell, int speed) {
        HexCell current = toCell ;
        pathList.Add(current);

        while ( fromCell != current) {
            int turn = (current.Distance - 1) / speed ;
            current.SetLabel( turn );
            current.EnableHighlight();
            current = current.pathFrom ;
            pathList.Add( current );
        }
        pathList.Reverse();
    }

    public List<HexCell> GetPath() {
        return pathList ;
    }

    #endregion

    #region 视野

    public void IncreaseVisibility( HexCell fromCell , int range ) {
        List<HexCell> visibleCells = GetVisibleCells( fromCell , range ) ;
        for ( int i = 0 ; i < visibleCells.Count ; i++ ) {
            visibleCells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(visibleCells);
    }

    public void DecreaseVisibility( HexCell fromCell , int range ) {
        List<HexCell> visibleCells = GetVisibleCells( fromCell , range ) ;
        for ( int i = 0 ; i < visibleCells.Count ; i++ ) {
            visibleCells[ i ].DecreaseVisibility() ;
        }
        ListPool<HexCell>.Add(visibleCells) ;
    }

    private List<HexCell> GetVisibleCells( HexCell fromCell , int range ) {
        List<HexCell> visibilityCells = ListPool<HexCell>.Get() ;
        searchFrontierPhase += 2 ;

        if ( searchFrontier == null ) searchFrontier = new HexCellPriorityQueue() ;
        else searchFrontier.Clear() ;

        fromCell.SearchPhase = searchFrontierPhase ;
        fromCell.Distance = 0 ;
        searchFrontier.Enqueue( fromCell ) ;

        while ( searchFrontier.Count > 0 ) {
            HexCell current = searchFrontier.Dequeue() ;
            current.SearchPhase += 1 ;

            visibilityCells.Add( current ) ;

            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;

                if ( neighbor == null || neighbor.SearchPhase > searchFrontierPhase ) continue ;

                int distance = current.Distance + 1 ;
                if ( distance > range ) continue ;

                if ( neighbor.SearchPhase < searchFrontierPhase ) {
                    neighbor.SearchPhase = searchFrontierPhase ;
                    neighbor.Distance = distance ;
                    neighbor.SearchHeuristic = 0 ;
                    searchFrontier.Enqueue( neighbor ) ;
                }
                else if ( distance < neighbor.Distance ) {
                    int oldPriority = neighbor.SearchPriority ;
                    neighbor.Distance = distance ;
                    searchFrontier.Change( neighbor , oldPriority ) ;
                }
            }
        }
        return visibilityCells ;
    }

    #endregion



    public void Clean() {
        ClearPath() ;
        CleanUnits() ;
    }

    

    private bool Obstacle( HexCell cell , HexCell neighbor ) {
        if ( neighbor == null ) return true ;
        if ( neighbor.Unit ) return true ;
        if ( cell.HasRiver ) return true ;
        if ( neighbor.IsUnderWater ) return true ;
        if ( cell.Walled != neighbor.Walled ) return true;
        if ( cell.GetEdgeType( neighbor ) == HexEdgeType.Cliff ) return true;
        return false ;
    }

}
