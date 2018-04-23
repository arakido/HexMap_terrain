using UnityEngine;
using System.Collections;
using UnityEditor ;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesEditor : PropertyDrawer {
    public override void OnGUI( Rect position , SerializedProperty property , GUIContent label ) {
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative( "pointX" ).intValue ,
            property.FindPropertyRelative( "pointZ" ).intValue
            ) ;
        position = EditorGUI.PrefixLabel( position , label ) ;
        GUI.Label( position,coordinates.ToString() );
    }
}
