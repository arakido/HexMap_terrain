using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour {

    public HexFeatureCollection[] urbanCollections ;
    public HexFeatureCollection[] farmCollections ;
    public HexFeatureCollection[] plantCollections ;
    public HexMesh walls;
    public Transform wallTower ;
    public Transform bridge ;

    private Transform container;

    public void AddFeature(HexCell cell, Vector3 position ) {

        HexHash hash = HexMetrics.SampleHashGrid( position ) ;
        Transform prefab = PickPrefab(urbanCollections, cell.UrbanLevel , hash.a ,hash.d) ;
        Transform otherPrefab = PickPrefab(farmCollections, cell.FarmLevel , hash.b ,hash.d) ;
        float usedHash = hash.a ;
        if ( otherPrefab != null ) {
            if ( prefab == null || hash.b < hash.a ) {
                prefab = otherPrefab ;
                usedHash = hash.b ;
            }
        }

        otherPrefab = PickPrefab(plantCollections, cell.PlantLevel, hash.c, hash.d);
        if ( otherPrefab != null ) {
            if ( prefab == null || hash.c < usedHash) {
                prefab = otherPrefab ;
            }
        }

        if ( prefab == null ) return ;

        Transform item = Instantiate( prefab ) ;
        position.y += item.localScale.y * 0.5f ;
        item.localPosition = HexMetrics.Perturb(position) ;
        item.localRotation = Quaternion.Euler( 0f , 360f * hash.e, 0f ) ;
        item.SetParent( container , false ) ;
    }

    private Transform PickPrefab(HexFeatureCollection[] collections, int level , float hash ,float choice) {
        if ( level <= 0 ) return null ;
        float[] thresholds = HexMetrics.GetFeatureThresholds( level - 1 ) ;
        for ( int i = 0 ; i < thresholds.Length ; i++ ) {
            if ( hash < thresholds[ i ] ) return collections[ i ].Pick( choice ) ;
        }

        return null ;
    }

    public void Apply() {
        walls.Apply();
    }

    public void Clear() {
        if ( container ) DestroyImmediate( container.gameObject ) ;
        container = new GameObject("Features Container").transform;
        container.SetParent( transform , false ) ;
        walls.Clear();
    }

    #region 围墙

    private bool IsCanBuildWall(HexCell nearCell,HexCell farCell) {
        return nearCell.Walled != farCell.Walled &&
               !nearCell.IsUnderWater &&
               !farCell.IsUnderWater &&
               nearCell.GetEdgeType( farCell ) != HexEdgeType.Cliff ;
    }
    
    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell,bool hasRiver,bool hasRoad) {
        if ( IsCanBuildWall( nearCell , farCell ) ) {
            AddWallSegment( near.v1 , far.v1 , near.v2 , far.v2 ) ;
            if ( hasRiver || hasRoad ) {
                AddWallCap( near.v2 , far.v2 ) ;
                AddWallCap( far.v4 , near.v4 ) ;
            }
            else {
                AddWallSegment( near.v2 , far.v2 , near.v3 , far.v3 ) ;
                AddWallSegment( near.v3 , far.v3 , near.v4 , far.v4 ) ;
            }
            AddWallSegment( near.v4 , far.v4 , near.v5 , far.v5 ) ;
        }
    }

    public void AddWall( Vector3 c1 , HexCell cell , Vector3 c2 , HexCell leftCell , Vector3 c3 , HexCell rightCell ) {
        if ( cell.Walled ) {
            if ( leftCell.Walled ) {
                if ( !rightCell.Walled ) AddWallSegment( c3 , rightCell , c1 , cell , c2 , leftCell ) ;
            }
            else if ( rightCell.Walled ) AddWallSegment( c2 , leftCell , c3 , rightCell , c1 , cell ) ;
            else AddWallSegment( c1 , cell , c2 , leftCell , c3 , rightCell ) ;
        }
        else if ( leftCell.Walled ) {
            if ( rightCell.Walled ) AddWallSegment(c1, cell, c2, leftCell, c3, rightCell);
            else AddWallSegment(c2, leftCell, c3, rightCell, c1, cell);
        }
        else if ( rightCell.Walled ) {
            AddWallSegment(c3, rightCell, c1, cell, c2, leftCell);
        }
    }

    private void AddWallSegment( Vector3 nearLeft , Vector3 farLeft , Vector3 nearRight , Vector3 farRight ) {

        nearLeft = HexMetrics.Perturb(nearLeft) ;
        farLeft = HexMetrics.Perturb( farLeft ) ;
        nearRight = HexMetrics.Perturb( nearRight ) ;
        farRight = HexMetrics.Perturb( farRight ) ;

        Vector3 left = HexMetrics.WallLerp( nearLeft , farLeft ) ;
        Vector3 right = HexMetrics.WallLerp( nearRight , farRight ) ;

        Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset( nearLeft , farLeft ) ;
        Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset( nearRight , farRight ) ;
        float leftTop = left.y + HexMetrics.wallHeight;
        float rightTop = right.y + HexMetrics.wallHeight;

        Vector3 v1 ;
        Vector3 v2 ;
        Vector3 v3 = v1 = left - leftThicknessOffset;
        Vector3 v4 = v2 = right - rightThicknessOffset;
        v3.y = leftTop ;
        v4.y = rightTop ;
        walls.AddQuad( v1 , v2 , v3 , v4 ) ;

        Vector3 t1 = v3 ;
        Vector3 t2 = v4 ;

        v1 = v3 = left + leftThicknessOffset ;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;

        walls.AddQuad( v2 , v1 , v4 , v3 ) ;
        walls.AddQuad( t1 , t2 , v3 , v4 ) ;
    }

    private void AddWallSegment( Vector3 pivot , HexCell pivotCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {
        if ( pivotCell.IsUnderWater ) return ;
        bool hasLeftWall = !leftCell.IsUnderWater && pivotCell.GetEdgeType( leftCell ) != HexEdgeType.Cliff ;
        bool hasRightWall = !rightCell.IsUnderWater && pivotCell.GetEdgeType( rightCell ) != HexEdgeType.Cliff ;
        if ( hasLeftWall ) {
            if ( hasRightWall ) AddWallSegment( pivot , left , pivot , right ) ;
            else if ( leftCell.Elevation < rightCell.Elevation ) AddWallWedge( pivot , left , right ) ;
            else AddWallCap( pivot , left ) ;
        }
        else if ( hasRightWall ) {
            if ( rightCell.Elevation < leftCell.Elevation ) AddWallWedge( right , pivot,left ) ;
            else AddWallCap( right , pivot ) ;
        }

        if ( leftCell.Elevation == rightCell.Elevation ) {
            HexHash hash = HexMetrics.SampleHashGrid( (pivot + left + right) * (1 / 3f) ) ;
            if ( hash.e < HexMetrics.wallTowerThreshold ) AddWallTower( pivot , left , pivot , right ) ;
        }
    }

    private void AddWallCap( Vector3 near , Vector3 far ) {
        near = HexMetrics.Perturb( near ) ;
        far = HexMetrics.Perturb( far ) ;

        Vector3 center = HexMetrics.WallLerp( near , far ) ;
        Vector3 thickness = HexMetrics.WallThicknessOffset( near , far ) ;

        Vector3 v1 ;
        Vector3 v2 ;
        Vector3 v3 = v1 = center - thickness ;
        Vector3 v4 = v2 = center + thickness ;
        v3.y = v4.y = center.y + HexMetrics.wallHeight ;
        walls.AddQuad( v1 , v2 , v3 , v4 ) ;

    }

    private void AddWallWedge( Vector3 near , Vector3 far , Vector3 point ) {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1;
        Vector3 v2;
        Vector3 v3 = v1 = center - thickness;
        Vector3 v4 = v2 = center + thickness;
        Vector3 pointTop = point ;
        point.y = center.y ;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;
        walls.AddQuad(v1, point, v3, pointTop);
        walls.AddQuad(point, v2, pointTop, v4);
        walls.AddTriangle( pointTop , v3 , v4 ) ;
    }

    private void AddWallTower(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight) {

        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Transform towerInstance = Instantiate(wallTower);
        towerInstance.transform.localPosition = (left + right) * 0.5f;
        Vector3 rightDirection = right - left;
        rightDirection.y = 0;
        towerInstance.transform.right = rightDirection;
        towerInstance.SetParent(container, false);
    }

    #endregion

    #region 桥梁

    public void AddBridge( Vector3 roadCenter1 , Vector3 roadCenter2 ) {
        roadCenter1 = HexMetrics.Perturb( roadCenter1 ) ;
        roadCenter2 = HexMetrics.Perturb( roadCenter2 ) ;
        Transform instance = Instantiate( bridge ) ;
        instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f ;
        instance.right = roadCenter2 - roadCenter1 ;
        instance.SetParent( container , false ) ;
    }

    #endregion
}
