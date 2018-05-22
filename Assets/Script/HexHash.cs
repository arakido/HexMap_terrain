using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexHash {

	public float a { get ; private set ; }
	public float b { get ; private set ; }

    public HexHash() {
        a = Random.value ;
        b = Random.value ;
    }
}
