using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems ;

public class HexMapManager : MonoBehaviour {

    public HexGrid hexGrid ;

    private Color[] colors ;
    private Color activeColor ;
    private int activeElevation;

    private bool applyColor = false;
    private bool applyElevation;
    private int brushSize;

    private void Awake() {
        colors = new Color[] { Color.yellow, Color.green, Color.blue, Color.cyan, Color.white, };
    }

    // Use this for initialization
	void Start () {
	
	}

    // Update is called once per frame
    private void Update() {
        if ( Input.GetMouseButton( 0 ) && !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput() ;
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            //hexGrid.TouchCell( hit.point , activeColor ) ;
            if ( brushSize > 0 ) EditCells( hexGrid.GetCell( hit.point ) );
            else EditCell( hexGrid.GetCell( hit.point ) );
        }
    }

    public void ShowUI( bool visible ) {
        hexGrid.ShowUI( visible );
    }

    public void SelectColor( int index ) {
        applyColor = index >= 0;
        if ( applyColor ) activeColor = colors[index];
    }

    public void SetApplyElevation( bool isApply ) {
        applyElevation = isApply;
    }

    public void SetElevation( float elevation ) {
        activeElevation = Mathf.FloorToInt(elevation);
    }

    public void SetBrushSize( float size ) {
        brushSize = Mathf.FloorToInt( size );
    }

    private void EditCells( HexCell center ) {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        
        for ( int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++ ) {
            for ( int x = centerX - r; x <= centerX + brushSize; x++ ) {
                EditCell( hexGrid.GetCell( new HexCoordinates( x, z ) ) );
            }
        }

        for ( int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++ ) {
            for ( int x = centerX - brushSize; x <= centerX + r; x++ ) {
                EditCell( hexGrid.GetCell( new HexCoordinates( x, z ) ) );
            }
        }
        
    }

    public void EditCell( HexCell cell ) {
        if ( cell == null ) return;
        if ( applyColor ) cell.Color = activeColor;
        if ( applyElevation ) cell.Elevation = activeElevation;
    }
}

