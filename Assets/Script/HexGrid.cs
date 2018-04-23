﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI ;

public class HexGrid : MonoBehaviour {

    public int width = 6 ;
    public int height = 6 ;

    public HexCell cellPrefab ;
    public Text cellLabelPrefab ;

    private HexCell[] cells ;
    private Canvas gridCanvas ;
    private HexMesh hexMesh ;
    private Color defaultColor = Color.white ;

    private void Awake() {
        gridCanvas = transform.parent.GetComponentInChildren<Canvas>() ;
        hexMesh = GetComponentInChildren<HexMesh>() ;
        cells = new HexCell[width * height];
        for ( int z = 0 ,i = 0; z < height ; z++ ) {
            for ( int x = 0 ; x < width ; x++ ) {
                CreateCell( x , z , i++ ) ;
            }
        }
    }

    // Use this for initialization
    void Start() {
        hexMesh.Triangulate( cells ) ;
    }

    //初始化六边形基础信息
    private void CreateCell( int x , int z , int i ) {
        Vector3 position ;
        position.x = (x + (z & 1) * 0.5f) * (HexMetrics.innerRadius * 2f) ;
        position.y = 0f ;
        position.z = z * (HexMetrics.outerRadius * 1.5f) ;

        HexCell cell = cells[ i ] = Instantiate<HexCell>( cellPrefab ) ;
        cell.transform.SetParent( transform,true );
        cell.transform.localPosition = position ;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates( x,z );
        cell.color = defaultColor ;
        cell.transform.name = "cell_" + i ;

        //添加相邻的HexCell
        if ( x > 0 ) {
            cell.SetNeighbor( HexDirectionEnum.Left , cells[ i - 1 ] ) ;
        }
        if ( z > 0 ) {
            if ( (z & 1) == 0 ) {
                cell.SetNeighbor( HexDirectionEnum.BottomRight , cells[ i - width] ) ;
                if ( x > 0 ) cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - width - 1 ] ) ;
            }
            else {
                cell.SetNeighbor( HexDirectionEnum.BottomLeft , cells[ i - width ] ) ;
                if ( x < width - 1 ) cell.SetNeighbor( HexDirectionEnum.BottomRight , cells[ i - width + 1 ] ) ;
            }
        }
        Text label = Instantiate<Text>( cellLabelPrefab ) ;
        label.rectTransform.SetParent( gridCanvas.transform,true );
        label.rectTransform.anchoredPosition3D = new Vector2(position.x,position.z);
        label.rectTransform.Rotate( Vector3.zero );
        label.text = cell.coordinates.ToStringOnSeparateLines() ;

        cell.uiRect = label.rectTransform;
    }

	
	
	// Update is called once per frame
	void Update () {
	}

    public void Refresh() {
        hexMesh.Triangulate( cells );
    }

    public HexCell GetCell( Vector3 position ) {
        position = transform.InverseTransformPoint( position );
        HexCoordinates coordinates = HexCoordinates.FromPositon( position );
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        return cells[ index ];
    }


    public void TouchCell( Vector3 position , Color color) {
        position = transform.InverseTransformPoint( position ) ;
        HexCoordinates coordinates = HexCoordinates.FromPositon( position );
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2 ;
        HexCell cell = cells[ index ] ;
        cell.color = color;
        hexMesh.Triangulate( cells );
    }

}
