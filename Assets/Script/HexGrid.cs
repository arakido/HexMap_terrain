using UnityEngine;
using System.Collections;
using System.Collections.Generic ;
using UnityEditorInternal ;
using UnityEngine.Experimental.Rendering ;
using UnityEngine.UI ;

public class HexGrid : MonoBehaviour {

    public int cellCountX = 20;
    public int cellCountZ = 15;

    public bool wrapping;

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
    private Transform[] columns ;

    public bool HasPath { get { return pathList.Count > 0 ; } }
    private List<HexCell> pathList = new List<HexCell>() ;//保存寻路结果

    private List<HexUnit> unitList = new List<HexUnit>() ; //单位保存

    private HexCellShaderData cellShaderData ;

    private int currentCenterColumnIndex = -1;  //当前中心Column的列


    private void Awake() {
        HexMetrics.noiseSource = noiseSource ;
        HexMetrics.InitTialzeHashGrid( hashSeed ) ;
        HexUnit.unitPrefab = unitPrefab ;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>() ;
        cellShaderData.hexGrid = this ;
        CreateMap( cellCountX , cellCountZ, wrapping);
    }

    private void OnEnable() {
        if ( !HexMetrics.noiseSource ) {
            HexMetrics.noiseSource = noiseSource ;
            HexMetrics.InitTialzeHashGrid(hashSeed);
            HexUnit.unitPrefab = unitPrefab;
            HexMetrics.wrapSize = wrapping ? cellCountX : 0;
            ResetVisibility();
        }
    }

    // Use this for initialization
    private void Start() {
    }


    public void Save( System.IO.BinaryWriter writer ) {
        writer.Write( cellCountX );
        writer.Write( cellCountZ );
        writer.Write( wrapping );
        for (int i = 0; i < cells.Length ; i++ ) {
            cells[i].Save( writer );
        }

        writer.Write( unitList.Count );
        for ( int i = 0 ; i < unitList.Count ; i++ ) {
            unitList[i].Save( writer );
        }
    }

    public void Load( System.IO.BinaryReader reader ) {
        Clean();

        CreateMap(reader.ReadInt32(), reader.ReadInt32() , reader.ReadBoolean());

        bool originaImmediateMode = cellShaderData.ImmediateMode ;
        cellShaderData.ImmediateMode = true ;
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

        cellShaderData.ImmediateMode = originaImmediateMode ;
    }

    #region 初始化地图

    public void CreateMap( int x , int z, bool _wrapping) {
        if ( x <= 0 || x % HexMetrics.chunkSizeX != 0 ) {
            Debug.LogError( "Error : Unsupported map X size :" + x ) ;
            return ;
        }
        if ( z <= 0 || z % HexMetrics.chunkSizeZ != 0 ) {
            Debug.LogError( "Error : Unsupported map Z size" + x ) ;
            return ;
        }

        Clean();

        if ( columns != null ) {
            for ( int i = 0 ; i < columns.Length ; i++ ) {
                Destroy(columns[ i ].gameObject ) ;
            }
        }

        cellCountX = x ;
        cellCountZ = z ;
        this.wrapping = _wrapping ;
        HexMetrics.wrapSize = wrapping ? cellCountX : 0 ;
        currentCenterColumnIndex = -1;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX ;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ ;

        cellShaderData.Initialize( cellCountX,cellCountZ );

        CreateChunks() ;
        CreateCells() ;

        HexMapCamera.ValidatePosition() ;
    }

