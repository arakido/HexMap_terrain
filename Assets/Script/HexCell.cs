﻿using UnityEngine;
using System.Collections;

public class HexCell : MonoBehaviour {
    public RectTransform uiRect;
    public HexCoordinates coordinates ;
    public Color color ;

    private int elevation;
    public int Elevation {
        get { return elevation; }
        set {
            elevation = value;
            SetPosition();
        }
    }
    [HideInInspector] public Vector3 center { get { return transform.localPosition; } }

    [SerializeField] private readonly HexCell[] neighbors = new HexCell[6];

	// Use this for initialization
    void Start( ) {

    }

    // Update is called once per frame
	void Update () {
	
	}

    private void SetPosition() {
        Vector3 position = transform.localPosition;
        position.y = Elevation * HexMetrics.elevationStep;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = elevation * -HexMetrics.elevationStep;
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
        return HexMetrics.GetEdgeType( Elevation , otherCell.Elevation ) ;
    }
}
