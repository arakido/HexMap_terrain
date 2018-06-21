using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexUnit : MonoBehaviour {

    public static HexUnit unitPrefab ;
    public HexGrid hexGrid { get ; set ; }

    public int Speed { get { return 24 ; } }

    private float travelSpeed = 3f ;
    private float rotationSpeed = 180f ;
    private List<HexCell> pathToTravel ;

    public int VisionRange { get { return 2 ; } }

    public HexCell Location {
        get { return location ; }
        set {
            if ( location == value ) return ;
            if ( location ) location.Unit = null ;
            location = value;
            location.Unit = this;
            //location.IncreaseVisibility();
            hexGrid.IncreaseVisibility( location, VisionRange);

            transform.localPosition = location.postion;
        }
    }
    private HexCell location ;

    private HexCell currentTravelLocation ;
    
    public float Orientation {
        get { return orientation ; }
        set {
            orientation = value ;
            transform.localRotation = Quaternion.Euler(0f, value, 0);
        }
    }
    private float orientation ;

    private void OnEnable() {
        if ( Location ) transform.localPosition = Location.postion ;
        if ( currentTravelLocation ) {
            hexGrid.IncreaseVisibility( location,VisionRange );
            hexGrid.DecreaseVisibility( currentTravelLocation,VisionRange );
            currentTravelLocation = null ;
        }
    }


    public void ValidateLoacation() {
        transform.localPosition = Location.postion ;
    }

    public bool IsValidDestination( HexCell cell ) {
        if ( cell == null ) return false ;
        return cell.IsExplored && !cell.IsUnderWater && !cell.Unit;
    }

    public void Save( System.IO.BinaryWriter writer ) {
        location.coordinates.Save( writer );
        writer.Write( Orientation );
    }

    public void Load( System.IO.BinaryReader reader ,HexGrid grid) {
        HexCoordinates coordinates = HexCoordinates.Load( reader );
        hexGrid = grid ;
        Location = hexGrid.GetCell(coordinates);
        Orientation = reader.ReadSingle(); ;
    }

    public void Travel( List<HexCell> path ) {
        //Location = path[ path.Count - 1 ] ;
        location.Unit = null ;
        location = path[ path.Count - 1 ] ;
        location.Unit = this ;

        pathToTravel = ListPool<HexCell>.Get() ;
        pathToTravel.AddRange( path );
        StopAllCoroutines();
        StartCoroutine( TravelPath() ) ;
    }

    private IEnumerator TravelPath() {

        Vector3 a, b, c = pathToTravel[0].postion;
        //transform.localPosition = c ;
        yield return LoolAt( pathToTravel[ 1 ].postion ) ;

        hexGrid.DecreaseVisibility( currentTravelLocation ? currentTravelLocation : pathToTravel[ 0 ] , VisionRange ) ;

        float t = Time.deltaTime * travelSpeed;
        for ( int i = 1 ; i < pathToTravel.Count ; i++ ) {
            currentTravelLocation = pathToTravel[ i ] ;
            a = c;
            b = pathToTravel[i - 1].postion;
            c = (b + currentTravelLocation.postion) * 0.5f;
            hexGrid.IncreaseVisibility(currentTravelLocation, VisionRange);
            for ( ; t < 1 ; t += Time.deltaTime * travelSpeed) {
                MovePosition(a, b, c, t);
                yield return null;
            }
            hexGrid.DecreaseVisibility(currentTravelLocation, VisionRange);
            t -= 1f ;
        }

        currentTravelLocation = null ;

        a = c;
        b = location.postion;
        c = b;
        hexGrid.IncreaseVisibility(location, VisionRange);
        for (; t < 1; t += Time.deltaTime * travelSpeed) {
            MovePosition(a, b, c, t);
            yield return null ;
        }
        transform.localPosition = Location.postion ;
        Orientation = transform.localRotation.eulerAngles.y ;

        ListPool<HexCell>.Add(pathToTravel);

    }

    private void MovePosition( Vector3 a , Vector3 b , Vector3 c , float t ) {
        transform.localPosition = Bezier.GetPoint(a, b, c, t);
        Vector3 d = Bezier.GetDerivative(a, b, c, t);
        d.y = 0f ;
        transform.localRotation = Quaternion.LookRotation(d);
    }

    private IEnumerator LoolAt( Vector3 point ) {
        point.y = transform.localPosition.y ;
        Quaternion fromRotation = transform.localRotation ;
        Quaternion toRotation = Quaternion.LookRotation( point - transform.localPosition );
        float angle = Quaternion.Angle( fromRotation , toRotation ) ;
        if ( angle > 0 ) {
            float speed = rotationSpeed / angle;
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
            transform.LookAt(point);
            Orientation = transform.localRotation.eulerAngles.y;
        }
        
    }

    public int GetMoveCost( HexCell fromCell , HexCell toCell , HexDirectionEnum direction ) {
        if ( !IsValidDestination( toCell ) ) return -1 ;
        if (fromCell.HasRiver) return -1;
        if (fromCell.Walled != toCell.Walled) return -1;

        HexEdgeType edgeType = fromCell.GetEdgeType( toCell ) ;
        if (edgeType == HexEdgeType.Cliff ) return -1 ;

        if ( fromCell.HasRoadThroughEdge( direction ) ) return 1 ;

        int moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
        moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;

        return moveCost ;
        
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if ( pathToTravel == null || pathToTravel.Count <= 0) return ;

        Color gColor = Gizmos.color ;
        Gizmos.color = Color.red;

        Vector3 a , b , c = pathToTravel[ 0 ].postion ;
        for ( int i = 1 ; i < pathToTravel.Count ; i++ ) {
            a = c ;
            b = pathToTravel[i - 1].postion;
            c = (b + pathToTravel[ i ].postion) * 0.5f ;
            for ( float t = 0 ; t < 1 ; t+=0.01f ) {
                Gizmos.DrawSphere( Bezier.GetPoint( a,b,c,t ) , .2f ) ;
            }
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].postion;
        c = b ;
        for (float t = 0; t < 1; t += 0.01f) {
            Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), .2f);
        }

        Gizmos.color = gColor ;
    }

#endif


    public void Die() {
        if ( Location ) {
            //Location.DecreaseVisibility();
            hexGrid.DecreaseVisibility(location, VisionRange);
            Location.Unit = null ;
        }
        Destroy( gameObject );
    }

}
