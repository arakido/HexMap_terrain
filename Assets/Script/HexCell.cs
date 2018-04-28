using UnityEngine;
using System.Collections;

public class HexCell : MonoBehaviour {
    public RectTransform uiRect;
    public HexCoordinates coordinates ;

    public HexGridChunk chunk;


    private Color _color ;

    public Color Color {
        get {return _color;}
        set {
            //if ( _color == value ) return ;
            _color = value ;
            Refresh();
        }
    }

    private int elevation = int.MinValue;
    public int Elevation {
        get { return elevation >= 0 ? elevation : 0 ; }
        set {
            //if ( elevation == value ) return ;
            elevation = value;
            SetPosition();
        }
    }
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
}
