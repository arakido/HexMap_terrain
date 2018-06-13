using UnityEngine;
using System.Collections;
using UnityEngine.UI ;

public class HexCell : MonoBehaviour {

    [SerializeField] private HexCell[] neighbors; //临边

    public RectTransform uiRect;
    public HexCoordinates coordinates ;

    public HexGridChunk chunk;

    public Vector3 postion { get { return transform.localPosition; } }

    public HexCell pathFrom { get ; set ; }
    public int SearchHeuristic { get ; set ; }
    public int SearchPriority { get { return Distance + SearchHeuristic ; } }
    public int SearchPhase { get ; set ; }  //0未在边界中  1 处于边界中  2已移除边界
    public HexCell NextWithSamePriority { get ; set ; }
    public HexCellShaderData ShaderData { get ; set ; }

    public int Index { get ; set ; }

    public int TerrainTypeIndex {
        get { return terrainTypeIndex ; }
        set {
            if ( terrainTypeIndex != value ) {
                terrainTypeIndex = value ;
                //Refresh();
                ShaderData.RefreshTerrain( this );
            }
        }
    }
    private int terrainTypeIndex;


    public int Elevation {
        get { return elevation >= 0 ? elevation : 0 ; }
        set {
            if ( elevation == value ) return ;
            elevation = value ;
            RefreshPosition() ;
            RefreshRiver() ;
            RefreshRoads() ;
            Refresh() ;
        }
    }
    private int elevation = int.MinValue;

    public int Distance {
        get { return distance ; }
        set {
            distance = value ;
            //UpdateDistanceLabel();
        }
    }
    private int distance ;

    


    // Use this for initialization
    void Start( ) {

    }

    // Update is called once per frame
	void Update () {
	
	}

    #region Save & Load

    public void Save( System.IO.BinaryWriter writer ) {
        writer.Write( (byte)TerrainTypeIndex );
        //Debug.Log( " : " +  );
        writer.Write( (byte)Elevation );
        writer.Write( (byte)WaterLevel );
        writer.Write( (byte)UrbanLevel );
        writer.Write( (byte)FarmLevel );
        writer.Write( (byte)PlantLevel );
        writer.Write( (byte)SpecialIndex );
        writer.Write( walled );


        if ( HasInComingRiver ) writer.Write( (byte) (InComingRive + 128) ) ;
        else writer.Write( (byte) 0 ) ;
        if ( HasOutGoingRive ) writer.Write( (byte) (OutGoingRive + 128) ) ;
        else writer.Write( (byte) 0 ) ;

        int roadFlags = 0 ;
        for ( int i = 0 ; i < roads.Length ; i++ ) {
            if ( roads[ i ] ) roadFlags |= 1 << i ;
        }

        writer.Write( (byte)roadFlags );
    }

    public void Load( System.IO.BinaryReader reader ) {
        TerrainTypeIndex = reader.ReadByte() ;
        Elevation = reader.ReadByte();
        WaterLevel = reader.ReadByte();
        UrbanLevel = reader.ReadByte();
        FarmLevel = reader.ReadByte();
        PlantLevel = reader.ReadByte();
        SpecialIndex = reader.ReadByte();
        walled = reader.ReadBoolean() ;

        byte riverData = reader.ReadByte() ;
        HasInComingRiver = riverData >= 128 ;
        if (HasInComingRiver) InComingRive = (HexDirectionEnum)(riverData - 128);
        riverData = reader.ReadByte();
        HasOutGoingRive = riverData >= 128;
        if ( HasOutGoingRive ) OutGoingRive = (HexDirectionEnum) (riverData - 128) ;

        int roadFlags = reader.ReadByte() ;
        for ( int i = 0 ; i < roads.Length ; i++ ) {
            roads[ i ] = (roadFlags & (1 << i)) != 0 ;
        }
    }

    #endregion

    public void Refresh() {
        if ( !chunk ) return ;

        chunk.Refresh() ;
        for ( int i = 0 ; i < neighbors.Length ; i++ ) {
            HexCell neighbor = neighbors[ i ] ;
            if ( neighbor != null && neighbor.chunk != chunk ) {
                neighbor.chunk.Refresh();
            }
        }
        if (Unit) Unit.ValidateLoacation();
    }

    private void RefreshSelfOnly() {
        if ( chunk ) chunk.Refresh() ;
        if(Unit)Unit.ValidateLoacation();
    }

