﻿using UnityEngine;
using System.Collections;

public class HexMetrics {
    public const float outerRadius = 10f; //六边型同心圆的半径
    //六边形边长
    public static float innerRadius { get { return outerRadius * Mathf.Sin( 60 * Mathf.Deg2Rad ); } }

    public const float solidFactor = 0.75f;     //内三角所占的比例
    public const float blendFactor = 1 - solidFactor;   //外梯形的比例
    public const float elevationStep = 3f;  //高度
    public const int terrarcesPerSlope = 2; //台阶数
    public const int terraceSetps = terrarcesPerSlope * 2 + 1;  //连接的段数
    public const float horizontalTerraceStepSize = 1f / terraceSetps;   //水平方向
    public const float verticalTerraceSetSize = 1f / ( terrarcesPerSlope + 1 ); //垂直

    private static Vector3[] _corners ;

    public static Vector3[] corners {
        get {
            if ( _corners == null || _corners.Length <= 0 ) {
                _corners = new Vector3[6 /*(int)HexDirection.Length*/] ;
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
    public static Vector3 GetSecondConrner( HexDirectionEnum direction) {
        direction += 1 ;
        if (direction >= HexDirectionEnum.Length ) direction -= corners.Length ;
        return GetFirstCorner(direction) ;
    }

    //获取边的内边第二个（v2）坐标点
    public static Vector3 GetSecondSolidConrner( HexDirectionEnum direction) {
        return GetSecondConrner(direction) * solidFactor ;
    }

    //获取一个三角形的外梯形高度
    public static Vector3 GetOneBridge( HexDirectionEnum direction ) {
        return GetTwoBridge( direction ) * 0.5f ;
    }

    //两个梯形高度
    public static Vector3 GetTwoBridge( HexDirectionEnum direction ) {
        return (GetFirstCorner( direction ) + GetSecondConrner( direction )) * blendFactor ;
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

    public static HexEdgeType GetEdgeType( int elevation1, int elevation2 )
    {
        if ( elevation1 == elevation2 ) return HexEdgeType.Flat;
        if ( Mathf.Abs( elevation1 - elevation2 ) == 1 ) return HexEdgeType.Slope;
        return HexEdgeType.Cliff;
    }

}


/// <summary>
/// 六边形方向枚举,
/// 要和六边形绘制的方向保持一致
/// </summary>
public enum HexDirectionEnum {
    Right ,
    BottomRight ,
    BottomLeft ,
    Left ,
    TopLeft ,
    TopRight ,

    Length ,
}

//桥接类型
public enum HexEdgeType {
    Flat,  //平地
    Slope, //斜坡
    Cliff, //绝壁
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
        direction -= 1 ;
        if ( direction < 0 ) direction += (int) HexDirectionEnum.Length ;
        return direction ;
    }

    //下一个三角
    public static HexDirectionEnum Next( this HexDirectionEnum direction ) {
        direction += 1 ;
        if ( direction >= HexDirectionEnum.Length ) direction -= (int) HexDirectionEnum.Length ;
        return direction ;
    }
}