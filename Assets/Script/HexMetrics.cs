using UnityEngine;
using System.Collections;

public class HexMetrics {

    public const int chunkSizeX = 5 ;   //六边形集的长
    public const int chunkSizeZ = 5 ;   //宽

    public static float outerToInner = Mathf.Sin( 60 * Mathf.Deg2Rad ) ;
    public static float innerToOuter = 1 / outerToInner ;
    public const float outerRadius = 10f; //六边型同心圆的半径
    //六边形边长
    public static float innerRadius = outerRadius * outerToInner ;

    public const float solidFactor = 0.8f;     //内三角所占的比例
    public const float blendFactor = 1 - solidFactor;   //外梯形的比例
    public const float elevationStep = 3f;  //高度
    public const int terrarcesPerSlope = 2; //台阶数
    public const int terraceSetps = terrarcesPerSlope * 2 + 1;  //连接的段数
    public const float horizontalTerraceStepSize = 1f / terraceSetps;   //水平方向
    public const float verticalTerraceSetSize = 1f / ( terrarcesPerSlope + 1 ); //垂直
    public const int elevationDiffer = 1 ;  //采集点系数

    public static Texture2D noiseSource;    //噪声纹理
    public const float cellPerturbStrength = 4f ;   //噪声干扰强度
    public const float noiseScale = 0.003f ;
    public const float elevationPerturbStrength = 1.5f ;    //y周方向的干扰范围

    public const float streamBedElevationOffset = -1.5f ; //河流河床偏移高度量
    public const float waterElevationOffest =  streamBedElevationOffset / 4f ;    //河水高度

    public const float waterFactor = 0.6f; //水内三角的比例
    public const float waterBlendFactor = 1 - waterFactor; //水内三角的比例

    public const int hashGridSize = 256 ; //散列网格数
    private static HexHash[] hashGrid;//散列网格

    public static Vector3[] corners {
        get {
            if ( _corners == null || _corners.Length <= 0 ) {
                _corners = new Vector3[(int)HexDirectionEnum.Length] ;
                for ( int i = 0 ; i < _corners.Length ; i++ ) {
                    //计算弧度
                    //-60 ：六边形可分成六个等边三角形，等边三角形的角是60°，-60 ：为了使绘制的材质正面朝上
                    //30 : 横向模式下,角度是:0,60,120,180,240,300 纵向模式下角度是: 30,90,150,210,270,330
                    float rad = (-60 * i + 30) * Mathf.Deg2Rad ;
                    _corners[ i ] = new Vector3( outerRadius * Mathf.Cos( rad ) , 0 , outerRadius * Mathf.Sin( rad ) ) ;
                }
            }
            return _corners ;
        }
    }
    private static Vector3[] _corners ;

