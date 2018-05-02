using UnityEngine;
using System.Collections;

public class HexCell : MonoBehaviour {
    public RectTransform uiRect;
    public HexCoordinates coordinates ;

    public HexGridChunk chunk;
    
    public bool HasInComingRiver { get { return hasIncomingRiver ; } private set { hasIncomingRiver = value ; } }
    private bool hasIncomingRiver ; //河源

    public bool HasOutGoingRive { get { return hasOutgoingRive ; } private set { hasOutgoingRive = value ; } }
    private bool hasOutgoingRive ;  //河口

    public HexDirectionEnum InComingRive { get { return incomingRive ; } private set { incomingRive = value ; } }
    private HexDirectionEnum incomingRive ; //流入方向

    public HexDirectionEnum OutGoingRive { get { return outgoingRive ; } private set { outgoingRive = value ; } }
    private HexDirectionEnum outgoingRive ; //流出方向

    //是否有河流
    public bool HasRiver { get { return HasInComingRiver || HasOutGoingRive ; } }

    //是否是河流的一端
    public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRive ; } }

    private Color _color;
    public Color Color {
        get {return _color;}
        set {
            if ( _color == value ) return ;
            _color = value ;
            Refresh();
        }
    }

    private int elevation = int.MinValue;
    public int Elevation {
        get { return elevation >= 0 ? elevation : 0 ; }
        set {
            if ( elevation == value ) return ;
            elevation = value;
            SetPosition();
            SetRiverOfElevation() ;
        }
    }

    public float StreamBedHight { get { return (Elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep ; } }

    [HideInInspector] public Vector3 postion { get { return transform.localPosition; } }

    [SerializeField] private HexCell[] neighbors ;

	// Use this for initialization
    void Start( ) {

    }

    // Update is called once per frame
	void Update () {
	
	}

    private void Refresh() {
        if ( chunk ) {
            chunk.Refresh() ;
            for ( int i = 0 ; i < neighbors.Length ; i++ ) {
                HexCell neighbor = neighbors[ i ] ;
                if ( neighbor != null && neighbor.chunk != chunk ) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    private void RefreshSelfOnly() {
        if ( chunk ) chunk.Refresh() ;
    }

    private void SetPosition() {
        Vector3 position = transform.localPosition;
        position.y = Elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise( position ).y * 2f - 1f) * HexMetrics.elevationPerturbStrength ;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
        Refresh() ;
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

    #region 河流

    public void SetRiverOfElevation() {
        if ( HasOutGoingRive && Elevation < GetNeighbor( OutGoingRive ).Elevation ) {
            RemoveOutGoingRiver();
        }
        if ( HasInComingRiver && Elevation > GetNeighbor( InComingRive ).Elevation ) {
            RemoveInComingRiver();
        }
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
        HasInComingRiver = true ;
        InComingRive = direction ;
        RefreshSelfOnly();
    }

    public void SetOutGoingRiver( HexDirectionEnum direction ) {
        if ( HasOutGoingRive && OutGoingRive == direction ) return ;
        HexCell neighbor = GetNeighbor( direction ) ;
        if ( neighbor == null || Elevation < neighbor.Elevation ) return ;

        RemoveOutGoingRiver();
        if ( HasInComingRiver && InComingRive == direction ) {
            RemoveInComingRiver();
        }
        HasOutGoingRive = true ;
        OutGoingRive = direction ;
        RefreshSelfOnly();

        neighbor.SetInComgingRiver(direction.Opposite());
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
}
