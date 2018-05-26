using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapItem : MonoBehaviour {

    private Text namText;

    private System.Action<string> clickAction;

    public string MapName {
        get { return namText.text; }
        set { namText.text = value; }
    }

    private void Start() {
        namText = GetComponentInChildren<Text>();
    }

    public void Init( string path , System.Action<string> action ) {
        if( namText == null) namText = GetComponentInChildren<Text>();
        MapName = System.IO.Path.GetFileNameWithoutExtension( path );
        clickAction = action;
    }

    public void OnClick() {
        if ( clickAction != null ) clickAction( MapName );
    }
}
