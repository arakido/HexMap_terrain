using UnityEngine;
using System.Collections;
using System.Collections.Generic ;
using System.Security.Cryptography.X509Certificates ;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

    private Mesh hexMesh ;
    private MeshCollider meshCollider ;
    private List<Vector3> vertices ;//顶点数组
    private List<int> triangles ;//三角形数组
    private List<Color> colors ; 

    public struct EdgeVertices {
        public Vector3 v1 ;
        public Vector3 v2 ;
        public Vector3 v3 ;
        public Vector3 v4 ;

        public EdgeVertices( Vector3 corner1 , Vector3 corner2) {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, 1f / 3);
            v3 = Vector3.Lerp(corner1, corner2, 2f / 3);
            v4 = corner2;
        }

        public static EdgeVertices Lerp( EdgeVertices e1 , EdgeVertices e2 ,int step) {
            EdgeVertices edge ;
            edge.v1 = HexMetrics.TerraceLerp( e1.v1,e2.v1,step );
            edge.v2 = HexMetrics.TerraceLerp(e1.v2, e2.v2, step);
            edge.v3 = HexMetrics.TerraceLerp(e1.v3, e2.v3, step);
            edge.v4 = HexMetrics.TerraceLerp(e1.v4, e2.v4, step);
            return edge ;
        }
    }

    private void Awake() {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh() ;
        meshCollider = gameObject.AddComponent<MeshCollider>() ;
        hexMesh.name = "Hex Mesh" ;
        vertices = new List<Vector3>() ;
        triangles = new List<int>() ;
        colors = new List<Color>() ;
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
            TriangulateEdgeFan( center , edge , cell.color ) ;
            

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
    }

    private void TriangulateEdgeStrip( EdgeVertices edge1 , Color c1 , EdgeVertices edge2 , Color c2 ) {
        AddQuad(edge1.v1, edge1.v2, edge2.v1, edge2.v2);
        AddQuadColor(c1, c2);

        AddQuad(edge1.v2, edge1.v3, edge2.v2, edge2.v3);
        AddQuadColor(c1, c2);

        AddQuad(edge1.v3, edge1.v4, edge2.v3, edge2.v4);
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

        EdgeVertices edge2 = new EdgeVertices(edge.v1 +bridge, edge.v4 + bridge);

        if ( cell.GetEdgeType( direction ) == HexEdgeType.Slope ) {
            TriangulateEdgeTerraces(edge , cell.color, edge2, neighbor.color) ;
        }
        else {
            TriangulateEdgeStrip( edge , cell.color , edge2 , neighbor.color ) ;
        }

        //处理三角形
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        if ( nextNeighbor != null ) {
            //避免重复绘制，只绘制左上和上方的三角
            if ( direction != HexDirectionEnum.BottomRight ) {
                Vector3 v5 = edge.v4 + HexMetrics.GetTwoBridge( direction.Next() ) ;
                v5.y = nextNeighbor.postion.y ;

                if ( cell.Elevation <= neighbor.Elevation ) {
                    if ( cell.Elevation <= nextNeighbor.Elevation ) {
                        TriangulateCorner(edge.v4, cell , edge2.v4, neighbor , v5 , nextNeighbor ) ;
                    }
                    else {
                        TriangulateCorner( v5 , nextNeighbor , edge.v4, cell , edge2.v4, neighbor ) ;
                    }
                }
                else if ( neighbor.Elevation <= nextNeighbor.Elevation ) {
                    TriangulateCorner(edge2.v4, neighbor , v5 , nextNeighbor , edge.v4, cell ) ;
                }
                else {
                    TriangulateCorner( v5 , nextNeighbor , edge.v4, cell , edge2.v4, neighbor ) ;
                }
            }
        }
        else {

            //绘制缺失的小三角 
            HexCell noneCell = new GameObject().AddComponent<HexCell>();
            noneCell.color = (cell.color + neighbor.color) * 0.5f ;
            Vector3 v5 = cell.postion + HexMetrics.GetSecondConrner( direction ) ;
            v5.y = 0 ;
            TriangulateCorner(edge.v4, cell , edge2.v4, neighbor , v5 , noneCell ) ;
            DestroyObject(noneCell.gameObject);
        }

        //绘制缺失的小三角 
        HexCell prevNeighbor = cell.GetNeighbor(direction.Previous());
        if (prevNeighbor == null)
        {
            HexCell noneCell = new GameObject().AddComponent<HexCell>();
            noneCell.color = (cell.color + cell.color + neighbor.color) / 3f;
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
        Vector3 v4 = edge.v4 + bridge ;
        Vector3 v5 = center + HexMetrics.GetFirstCorner( direction ) ;
        Vector3 v6 = center + HexMetrics.GetSecondConrner( direction ) ;

        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() ) ;
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        v6.y = v5.y = v4.y = v3.y = 0 ;

        EdgeVertices edge2 = new EdgeVertices(v5, v6);


        if ( nextNeighbor == null && prevNeighbor != null) {
            edge2 = new EdgeVertices(v3 , v6);

            HexCell noneCell = new GameObject().AddComponent<HexCell>();
            noneCell.color = (cell.color + cell.color + prevNeighbor.color) / 3f;
            TriangulateCorner(edge.v1, cell, v5, prevNeighbor, v3, noneCell);
            DestroyObject( noneCell.gameObject );
        }
        if (cell.Elevation == HexMetrics.elevationDiffer) {
            TriangulateEdgeTerraces(edge, cell.color, edge2, cell.color);
        }
        else {
            TriangulateEdgeStrip( edge , cell.color , edge2 , cell.color ) ;
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
                TriangulateCornerTerraces( begin,beginCell.color, left,leftCell.color, right,rightCell.color);
            }
            else if ( rightEdgeType == HexEdgeType.Flat ) {
                TriangulateCornerTerraces(left, leftCell.color, right, rightCell.color, begin, beginCell.color);
            }
            else {
                TriangulateCornerTerracesCliff( begin,beginCell,left,leftCell,right,rightCell );
            }
        }

        else if ( rightEdgeType == HexEdgeType.Slope ) {
            if ( leftEdgeType == HexEdgeType.Flat ) {
                TriangulateCornerTerraces( right,rightCell.color,begin,beginCell.color, left,leftCell.color);
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
            AddTriangleColor( beginCell.color,leftCell.color,rightCell.color );
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
        Color boundaryColor = Color.Lerp( beginCell.color , rightCell.color , b ) ;

        TriangulateBoundaryTriangle( begin,beginCell,left,leftCell, boundary, boundaryColor);

        if ( leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope ) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else {
            AddTriangle( Perturb( left ) , Perturb( right ) , boundary ) ;
            AddTriangleColor( leftCell.color,rightCell.color, boundaryColor);
        }
    }

    
    private void TriangulateCornerCliffTerraces( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell ) {
        float b = Mathf.Abs( 1f / (leftCell.Elevation - beginCell.Elevation) ) ;

        Vector3 boundary = Vector3.Lerp( Perturb( begin ) , Perturb( left ) , b ) ;
        Color boundaryColor = Color.Lerp( beginCell.color , leftCell.color , b ) ;

        TriangulateBoundaryTriangle( right, rightCell, begin, beginCell, boundary, boundaryColor );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle( left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else {
            AddTriangle( Perturb( left ) , Perturb( right ) , boundary ) ;
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor ) {

        Vector3 v2 = Perturb(begin);
        Color c2 = beginCell.color;

        for (int i = 1; i <= HexMetrics.terraceSetps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }
        
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
        /*int vertexIndex = vertices.Count ;
        vertices.Add( Perturb( v1 ) ) ;
        vertices.Add( Perturb( v2 ) ) ;
        vertices.Add( Perturb( v3 ) ) ;
        triangles.Add( vertexIndex );
        triangles.Add( vertexIndex + 1 );
        triangles.Add( vertexIndex + 2 );*/
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
