using UnityEngine;

public struct HexHash {
    public float a, b, c, d, e;

    public static HexHash Create() {
        var hash = new HexHash {
            a = Random.value * .999f,
            b = Random.value * .999f,
            c = Random.value * .999f,
            d = Random.value * .999f,
            e = Random.value * .999f
        };
        return hash;
    }
}
