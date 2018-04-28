using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour {

    private HexCell[] cells ;
    private HexMesh hexMesh ;
    private Canvas gridCanvas ;

    private void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>() ;
        hexMesh = GetComponentInChildren<HexMesh>() ;

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

	// Use this for initialization
	void Start () {
		hexMesh.Triangulate( cells );
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void LateUpate() {
        hexMesh.Triangulate( cells );
        enabled = false ;
    }

    public void Refresh() {
        //hexMesh.Triangulate(cells);
        enabled = true ;
    }

    public void AddCell( int index , HexCell cell ) {
        cell.chunk = this ;
        cells[ index ] = cell ;
        cell.transform.SetParent( transform,false );
        cell.uiRect.SetParent( gridCanvas.transform,false );
    }
}