    private void RefreshPosition() {
        Vector3 position = transform.localPosition;
        position.y = Elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise( position ).y * 2f - 1f) * HexMetrics.elevationPerturbStrength ;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    private void UpdateDistanceLabel() {
        Text label = uiRect.GetComponent<Text>() ;
        label.text = distance == int.MaxValue ? "" : distance.ToString() ;
    }

    public void SetLabel( int num ) {
        SetLabel( num.ToString() ) ;
    }

    public void SetLabel( string text ) {
        Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }


    //设置相邻的三角形
    public void SetNeighbor( HexDirectionEnum direction , HexCell cell ) {
        neighbors[ (int) direction ] = cell ;
        cell.neighbors[ (int) direction.Opposite() ] = this ;
    }

    public HexCell GetNeighbor( HexDirectionEnum direction ) {
        return neighbors[ (int) direction ] ;
    }

    public HexEdgeType GetEdgeType( HexDirectionEnum direction ) {
        return HexMetrics.GetEdgeType( Elevation , GetNeighbor( direction ).Elevation ) ;
    }

    public HexEdgeType GetEdgeType( HexCell otherCell ) {
        return HexMetrics.GetEdgeType( Elevation,otherCell.Elevation );
    }

    public void EnableHighlight() {
        EnableHighlight( Color.white ) ;
    }

    public void EnableHighlight( Color color ) {
        Image highlight = uiRect.GetComponentInChildren<Image>() ;
        highlight.enabled = true ;
        highlight.color = color ;
    }

    public void DisableHighlight() {
        Image highlight = uiRect.GetComponentInChildren<Image>() ;
        highlight.enabled = false ;
    }

    #region 河流
    //河源
    public bool HasInComingRiver { get ; private set ; }
    //河尾
    public bool HasOutGoingRive { get ; private set ; }
    //流入方向
    public HexDirectionEnum InComingRive { get ; private set ; }
    //流出方向
    public HexDirectionEnum OutGoingRive { get ; private set ; }

    //是否有河流
    public bool HasRiver { get { return HasInComingRiver || HasOutGoingRive; } }

    //是否是河流的一端
    public bool HasRiverBeginOrEnd { get { return HasInComingRiver != HasOutGoingRive; } }

    public HexDirectionEnum RiverBeginOrEndDirection { get { return HasInComingRiver ? InComingRive : OutGoingRive; } }

