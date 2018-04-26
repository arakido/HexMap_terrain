using UnityEngine;
using System.Collections;
using System.Collections.Generic ;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

    private Mesh hexMesh ;
    private MeshCollider meshCollider ;
    private List<Vector3> vertices ;//顶点数组
    private List<int> triangles ;//三角形数组
    private List<Color> colors ; 

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
        Vector3 center = cell.center ;
        for (HexDirectionEnum i = 0 ; i < HexDirectionEnum.Length ; i++ ) {
            //绘制内三角
            Vector3 v1 = center + HexMetrics.GetFirstSolidCorner( i ) ;
            Vector3 v2 = center + HexMetrics.GetSecondSolidConrner( i ) ;

            AddTriangle( center , v1 , v2 ) ;
            AddTriangleColor( cell.color, cell.color, cell.color);

            //绘制外梯形，为了避免重复绘制，两个三角形只绘制一次
            if ( i == HexDirectionEnum.TopRight || i == HexDirectionEnum.Right || i == HexDirectionEnum.BottomRight ) {
                RightTrangulateConnection( i,cell,v1,v2 );
            }
            else if ( cell.GetNeighbor( i ) == null ) {
                NoNeighborConnection( i , cell , v1 , v2 ) ;
            }

        }
        
    }

    private void RightTrangulateConnection( HexDirectionEnum direction , HexCell cell , Vector3 v1 , Vector3 v2 ) {
        HexCell neighbor = cell.GetNeighbor( direction ) ;
        if ( neighbor == null ) {
            NoNeighborConnection( direction , cell , v1 , v2 ) ;
            return ;
        }
        Vector3 bridge = HexMetrics.GetTwoBridge( direction ) ;
        Vector3 v3 = v1 + bridge ;
        Vector3 v4 = v2 + bridge ;

        v3.y = v4.y = neighbor.Elevation * HexMetrics.elevationStep ;

        if ( cell.GetEdgeType( direction ) == HexEdgeType.Slope ) {
            TriangulateEdgeTerraces( v1 , v2 , v3 , v4 , cell.color, neighbor.color) ;
        }
        else {
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(cell.color, neighbor.color);
        }

        //处理三角形
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        if ( nextNeighbor != null ) {
            //避免重复绘制，只绘制左上和上方的三角
            if ( direction != HexDirectionEnum.BottomRight ) {
                Vector3 v5 = v2 + HexMetrics.GetTwoBridge( direction.Next() ) ;
                v5.y = nextNeighbor.Elevation * HexMetrics.elevationStep ;
                //TestTriangulateCorner( v2 , cell , v4 , neighbor , v5 , nextNeighbor ) ;

                if ( cell.Elevation <= neighbor.Elevation ) {
                    if ( cell.Elevation <= nextNeighbor.Elevation ) {
                        TriangulateCorner( v2 , cell , v4 , neighbor , v5 , nextNeighbor ) ;
                    }
                    else {
                        TriangulateCorner( v5 , nextNeighbor , v2 , cell , v4 , neighbor ) ;
                    }
                }
                else if ( neighbor.Elevation <= nextNeighbor.Elevation ) {
                    TriangulateCorner( v4 , neighbor , v5 , nextNeighbor , v2 , cell ) ;
                }
                else {
                    TriangulateCorner( v5 , nextNeighbor , v2 , cell , v4 , neighbor ) ;
                }
            }
        }
        else {

            //绘制缺失的小三角 
            HexCell noneCell = new HexCell() ;
            noneCell.color = (cell.color + neighbor.color) * 0.5f ;
            Vector3 v5 = cell.center + HexMetrics.GetSecondConrner( direction ) ;
            v5.y = 0 ;
            TriangulateCorner( v2 , cell , v4 , neighbor , v5 , noneCell ) ;
        }

        //绘制缺失的小三角 
        HexCell prevNeighbor = cell.GetNeighbor(direction.Previous());
        if (prevNeighbor == null)
        {
            HexCell noneCell = new HexCell();
            noneCell.color = (cell.color + cell.color + neighbor.color) / 3f;
            Vector3 v5 = cell.center + HexMetrics.GetFirstCorner(direction);
            v5.y = 0;
            TriangulateCorner(v1, cell, v5, noneCell, v3, neighbor);
        }
    }

    //仅处理无临边的情况
    private void NoNeighborConnection( HexDirectionEnum direction , HexCell cell , Vector3 v1 , Vector3 v2 ) {
        Vector3 center = cell.center ;

        Vector3 bridge = HexMetrics.GetOneBridge( direction ) ;
        Vector3 v3 = v1 + bridge ;
        Vector3 v4 = v2 + bridge ;
        Vector3 v5 = center + HexMetrics.GetFirstCorner( direction ) ;
        Vector3 v6 = center + HexMetrics.GetSecondConrner( direction ) ;

        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() ) ;
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;
        v6.y = v5.y = v4.y = v3.y = 0 ;

        Vector3 v7 = v5 ;
        Vector3 v8 = v6;

        if ( nextNeighbor == null && prevNeighbor != null) {
            v7 = v3 ;
            v8 = v6 ;

            HexCell noneCell = new HexCell();
            noneCell.color = (cell.color + cell.color + prevNeighbor.color) / 3f;
            TriangulateCorner( v1, cell, v5, prevNeighbor, v3, noneCell);
        }
        if (cell.Elevation == HexMetrics.elevationDiffer) {
            TriangulateEdgeTerraces(v1, v2, v7, v8, cell.color, cell.color);
        }
        else {
            AddQuad(v1, v2, v7, v8);
            AddQuadColor(cell.color);
        }
    }

    //绘制阶梯
    private void TriangulateEdgeTerraces( Vector3 beginLeft, Vector3 beginRight, Vector3 endLeft, Vector3 endRight, Color beginColor, Color endColor ) 
    {
        Vector3 v3 = beginLeft ;
        Vector4 v4 = beginRight ;
        Color color = beginColor ;

        for ( int i = 1; i <= HexMetrics.terraceSetps; i++ ) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = color;
            v3 = HexMetrics.TerraceLerp( beginLeft, endLeft, i );
            v4 = HexMetrics.TerraceLerp( beginRight, endRight, i );
            color = HexMetrics.TerraceLerp(beginColor, endColor, i );

            AddQuad( v1, v2, v3, v4 );
            AddQuadColor( c1, color );
        }
    }

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
            AddTriangle( begin,left,right );
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
                AddTriangle(v1, v3, v4);
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

        float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation)) ;
        Vector3 boundary = Vector3.Lerp( begin , right , b ) ;
        Color boundaryColor = Color.Lerp( beginCell.color , rightCell.color , b ) ;

        TriangulateBoundaryTriangle( begin,beginCell,left,leftCell, boundary, boundaryColor);

        if ( leftCell.GetEdgeType( rightCell ) == HexEdgeType.Slope ) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else {
            AddTriangle( left,right,boundary );
            AddTriangleColor( leftCell.color,rightCell.color, boundaryColor);
        }
    }

    
    private void TriangulateCornerCliffTerraces( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell ) {
        float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));

        Vector3 boundary = Vector3.Lerp(begin, left, b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        AddTriangle(begin, v2, boundary);
        AddTriangleColor(beginCell.color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.terraceSetps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        AddTriangle(v2, left, boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }


    //添加内三角的三个标点和绘制顶点顺序？
    private void AddTriangle( Vector3 v1 , Vector3 v2 , Vector3 v3 ) {
        int vertexIndex = vertices.Count ;
        vertices.Add( v1 );
        vertices.Add( v2 );
        vertices.Add( v3 );
        triangles.Add( vertexIndex );
        triangles.Add( vertexIndex + 1 );
        triangles.Add( vertexIndex + 2 );
    }

    //添加四边形梯形边的。。。
    private void AddQuad( Vector3 v1 , Vector3 v2 , Vector3 v3 , Vector3 v4 ) {

        AddTriangle(v1, v3,v2) ;
        AddTriangle(v2, v3,v4) ;
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
}
