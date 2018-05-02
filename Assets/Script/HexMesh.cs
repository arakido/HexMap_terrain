using UnityEngine;
using System.Collections;
using System.Collections.Generic ;
using System.Security.Cryptography.X509Certificates ;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

    private Mesh hexMesh ;
    private MeshCollider meshCollider ;
    static List<Vector3> vertices = new List<Vector3>();//顶点数组
    static List<int> triangles = new List<int>();//三角形数组
    static List<Color> colors = new List<Color>(); 

    public struct EdgeVertices {
        public Vector3 v1 ;
        public Vector3 v2 ;
        public Vector3 v3 ;
        public Vector3 v4 ;
        public Vector3 v5 ;

        public EdgeVertices( Vector3 corner1 , Vector3 corner2) {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, 1f / 4);
            v3 = Vector3.Lerp(corner1, corner2, 2f / 4);
            v4 = Vector3.Lerp(corner1, corner2, 3f / 4);
            v5 = corner2;
        }

        public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep) {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, outerStep);
            v3 = Vector3.Lerp(corner1, corner2, 0.5f);
            v4 = Vector3.Lerp(corner1, corner2, 1 - outerStep);
            v5 = corner2;
        }

        public static EdgeVertices Lerp( EdgeVertices e1 , EdgeVertices e2 ,int step) {
            EdgeVertices edge ;
            edge.v1 = HexMetrics.TerraceLerp( e1.v1 , e2.v1 , step ) ;
            edge.v2 = HexMetrics.TerraceLerp( e1.v2 , e2.v2 , step ) ;
            edge.v3 = HexMetrics.TerraceLerp( e1.v3 , e2.v3 , step ) ;
            edge.v4 = HexMetrics.TerraceLerp( e1.v4 , e2.v4 , step ) ;
            edge.v5 = HexMetrics.TerraceLerp( e1.v5 , e2.v5 , step ) ;
            return edge ;
        }
    }

    private void Awake() {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh() ;
        meshCollider = gameObject.AddComponent<MeshCollider>() ;
        hexMesh.name = "Hex Mesh" ;
        /*vertices = new List<Vector3>() ;
        triangles = new List<int>() ;
        colors = new List<Color>() ;*/
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Triangulate( HexCell[] cells ) {
        hexMesh.Clear() ;
        vertices.Clear() ;
        triangles.Clear();
        colors.Clear() ;
        for ( int i = 0 ; i < cells.Length ; i++ ) {
            Triangulate( cells[i] );
        }
        hexMesh.vertices = vertices.ToArray() ;
        hexMesh.triangles = triangles.ToArray() ;
        hexMesh.colors = colors.ToArray() ;
        hexMesh.RecalculateNormals();
        meshCollider.sharedMesh = hexMesh ;
    }
    

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
        Vector3 center = cell.postion ;
        for (HexDirectionEnum i = 0 ; i < HexDirectionEnum.Length ; i++ ) {
            //绘制内三角
            Vector3 v1 = center + HexMetrics.GetFirstSolidCorner( i ) ;
            Vector3 v2 = center + HexMetrics.GetSecondSolidConrner( i ) ;

            EdgeVertices edge = new EdgeVertices( v1 , v2 ) ;

            if ( cell.HasRiver ) {
                if ( cell.HasRiverThroughEdge( i ) ) {
                    edge.v3.y = cell.StreamBedHight ;
                    if ( cell.HasRiverBeginOrEnd ) {
                        TriangulateWithRiverBegubOrEnd( cell , i , center , edge ) ;
                    }
                    else {
                        TriangulateWithRiver(cell, i, center, edge);
                    }
                }
                else {
                    TriangulateAdjacentToRiver( cell , i , center , edge ) ;
                }
            }
            else {
                TriangulateEdgeFan( center , edge , cell.Color ) ;
            }

            //绘制外梯形，为了避免重复绘制，两个三角形只绘制一次
            if ( i == HexDirectionEnum.TopRight || i == HexDirectionEnum.Right || i == HexDirectionEnum.BottomRight ) {
                TrangulateConnection( i,cell, edge);
            }
            else if ( cell.GetNeighbor( i ) == null ) {
                NoNeighborConnection( i , cell , edge) ;
            }
        }
    }

    

    private void TriangulateEdgeFan( Vector3 center , EdgeVertices edge , Color color ) {
        AddPerturTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);

        AddPerturTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);

        AddPerturTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);

        AddPerturTriangle(center, edge.v4, edge.v5);
        AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip( EdgeVertices edge1 , Color c1 , EdgeVertices edge2 , Color c2 ) {
        AddQuad(edge1.v1, edge1.v2, edge2.v1, edge2.v2);
        AddQuadColor(c1, c2);

        AddQuad(edge1.v2, edge1.v3, edge2.v2, edge2.v3);
        AddQuadColor(c1, c2);

        AddQuad(edge1.v3, edge1.v4, edge2.v3, edge2.v4);
        AddQuadColor(c1, c2);

        AddQuad(edge1.v4, edge1.v5, edge2.v4, edge2.v5);
        AddQuadColor(c1, c2);
    }

    private void TrangulateConnection( HexDirectionEnum direction , HexCell cell , EdgeVertices edge) {
        HexCell neighbor = cell.GetNeighbor( direction ) ;
        if ( neighbor == null ) {
            NoNeighborConnection( direction , cell , edge) ;
            return ;
        }

        Vector3 bridge = HexMetrics.GetTwoBridge( direction ) ;
        bridge.y = neighbor.postion.y - cell.postion.y ;

        EdgeVertices edge2 = new EdgeVertices(edge.v1 +bridge, edge.v5 + bridge);

        if ( cell.HasRiverThroughEdge( direction ) ) {
            edge2.v3.y = neighbor.StreamBedHight ;
        }

        if ( cell.GetEdgeType( direction ) == HexEdgeType.Slope ) {
            TriangulateEdgeTerraces(edge , cell.Color, edge2, neighbor.Color) ;
        }
        else {
            TriangulateEdgeStrip( edge , cell.Color , edge2 , neighbor.Color ) ;
        }

        //处理三角形
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        if ( nextNeighbor != null ) {
            //避免重复绘制，只绘制左上和上方的三角
            if ( direction != HexDirectionEnum.BottomRight ) {
                Vector3 v5 = edge.v5 + HexMetrics.GetTwoBridge( direction.Next() ) ;
                v5.y = nextNeighbor.postion.y ;

                if ( cell.Elevation <= neighbor.Elevation ) {
                    if ( cell.Elevation <= nextNeighbor.Elevation ) {
                        TriangulateCorner(edge.v5, cell , edge2.v5, neighbor , v5 , nextNeighbor ) ;
                    }
                    else {
                        TriangulateCorner( v5 , nextNeighbor , edge.v5, cell , edge2.v5, neighbor ) ;
                    }
                }
                else if ( neighbor.Elevation <= nextNeighbor.Elevation ) {
                    TriangulateCorner(edge2.v5, neighbor , v5 , nextNeighbor , edge.v5, cell ) ;
                }
                else {
                    TriangulateCorner( v5 , nextNeighbor , edge.v5, cell , edge2.v5, neighbor ) ;
                }
            }
        }
        else {

            //绘制缺失的小三角 
            HexCell noneCell = new GameObject().AddComponent<HexCell>();
            noneCell.Color = (cell.Color + neighbor.Color) * 0.5f ;

            Vector3 v5 = cell.postion + HexMetrics.GetSecondConrner( direction ) ;
            v5.y = 0 ;
            TriangulateCorner(edge.v5, cell , edge2.v5, neighbor , v5 , noneCell ) ;
            DestroyObject(noneCell.gameObject);
        }

        //绘制缺失的小三角 
        HexCell prevNeighbor = cell.GetNeighbor(direction.Previous());
        if (prevNeighbor == null)
        {
            HexCell noneCell = new GameObject().AddComponent<HexCell>();
            noneCell.Color = (cell.Color + cell.Color + neighbor.Color) / 3f;

            Vector3 v5 = cell.postion + HexMetrics.GetFirstCorner(direction);
            v5.y = 0;
            TriangulateCorner(edge.v1, cell, v5, noneCell, edge2.v1, neighbor);
            DestroyObject(noneCell.gameObject);
        }
    }

    //仅处理无临边的情况
    private void NoNeighborConnection( HexDirectionEnum direction , HexCell cell , EdgeVertices edge) {
        Vector3 center = cell.postion ;

        Vector3 bridge = HexMetrics.GetOneBridge( direction ) ;
        Vector3 v3 = edge.v1 + bridge ;
        Vector3 v4 = edge.v5 + bridge ;
        Vector3 v5 = center + HexMetrics.GetFirstCorner( direction ) ;
        Vector3 v6 = center + HexMetrics.GetSecondConrner( direction ) ;

        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() ) ;
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        v6.y = v5.y = v4.y = v3.y = 0 ;

        EdgeVertices edge2 = new EdgeVertices(v5, v6);


        if ( nextNeighbor == null && prevNeighbor != null) {
            edge2 = new EdgeVertices(v3 , v6);

            HexCell noneCell = new GameObject().AddComponent<HexCell>();
            noneCell.Color = (cell.Color + cell.Color + prevNeighbor.Color) / 3f;

            TriangulateCorner(edge.v1, cell, v5, prevNeighbor, v3, noneCell);
            DestroyObject( noneCell.gameObject );
        }
        if (cell.Elevation == HexMetrics.elevationDiffer) {
            TriangulateEdgeTerraces(edge, cell.Color, edge2, cell.Color);
        }
        else {
            TriangulateEdgeStrip( edge , cell.Color , edge2 , cell.Color ) ;
        }
    }
    

    //绘制阶梯
    private void TriangulateEdgeTerraces( EdgeVertices begin,  Color beginColor, EdgeVertices end, Color endColor ) {

        EdgeVertices e2 = begin ;
        Color c2 = beginColor ;

        for ( int i = 1; i <= HexMetrics.terraceSetps; i++ ) {

            EdgeVertices e1 = e2 ;
            Color c1 = c2;

            e2 = EdgeVertices.Lerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginColor, endColor, i );

            TriangulateEdgeStrip(e1, c1, e2, c2);
        }
    }

    #region 绘制小三角

    //绘制小三角
    private void TriangulateCorner( Vector3 begin , HexCell beginCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {
        HexEdgeType leftEdgeType = beginCell.GetEdgeType( leftCell ) ;
        HexEdgeType rightEdgeType = beginCell.GetEdgeType( rightCell ) ;

        if ( leftEdgeType == HexEdgeType.Slope ) {
            if ( rightEdgeType == HexEdgeType.Slope ) {
                TriangulateCornerTerraces( begin,beginCell.Color, left,leftCell.Color, right,rightCell.Color);
            }
            else if ( rightEdgeType == HexEdgeType.Flat ) {
                TriangulateCornerTerraces(left, leftCell.Color, right, rightCell.Color, begin, beginCell.Color);
            }
            else {
                TriangulateCornerTerracesCliff( begin,beginCell,left,leftCell,right,rightCell );
            }
        }

        else if ( rightEdgeType == HexEdgeType.Slope ) {
            if ( leftEdgeType == HexEdgeType.Flat ) {
                TriangulateCornerTerraces( right,rightCell.Color,begin,beginCell.Color, left,leftCell.Color);
            }
            else {
                TriangulateCornerCliffTerraces(begin, beginCell, left, leftCell, right, rightCell);
            }
        }
        else if(leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope){
            if ( leftCell.Elevation < rightCell.Elevation ) {
                TriangulateCornerCliffTerraces( right , rightCell , begin , beginCell , left , leftCell ) ;
            }
            else {
                TriangulateCornerTerracesCliff( left , leftCell , right , rightCell , begin , beginCell ) ;
            }
        }
        else {
            AddPerturTriangle( begin,left,right );
            AddTriangleColor( beginCell.Color,leftCell.Color,rightCell.Color );
        }
    }

    //绘制斜坡小三角
    private void TriangulateCornerTerraces( Vector3 begin , Color beginColor , Vector3 left , Color leftColor , Vector3 right , Color rightColor ) {

        Vector3 v3 = begin ;
        Vector3 v4 = begin ;
        Color c3 = beginColor ;
        Color c4 = beginColor ;
        
        for ( int i = 1 ; i <= HexMetrics.terraceSetps ; i++ ) {
            Vector3 v1 = v3 ;
            Vector3 v2 = v4 ;
            Color c1 = c3 ;
            Color c2 = c4 ;
            v3 = HexMetrics.TerraceLerp( begin , left , i ) ;
            v4 = HexMetrics.TerraceLerp( begin , right , i ) ;
            c3 = HexMetrics.TerraceLerp( beginColor , leftColor , i ) ;
            c4 = HexMetrics.TerraceLerp( beginColor , rightColor , i ) ;

            if ( v1 == v2 ) {
                AddPerturTriangle(v1, v3, v4);
                AddTriangleColor(c1, c3, c4);
            }
            else {
                AddQuad(v1, v2, v3, v4);
                AddQuadColor(c1, c2, c3, c4);
            }
        }
    }


    //绘制绝壁三角
    private void TriangulateCornerTerracesCliff( Vector3 begin , HexCell beginCell , Vector3 left , HexCell leftCell , Vector3 right , HexCell rightCell ) {

        float b = Mathf.Abs( 1f / (rightCell.Elevation - beginCell.Elevation) ) ;
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b ) ;
        Color boundaryColor = Color.Lerp( beginCell.Color , rightCell.Color , b ) ;

        TriangulateBoundaryTriangle( begin,beginCell,left,leftCell, boundary, boundaryColor);

        if ( leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope ) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else {
            AddTriangle( Perturb( left ) , Perturb( right ) , boundary ) ;
            AddTriangleColor( leftCell.Color,rightCell.Color, boundaryColor);
        }
    }

    
    private void TriangulateCornerCliffTerraces( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell ) {
        float b = Mathf.Abs( 1f / (leftCell.Elevation - beginCell.Elevation) ) ;

        Vector3 boundary = Vector3.Lerp( Perturb( begin ) , Perturb( left ) , b ) ;
        Color boundaryColor = Color.Lerp( beginCell.Color , leftCell.Color , b ) ;

        TriangulateBoundaryTriangle( right, rightCell, begin, beginCell, boundary, boundaryColor );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle( left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else {
            AddTriangle( Perturb( left ) , Perturb( right ) , boundary ) ;
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor ) {

        Vector3 v2 = Perturb(begin);
        Color c2 = beginCell.Color;

        for (int i = 1; i <= HexMetrics.terraceSetps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }
        
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
    private void TriangulateWithRiver(HexCell cell, HexDirectionEnum direction, Vector3 center, EdgeVertices edge) {
        Vector3 centerL ;
        Vector3 centerR ;
        if ( cell.HasRiverThroughEdge( direction.Opposite() ) ) {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 1 / 4f;
            centerR = center + HexMetrics.GetSecondSolidConrner(direction.Next()) * 1 / 4f;
        }
        else if ( cell.HasRiverThroughEdge( direction.Previous() ) ) {
            centerL = Vector3.Lerp( center , edge.v1 , 2/3f ) ;
            centerR = center ;
        }
        else if ( cell.HasRiverThroughEdge( direction.Next() ) ) {
            centerL = center ;
            centerR = Vector3.Lerp( center , edge.v5 , 2/3f ) ;
        }
        else if ( cell.HasRiverThroughEdge( direction.Next( 2 ) ) ) {
            centerL = center ;
            centerR = center + HexMetrics.GetSolidEdgeMiddle( direction.Next() ) * (0.5f * HexMetrics.innerToOuter) ;
        }
        else {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center ;
        }

        center = Vector3.Lerp( centerL , centerR , 0.5f ) ;

        EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, edge.v1, 0.5f), Vector3.Lerp(centerR, edge.v5, 0.5f), 1 / 6f);
        m.v3.y = center.y = edge.v3.y;
        TriangulateEdgeStrip(m, cell.Color, edge, cell.Color);

        AddTriangle(centerL, m.v1, m.v2);
        AddTriangleColor(cell.Color);

        AddQuad(centerL, center, m.v2, m.v3);
        AddQuadColor(cell.Color);

        AddQuad(center, centerR, m.v3, m.v4);
        AddQuadColor(cell.Color);

        AddTriangle(centerR, m.v4, m.v5);
        AddTriangleColor(cell.Color);
    }

    private void TriangulateWithRiverBegubOrEnd(HexCell cell, HexDirectionEnum direction, Vector3 center, EdgeVertices edge) {

        EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, edge.v1, 0.5f), Vector3.Lerp(center, edge.v5, 0.5f));
        m.v3.y = edge.v3.y;
        TriangulateEdgeStrip(m, cell.Color, edge, cell.Color);
        TriangulateEdgeFan( center,m,cell.Color );
    }

    private void TriangulateAdjacentToRiver( HexCell cell , HexDirectionEnum direction , Vector3 center , EdgeVertices edge ) {
        if ( cell.HasRiverThroughEdge( direction.Next() ) ) {
            if ( cell.HasRiverThroughEdge( direction.Previous() ) ) {
                center += HexMetrics.GetSolidEdgeMiddle( direction ) * (HexMetrics.innerToOuter * 0.5f) ;
            }
            else if ( cell.HasRiverThroughEdge( direction.Previous( 2 ) ) ) {
                center += HexMetrics.GetFirstSolidCorner( direction ) * 0.25f ;
            }
        }
        else if ( cell.HasRiverThroughEdge( direction.Previous() ) && cell.HasRiverThroughEdge( direction.Next( 2 ) ) ) {
            center += HexMetrics.GetSecondSolidConrner( direction ) * 0.25f ;
        }
        EdgeVertices m = new EdgeVertices( Vector3.Lerp( center , edge.v1 , 0.5f ) , Vector3.Lerp( center , edge.v5 , 0.5f ) ) ;
        TriangulateEdgeStrip( m , cell.Color , edge , cell.Color ) ;
        TriangulateEdgeFan( center , m , cell.Color ) ;
    }

    #endregion

    //添加内三角的三个标点和绘制顶点顺序？
    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add( v1 ) ;
        vertices.Add( v2 ) ;
        vertices.Add( v3 ) ;
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    private void AddPerturTriangle( Vector3 v1 , Vector3 v2 , Vector3 v3 ) {
        AddTriangle( Perturb( v1 ) , Perturb( v2 ) , Perturb( v3 ) ) ;
    }



    //添加四边形梯形边的。。。
    private void AddQuad( Vector3 v1 , Vector3 v2 , Vector3 v3 , Vector3 v4 ) {

        AddPerturTriangle(v1, v3,v2) ;
        AddPerturTriangle(v2, v3,v4) ;
    }

    private void AddTriangleColor(Color c1) {
        AddTriangleColor( c1 , c1 , c1 ) ;
    }

    //内三角颜色
    private void AddTriangleColor(Color c1, Color c2, Color c3) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    //梯形边颜色
    private void AddQuadColor(Color c1) {
        AddQuadColor(c1, c1);
    }

    private void AddQuadColor( Color c1 , Color c2  ) {
        AddTriangleColor( c1 , c2 , c1 ) ;
        AddTriangleColor( c1 , c2 , c2) ;
    }

    private void AddQuadColor(Color c1, Color c2,Color c3,Color c4)
    {
        AddTriangleColor(c1, c3, c2);
        AddTriangleColor(c2, c3, c4);
    }

    private Vector3 Perturb(Vector3 position) {
        return HexMetrics.SampleNoisePerturb(position); ;
    }
}
