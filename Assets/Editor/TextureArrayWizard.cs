using System.Collections;
using System.Collections.Generic;
using UnityEditor ;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard {

    public Texture2D[] textures ;

    [MenuItem("Tools/Texture Array")]
    static void CreateWizard() {
        TextureArrayWizard textureWizard = ScriptableWizard.DisplayWizard<TextureArrayWizard>( "Create Texture Array" , "Create" ) ;
        textureWizard.minSize = new Vector2(700,500) ;
    }

    private void OnWizardCreate() {
        if (textures == null || textures.Length <= 0 ) return ;

        string path = EditorUtility.SaveFilePanelInProject( "Save Texture Array" , "Texture Array" , "asset" , "Save Texture Array" ) ;
        if ( string.IsNullOrEmpty( path ) ) return ;

        Texture2D t = textures[ 0 ] ;
        Texture2DArray textureArray = new Texture2DArray( t.width , t.height , textures.Length , t.format , t.mipmapCount > 1 ) ;
        textureArray.anisoLevel = t.anisoLevel ;
        textureArray.filterMode = t.filterMode ;
        textureArray.wrapMode = t.wrapMode ;

        for ( int i = 0 ; i < textures.Length ; i++ ) {
            for ( int m = 0 ; m < t.mipmapCount ; m++ ) {
                Graphics.CopyTexture( textures[ i ] , 0 , m , textureArray , i , m ) ;
            }
        }
        AssetDatabase.CreateAsset( textureArray , path ) ;
    }
}
