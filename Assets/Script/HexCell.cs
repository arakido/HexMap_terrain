using UnityEngine;
using System.Collections;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates ;
    public Color color ;
    [HideInInspector] public Vector3 center { get { return transform.localPosition; } }

    [SerializeField] private readonly HexCell[] neighbors = new HexCell[6];

	// Use this for initialization
    void Start( ) {

    }

    // Update is called once per frame
	void Update () {
	
	}

    //设置相邻的三角形
    public void SetNeighbor( HexDirectionEnum direction , HexCell cell ) {
        neighbors[ (int) direction ] = cell ;
        cell.neighbors[ (int) direction.Opposite() ] = this ;

    }

    public HexCell GetNeighbor( HexDirectionEnum direction ) {
        return neighbors[ (int) direction ] ;
    }
}
