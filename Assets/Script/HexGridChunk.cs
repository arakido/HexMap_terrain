using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking ;

public class HexGridChunk : MonoBehaviour {

    private HexCell[] cells ;
    private Canvas gridCanvas ;

    public HexMesh terrain ;
    public HexMesh rivers ;
    public HexMesh roads;
    public HexMesh water ;
    public HexMesh waterShore ;
    public HexMesh estuaries;

    public HexFeatureManager feature ;

    //三角拼图的三种纹理颜色
    private static Color weights1 = new Color( 1f , 0f , 0f ) ;
    private static Color weights2 = new Color( 0f , 1f , 0f ) ;
    private static Color weights3 = new Color( 0f , 0f , 1f ) ;

    private void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>() ;

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];

        ShowUI( false );
    }

	// Use this for initialization
	void Start () {
	}

    public void ShowUI( bool visible ) {
        gridCanvas.gameObject.SetActive( visible );
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void LateUpdate() {
        Triangulate();
        enabled = false ;
    }

    public void Refresh() {

        enabled = true ;
    }

    public void AddCell( int index , HexCell cell ) {
        cell.chunk = this ;
        cells[ index ] = cell ;
        cell.transform.SetParent( terrain.transform , false ) ;
        cell.uiRect.SetParent( gridCanvas.transform , false ) ;
    }

    private void Triangulate() {
        terrain.Clear() ;
        rivers.Clear();
        roads.Clear() ;
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        feature.Clear();
        for ( int i = 0 ; i < cells.Length ; i++ ) {
            Triangulate( cells[ i ] ) ;
        }
        terrain.Apply() ;
        rivers.Apply() ;
        roads.Apply() ;
        water.Apply() ;
        waterShore.Apply();
        estuaries.Apply();
        feature.Apply();
    }

    #region 绘制六边形

    /*
     * 参考资料：https://indienova.com/indie-game-development/hex-map-part-2/
     * 
     *      v3     v4
     * v5 ____________ v6
     *    \ |      | / 
     *   v1\|______|/v2
     *      \      /
     *       \    /
     *        \  /
     *         \/
     *        center
     */
    /// <summary>
    /// 绘制六边形
    /// </summary>
    /// <param name="cell"></param>
    private void Triangulate( HexCell cell ) {
        if ( cell == null ) {
            Debug.LogError( "Cell is Null" );
        };

        for ( HexDirectionEnum i = 0 ; i < HexDirectionEnum.Length ; i++ ) {
            Triangulate( cell , i ) ;
        }

        //特征物体
        if ( !cell.IsUnderWater ) {
            if ( !cell.HasRiver && !cell.HasRoads ) {
                feature.AddFeature( cell , cell.postion ) ;
            }
            if ( cell.IsSpecial ) {
                feature.AddSpecialFeature( cell , cell.postion ) ;
            }
        }
    }

    private void Triangulate( HexCell cell , HexDirectionEnum direction ) {
        Vector3 center = cell.postion ;
        //绘制内三角
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner( direction ) ;
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner( direction ) ;

        EdgeVertices edge = new EdgeVertices( v1 , v2 ) ;

        if ( cell.HasRiver ) {
            if ( cell.HasRiverThroughEdge( direction ) ) {
                edge.v3.y = cell.StreamBedHight ;
                if ( cell.HasRiverBeginOrEnd ) {
                    TriangulateWithRiverBeginOrEnd( cell , direction , center , edge ) ;
                }
                else {
                    TriangulateWithRiver( cell , direction , center , edge ) ;
                }
            }
            else {
                TriangulateAdjacentToRiver( cell , direction , center , edge ) ;
                if ( !cell.IsUnderWater && !cell.HasRoadThroughEdge( direction ) ) {
                    feature.AddFeature( cell , (center + edge.v1 + edge.v5) * (1 / 3f) ) ;
                }
            }
        }
        else {
            TriangulateWithoutRiver( cell , direction , center , edge ) ;
            if ( !cell.IsUnderWater && !cell.HasRoadThroughEdge( direction ) ) {
                feature.AddFeature( cell , (center + edge.v1 + edge.v5) * (1 / 3f) ) ;
            }
        }

        //绘制外梯形，为了避免重复绘制，两个三角形只绘制一次
        if ( DirectionOnRight( direction ) ) {
            TrangulateConnection( direction , cell , edge ) ;
        }
        /*else if ( cell.GetNeighbor( direction ) == null ) {
            NoNeighborConnection( direction , cell , edge ) ;
        }*/

        if ( cell.IsUnderWater ) {
            TriangulateWater( direction , cell , center ) ;
        }
    }

    private void TriangulateEdgeFan( Vector3 center , EdgeVertices edge , float index) {
        AddTerrainPerturTriangle( center , edge.v1 , edge.v2 ) ;
        AddTerrainPerturTriangle( center , edge.v2 , edge.v3 ) ;
        AddTerrainPerturTriangle( center , edge.v3 , edge.v4 ) ;
        AddTerrainPerturTriangle( center , edge.v4 , edge.v5 ) ;

        Vector3 indices ;
        indices.x = indices.y = indices.z = index;
        terrain.AddTriangleCellData( indices,weights1 );
        terrain.AddTriangleCellData( indices,weights1 );
        terrain.AddTriangleCellData( indices,weights1 );
        terrain.AddTriangleCellData( indices,weights1 );
        
    }

    private void TriangulateEdgeStrip( EdgeVertices edge1 , Color w1 , float index1,
                                       EdgeVertices edge2 , Color w2 , float index2, bool hasRoad = false) {
        AddTerrainQuad( edge1.v1 , edge1.v2 , edge2.v1 , edge2.v2 ) ;
        AddTerrainQuad( edge1.v2 , edge1.v3 , edge2.v2 , edge2.v3 ) ;
        AddTerrainQuad( edge1.v3 , edge1.v4 , edge2.v3 , edge2.v4 ) ;
        AddTerrainQuad( edge1.v4 , edge1.v5 , edge2.v4 , edge2.v5 ) ;

        Vector3 indices ;
        indices.x = indices.z = index1 ;
        indices.y = index2 ;
        terrain.AddQuadCellData( indices,w1 ,w2);
        terrain.AddQuadCellData( indices,w1 ,w2);
        terrain.AddQuadCellData( indices,w1 ,w2);
        terrain.AddQuadCellData( indices,w1 ,w2);
        
        if ( hasRoad ) {
            TriangulateRoadSegment( edge1.v2 , edge1.v3 , edge1.v4 , edge2.v2 , edge2.v3 , edge2.v4 , w1 , w2 , indices ) ;
        }
    }

    private void TrangulateConnection( HexDirectionEnum direction , HexCell cell , EdgeVertices edge ) {
        HexCell neighbor = cell.GetNeighbor( direction ) ;
        if ( neighbor == null ) {
            //NoNeighborConnection( direction , cell , edge ) ;
            return ;
        }
        Vector3 bridge = HexMetrics.GetTwoBridge( direction ) ;
        bridge.y = neighbor.postion.y - cell.postion.y ;

        EdgeVertices edge2 = new EdgeVertices( edge.v1 + bridge , edge.v5 + bridge ) ;

        bool hasRiver = cell.HasRiverThroughEdge(direction);
        bool hasRoad = cell.HasRoadThroughEdge(direction);

        if ( hasRiver ) {
            edge2.v3.y = neighbor.StreamBedHight ;

            Vector3 indices ;
            indices.x = indices.z = cell.Index ;
            indices.y = neighbor.Index ;

            if ( !cell.IsUnderWater ) {
                if ( !neighbor.IsUnderWater ) {
                    TriangulateRiverQuad( edge.v2 , edge.v4 , edge2.v2 , edge2.v4 , cell.RiverSurfaceHight ,
                                          neighbor.RiverSurfaceHight , 0.8f ,
                                          cell.HasInComingRiver && cell.InComingRive == direction , indices) ;
                }
                else if ( cell.Elevation > neighbor.WaterLevel ) {
                    TriangulateWaterFallInWater( edge.v2 , edge.v4 , edge2.v2 , edge2.v4 , cell.RiverSurfaceHight ,
                                                 neighbor.RiverSurfaceHight , neighbor.WaterSurfaceHight , indices) ;
                }
            }
            else if ( !neighbor.IsUnderWater && neighbor.Elevation > cell.WaterLevel ) {
                TriangulateWaterFallInWater( edge2.v4 , edge2.v2 , edge.v4 , edge.v2 , neighbor.RiverSurfaceHight ,
                                             cell.RiverSurfaceHight , cell.WaterSurfaceHight , indices) ;
            }
        }
        

        if ( cell.GetEdgeType( direction ) == HexEdgeType.Slope ) {
            TriangulateEdgeTerraces( edge , cell, edge2 , neighbor, hasRoad ) ;
        }
        else {
            TriangulateEdgeStrip( edge , weights1 , cell.Index, edge2 , weights2 , neighbor.Index, hasRoad ) ;
        }

        feature.AddWall( edge , cell , edge2 , neighbor ,hasRiver,hasRoad) ;
        

        //处理三角形
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        if ( nextNeighbor != null ) {
            //避免重复绘制，只绘制左上和上方的三角
            if ( direction != HexDirectionEnum.BottomRight ) {
                Vector3 v5 = edge.v5 + HexMetrics.GetTwoBridge( direction.Next() ) ;
                v5.y = nextNeighbor.postion.y ;

                if ( cell.Elevation <= neighbor.Elevation ) {
                    if ( cell.Elevation <= nextNeighbor.Elevation ) {
                        TriangulateCorner( edge.v5 , cell , edge2.v5 , neighbor , v5 , nextNeighbor ) ;
                    }
                    else {
                        TriangulateCorner( v5 , nextNeighbor , edge.v5 , cell , edge2.v5 , neighbor) ;
                    }
                }
                else if ( neighbor.Elevation <= nextNeighbor.Elevation ) {
                    TriangulateCorner( edge2.v5 , neighbor , v5 , nextNeighbor , edge.v5 , cell) ;
                }
                else {
                    TriangulateCorner( v5 , nextNeighbor , edge.v5 , cell , edge2.v5 , neighbor) ;
                }
            }
        }
        /*else {

            //绘制缺失的小三角 
            HexCell noneCell = new GameObject().AddComponent<HexCell>() ;
            noneCell.Index = neighbor.Index ;
            Vector3 v5 = cell.postion + HexMetrics.GetSecondCorner( direction ) ;
            v5.y = 0 ;
            TriangulateCorner( edge.v5 , cell , edge2.v5 , neighbor , v5 , noneCell ) ;
            DestroyObject( noneCell.gameObject ) ;
        }

        //绘制缺失的小三角 
        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() ) ;
        if ( prevNeighbor == null ) {
            HexCell noneCell = new GameObject().AddComponent<HexCell>() ;
            noneCell.Index = neighbor.Index;
            Vector3 v5 = cell.postion + HexMetrics.GetFirstCorner( direction ) ;
            v5.y = 0 ;
            TriangulateCorner( edge.v1 , cell , v5 , noneCell , edge2.v1 , neighbor) ;
            DestroyObject( noneCell.gameObject ) ;
        }*/
    }

    //仅处理无临边的情况
    private void NoNeighborConnection( HexDirectionEnum direction , HexCell cell , EdgeVertices edge ) {
        Vector3 center = cell.postion ;
        Vector3 bridge = HexMetrics.GetOneBridge( direction ) ;
        Vector3 v3 = edge.v1 + bridge ;
        Vector3 v4 = edge.v5 + bridge ;
        Vector3 v5 = center + HexMetrics.GetFirstCorner( direction ) ;
        Vector3 v6 = center + HexMetrics.GetSecondCorner( direction ) ;

        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() ) ;
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        v6.y = v5.y = v4.y = v3.y = 0 ;

        EdgeVertices edge2 = new EdgeVertices( v5 , v6 ) ;


        if ( nextNeighbor == null && prevNeighbor != null ) {
            edge2 = new EdgeVertices( v3 , v6 ) ;

            HexCell noneCell = new GameObject().AddComponent<HexCell>() ;
            noneCell.Index = cell.Index ;
            TriangulateCorner( edge.v1 , cell , v5 , prevNeighbor , v3 , noneCell) ;
            DestroyObject( noneCell.gameObject ) ;
        }
        if ( cell.Elevation == HexMetrics.elevationDiffer ) {
            TriangulateEdgeTerraces( edge , cell , edge2 , cell ,cell.HasRoadThroughEdge( direction )) ;
        }
        else {
            TriangulateEdgeStrip( edge , weights1 , cell.Index, edge2 , weights1, cell.Index, cell.HasRoadThroughEdge( direction ) ) ;
        }
    }
    
    //绘制阶梯
    private void TriangulateEdgeTerraces( EdgeVertices begin , HexCell beginCell , EdgeVertices end , HexCell endCell ,bool hasRoad) {

        EdgeVertices e2 = begin ;
        Color c2 = weights1;
        float t1 = beginCell.Index;
        float t2 = endCell.Index;

        for ( int i = 1 ; i <= HexMetrics.terraceSetps ; i++ ) {

            EdgeVertices e1 = e2 ;
            Color c1 = c2 ;

            e2 = EdgeVertices.Lerp( begin , end , i ) ;
            c2 = HexMetrics.TerraceLerp( weights1 , weights2 , i ) ;

            TriangulateEdgeStrip( e1, c1,t1, e2, c2,t2, hasRoad );
        }
    }


    //绘制小三角
    private void TriangulateCorner( Vector3 begin , HexCell beginCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {
        HexEdgeType leftEdgeType = beginCell.GetEdgeType( leftCell ) ;
        HexEdgeType rightEdgeType = beginCell.GetEdgeType( rightCell ) ;

        if ( leftEdgeType == HexEdgeType.Slope ) {
            if ( rightEdgeType == HexEdgeType.Slope ) {
                TriangulateCornerTerraces( begin , beginCell, left , leftCell, right , rightCell) ;
            }
            else if ( rightEdgeType == HexEdgeType.Flat ) {
                TriangulateCornerTerraces( left , leftCell, right , rightCell, begin , beginCell) ;
            }
            else {
                TriangulateCornerTerracesCliff( begin , beginCell , left , leftCell , right , rightCell ) ;
            }
        }

        else if ( rightEdgeType == HexEdgeType.Slope ) {
            if ( leftEdgeType == HexEdgeType.Flat ) {
                TriangulateCornerTerraces( right , rightCell, begin , beginCell, left , leftCell) ;
            }
            else {
                TriangulateCornerCliffTerraces( begin , beginCell , left , leftCell , right , rightCell ) ;
            }
        }
        else if ( leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope ) {
            if ( leftCell.Elevation < rightCell.Elevation ) {
                TriangulateCornerCliffTerraces( right , rightCell , begin , beginCell , left , leftCell ) ;
            }
            else {
                TriangulateCornerTerracesCliff( left , leftCell , right , rightCell , begin , beginCell ) ;
            }
        }
        else {
            AddTerrainPerturTriangle( begin , left , right ) ;
            Vector3 indices ;
            indices.x = beginCell.Index ;
            indices.y = leftCell.Index ;
            indices.z = rightCell.Index ;
            terrain.AddTriangleCellData( indices , weights1 , weights2 , weights3 ) ;
            
        }

        feature.AddWall( begin , beginCell , left , leftCell , right , rightCell ) ;
    }

    //绘制斜坡小三角
    private void TriangulateCornerTerraces( Vector3 begin , HexCell beginCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {

        Vector3 v3 = begin ;
        Vector3 v4 = begin ;
        Color w3 = weights1 ;
        Color w4 = weights1;

        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        for ( int i = 1 ; i <= HexMetrics.terraceSetps ; i++ ) {
            Vector3 v1 = v3 ;
            Vector3 v2 = v4 ;
            Color w1 = w3 ;
            Color w2 = w4 ;
            v3 = HexMetrics.TerraceLerp( begin , left , i ) ;
            v4 = HexMetrics.TerraceLerp( begin , right , i ) ;
            w3 = HexMetrics.TerraceLerp( weights1 , weights2 , i ) ;
            w4 = HexMetrics.TerraceLerp( weights1 , weights3 , i ) ;

            if ( v1 == v2 ) {
                AddTerrainPerturTriangle( v1 , v3 , v4 ) ;
                terrain.AddTriangleCellData( indices,w1,w3,w4 );
            }
            else {
                AddTerrainQuad( v1 , v2 , v3 , v4 ) ;

                terrain.AddQuadCellData( indices,w1,w2,w3,w4 );
            }
        }
    }

    //绘制绝壁三角
    private void TriangulateCornerTerracesCliff( Vector3 begin , HexCell beginCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {

        float b = Mathf.Abs( 1f / (rightCell.Elevation - beginCell.Elevation) ) ;
        Vector3 boundary = Vector3.Lerp( HexMetrics.Perturb( begin ) , HexMetrics.Perturb( right ) , b ) ;
        Color boundaryWeights = Color.Lerp( weights1 , weights3 , b ) ;
        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        TriangulateBoundaryTriangle( begin , weights1, left , weights2 , boundary , boundaryWeights , indices) ;

        if ( leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope ) {
            TriangulateBoundaryTriangle( left , weights2, right , weights3, boundary , boundaryWeights, indices) ;
        }
        else {
            AddTerrainTriangle( HexMetrics.Perturb( left ) , HexMetrics.Perturb( right ) , boundary ) ;
            terrain.AddTriangleCellData( indices , weights2 , weights3 , boundaryWeights ) ;
        }
    }

    private void TriangulateCornerCliffTerraces( Vector3 begin , HexCell beginCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {
        float b = Mathf.Abs( 1f / (leftCell.Elevation - beginCell.Elevation) ) ;

        Vector3 boundary = Vector3.Lerp( HexMetrics.Perturb( begin ) , HexMetrics.Perturb( left ) , b ) ;
        Color boundaryWeights = Color.Lerp( weights1 , weights2 , b ) ;
        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        TriangulateBoundaryTriangle( right , weights3 , begin , weights1, boundary , boundaryWeights, indices) ;

        if ( leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope ) {
            TriangulateBoundaryTriangle( left , weights2, right , weights3, boundary , boundaryWeights, indices) ;
        }
        else {
            AddTerrainTriangle( HexMetrics.Perturb( left ) , HexMetrics.Perturb( right ) , boundary ) ;
            terrain.AddTriangleCellData( indices , weights2 , weights3 , boundaryWeights ) ;
        }
    }

    private void TriangulateBoundaryTriangle( Vector3 begin , Color beginWeights , Vector3 left , Color leftWeights , Vector3 boundary , Color boundaryWeights ,Vector3 indices) {

        Vector3 v2 = HexMetrics.Perturb( begin ) ;
        Color w2 = beginWeights;

        for ( int i = 1 ; i <= HexMetrics.terraceSetps ; i++ ) {
            Vector3 v1 = v2 ;
            Color w1 = w2 ;
            v2 = HexMetrics.Perturb( HexMetrics.TerraceLerp( begin , left , i ) ) ;
            w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i ) ;
            AddTerrainTriangle( v1 , v2 , boundary ) ;
            terrain.AddTriangleCellData(indices, w1,w2,boundaryWeights );
        }

    }


    private void AddTerrainTriangle( Vector3 v1, Vector3 v2, Vector3 v3 ) {
        terrain.AddTriangle( v1, v2, v3 );
    }

    private void AddTerrainPerturTriangle( Vector3 v1, Vector3 v2, Vector3 v3 ) {
        terrain.AddPerturTriangle( v1, v2, v3 );
    }

    private void AddTerrainQuad( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4 ) {
        terrain.AddPerturQuad( v1, v2, v3, v4 );
    }

    

    #endregion

    #region  处理河流

    /*
     * e.v1 _________________ e.v5
     *      \   |   |   |   /
     *   m.v1\__|___|___|__/m.v5
     *        \ |   |   | /
     *         \|___|___|/
     *         cL   c   cR
     * e.v1 ~ e.v5 平均分成4分  每份是1/4
     * m/e = 3/4,所以 m.v1 ~ m.v5 是1/6，1/4，1/4，1/6
     */

    /// <summary>
    /// 河道三角化
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="direction"></param>
    /// <param name="center"></param>
    /// <param name="edge"></param>
    private void TriangulateWithRiver( HexCell cell , HexDirectionEnum direction , Vector3 center , EdgeVertices edge ) {
        Vector3 centerL ;
        Vector3 centerR ;
        if ( cell.HasRiverThroughEdge( direction.Opposite() ) ) {
            centerL = center + HexMetrics.GetFirstSolidCorner( direction.Previous() ) * 1 / 4f ;
            centerR = center + HexMetrics.GetSecondSolidCorner( direction.Next() ) * 1 / 4f ;
        }
        else if ( cell.HasRiverThroughEdge( direction.Previous() ) ) {
            centerL = Vector3.Lerp( center , edge.v1 , 2 / 3f ) ;
            centerR = center ;
        }
        else if ( cell.HasRiverThroughEdge( direction.Next() ) ) {
            centerL = center ;
            centerR = Vector3.Lerp( center , edge.v5 , 2 / 3f ) ;
        }
        else if ( cell.HasRiverThroughEdge( direction.Next( 2 ) ) ) {
            centerL = center ;
            centerR = center + HexMetrics.GetSolidEdgeMiddle( direction.Next() ) * (0.5f * HexMetrics.innerToOuter) ;
        }
        else {
            centerL = center + HexMetrics.GetSolidEdgeMiddle( direction.Previous() ) * (0.5f * HexMetrics.innerToOuter) ;
            centerR = center ;
        }

        center = Vector3.Lerp( centerL , centerR , 0.5f ) ;

        EdgeVertices m = new EdgeVertices( Vector3.Lerp( centerL , edge.v1 , 0.5f ) ,
                                           Vector3.Lerp( centerR , edge.v5 , 0.5f ) , 1 / 6f ) ;
        m.v3.y = center.y = edge.v3.y ;
        TriangulateEdgeStrip( m , weights1,cell.Index, edge , weights1, cell.Index) ;

        AddTerrainPerturTriangle( centerL , m.v1 , m.v2 ) ;
        AddTerrainQuad( centerL , center , m.v2 , m.v3 ) ;
        AddTerrainQuad( center , centerR , m.v3 , m.v4 ) ;
        AddTerrainPerturTriangle( centerR , m.v4 , m.v5 ) ;

        Vector3 indices;
        indices.x = indices.y =indices.z = cell.Index;
        terrain.AddTriangleCellData( indices,weights1 );
        terrain.AddQuadCellData( indices,weights1 );
        terrain.AddQuadCellData( indices,weights1 );
        terrain.AddTriangleCellData( indices,weights1 );
        
        bool reversed = cell.InComingRive == direction ;
        TriangulateRiverQuad( centerL , centerR , m.v2 , m.v4 , cell.RiverSurfaceHight , 0.4f , reversed, indices) ;
        TriangulateRiverQuad( m.v2 , m.v4 , edge.v2 , edge.v4 , cell.RiverSurfaceHight , 0.6f , reversed, indices) ;
    }

    private void TriangulateWithoutRiver( HexCell cell , HexDirectionEnum direction , Vector3 center , EdgeVertices edge ) {
        TriangulateEdgeFan( center, edge, cell.Index );
        if ( cell.IsUnderWater ) {
            return ;
        }
        if ( cell.HasRoads ) {
            Vector2 interpoltors = GetRoadInterpolators( direction, cell );
            TriangulateRoad( center, 
                             Vector3.Lerp( center, edge.v1, interpoltors.x ), 
                             Vector3.Lerp( center, edge.v5, interpoltors.y ), 
                             edge, cell.HasRoadThroughEdge( direction ) , cell.Index);
        }
    }


    private void TriangulateWithRiverBeginOrEnd( HexCell cell , HexDirectionEnum direction , Vector3 center , EdgeVertices edge ) {

        EdgeVertices m = new EdgeVertices( Vector3.Lerp( center , edge.v1 , 0.5f ) , Vector3.Lerp( center , edge.v5 , 0.5f ) ) ;
        m.v3.y = edge.v3.y ;
        TriangulateEdgeStrip( m , weights1, cell.Index, edge, weights1, cell.Index) ;
        TriangulateEdgeFan( center , m , cell.Index) ;

        if ( cell.IsUnderWater ) return ;

        bool reversed = cell.HasInComingRiver ;

        Vector3 indices ;
        indices.x = indices.y = indices.z = cell.Index ;

        TriangulateRiverQuad( m.v2 , m.v4 , edge.v2 , edge.v4 , cell.RiverSurfaceHight , 0.6f ,reversed, indices);

        center.y = m.v2.y = m.v4.y = cell.RiverSurfaceHight ;
        rivers.AddPerturTriangle( center , m.v2 , m.v4 ) ;
        if ( reversed ) {
            rivers.AddTriangleUV( new Vector2( 0.5f , 0.4f ) , new Vector2( 1f , 0.2f ) , new Vector2( 0f , 0.2f ) ) ;
        }
        else {
            rivers.AddTriangleUV( new Vector2( 0.5f , 0.4f ) , new Vector2( 0f , 0.6f ) , new Vector2( 1f , 0.6f ) ) ;
        }

        rivers.AddTriangleCellData( indices,weights1 );
        
    }

    private void TriangulateAdjacentToRiver( HexCell cell , HexDirectionEnum direction , Vector3 center , EdgeVertices edge ) {
        if ( cell.HasRoads ) {
            TriangulateRoadAdjacentToRiver( direction,cell,center,edge );
        }
        if ( cell.HasRiverThroughEdge( direction.Next() ) ) {
            if ( cell.HasRiverThroughEdge( direction.Previous() ) ) {
                center += HexMetrics.GetSolidEdgeMiddle( direction ) * (HexMetrics.innerToOuter * 0.5f) ;
            }
            else if ( cell.HasRiverThroughEdge( direction.Previous( 2 ) ) ) {
                center += HexMetrics.GetFirstSolidCorner( direction ) * 0.25f ;
            }
        }
        else if ( cell.HasRiverThroughEdge( direction.Previous() ) && cell.HasRiverThroughEdge( direction.Next( 2 ) ) ) {
            center += HexMetrics.GetSecondSolidCorner( direction ) * 0.25f ;
        }
        EdgeVertices m = new EdgeVertices( Vector3.Lerp( center , edge.v1 , 0.5f ) ,
                                           Vector3.Lerp( center , edge.v5 , 0.5f ) ) ;
        TriangulateEdgeStrip( m , weights1 , cell.Index,edge, weights1, cell.Index) ;
        TriangulateEdgeFan( center , m , cell.Index) ;
    }

    private void TriangulateRiverQuad( Vector3 v1 , Vector3 v2 , Vector3 v3 , Vector3 v4 ,float y , float v, bool reversed ,Vector3 indices) {
        TriangulateRiverQuad( v1 , v2 , v3 , v4 , y , y , v , reversed ,indices) ;
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2 , float v, bool reversed, Vector3 indices) {
        v1.y = v2.y = y1 ;
        v3.y = v4.y = y2 ;
        rivers.AddPerturQuad( v1, v2, v3, v4);
        if ( reversed ) rivers.AddQuadUV( 1f , 0f , 0.8f - v , 0.6f - v ) ;
        else rivers.AddQuadUV( 0f , 1f , v , v + 0.2f ) ;

        rivers.AddQuadCellData( indices , weights1 , weights2 ) ;
    }
    

    #endregion

    #region 处理道路

    /*
     *e.v1 ___________________e.v5
     *     \  |     |     |  /
     *      \ |     |     | / 
     *     mL\|_____|_____|/mR
     *        \     |     /
     *          \   |   /
     *            \ | /
     *             \|/
     *             center
     */
    private void TriangulateRoad( Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices edge ,bool hasDirectionRoad ,float index) {
        if ( hasDirectionRoad ) {
            Vector3 indices;
            indices.x = indices.y = indices.z = index;

            Vector3 mC = Vector3.Lerp( mL, mR, 0.5f );
            TriangulateRoadSegment( mL , mC , mR , edge.v2 , edge.v3 , edge.v4 , weights1 , weights1 , indices ) ;
            AddRoadTriangle( center, mL, mC );
            AddRoadTriangle( center, mC, mR );
            AddRoadTriangleUV( new Vector2( 1f, 0f ), Vector2.zero, new Vector2( 1f, 0f ) );
            AddRoadTriangleUV( new Vector2( 1f, 0f ), new Vector2( 1f, 0f ), Vector2.zero );

            roads.AddTriangleCellData( indices,weights1 );
            roads.AddTriangleCellData( indices,weights1 );
        }
        else {
            TriangulateRoadEdge( center, mL, mR ,index);
        }
    }

    private void TriangulateRoadEdge( Vector3 center, Vector3 mL, Vector3 mR ,float index) {
        AddRoadTriangle( center, mL, mR );
        AddRoadTriangleUV( new Vector2( 1f, 0f ), Vector2.zero, Vector2.zero );
        Vector3 indices ;
        indices.x = indices.y = indices.z = index ;
        roads.AddTriangleCellData( indices , weights1 ) ;
    }

    private void TriangulateRoadAdjacentToRiver( HexDirectionEnum direction, HexCell cell, Vector3 center, EdgeVertices edge ) {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge( direction );
        bool previousHasRiver = cell.HasRiverThroughEdge( direction.Previous( ) );
        bool nextHasRiver = cell.HasRiverThroughEdge( direction.Next( ) );
        Vector2 interpolators = GetRoadInterpolators( direction, cell );
        Vector3 roadCenter = center;

        if ( cell.HasRiverBeginOrEnd ) {
            roadCenter += HexMetrics.GetSolidEdgeMiddle( cell.RiverBeginOrEndDirection.Opposite( ) ) * ( 1f / 3f );
        }
        else if(cell.InComingRive == cell.OutGoingRive.Opposite()) {
            Vector3 corner;
            if ( previousHasRiver ) {
                if ( !hasRoadThroughEdge && !cell.HasRoadThroughEdge( direction.Next( ) ) ) return;
                corner = HexMetrics.GetSecondSolidCorner( direction );
            }
            else {
                if ( !hasRoadThroughEdge && !cell.HasRoadThroughEdge( direction.Previous() ) ) return;
                corner = HexMetrics.GetFirstSolidCorner( direction );
            }
            roadCenter += corner * 0.5f;
            //桥梁
            if ( cell.InComingRive == direction.Next() && cell.HasRoadThroughEdge( direction.Next(2) )|| cell.HasRoadThroughEdge( direction.Opposite() )) {
                feature.AddBridge( roadCenter , center - corner * 0.5f ) ;
            }

            center += corner * 0.25f;
        }
        else if ( cell.InComingRive == cell.OutGoingRive.Previous( ) ) {
            roadCenter -= HexMetrics.GetSecondCorner( cell.InComingRive ) * 0.2f;
        }
        else if ( cell.InComingRive == cell.OutGoingRive.Next( ) ) {
            roadCenter -= HexMetrics.GetFirstCorner( cell.InComingRive ) * 0.2f;
        }
        else if( previousHasRiver && nextHasRiver ) {
            if ( !hasRoadThroughEdge ) return;
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle( direction ) * HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else {
            HexDirectionEnum middle;
            if ( previousHasRiver ) middle = direction.Next( );
            else if ( nextHasRiver ) middle = direction.Previous( );
            else middle = direction;
            if ( !cell.HasRoadThroughEdge( middle ) && !cell.HasRoadThroughEdge( middle.Previous( ) ) &&
                 !cell.HasRoadThroughEdge( middle.Next( ) ) ) return;

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle( middle ) ;
            roadCenter += offset * 0.25f;
            //桥梁
            if ( direction == middle && cell.HasRoadThroughEdge( direction.Opposite() ) ) {
                feature.AddBridge( roadCenter , center - offset * (HexMetrics.innerToOuter * 0.7f) ) ;
            }
        }

        Vector3 mL = Vector3.Lerp( roadCenter, edge.v1, interpolators.x );
        Vector3 mR = Vector3.Lerp( roadCenter, edge.v5, interpolators.y );
        TriangulateRoad( roadCenter, mL, mR, edge, hasRoadThroughEdge ,cell.Index);
        if ( previousHasRiver ) {
            TriangulateRoadEdge( roadCenter, center, mL, cell.Index);
        }
        if ( nextHasRiver ) {
            TriangulateRoadEdge( roadCenter, mR, center, cell.Index);
        }
    }

    private Vector2 GetRoadInterpolators( HexDirectionEnum direction, HexCell cell ) {
        Vector2 interpolators = new Vector2();
        if ( cell.HasRoadThroughEdge( direction ) ) {
            interpolators.x = interpolators.y = 0.5f;
        }
        else {
            interpolators.x = cell.HasRoadThroughEdge( direction.Previous( ) ) ? 0.5f : 0.25f;
            interpolators.y = cell.HasRoadThroughEdge( direction.Next( ) ) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    

    /*
     *    v4  v5  v6
     * _________________
     * |\  |\  |\  |\  |
     * | \ | \ | \ | \ |
     * |__\|__\|__\|__\|
     *     v1  v2  v3
     *    0.0 1.0 0.0
     */
    private void TriangulateRoadSegment( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6 ,Color w1,Color w2,Vector3 indices) {
        AddRoadQuad( v1, v2, v4, v5 );
        AddRoadQuad( v2, v3, v5, v6 );
        AddRoadQuadUV( 0f, 1f, 0f, 0f );
        AddRoadQuadUV( 1f, 0f, 0f, 0f );

        roads.AddQuadCellData( indices,w1,w2 );
        roads.AddQuadCellData( indices,w1,w2 );
    }

    private void AddRoadTriangle( Vector3 v1, Vector3 v2, Vector3 v3 ) {
        roads.AddPerturTriangle( v1, v2, v3 );
    }

    private void AddRoadTriangleUV( Vector2 uv1, Vector2 uv2, Vector2 uv3 ) {
        roads.AddTriangleUV( uv1, uv2, uv3 );
    }

    private void AddRoadQuad( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4 ) {
        roads.AddPerturQuad( v1, v2, v3, v4 );
    }

    private void AddRoadQuadUV( float uMin, float uMax, float vMin, float vMax ) {
        roads.AddQuadUV( uMin, uMax, vMin, vMax );
    }

    #endregion

    #region 处理水

    private void TriangulateWater( HexDirectionEnum direction , HexCell cell , Vector3 center ) {
        center.y = cell.WaterSurfaceHight ;
        HexCell neighbor = cell.GetNeighbor( direction ) ;
        if ( neighbor != null && !neighbor.IsUnderWater ) {
            TriangulateWaterShore( direction,cell,neighbor,center );
        }
        else {
            TriangulateOpenWater( direction,cell,neighbor,center );
        }
           
    }

    private void TriangulateOpenWater( HexDirectionEnum direction , HexCell cell , HexCell neighbor , Vector3 center ) {
        Vector3 c1 = center + HexMetrics.GetWaterFirstCorner( direction ) ;
        Vector3 c2 = center + HexMetrics.GetWaterSecondCorner( direction ) ;
        water.AddPerturTriangle( center , c1 , c2 ) ;

        Vector3 indices ;
        indices.x = indices.y = indices.z = cell.Index ;
        water.AddTriangleCellData( indices , weights1 ) ;

        if ( neighbor != null && DirectionOnRight( direction ) ) {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;
            water.AddPerturQuad( c1 , c2 , e1 , e2 ) ;

            indices.y = neighbor.Index ;
            water.AddQuadCellData( indices , weights1 , weights2 ) ;

            if (direction == HexDirectionEnum.TopRight || direction == HexDirectionEnum.Right) {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderWater) return;
                water.AddPerturTriangle( c2 , e2 , c2 + HexMetrics.GetWaterBridge( direction.Next() ) ) ;

                indices.z = nextNeighbor.Index;
                water.AddTriangleCellData( indices , weights1 , weights2 , weights3 ) ;
            }
        }
    }

    private void TriangulateWaterShore( HexDirectionEnum direction , HexCell cell , HexCell neighbor , Vector3 center ) {
        Vector3 c1 = center + HexMetrics.GetWaterFirstCorner(direction);
        Vector3 c2 = center + HexMetrics.GetWaterSecondCorner(direction);
        EdgeVertices e1 = new EdgeVertices( c1 , c2 ) ;
        water.AddPerturTriangle( center , e1.v1 , e1.v2 ) ;
        water.AddPerturTriangle( center , e1.v2 , e1.v3 ) ;
        water.AddPerturTriangle( center , e1.v3 , e1.v4 ) ;
        water.AddPerturTriangle( center , e1.v4 , e1.v5 ) ;

        Vector3 indices;
        indices.x = indices.z = cell.Index;
        indices.y = neighbor.Index ;

        water.AddTriangleCellData( indices , weights1 ) ;
        water.AddTriangleCellData( indices , weights1 ) ;
        water.AddTriangleCellData( indices , weights1 ) ;
        water.AddTriangleCellData( indices , weights1 ) ;

        Vector3 center2 = neighbor.postion ;
        if (neighbor.ColumnIndex < cell.ColumnIndex - 1) {
            center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        else if (neighbor.ColumnIndex > cell.ColumnIndex + 1) {
            center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        center2.y = center.y ;
        EdgeVertices e2 = new EdgeVertices( center2 + HexMetrics.GetSecondSolidCorner( direction.Opposite() ) ,
                                            center2 + HexMetrics.GetFirstSolidCorner( direction.Opposite() ) ) ;
        

        if ( cell.HasRiverThroughEdge( direction ) ) {
            TriangulateEstuary( e1 , e2 ,cell.HasInComingRiver && cell.InComingRive == direction ,indices) ;
        }
        else {
            waterShore.AddPerturQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddPerturQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddPerturQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddPerturQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);

            waterShore.AddQuadCellData( indices , weights1 , weights2 ) ;
            waterShore.AddQuadCellData( indices , weights1 , weights2 ) ;
            waterShore.AddQuadCellData( indices , weights1 , weights2 ) ;
            waterShore.AddQuadCellData( indices , weights1 , weights2 ) ;
        }
        

        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        if ( nextNeighbor != null ) {

            Vector3 center3 = nextNeighbor.postion ;
            if ( nextNeighbor.ColumnIndex < cell.ColumnIndex - 1 ) {
                center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter ;
            }
            else if ( nextNeighbor.ColumnIndex > cell.ColumnIndex + 1 ) {
                center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter ;
            }

            Vector3 v3 = center3 + ( nextNeighbor.IsUnderWater
                                        ? HexMetrics.GetWaterFirstCorner( direction.Previous() )
                                        : HexMetrics.GetFirstSolidCorner( direction.Previous() )) ;
            v3.y = center.y ;

            waterShore.AddPerturTriangle( e1.v5 , e2.v5 , v3) ;
            waterShore.AddTriangleUV( Vector2.zero , Vector2.up , nextNeighbor.IsUnderWater ? Vector2.zero : Vector2.up ) ;

            indices.z = nextNeighbor.Index ;
            waterShore.AddTriangleCellData( indices , weights1 , weights2 , weights3 ) ;
        }

    }

    //瀑布
    private void TriangulateWaterFallInWater( Vector3 v1 , Vector3 v2 , Vector3 v3 , Vector3 v4 , float y1 , float y2 , float waterY ,Vector3 indices) {
        v1.y = v2.y = y1 ;
        v3.y = v4.y = y2 ;
        v1 = HexMetrics.Perturb( v1 ) ;
        v2 = HexMetrics.Perturb( v2 ) ;
        v3 = HexMetrics.Perturb( v3 ) ;
        v4 = HexMetrics.Perturb( v4 ) ;
        float t = (waterY - y2) / (y1 - y2) ;
        v3 = Vector3.Lerp( v3 , v1 , t ) ;
        v4 = Vector3.Lerp( v4 , v2 , t ) ;
        rivers.AddQuad( v1 , v2 , v3 , v4 ) ;
        rivers.AddQuadUV( 0f , 1f , 0.8f , 1f ) ;

        rivers.AddQuadCellData( indices,weights1,weights2 );
    }

    //河口
    private void TriangulateEstuary( EdgeVertices e1 , EdgeVertices e2, bool incomingRive ,Vector3 indices) {
        waterShore.AddPerturTriangle( e2.v1, e1.v2, e1.v1 );
        waterShore.AddPerturTriangle( e2.v5, e1.v5, e1.v4 );
        waterShore.AddTriangleUV( Vector2.up, Vector2.zero, Vector2.zero );
        waterShore.AddTriangleUV( Vector2.up, Vector2.zero, Vector2.zero );

        waterShore.AddTriangleCellData( indices , weights2 , weights1 , weights1 ) ;
        waterShore.AddTriangleCellData( indices , weights2 , weights1 , weights1 ) ;

        estuaries.AddPerturQuad( e2.v1, e1.v2, e2.v2, e1.v3 );
        estuaries.AddPerturTriangle( e1.v3 , e2.v2 , e2.v4 ) ;
        estuaries.AddPerturQuad( e1.v3, e1.v4, e2.v4, e2.v5 );

        estuaries.AddQuadUV( Vector2.up, Vector2.zero, Vector2.one, Vector2.zero );
        estuaries.AddTriangleUV( Vector2.zero, Vector2.one, Vector2.one );
        estuaries.AddQuadUV( Vector2.zero, Vector2.zero, Vector2.one, Vector2.up );

        estuaries.AddQuadCellData( indices , weights2 , weights1 , weights2 , weights1 ) ;
        estuaries.AddTriangleCellData( indices , weights1 , weights2 , weights2 ) ;
        estuaries.AddQuadCellData( indices , weights1 , weights2 ) ;

        if ( incomingRive ) {
            estuaries.AddQuadUV2( new Vector2( 1.5f, 1f ), new Vector2( 0.7f, 1.15f ), new Vector2( 1f, 0.8f ), new Vector2( 0.5f, 1.1f ) );
            estuaries.AddTriangleUV2( new Vector2(0.5f,1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f) );
            estuaries.AddQuadUV2( new Vector2( 0.5f, 1.1f ), new Vector2( 0.3f, 1.15f ), new Vector2( 0f, 0.8f ), new Vector2( -0.5f, 1f ) );
        }
        else {
            estuaries.AddQuadUV2( new Vector2( -0.5f , -0.2f ) , new Vector2( 0.3f , -0.35f ) , Vector2.zero , new Vector2( 0.5f , -0.3f ) );
            estuaries.AddTriangleUV2( new Vector2( 0.5f , -0.3f ) , Vector2.zero , Vector2.right );
            estuaries.AddQuadUV2( new Vector2( 0.5f , -0.3f ) , new Vector2( 0.7f , -0.35f ) , Vector2.right, new Vector2( 1.5f , -0.2f ) );
        }
    }
    

    #endregion

    

    private bool DirectionOnRight( HexDirectionEnum direction ) {
        return direction == HexDirectionEnum.TopRight ||
               direction == HexDirectionEnum.Right ||
               direction == HexDirectionEnum.BottomRight ;
    }

    public struct TriangulateVertices {
        public Vector3 begin ;
        public Color beginColor ;
        public HexCell beginCell ;
        public Vector3 left ;
        public Color leftColor ;
        public HexCell leftCell ;
        public Vector3 right ;
        public Color rightColor ;
        public HexCell rightCell ;
    }
}
