using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems ;

public class HexMapManager : MonoBehaviour {

    public HexGrid hexGrid ;

    private Color[] colors ;
    private Color activeColor ;
    private int activeElevation;

    private void Awake() {
        colors = new Color[] { Color.yellow, Color.green, Color.blue, Color.cyan, Color.white, };
        SelectColor( 0 );
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
            EditCell( hexGrid.GetCell( hit.point ) );
        }
    }

    public void SelectColor( int index ) {
        activeColor = colors[ index ] ;
    }

    public void SetElevation( float elevation ) {
        activeElevation = (int)elevation;
    }

    public void EditCell( HexCell cell ) {
        cell.color = activeColor;
        cell.Elevation = activeElevation;
        hexGrid.Refresh();
    }
}

