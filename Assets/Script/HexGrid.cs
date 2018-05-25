using UnityEngine;
using System.Collections;
using UnityEngine.UI ;

public class HexGrid : MonoBehaviour {

    public int chunkCountX = 4 ;
    public int chunkCountZ = 3 ;

    public HexCell cellPrefab ;
    public HexGridChunk chunkPrefab ;
    public Text cellLabelPrefab ;
    public Texture2D noiseSource ;
    public int hashSeed ;

    private int cellCountX ;
    private int cellCountZ ;

    private HexCell[] cells ;
    private HexGridChunk[] chunks ;
    private Canvas gridCanvas ;
    private HexMesh hexMesh ;


    private void Awake() {
        HexMetrics.noiseSource = noiseSource ;
        HexMetrics.InitTialzeHashGrid( hashSeed ) ;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX ;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ ;

        CreateChunks() ;
        CreateCells() ;
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
        for ( int i = 0 ; i < cells.Length ; i++ ) {
            cells[i].Save( writer );
        }
    }

    public void Load( System.IO.BinaryReader reader ) {
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
        //label.rectTransform.SetParent( gridCanvas.transform,true );
        label.rectTransform.anchoredPosition3D = new Vector2(position.x,position.z);
        label.rectTransform.Rotate( Vector3.zero );
        label.text = cell.coordinates.ToStringOnSeparateLines() ;

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

}
