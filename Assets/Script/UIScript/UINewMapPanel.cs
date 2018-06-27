using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINewMapPanel : MonoBehaviour {

    public HexGrid hexGrid;
    public HexMapGenerator mapGenerator;

    public GameObject customizePanle;
    public InputField xInput;
    public Text xErrorText;
    public InputField zInput;
    public Text zErrorText;

    private bool generateMaps = true ;
    private bool wrapping = true ;

    public void Show() {
        HexMapCamera.Locked = true;
        gameObject.SetActive( true );
    }

    public void Hide() {
        HexMapCamera.Locked = false;
        gameObject.SetActive( false );
    }

    private void CreatMap( int x , int z ) {
        if(generateMaps) mapGenerator.GeneratorMap( x,z , wrapping);
        else hexGrid.CreateMap( x,z , wrapping);
        Hide();
    }


    public void ToggleMapGeneration(bool toggle) {
        generateMaps = toggle;
    }

    public void ToggleWrapping( bool toggle ) {
        wrapping = toggle ;
    }


    public void SmallButton() {
        CreatMap( 20 , 15 );
    }

    public void MediumButton() {
        CreatMap( 40, 30 );
    }

    public void LargeButton() {
        CreatMap( 80, 60 );
    }

    public void CustomizeButton() {
        customizePanle.SetActive( true );
    }

    public void HideCustomize() {
        customizePanle.SetActive( false );
    }

    public void DefineButton() {
        int x , z;
        if ( int.TryParse( xInput.text , out x ) && int.TryParse( zInput.text , out z ) ) {
            if ( !CheckSize( x ) ) return;
            if ( !CheckSize( z ) ) return;
            CreatMap( x, z );
        }
        HideCustomize() ;
    }

    public void XInputCallBack( string msg ) {
        int size;
        if ( int.TryParse( xInput.text , out size ) ) {
            if ( !CheckSize( size ) ) {
                xErrorText.text = string.Format( "Error : X is 0  or {0} % {1} != 0" , size , HexMetrics.chunkSizeX );
            }
        }
        else xErrorText.text = "";
    }

    public void ZInputCallBack( string msg ) {
        int size;
        if ( int.TryParse( xInput.text , out size ) ) {
            if ( !CheckSize( size ) ) {
                zErrorText.text = string.Format( "Error : Z is 0  or Z % {1} != 0" , size , HexMetrics.chunkSizeZ );
            }
        }
        else zErrorText.text = "";
    }

    private bool CheckSize( int size ) {
        if ( size <= 0 || size % HexMetrics.chunkSizeX != 0 ) return false;
        return true;
    }
}
