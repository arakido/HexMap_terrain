using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier  {

    //二次贝塞尔曲线的公式是： (1 − t)²A + 2(1 − t)tB + t²C
    public static Vector3 GetPoint( Vector3 a , Vector3 b , Vector3 c , float t ) {
        float r = 1f - t ;
        return r * r * a + 2f * r * t * b + t * t * c ;
    }

    public static Vector3 GetDerivative( Vector3 a , Vector3 b , Vector3 c , float t ) {
        return 2f * ((1f - t) * (b - a)) + t * (c - b) ;
    }
}
