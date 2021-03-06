﻿using System ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour {

    private Transform swivel ;
    private Transform stick ;
    private float zoom = 1f ;

    public float stickMinZoom ;
    public float stickMaxZoom ;
    public float swivelMinZoom ;
    public float swivelMaxZoom ;
    public float moveSpeedMinZoom ;
    public float moveSpeedMaxZoom ;
    public float rotationSpeed ;

    public HexGrid grid ;

    private float rotationAngleX ;
    private float rotationAngleY ;

    private static HexMapCamera instance;
    public static bool Locked {
        set { instance.enabled = !value; }
    }

    private void Awake() {
        instance = this;
        swivel = transform.GetChild( 0 ) ;
        stick = swivel.GetChild( 0 ) ;
    }

    private void OnEnable() {
        instance = this ;
        ValidatePosition();
    }

    public static void ValidatePosition() {
        instance.AdjustPostion( 0 , 0 );
    }
	
	// Update is called once per frame
	void Update () {
	    float zoomDelta = Input.GetAxis( "Mouse ScrollWheel" ) ;
        if(Math.Abs( zoomDelta ) > 0f) AdjustZoom( zoomDelta );
        if ( Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton( 1 ) ) {
            float rotationX = Input.GetAxis( "Mouse X" ) ;
            float rotationY = Input.GetAxis( "Mouse Y" ) ;
            if ( Math.Abs( rotationX ) > 0f || Math.Abs( rotationY ) > 0f ) {
                AdjustRotation(rotationX, rotationY) ;
            }
	    }

        float xDelta = Input.GetAxis("Horizontal") ;
	    float zDelta = Input.GetAxis("Vertical") ;
	    if ( Math.Abs( xDelta ) > 0f || Math.Abs( zDelta ) > 0f ) {
	        AdjustPostion( xDelta , zDelta ) ;
	    }
	}

    private void AdjustZoom( float delta ) {
        zoom = Mathf.Clamp01( zoom + delta ) ;

        float distance = Mathf.Lerp( stickMinZoom , stickMaxZoom , zoom ) ;
        stick.localPosition = new Vector3( 0f , 0f , distance ) ;

        float angle = Mathf.Lerp( swivelMinZoom , swivelMaxZoom , zoom ) ;
        swivel.localRotation = Quaternion.Euler( angle , 0f , 0f ) ;
        AdjustPostion(0,0);
    }

    private void AdjustPostion( float xDelta , float zDelta ) {
        Vector3 direction = transform.localRotation * new Vector3(xDelta,0f,zDelta).normalized;//向量化
        float damping = Mathf.Max( Mathf.Abs( xDelta ) , Mathf.Abs( zDelta ) ) ;
        float speed = Mathf.Lerp( moveSpeedMinZoom , moveSpeedMaxZoom , zoom ) ;
        float distance = speed * damping* Time.deltaTime ;
        Vector3 position = transform.localPosition ;
        position += direction * distance ;
        transform.localPosition = grid.wrapping ? WrapPosition( position ) : ClampPosition( position ) ;
    }

    private Vector3 WrapPosition( Vector3 position ) {
        float width = grid.cellCountX * HexMetrics.innerDiameter ;
        if ( position.x < 0 ) position.x += width;
        else if ( position.x > width ) position.x -= width;

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius) ;
        position.z = Mathf.Clamp( position.z , 0f , zMax ) ;
        grid.CenterMap( position.x );
        return position ;
    }

    //限制摄像机的位置
    private Vector3 ClampPosition( Vector3 position ) {
        float xMax = (grid.cellCountX /** HexMetrics.chunkSizeX*/ - 0.5f ) * HexMetrics.innerDiameter;
        float zMax = (grid.cellCountZ /** HexMetrics.chunkSizeZ*/ - 1f ) * (1.5f * HexMetrics.outerRadius);
        position.x = Mathf.Clamp( position.x , 0f , xMax ) ;
        position.z = Mathf.Clamp( position.z , 0 , zMax ) ;
        return position ;
    }

    private void AdjustRotation( float xDelta ,float yDelta) {
        rotationAngleX += xDelta * rotationSpeed * Time.deltaTime ;
        rotationAngleX %= 360f ;
        rotationAngleY += yDelta * rotationSpeed * Time.deltaTime ;
        rotationAngleY %= 360f;

        transform.localRotation = Quaternion.Euler(rotationAngleY, rotationAngleX, 0);
    }
}
