using UnityEngine;

public struct HexHash {
    public float a, b, c;

    public static HexHash Create() {
        var hash = new HexHash {
            a = Random.value * .999f, 
            b = Random.value * .999f,
            c = Random.value * .999f
        };
        return hash;
    }
}
