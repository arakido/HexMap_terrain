using UnityEngine;
using System.Collections;
using System.IO ;
using UnityEngine.EventSystems ;

public class HexMapManager : MonoBehaviour {

    public HexGrid hexGrid ;
    public Material terrainMaterial ;

    private int activeTerrainTypeIndex  ;

    private int brushSize;
    private int activeElevation;
    private int activeWaterLevel ;
    private int activeUrbanlevel ;
    private int activeFarmlevel ;
    private int activePlantlevel ;
    private int activeSpecialIndex ;

    private bool editMode = true;
    private bool applyElevation;
    private bool applyWaterLevel ;
    private bool applyUrbanLevel ;
    private bool applyFarmLevel ;
    private bool applyPlantLevel ;
    private bool applySpecialIndex;

    private OptionalToggle riverMode ;
    private OptionalToggle roadMode ;
    private OptionalToggle walledMode ;

    private bool isDrag ;
    private HexDirectionEnum dragDirection ;
    private HexCell previousCell ;

    private void Awake() {
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

    

    private void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay( Input.mousePosition ) ;
        RaycastHit hit ;
        if ( Physics.Raycast( inputRay , out hit ) ) {
            //hexGrid.TouchCell( hit.point , activeColor ) ;
            HexCell currentCell = hexGrid.GetCell( hit.point ) ;
            if ( currentCell == null ) return ;
            if ( previousCell && previousCell != currentCell ) {
                ValidateDrag( currentCell ) ;
            }
            else {
                isDrag = false ;
            }
            if ( editMode ) EditCells(currentCell);
            else hexGrid.FindDistancesTo( currentCell );
            
            previousCell = currentCell ;
        }
        else {
            previousCell = null ;
        }
    }

    private void ValidateDrag( HexCell currentCell ) {
        for ( HexDirectionEnum i = 0 ; i < HexDirectionEnum.Length ; i++ ) {
            if ( previousCell.GetNeighbor( i ) == currentCell ) {
                dragDirection = i ;
                isDrag = true ;
                return ;
            }
        }
        isDrag = false ;
    }

    private void EditCells( HexCell center ) {
        if ( brushSize <= 0 ) {
            EditCell( center ) ;
            return ;
        }

        int centerX = center.coordinates.X ;
        int centerZ = center.coordinates.Z ;

        for ( int r = 0 , z = centerZ - brushSize ; z <= centerZ ; z++, r++ ) {
            for ( int x = centerX - r ; x <= centerX + brushSize ; x++ ) {
                EditCell( hexGrid.GetCell( new HexCoordinates( x , z ) ) ) ;
            }
        }
        for ( int r = 0 , z = centerZ + brushSize ; z > centerZ ; z--, r++ ) {
            for ( int x = centerX - brushSize ; x <= centerX + r ; x++ ) {
                EditCell( hexGrid.GetCell( new HexCoordinates( x , z ) ) ) ;
            }
        }

    }

    public void EditCell( HexCell cell ) {
        if ( cell == null ) return ;
        if ( activeTerrainTypeIndex >= 0 ) cell.TerrainTypeIndex = activeTerrainTypeIndex ;
        if ( applyElevation ) cell.Elevation = activeElevation ;
        if ( applyWaterLevel ) cell.WaterLevel = activeWaterLevel ;
        if ( applyUrbanLevel ) cell.UrbanLevel = activeUrbanlevel ;
        if ( applyFarmLevel ) cell.FarmLevel = activeFarmlevel ;
        if ( applyPlantLevel ) cell.PlantLevel = activePlantlevel ;
        if ( applySpecialIndex ) cell.SpecialIndex = activeSpecialIndex ;

        if ( riverMode == OptionalToggle.Remove ) cell.RemoveRiver() ;
        if ( roadMode == OptionalToggle.Remove ) cell.RemoveRoads() ;
        if ( walledMode != OptionalToggle.Ignore ) cell.Walled = walledMode == OptionalToggle.Add ;
        if ( isDrag ) {
            HexCell otherCell = cell.GetNeighbor( dragDirection.Opposite() ) ;
            if ( otherCell != null ) {
                if ( riverMode == OptionalToggle.Add ) previousCell.SetOutGoingRiver( dragDirection ) ;
                if ( roadMode == OptionalToggle.Add ) previousCell.AddRoad( dragDirection ) ;
            }
        }
    }

    public void ShowUI( bool visible ) {
        hexGrid.ShowUI( visible );
    }

    public void ShowGrid( bool visible ) {
        if(visible)terrainMaterial.EnableKeyword( "GRID_ON" );
        else terrainMaterial.DisableKeyword( "GRID_ON" );
    }

    public void SetEditMode( bool toggle ) {
        editMode = toggle;
        if(toggle) hexGrid.Clean();
        hexGrid.ShowUI( !toggle );
        ShowGrid( !toggle ) ;
    }

    public void SetTerrainTypeIndex( int index ) {
        activeTerrainTypeIndex = index ;
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

    public void SetAppWaterLevel( bool toggle ) {
        applyWaterLevel = toggle ;
    }

    public void SetWaterLevel( float level ) {
        activeWaterLevel = Mathf.FloorToInt( level ) ;
    }

    public void SetAppUrbanLevel( bool toggle ) {
        applyUrbanLevel = toggle ;
    }

    public void SetUrbanLevel( float level ) {
        activeUrbanlevel = (int) level ;
    }

    public void SetAppFarmLevel(bool toggle) {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level) {
        activeFarmlevel = (int)level;
    }

    public void SetAppPlantLevel(bool toggle) {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level) {
        activePlantlevel = (int)level;
    }

    public void SetWalledModle( int mode ) {
        walledMode = (OptionalToggle) mode ;
    }

    public void SetApplySpecialIndex( bool toggle ) {
        applySpecialIndex = toggle ;
    }

    public void SetSpecialIndex( float index ) {
        activeSpecialIndex = (int) index ;
    }

    //private readonly string mapPath = Application.dataPath + "/Respurce/" ;

    public void Save() {
        using ( FileStream fs = File.Open( GetMapPath() , FileMode.Create ) ) {
            using ( BinaryWriter writer = new BinaryWriter( fs ) ) {
                writer.Write( 0 );
                hexGrid.Save( writer ) ;
            }
        }
    }

    public void Load() {
        using ( BinaryReader reader = new BinaryReader( System.IO.File.OpenRead( GetMapPath() ) ) ) {
            int header = reader.ReadInt32();
            if ( header == 0 ) hexGrid.Load( reader );
            else Debug.LogError( "Error :Unknown map format " + header );
        }
    }

    private string GetMapPath() {
        return Application.dataPath + "/Resources/test.map";
    }

    private void OnDestroy() {
        terrainMaterial.DisableKeyword("GRID_ON");
    }
}

