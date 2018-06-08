using UnityEngine;
using System.Collections;

[System.Serializable]
public class HexCoordinates {
    [SerializeField] private int pointX;
    [SerializeField] private int pointY ;
    [SerializeField] private int pointZ;

    public int X { get { return pointX ; } }
    public int Z { get { return pointZ; } }
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

    public int DistancesTo( HexCoordinates other ) {
        //使用的是立方体坐标，XYZ坐标总和为0，对其取绝对值后XYZ的和等于最大绝对值的2倍
        return (Mathf.Abs( X - other.X ) + Mathf.Abs( Y - other.Y ) + Mathf.Abs( Z - other.Z )) / 2 ;
    }

    public override string ToString() {
        return string.Format( "({0},{1},{2})" , X ,Y, Z ) ;
    }

    public string ToStringOnSeparateLines() {
        return X + "\n" +Y+"\n"+ Z ;
    }

    public void Save( System.IO.BinaryWriter writer ) {
        writer.Write( X );
        writer.Write( Z );
    }

    public static HexCoordinates Load( System.IO.BinaryReader reader ) {
        return new HexCoordinates(reader.ReadInt32(), reader.ReadInt32());
    }

}
