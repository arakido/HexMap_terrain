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

    private OptionalToggle riverMode ;
    private OptionalToggle roadMode ;
    private bool isDrag ;
    private HexDirectionEnum dragDirection ;
    private HexCell previousCell ;

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
        else {
            previousCell = null ;
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            //hexGrid.TouchCell( hit.point , activeColor ) ;
            HexCell currentCell = hexGrid.GetCell( hit.point ) ;
            if ( previousCell && previousCell != currentCell ) {
                ValidateDrag( currentCell ) ;
            }
            else {
                isDrag = false ;
            }
            if ( brushSize > 0 ) EditCells(currentCell);
            else EditCell(currentCell);
            previousCell = currentCell ;
        }
        else {
            previousCell = null ;
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
        brushSize = Mathf.FloorToInt( size ) ;
    }

    public void SetRiverMode(int mode) {
        riverMode = (OptionalToggle) mode ;
    }

    public void SetRroadMode( int mode ) {
        roadMode = (OptionalToggle)mode ;
    }

    private void ValidateDrag( HexCell currentCell ) {
        for (HexDirectionEnum i = 0 ; i < HexDirectionEnum.Length ; i++ ) {
            if ( previousCell.GetNeighbor( i ) == currentCell ) {
                dragDirection = i ;
                isDrag = true ;
                return ;
            }
        }
        isDrag = false ;
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
        if ( riverMode == OptionalToggle.Remove ) cell.RemoveRiver();
        if ( roadMode == OptionalToggle.Remove ) cell.RemoveRoads() ;
        if ( isDrag ) {
            HexCell otherCell = cell.GetNeighbor( dragDirection.Opposite() ) ;
            if ( otherCell != null ) {
                if ( riverMode == OptionalToggle.Add ) previousCell.SetOutGoingRiver( dragDirection ) ;
                if ( roadMode == OptionalToggle.Add ) previousCell.AddRoad( dragDirection ) ;
            }
        }
    }

    
}

