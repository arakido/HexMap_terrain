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

            /*Vector3 bridge = HexMetrics.GetBridge( i ) ;
            Vector3 v3 = v1 + bridge;
            Vector3 v4 = v2 + bridge;

            AddQuad( v1 , v2 , v3 , v4 ) ;

            HexCell prevNeighbor = cell.GetNeighbor( i.Previous() ) ?? cell ;
            HexCell neighbor = cell.GetNeighbor( i ) ?? cell ;
            HexCell nextNeighbor = cell.GetNeighbor( i.Next() ) ?? cell ;

            Color firstColor = (cell.color + prevNeighbor.color + neighbor.color) / 3f ;
            Color secondColor = (cell.color + neighbor.color + nextNeighbor.color) / 3f ;
            Color bridgeColor = (cell.color + neighbor.color) * 0.5f ;

            AddQuadColor( cell.color , bridgeColor) ;

            AddTriangle( v1,center+HexMetrics.GetFirstCorner( i ),v3 );
            AddTriangleColor( cell.color,firstColor,bridgeColor );

            AddTriangle(v2, v4, center + HexMetrics.GetSecondConrner(i));
            AddTriangleColor(cell.color, bridgeColor, secondColor);*/

            //绘制外梯形，为了避免重复绘制，两个三角形只绘制一次
            if ( i == HexDirectionEnum.TopRight || i == HexDirectionEnum.Right || i == HexDirectionEnum.BottomRight ) {
                RightTrangulateConnection( i,cell,v1,v2 );
            }
            else if ( cell.GetNeighbor( i ) == null ) {
                NoNeighborConnection( i, cell, v1, v2 );
            }

        }
        
    }

    private void RightTrangulateConnection( HexDirectionEnum direction , HexCell cell , Vector3 v1 , Vector3 v2 ) {
        HexCell neighbor = cell.GetNeighbor( direction ) ;
        if ( neighbor == null ) {
            NoNeighborConnection( direction, cell, v1, v2 );
            return;
        }
        Vector3 bridge = HexMetrics.GetTwoBridge( direction ) ;
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;

        AddQuad( v1,v2,v3,v4 );
        AddQuadColor( cell.color,neighbor.color );

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ;
        if ( nextNeighbor != null ) {
            AddTriangle( v2,v4,v2 + HexMetrics.GetTwoBridge( direction.Next() ) );
            AddTriangleColor( cell.color, neighbor.color,nextNeighbor.color );
        }
        else {
            //绘制缺失的小三角 
            AddTriangle( v2, v4 , cell.center + HexMetrics.GetSecondConrner( direction ) );
            AddTriangleColor( cell.color, neighbor.color, ( cell.color + neighbor.color ) * 0.5f );
        }
        //绘制缺失的小三角 
        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() );
        if ( prevNeighbor == null ) {
            AddTriangle( v1, cell.center + HexMetrics.GetFirstCorner( direction ), v3 );
            AddTriangleColor( cell.color, ( cell.color + cell.color + neighbor.color ) /3f , neighbor.color);
        }
    }

    //仅处理无临边的情况
    private void NoNeighborConnection( HexDirectionEnum direction, HexCell cell, Vector3 v1, Vector3 v2 )
    {
        Vector3 center = cell.center;

        Vector3 bridge = HexMetrics.GetOneBridge( direction );
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        Vector3 v5 = center + HexMetrics.GetFirstCorner( direction ) ;
        Vector3 v6 = center + HexMetrics.GetSecondConrner( direction ) ;

        HexCell prevNeighbor = cell.GetNeighbor( direction.Previous() ) ;
        HexCell nextNeighbor = cell.GetNeighbor( direction.Next() ) ;


        if ( prevNeighbor ==null && nextNeighbor == null ) {
            AddQuad( v1, v2, v5, v6 );
            AddQuadColor( cell.color, cell.color );
        }
        else if ( prevNeighbor == null ) {
            AddQuad( v1, v2, v5, v4 );
            AddQuadColor( cell.color, cell.color );

            Color secondColor = ( cell.color + cell.color + nextNeighbor.color ) / 3f;
            AddTriangle( v2, v4, v6 );
            AddTriangleColor( cell.color, cell.color, secondColor );
        }
        else if ( nextNeighbor == null ) {
            AddQuad( v1, v2, v3, v6 );
            AddQuadColor( cell.color, cell.color );

            Color firstColor = ( cell.color + cell.color + prevNeighbor.color ) / 3f;
            AddTriangle( v1, v5, v3 );
            AddTriangleColor( cell.color, firstColor, cell.color );
        }
        
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
    private void AddQuadColor( Color c1 , Color c2  ) {
        AddTriangleColor( c1 , c2 , c1 ) ;
        AddTriangleColor( c1 , c2 , c2) ;
    }
}
