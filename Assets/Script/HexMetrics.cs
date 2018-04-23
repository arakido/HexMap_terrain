using UnityEngine;
using System.Collections;

public class HexMetrics {
    public const float outerRadius = 10f ;

    public static float innerRadius {
        get { return outerRadius * Mathf.Sin( 60 * Mathf.Deg2Rad ) ; }
    }

    public const float solidFactor = 0.75f ;
    public const float blendFactor = 1 - solidFactor ;

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

    public static Vector3 GetOneBridge( HexDirectionEnum direction ) {
        return GetTwoBridge( direction ) * 0.5f ;
    }

    public static Vector3 GetTwoBridge( HexDirectionEnum direction ) {
        return (GetFirstCorner( direction ) + GetSecondConrner( direction )) * blendFactor ;
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