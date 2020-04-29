using UnityEngine;

public struct HexHash {
    public float a, b;

    public static HexHash Create() {
        var hash = new HexHash {
            a = Random.value, 
            b = Random.value
        };
        return hash;
    }
}