    //河床高度
    public float StreamBedHight { get { return (Elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; } }

    //河水高度
    public float RiverSurfaceHight { get { return (Elevation + HexMetrics.waterElevationOffest) * HexMetrics.elevationStep; } }


    public void RefreshRiver() {
        ValidateRivers();
    }

    /// <summary>
    /// 此边是否有河流
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRiverThroughEdge( HexDirectionEnum direction ) {
        return HasInComingRiver && InComingRive == direction || HasOutGoingRive && OutGoingRive == direction ;
    }

    public void SetInComgingRiver( HexDirectionEnum direction ) {
        RemoveInComingRiver();
        SpecialIndex = 0;
        HasInComingRiver = true ;
        InComingRive = direction ;
    }

    public void SetOutGoingRiver( HexDirectionEnum direction ) {
        if ( GetNeighbor( direction ).IsUnderWater ) return ;
        if ( HasOutGoingRive && OutGoingRive == direction ) return ;
        HexCell neighbor = GetNeighbor( direction ) ;
        if ( !IsValidRiverDestination( neighbor ) ) return;
        RemoveOutGoingRiver();
        if ( HasInComingRiver && InComingRive == direction ) {
            RemoveInComingRiver();
        }

        SpecialIndex = 0 ;
        HasOutGoingRive = true ;
        OutGoingRive = direction ;

        neighbor.SetInComgingRiver(direction.Opposite());

        SetRoad( (int) direction , false ) ;
    }

    private bool IsValidRiverDestination( HexCell neighbor ) {
        return neighbor && ( Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation );
    }

    private void ValidateRivers( ) {
        if ( HasOutGoingRive && !IsValidRiverDestination( GetNeighbor( OutGoingRive ) ) ) {
            RemoveOutGoingRiver(  );
        }

        if ( HasInComingRiver && !GetNeighbor( InComingRive ).IsValidRiverDestination( this ) ) {
            RemoveInComingRiver(  );
        }
    }


    //移除河流
    public void RemoveRiver() {
        RemoveInComingRiver();
        RemoveOutGoingRiver();
    }

    public void RemoveInComingRiver() {
        if ( !HasInComingRiver ) return ;
        HasInComingRiver = false ;
        RefreshSelfOnly() ;

        HexCell neighbor = GetNeighbor( InComingRive ) ;
        neighbor.RemoveOutGoingRiver();
    }

    //移除河流的出口
    public void RemoveOutGoingRiver() {
        if ( !HasOutGoingRive ) return ;
        HasOutGoingRive = false ;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor( OutGoingRive ) ;
        neighbor.RemoveInComingRiver() ;
    }

    #endregion

    #region 道路

    private bool[] roads = new bool[6];

    public bool HasRoads {
        get {
            for ( int i = 0 ; i < roads.Length ; i++ ) {
                if ( roads[ i ] ) return true;
            }
            return false ;
        }
    }

    private void RefreshRoads() {
        for ( int i = 0 ; i < roads.Length ; i++ ) {
            if ( roads[ i ] && GetElevationDifference( (HexDirectionEnum) i ) > 1 ) {
                SetRoad( i , false ) ;
            }
        }
    }


    public bool HasRoadThroughEdge( HexDirectionEnum direction ) {
        return roads[ (int) direction ] ;
    }

    public void AddRoad( HexDirectionEnum direction ) {
        if ( RoadElevationSuitable( direction ) ) SetRoad( (int) direction , true ) ;
    }

    private bool RoadElevationSuitable( HexDirectionEnum direction ) {
        if ( IsSpecial || GetNeighbor( direction ).IsSpecial ) return false ;
        return  !roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference( direction ) <= 1 ;
    }

    public void RemoveRoads() {
        for ( int i = 0 ; i < roads.Length ; i++ ) {
            if ( roads[ i ] ) {
                SetRoad( i,false );
            }
        }
    }

    private void SetRoad( int index , bool state ) {
        /*SetRoadSelfOnly( index , state ) ;
        neighbors[ index ].SetRoadSelfOnly( index , state ) ;*/

        roads[index] = state;
        neighbors[index].roads[(int)((HexDirectionEnum)(index)).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public void SetRoadSelfOnly( int index , bool state ) {
        roads[index] = state;
        RefreshSelfOnly();
    }

    

    public int GetElevationDifference( HexDirectionEnum direction ) {
        int difference = Elevation - GetNeighbor( direction ).Elevation ;
        return Mathf.Abs( difference ) ;
    }

    #endregion

    #region 水平面

    public int WaterLevel {
        get { return _waterLevel; }
        set {
            if ( _waterLevel == value ) return ;
            _waterLevel = value ;
            ValidateRivers();
            Refresh();
        }
    }
    private int _waterLevel ;

    public bool IsUnderWater { get { return WaterLevel > Elevation ; } }

    public float WaterSurfaceHight { get { return (WaterLevel + HexMetrics.waterElevationOffest) * HexMetrics.elevationStep ; } }

    #endregion

    #region 城市

    public int UrbanLevel {
        get { return urbanLevel ; }
        set {
            if ( urbanLevel != value ) {
                urbanLevel = value ;
                RefreshSelfOnly();
            }
        }        
    }
    private int urbanLevel ;

    public int FarmLevel {
        get { return farmLevel ; }
        set {
            if ( farmLevel != value ) {
                farmLevel = value ;
                RefreshSelfOnly();
            }
        }        
    }
    private int farmLevel ;

    public int PlantLevel {
        get { return plantLevel ; }
        set {
            if ( plantLevel != value ) {
                plantLevel = value ;
                RefreshSelfOnly();
            }
        }
    }
    private int plantLevel ;

    public bool Walled {
        get { return walled ; }
        set {
            if ( walled != value ) {
                walled = value ;
                Refresh();
            }
        }
    }
    private bool walled ;

    public int SpecialIndex {
        get { return specialIndex ; }
        set {
            if ( specialIndex != value && !HasRiver) {
                specialIndex = value ;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }
    private int specialIndex ;

    public bool IsSpecial { get { return SpecialIndex > 0 ; } }

    #endregion

    public HexUnit Unit {
        get { return unit ; }
        set { unit = value ; }
    }
    private HexUnit unit ;

    public bool IsVisible { get { return visibility > 0; } }
    private int visibility;

    public void IncreaseVisibility() {
        visibility += 1 ;
        if(visibility == 1) ShaderData.RefreshVisibility( this );
    }

    public void DecreaseVisibility() {
        visibility -= 1 ;
        if(visibility == 0)ShaderData.RefreshVisibility( this );
    }
}
