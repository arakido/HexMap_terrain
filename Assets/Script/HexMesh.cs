using UnityEngine;
using System.Collections;
using System.Collections.Generic ;
using System.Security.Cryptography.X509Certificates ;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

    public bool useCollider ;
    public bool useColors ;
    public bool useUvCoordinates ;

    private Mesh hexMesh ;
    private MeshCollider meshCollider ;
    [System.NonSerialized] List<Vector3> vertices ;//顶点数组
    [System.NonSerialized] List<int> triangles;//三角形数组
    [System.NonSerialized] List<Color> colors ;
    [System.NonSerialized] List<Vector2> uvs;   //uv

    private void Awake() {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh() ;
        if ( useCollider ) meshCollider = gameObject.AddComponent<MeshCollider>() ;
        hexMesh.name = "Hex Mesh" ;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Clear() {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get() ;
        triangles = ListPool<int>.Get() ;
        if ( useColors ) colors = ListPool<Color>.Get() ;
        if ( useUvCoordinates ) uvs = ListPool<Vector2>.Get() ;
    }

    public void Apply() {
        hexMesh.SetVertices( vertices ) ;
        ListPool<Vector3>.Add( vertices );
        hexMesh.SetTriangles( triangles , 0 ) ;
        ListPool<int>.Add( triangles );
        if(useColors) {
            hexMesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }
        if ( useUvCoordinates ) {
            hexMesh.SetUVs( 0,uvs );
            ListPool<Vector2>.Add( uvs );
        }
        hexMesh.RecalculateNormals();
        if ( useCollider ) meshCollider.sharedMesh = hexMesh ;
    }



    //添加内三角的三个标点和绘制顶点顺序？
    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add( v1 ) ;
        vertices.Add( v2 ) ;
        vertices.Add( v3 ) ;
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddPerturTriangle( Vector3 v1 , Vector3 v2 , Vector3 v3 ) {
        AddTriangle( HexMetrics.Perturb( v1 ) , HexMetrics.Perturb( v2 ) , HexMetrics.Perturb( v3 ) ) ;
    }



    //添加四边形梯形边的。。。
    public void AddQuad( Vector3 v1 , Vector3 v2 , Vector3 v3 , Vector3 v4 ) {
        int vertexIndex = vertices.Count ;
        vertices.Add( HexMetrics.Perturb( v1 ) ) ;
        vertices.Add( HexMetrics.Perturb( v2 ) ) ;
        vertices.Add( HexMetrics.Perturb( v3 ) ) ;
        vertices.Add( HexMetrics.Perturb( v4 ) ) ;
        triangles.Add( vertexIndex ) ;
        triangles.Add( vertexIndex + 2 ) ;
        triangles.Add( vertexIndex + 1 ) ;
        triangles.Add( vertexIndex + 1 ) ;
        triangles.Add( vertexIndex + 2 ) ;
        triangles.Add( vertexIndex + 3 ) ;
    }

    public void AddTriangleColor(Color c1) {
        AddTriangleColor( c1 , c1 , c1 ) ;
    }

    //内三角颜色
    public void AddTriangleColor(Color c1, Color c2, Color c3) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    //梯形边颜色
    public void AddQuadColor(Color c1) {
        AddQuadColor(c1, c1);
    }

    public void AddQuadColor( Color c1 , Color c2  ) {
        AddQuadColor( c1  , c1 , c2, c2) ;
    }

    public void AddQuadColor( Color c1 , Color c2 , Color c3 , Color c4 ) {
        colors.Add( c1 ) ;
        colors.Add( c2 ) ;
        colors.Add( c3 ) ;
        colors.Add( c4 ) ;
    }

    public void AddTriangleUV(Vector2 uv) {
        AddTriangleUV( uv , uv , uv ) ;
    }

    public void AddTriangleUV( Vector2 uv1 , Vector2 uv2 , Vector2 uv3 ) {
        uvs.Add( uv1 );
        uvs.Add( uv2 );
        uvs.Add( uv3 );
        if (useUvCoordinates) Debug.Log("222");
    }

    public void AddQuadUV( Vector2 uv ) {
        AddQuadUV( uv , uv , uv , uv ) ;
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
        if (useUvCoordinates) Debug.Log("222");
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax) {
        AddQuadUV( new Vector2( uMin , vMin ) ,
                   new Vector2( uMax , vMin ) ,
                   new Vector2( uMin , vMax ) ,
                   new Vector2( uMax , vMax ) ) ;
    }
}


public struct EdgeVertices
{
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;
    public Vector3 v4;
    public Vector3 v5;

    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 1f / 4);
        v3 = Vector3.Lerp(corner1, corner2, 2f / 4);
        v4 = Vector3.Lerp(corner1, corner2, 3f / 4);
        v5 = corner2;
    }

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        v5 = corner2;
    }

    public static EdgeVertices Lerp(EdgeVertices e1, EdgeVertices e2, int step)
    {
        EdgeVertices edge;
        edge.v1 = HexMetrics.TerraceLerp(e1.v1, e2.v1, step);
        edge.v2 = HexMetrics.TerraceLerp(e1.v2, e2.v2, step);
        edge.v3 = HexMetrics.TerraceLerp(e1.v3, e2.v3, step);
        edge.v4 = HexMetrics.TerraceLerp(e1.v4, e2.v4, step);
        edge.v5 = HexMetrics.TerraceLerp(e1.v5, e2.v5, step);
        return edge;
    }
}