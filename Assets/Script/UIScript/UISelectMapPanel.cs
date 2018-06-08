using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class UISelectMapPanel : MonoBehaviour {

    public HexGrid hexGrid;
    public UIMapItem mapItem;
    public ScrollRect mapScroll;
    public InputField selectInput;
    public Text titleText;
    public Text selectText;
    public Text errorText;

    private System.Action<string> selectAction;

    private void Show() {
        HexMapCamera.Locked = true;
        gameObject.SetActive( true );
        InitMapItems();
    }

    public void Hide() {
        Clean();
        HexMapCamera.Locked = false;
        gameObject.SetActive( false );
    }

    public void ShowSave() {
        selectAction = Save;
        titleText.text = "Save Map";
        selectText.text = "Save";
        Show();
    }

    public void ShowLoad() {
        selectAction = Load;
        titleText.text = "Load Map";
        selectText.text = "Load";
        Show();
    }

    public void DeleteButton() {
        string path = GetMapPath( selectInput.text );
        if ( System.IO.File.Exists( path ) ) {
            System.IO.File.Delete( path );
        }

        selectInput.text = "";
        Clean();
        InitMapItems();
    }

    public void SeleteButton() {
        string mapName = selectInput.text;
        if ( string.IsNullOrEmpty( mapName ) ) {
            errorText.text = "Error : Enter or Select a map name";
            return;
        }
        errorText.text = "";

        if ( selectAction != null ) {
            selectAction( GetMapPath( mapName ) );
        }
    }

    private void SetSelectMap( string mapName ) {
        selectInput.text = mapName;
    }

    private void InitMapItems() {
        string[] paths = System.IO.Directory.GetFiles(GetMapDirectory(), "*.map" );
        Array.Sort( paths );
        for ( int i = 0; i < paths.Length; i++ ) {
            UIMapItem item = Instantiate( mapItem );
            item.Init( paths[i], SetSelectMap );
            item.transform.SetParent( mapScroll.content,false );
        }
    }

    private void Clean() {
        for ( int i = 0 ; i < mapScroll.content.childCount ; i++ ) {
            Destroy( mapScroll.content.GetChild( i ).gameObject );
        }
    }


    public void Save( string path ) {
        using ( System.IO.FileStream fs = System.IO.File.Open( path, System.IO.FileMode.Create ) ) {
            using ( System.IO.BinaryWriter writer = new System.IO.BinaryWriter( fs ) ) {
                writer.Write( 0 );
                hexGrid.Save( writer );
            }
        }
        RefreshProject();
        Hide();
    }

    public void Load(string path) {
        if ( !System.IO.File.Exists( path ) ) {
            Debug.LogError( "not find path:" + path );
            int index = path.IndexOf( "/Resources/", StringComparison.Ordinal );
            errorText.text = "Error : Map not find:"+path.Substring( index );
            return;
        }
        errorText.text = "";
        Hide();

        using ( System.IO.BinaryReader reader = new System.IO.BinaryReader( System.IO.File.OpenRead( path ) ) ) {
            int header = reader.ReadInt32();
            if ( header == 0 ) hexGrid.Load( reader );
            else Debug.LogError( "Error :Unknown map format " + header );
        }
    }

    private string GetMapPath(string mapName) {
        return GetMapDirectory() + mapName+".map";
    }

    private string GetMapDirectory() {
        string path = Application.dataPath + "/Resources/Map/" ;
        if ( !System.IO.Directory.Exists( path ) ) {
            System.IO.Directory.CreateDirectory( path ) ;
            RefreshProject() ;
        }
        return path ;
    }

    private void RefreshProject() {
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

}
