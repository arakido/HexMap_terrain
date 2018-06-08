using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems ;

public class UIGamePanel : MonoBehaviour {

    public HexGrid hexGrid ;

    private HexCell currentCell ;
    private HexUnit selectedUnit ;

    
    public void SetEditMode( bool toggle ) {
        enabled = !toggle ;
        hexGrid.ShowUI( !toggle ) ;
    }

    private void Update() {
        if ( !EventSystem.current.IsPointerOverGameObject() ) {
            if ( Input.GetMouseButtonDown( 0 ) ) {
                DoSelection();
            }
            else if ( selectedUnit ) {
                if ( Input.GetMouseButtonDown( 1 ) ) DoMove() ;
                else DoPathfinding() ;
            }
        }
    }
    

    private void DoSelection() {
        UpdateCurrentCell() ;
        if ( currentCell && currentCell.Unit) {
            if(selectedUnit) selectedUnit.Location.EnableHighlight();
            selectedUnit = currentCell.Unit ;
            hexGrid.SetBeginCell( selectedUnit.Location );
        }
    }

    private void DoPathfinding() {
        if ( UpdateCurrentCell() ) {
            if ( selectedUnit.IsValidDestination( currentCell ) ) {
                hexGrid.FindPath(selectedUnit.Location, currentCell, 24);
            }
        }
    }

    private bool UpdateCurrentCell() {
        HexCell cell = hexGrid.GetCell( Camera.main.ScreenPointToRay( Input.mousePosition ) ) ;
        if ( cell != currentCell ) {
            currentCell = cell ;
            return true ;
        }
        return false ;
    }


    private void DoMove() {
        if ( hexGrid.HasPath ) {
            //selectedUnit.Location = currentCell;
            selectedUnit.Travel( hexGrid.GetPath() );
            hexGrid.ClearPath();
        }
        selectedUnit = null ;
    }
}
