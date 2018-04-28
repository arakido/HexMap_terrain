using System ;
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


    private void Awake() {
        swivel = transform.GetChild( 0 ) ;
        stick = swivel.GetChild( 0 ) ;
    }

	// Use this for initialization
	void Start () {
		
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
    }

    private void AdjustPostion( float xDelta , float zDelta ) {
        Vector3 direction = transform.localRotation * new Vector3(xDelta,0f,zDelta).normalized;//向量化
        float damping = Mathf.Max( Mathf.Abs( xDelta ) , Mathf.Abs( zDelta ) ) ;
        float speed = Mathf.Lerp( moveSpeedMinZoom , moveSpeedMaxZoom , zoom ) ;
        float distance = speed * damping* Time.deltaTime ;
        Vector3 position = transform.localPosition ;
        position += direction * distance ;
        transform.localPosition = ClampPosition(position);
    }

    //限制摄像机的位置
    private Vector3 ClampPosition( Vector3 position ) {
        float xMax = (grid.chunkCountX * HexMetrics.chunkSizeX - 0.5f ) * (2f * HexMetrics.innerRadius) ;
        float zMax = (grid.chunkCountZ * HexMetrics.chunkSizeZ - 1f ) * (1.5f * HexMetrics.outerRadius);
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