    private void CreateChunks() {
        columns = new Transform[chunkCountX];
        for ( int x = 0 ; x < chunkCountX ; x++ ) {
            columns[x] = new GameObject("Column"+x).transform;
            columns[x].SetParent( transform,false );
        }
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for ( int z = 0, i = 0 ; z < chunkCountZ ; z++ ) {
            for ( int x = 0 ; x < chunkCountX ; x++ ) {
                HexGridChunk chunk = chunks[ i++ ] = Instantiate( chunkPrefab ) ;
                chunk.transform.SetParent( columns[x],false );
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
        position.x = (x + (z & 1) * 0.5f) * HexMetrics.innerDiameter ;
        position.y = 0f ;
        position.z = z * (HexMetrics.outerRadius * 1.5f) ;

        HexCell cell = cells[ i ] = Instantiate<HexCell>( cellPrefab ) ;
        //cell.transform.SetParent( transform,true );
        cell.transform.localPosition = position ;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates( x , z ) ;
        cell.Index = i ;
        cell.ColumnIndex = x / HexMetrics.chunkSizeX ;
        cell.ShaderData = cellShaderData ;
        cell.gameObject.name = "cell_" + i ;

        if ( wrapping ) cell.Explorable = z > 0 && z < cellCountZ - 1 ;
        else cell.Explorable = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1 ;

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition3D = new Vector2(position.x, position.z);
        label.rectTransform.Rotate(Vector3.zero);
        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        //添加相邻的HexCell
        if ( x > 0 ) {
            cell.SetNeighbor( HexDirectionEnum.Left , cells[ i - 1 ] ) ;
            if ( wrapping && x == cellCountX - 1 ) {
                cell.SetNeighbor( HexDirectionEnum.Right , cells[ i - x ] ) ;
            }
        }
        if ( z > 0 ) {
            if ( (z & 1) == 0 ) {
                cell.SetNeighbor(HexDirectionEnum.BottomRight , cells[ i - cellCountX] ) ;
                if ( x > 0 ) cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - cellCountX - 1 ] ) ;
                else if ( wrapping ) {
                    cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - 1 ] ) ;
                }
            }
            else {
                cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - cellCountX] ) ;
                if ( x < cellCountX - 1 ) cell.SetNeighbor( HexDirectionEnum.BottomRight , cells[ i - cellCountX + 1 ] ) ;
                else if ( wrapping ) {
                    cell.SetNeighbor( HexDirectionEnum.BottomRight , cells[ i - cellCountX * 2 + 1 ] ) ;
                }
            }
        }
        
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
        /*int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2 ;
        return GetCell(index) ;*/
        return GetCell( coordinates ) ;
    }

    public HexCell GetCell( HexCoordinates coordinates ) {
        int z = coordinates.Z ;
        if ( z < 0 || z >= cellCountZ ) return null ;
        int x = coordinates.X + z / 2 ;
        if ( x < 0 || x >= cellCountX ) return null ;
        return GetCell( x , z ) ;
    }

    public HexCell GetCell( int xOffset , int zOffset ) {

        return GetCell( xOffset + zOffset * cellCountX ) ;
    }

    public HexCell GetCell( int cellIndex ) {
        if (cellIndex < 0 || cellIndex >= cells.Length) return null;
        return cells[ cellIndex ] ;
    }

    public HexCell GetCell( Ray ray ) {
        RaycastHit hit ;
        if ( Physics.Raycast( ray , out hit ) ) {
            return GetCell( hit.point ) ;
        }
        return null ;
    }

    #endregion


    public void ShowUI( bool visible ) {
        ClearPath();
        for ( int i = 0; i < chunks.Length; i++ ) {
            chunks[i].ShowUI( visible );
        }
    }

    public void CenterMap( float xPosition ) {
        float columSizeX = HexMetrics.innerDiameter * HexMetrics.chunkSizeX ;
        int centerColumnIndex = (int) (xPosition / columSizeX) ;
        if ( centerColumnIndex == currentCenterColumnIndex ) return ;
        currentCenterColumnIndex = centerColumnIndex ;
        int minColumnIndex = centerColumnIndex - chunkCountX / 2 ;
        int maxColumnIndex = centerColumnIndex + chunkCountX / 2 ;

        Vector3 position  = Vector3.zero;
        for ( int i = 0 ; i < columns.Length ; i++ ) {
            if ( i < minColumnIndex ) {
                position.x = chunkCountX * columSizeX;
            }
            else if ( i > maxColumnIndex ) {
                position.x = chunkCountX * -columSizeX ;
            }
            else position.x = 0f ;
            columns[ i ].localPosition = position ;
        }
    }

    public void ResetVisibility() {
        for ( int i = 0 ; i < cells.Length ; i++ ) {
            cells[i].ResetVisibility();
        }
        for ( int i = 0 ; i < unitList.Count ; i++ ) {
            HexUnit unit = unitList[ i ] ;
            IncreaseVisibility( unit.Location,unit.VisionRange );
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
        //unit.transform.SetParent( transform , false ) ;
        unit.hexGrid = this ;
        unit.Location = location ;
        unit.Orientation = orientation ;
        unitList.Add( unit ) ;
    }

    public void MakeChildColumn( Transform child , int columnIndex ) {
        child.SetParent( columns[ columnIndex ] , false ) ;
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
    
    public void FindPath( HexCell fromCell , HexCell toCell , HexUnit unit ) {
        ClearPath();

        bool pathExists = Search( fromCell , toCell , unit) ;
        if ( pathExists ) ShowPath( fromCell , toCell , unit) ;

        fromCell.EnableHighlight( Color.blue ) ;
        toCell.EnableHighlight( Color.red ) ;
    }
    
    private bool Search( HexCell fromCell , HexCell toCell , HexUnit unit) {
        int speed = unit.Speed ;
        searchFrontierPhase += 2 ;

        if ( searchFrontier == null ) searchFrontier = new HexCellPriorityQueue() ;
        else searchFrontier.Clear() ;

        fromCell.SearchPhase = searchFrontierPhase ;
        fromCell.Distance = 0 ;
        searchFrontier.Enqueue( fromCell ) ;

        while ( searchFrontier.Count > 0 ) {
            HexCell current = searchFrontier.Dequeue() ;
            current.SearchPhase += 1 ;
            if ( current == toCell ) return true ;

            int currentTurn = (current.Distance - 1) / speed;
            for ( HexDirectionEnum d = HexDirectionEnum.Right ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;

                if ( neighbor == null || neighbor.SearchPhase > searchFrontierPhase ) continue ;
                if ( !unit.IsValidDestination( neighbor ) )  continue ;
                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if ( moveCost < 0 ) continue; 

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if ( turn > currentTurn ) distance = turn * speed + moveCost ;

                if ( neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase ;
                    neighbor.Distance = distance ;
                    neighbor.PathFrom = current ;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo( toCell.coordinates ) ;
                    searchFrontier.Enqueue( neighbor ) ;
                }
                else if ( distance < neighbor.Distance ) {
                    int oldPriority = neighbor.SearchPriority ;
                    neighbor.Distance = distance ;
                    neighbor.PathFrom = current ;
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

    private void ShowPath(HexCell fromCell, HexCell toCell, HexUnit unit) {
        HexCell current = toCell ;
        pathList.Add(current);
        int speed = unit.Speed ;

        while ( fromCell != current) {
            int turn = (current.Distance - 1) / speed;
            current.SetLabel( turn ) ;
            current.EnableHighlight();
            current = current.PathFrom ;
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

        range += fromCell.ViewElevation ;
        fromCell.SearchPhase = searchFrontierPhase ;
        fromCell.Distance = 0 ;
        searchFrontier.Enqueue( fromCell ) ;

        HexCoordinates fromCoordinates = fromCell.coordinates ;

        while ( searchFrontier.Count > 0 ) {
            HexCell current = searchFrontier.Dequeue() ;
            current.SearchPhase += 1 ;

            visibilityCells.Add( current ) ;

            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;

                if ( neighbor == null || neighbor.SearchPhase > searchFrontierPhase || !neighbor.Explorable) continue ;

                int distance = current.Distance + 1 ;
                if ( distance + neighbor.ViewElevation > range || distance > fromCoordinates.DistanceTo( neighbor.coordinates )) continue ;

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

}
