using UnityEngine;
using System.Collections;

[System.Serializable]
public class HexCoordinates {
    [SerializeField] private int pointX;
    [SerializeField] private int pointY ;
    [SerializeField] private int pointZ;

    public int X { get { return pointX ; } }
    public int Z { get { return pointZ; } }
    /*
     * 六边形坐标使用的是立体坐标
     * 使用 x+y+z=0 切割立方网格对角线平面得到的
     * 参考资料：https://www.indienova.com/indie-game-development/hex-grids-reference/
     * https://www.redblobgames.com/grids/hexagons/
     * 本工程资料：http://gad.qq.com/program/translateview/7173811
     * http://catlikecoding.com/unity/tutorials/hex-map/part-1/
     */
    public int Y { get { return pointY; } }


    public HexCoordinates( int x , int z ) {
        pointX = x ;
        pointZ = z ;
        pointY = -X - Z ;
    }

    public static HexCoordinates FromOffsetCoordinates( int x , int z ) {
        //移位
        return new HexCoordinates( x - z / 2 , z ) ;
    }

    /// <summary>
    /// 点击位置转换成六边形坐标
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static HexCoordinates FromPositon( Vector3 position ) {
        float x = position.x / (HexMetrics.innerRadius * 2f) ;
        float y = -x ;
        float offset = position.z / (HexMetrics.outerRadius * 3f) ;
        x -= offset ;
        y -= offset ;
        int iX = Mathf.RoundToInt( x ) ;
        int iY = Mathf.RoundToInt( y ) ;
        int iZ = Mathf.RoundToInt( -x - y ) ;
        if ( iX + iY + iZ != 0 ) {
            float dX = Mathf.Abs( x - iX ) ;
            float dY = Mathf.Abs( y - iY ) ;
            float dZ = Mathf.Abs( -x - y - iZ ) ;
            if ( dX > dY && dX > dZ ) iX = -iY - iZ ;
            else if ( dZ > dY ) iZ = -iX - iY ;
        }
        return new HexCoordinates( iX,iZ );
    }


    public override string ToString() {
        return string.Format( "({0},{1},{2})" , X ,Y, Z ) ;
    }

    public string ToStringOnSeparateLines() {
        return X + "\n" +Y+"\n"+ Z ;
    }

}
