using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems ;

public class HexMapManager : MonoBehaviour {

    private Color[] colors ;
    public HexGrid hexGrid ;

    private Color activeColor ;

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
            hexGrid.TouchCell( hit.point , activeColor ) ;
        }
    }

    public void SelectColor( int index ) {
        activeColor = colors[ index ] ;
    }
}