    /*
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

    //获取边的第一个（v3）坐标点
    public static Vector3 GetFirstCorner( HexDirectionEnum direction) {
        return corners[ (int)direction] ;
    }

    //获取边的内边第一个（v1）坐标点
    public static Vector3 GetFirstSolidCorner( HexDirectionEnum direction) {
        return GetFirstCorner(direction) * solidFactor ;
    }

    //获取边的第二个(v4)坐标点
    public static Vector3 GetSecondCorner( HexDirectionEnum direction) {
        direction += 1 ;
        if (direction >= HexDirectionEnum.Length ) direction -= corners.Length ;
        return GetFirstCorner(direction) ;
    }

    //获取边的内边第二个（v2）坐标点
    public static Vector3 GetSecondSolidCorner( HexDirectionEnum direction) {
        return GetSecondCorner(direction) * solidFactor ;
    }

    //获取一个三角形的外梯形高度
    public static Vector3 GetOneBridge( HexDirectionEnum direction ) {
        return GetTwoBridge( direction ) * 0.5f ;
    }

    //两个梯形高度
    public static Vector3 GetTwoBridge( HexDirectionEnum direction ) {
        return (GetFirstCorner( direction ) + GetSecondCorner( direction )) * blendFactor ;
    }

    public static Vector3 TerraceLerp( Vector3 a, Vector3 b, int step )
    {

        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += ( b.x - a.x ) * h;
        a.z += ( b.z - a.z ) * h;
        float v = Mathf.FloorToInt( ( step + 1 ) / 2f ) * HexMetrics.verticalTerraceSetSize;
        a.y += ( b.y - a.y ) * v;
        return a;
    }

    public static Color TerraceLerp( Color a, Color b, int step )
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp( a, b, h );
    }

    public static HexEdgeType GetEdgeType( int elevation1, int elevation2 ) {
        if ( elevation1 == elevation2 ) return HexEdgeType.Flat;
        if ( Mathf.Abs( elevation1 - elevation2 ) == elevationDiffer) return HexEdgeType.Slope;
        return HexEdgeType.Cliff;
    }

    #region 噪声
    

    public static Vector4 SampleNoise( Vector3 position ) {
        return noiseSource.GetPixelBilinear( position.x * noiseScale, position.z * noiseScale) ;
    }

    public static Vector3 Perturb(Vector3 position) {
        return SampleNoisePerturb(position); ;
    }

    public static Vector3 SampleNoisePerturb( Vector3 position ) {
        Vector4 sample = SampleNoise( position ) ;
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength ;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength ;

        return position ;
    }

    #endregion
    
    #region 水

    public static Vector3 GetWaterFirstCorner(HexDirectionEnum direction) {
        return corners[(int)direction] * waterFactor;
    }

    public static Vector3 GetWaterSecondCorner(HexDirectionEnum direction) {
        direction += 1;
        if (direction >= HexDirectionEnum.Length) direction -= corners.Length;
        return GetFirstCorner(direction) * waterFactor;
    }

    public static Vector3 GetWaterBridge(HexDirectionEnum direction) {
        return (GetFirstCorner(direction) + GetSecondCorner(direction)) * waterBlendFactor;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirectionEnum direction) {
        HexDirectionEnum next = direction + 1;
        if (next >= HexDirectionEnum.Length) next -= HexDirectionEnum.Length;
        return (corners[(int)direction] + corners[(int)next]) * (0.5f * solidFactor);
    }

    #endregion

    #region 城市

    public static void InitTialzeHashGrid() {
        hashGrid = new HexHash[hashGridSize * hashGridSize] ;
        for ( int i = 0 ; i < hashGrid.Length ; i++ ) {
            hashGrid[ i ] = new HexHash() ;
        }
    }

    public static void InitTialzeHashGrid( int seed ) {
        hashGrid = new HexHash[hashGridSize * hashGridSize] ;
        Random.State currentState = Random.state ;
        Random.InitState( seed ) ;
        for ( int i = 0 ; i < hashGrid.Length ; i++ ) {
            hashGrid[ i ] = new HexHash() ;
        }
        Random.state = currentState ;
    }

    public static HexHash SampleHashGrid( Vector3 position ) {
        int x = (int) position.x % hashGridSize ;
        if ( x < 0 ) x += hashGridSize ;
        int z = (int) position.z % hashGridSize ;
        if ( z < 0 ) z += hashGridSize ;
        return hashGrid[ x + z * hashGridSize ] ;
    }

    private static float[][] featureThresholds = {   new[] {0f , 0f , 0.4f} ,
                                                     new[] {0f , 0.4f , 0.6f} ,
                                                     new[] {0.4f , 0.6f , 0.8f}
                                                 } ;

    public static float[] GetFeatureThresholds( int level ) {
        return featureThresholds[ level ] ;
    }

    public static int GetFeatureRandomLevel( int level ,float hash) {
        Random.State currentState = Random.state;
        Random.InitState(GetRandomSeed());
        int rand = Random.Range( 1 , 101 ) ;
        int weight = 0 ;
        for ( int i = 0 ; i < level ; i++ ) {
            weight += Random.Range( 1 , (100 - weight) / (level - i) ) ;
            if ( rand <= weight ) {
                Random.state = currentState;
                if ( hash * 100 < weight ) return i ; 
                return -1 ;
            }
        }
        Random.state = currentState;
        return level -1 ;
    }

    private static int GetRandomSeed() {
        byte[] bytes = new byte[4];
        System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
        rng.GetBytes(bytes);
        return System.BitConverter.ToInt32(bytes, 0);
    }

    #endregion

    #region 围墙

    public const float wallHeight = 4f;
    public const float wallYOffset = -1f ;
    public const float wallTowerThreshold = 0.5f ;
    private const float wallThickness = 0.75f;
    private const float wallElevationOffset = verticalTerraceSetSize ;

    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far) {
        Vector3 offset = Vector3.zero;
        offset.x = far.x - near.x;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * 0.5f);
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far) {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v = near.y < far.y ? wallElevationOffset : (1 - wallElevationOffset) ;
        near.y += (far.y - near.y) * v + wallYOffset ;
        return near ;
    }

    #endregion


}


/// <summary>
/// 六边形方向枚举,
/// 要和六边形绘制的方向保持一致
/// NE, E, SE, SW, W, NW
/// </summary>
public enum HexDirectionEnum {
    Right ,         //E
    BottomRight ,   //SE
    BottomLeft ,    //SW
    Left ,          //W
    TopLeft ,       //NW
    TopRight ,      //NE

    Length ,
}

//桥接类型
public enum HexEdgeType {
    /// <summary>
    /// 平地
    /// </summary>
    Flat,
    /// <summary>
    /// 斜坡
    /// </summary>
    Slope,
    /// <summary>
    /// 绝壁
    /// </summary>
    Cliff,
}

//河流编辑模式
public enum OptionalToggle {
    Ignore,
    Add,
    Remove,
}

public static class HexDirectionExtensions {

    //对面三角
    public static HexDirectionEnum Opposite( this HexDirectionEnum direction ) {
        direction += (int) HexDirectionEnum.Length / 2 ;
        if ( direction >= HexDirectionEnum.Length ) direction -= (int) HexDirectionEnum.Length ;
        return direction ;
    }

    //上一个三角
    public static HexDirectionEnum Previous( this HexDirectionEnum direction ) {
        /*direction -= 1 ;
        if ( direction < 0 ) direction += (int) HexDirectionEnum.Length ;*/
        return direction.Previous( 1 ) ;
    }

    public static HexDirectionEnum Previous(this HexDirectionEnum direction,int num)
    {
        direction -= num;
        if (direction < 0) direction += (int)HexDirectionEnum.Length;
        return direction;
    }

    //下一个三角
    public static HexDirectionEnum Next( this HexDirectionEnum direction ) {
        /*direction += 1 ;
        if ( direction >= HexDirectionEnum.Length ) direction -= (int) HexDirectionEnum.Length ;*/
        return direction.Next( 1 ) ;
    }

    public static HexDirectionEnum Next(this HexDirectionEnum direction,int num)
    {
        direction += num;
        if (direction >= HexDirectionEnum.Length) direction -= (int)HexDirectionEnum.Length;
        return direction;
    }
}