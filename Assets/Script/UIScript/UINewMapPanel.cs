using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINewMapPanel : MonoBehaviour {

    public HexGrid hexGrid;
    public GameObject customizePanle;
    public InputField xInput;
    public Text xErrorText;
    public InputField zInput;
    public Text zErrorText;

    public void Show() {
        HexMapCamera.Locked = true;
        gameObject.SetActive( true );
    }

    public void Hide() {
        HexMapCamera.Locked = false;
        gameObject.SetActive( false );
    }

    private void SendMapSize( int x , int z ) {
        hexGrid.CreateMap( x,z );
        Hide();
    }

    public void SmallButton() {
        SendMapSize( 20 , 15 );
    }

    public void MediumButton() {
        SendMapSize( 40, 30 );
    }

    public void LargeButton() {
        SendMapSize( 80, 60 );
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
            SendMapSize( x, z );
        }
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
