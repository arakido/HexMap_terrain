using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexHash {

	public float a { get ; private set ; }
	public float b { get ; private set ; }
	public float c { get ; private set ; }
	public float d { get ; private set ; }
	public float e { get ; private set ; }

    public HexHash() {
        a = Random.value * 0.999f;
        b = Random.value * 0.999f;
        c = Random.value * 0.999f;
        d = Random.value * 0.999f;
        e = Random.value * 0.999f;
    }
}
