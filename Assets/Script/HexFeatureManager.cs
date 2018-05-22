using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour {
    [Range(0,1)]
    public float range = 0.8f ;
    //public Transform featurePrefab ;
    public Transform[] urbanPrefabs ;
    private Transform container ;

    public void AddFeature(HexCell cell, Vector3 position ) {
        HexHash hash = HexMetrics.SampleHashGrid( position ) ;
        int level = HexMetrics.GetFeatureRandomLevel( cell.UrbanLevel , hash.a ) ;
        if ( level < 0 ) return ;
        Transform item = Instantiate( urbanPrefabs[ level ] ) ;
        position.y += item.localScale.y * 0.5f ;
        item.localPosition = HexMetrics.Perturb(position) ;
        item.localRotation = Quaternion.Euler( 0f , 360f * hash.b, 0f ) ;
        item.SetParent( container , false ) ;
    }

    public void Apply() { }

    public void Clear() {
        if ( container ) Destroy( container.gameObject ) ;
        container = new GameObject("Features Container").transform;
        container.SetParent( transform , false ) ;
    }
}
