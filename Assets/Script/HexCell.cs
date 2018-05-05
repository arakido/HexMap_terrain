﻿using UnityEngine;
using System.Collections;

public class HexCell : MonoBehaviour {

    [SerializeField] private HexCell[] neighbors; //临边

    public RectTransform uiRect;
    public HexCoordinates coordinates ;

    public HexGridChunk chunk;

    public Color Color { get { return _color ; }
        set {
            if ( _color == value ) return ;
            _color = value ;
            Refresh() ;
        }
    }
    private Color _color;

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

    [HideInInspector] public Vector3 postion { get { return transform.localPosition; } }



    // Use this for initialization
    void Start( ) {

    }

    // Update is called once per frame
	void Update () {
	
	}

    private void Refresh() {
        if ( !chunk ) return ;

        chunk.Refresh() ;
        for ( int i = 0 ; i < neighbors.Length ; i++ ) {
            HexCell neighbor = neighbors[ i ] ;
            if ( neighbor != null && neighbor.chunk != chunk ) {
                neighbor.chunk.Refresh();
            }
        }
    }

    private void RefreshSelfOnly() {
        if ( chunk ) chunk.Refresh() ;
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

    public bool HasInComingRiver { get { return hasIncomingRiver; } private set { hasIncomingRiver = value; } }
    private bool hasIncomingRiver; //河源

    public bool HasOutGoingRive { get { return hasOutgoingRive; } private set { hasOutgoingRive = value; } }
    private bool hasOutgoingRive;  //河口

    public HexDirectionEnum InComingRive { get { return incomingRive; } private set { incomingRive = value; } }
    private HexDirectionEnum incomingRive; //流入方向

    public HexDirectionEnum OutGoingRive { get { return outgoingRive; } private set { outgoingRive = value; } }
    private HexDirectionEnum outgoingRive; //流出方向

    //是否有河流
    public bool HasRiver { get { return HasInComingRiver || HasOutGoingRive; } }

    //是否是河流的一端
    public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRive; } }

    public HexDirectionEnum RiverBeginOrEndDirection { get { return HasInComingRiver ? InComingRive : OutGoingRive; } }

    //河床高度
    public float StreamBedHight { get { return (Elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; } }

    //河水高度
    public float RiverSurfaceHight { get { return (Elevation + HexMetrics.riverSurfaceElevationOffest) * HexMetrics.elevationStep; } }

    public void RefreshRiver() {
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

        neighbor.SetInComgingRiver(direction.Opposite());

        SetRoad( (int) direction , false ) ;
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
        return !roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference( direction ) <= 1 ;
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
}
