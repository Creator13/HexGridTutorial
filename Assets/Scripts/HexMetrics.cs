using UnityEngine;

public enum HexEdgeType {
    Flat, Slope, Cliff
}

public static class HexMetrics {
    public const float outerToInner = .866025404f;
    public const float innerToOuter = 1f / outerToInner;
    
    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * outerToInner;

    public const float solidFactor = .8f;
    public const float blendFactor = 1 - solidFactor;

    public const float elevationStep = 3f;

    public const int terracesPerSlope = 2;
    public const int terraceSteps = terracesPerSlope * 2 + 1;

    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    public static Texture2D noiseSource;

    public const float cellPerturbStrength = 4f;
    public const float noiseScale = 0.003f;
    public const float elevationPerturbStrength = 1.5f;

    public const float streamBedElevationOffset = -1.75f;

    public const float waterElevationOffset = -.5f;

    public const int chunkSizeX = 5, chunkSizeZ = 5;

    private static readonly Vector3[] corners = {
        new Vector3(0, 0, outerRadius),
        new Vector3(innerRadius, 0, .5f * outerRadius),
        new Vector3(innerRadius, 0, -.5f * outerRadius),
        new Vector3(0, 0, -outerRadius),
        new Vector3(-innerRadius, 0, -.5f * outerRadius),
        new Vector3(-innerRadius, 0, .5f * outerRadius),
        new Vector3(0, 0, outerRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection dir) {
        return corners[(int) dir];
    }

    public static Vector3 GetSecondCorner(HexDirection dir) {
        return corners[(int) dir + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection dir) {
        return corners[(int) dir] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection dir) {
        return corners[(int) dir + 1] * solidFactor;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection dir) {
        return (corners[(int) dir] + corners[(int) dir + 1]) * (.5f * solidFactor);
    }
    
    public static Vector3 GetBridge(HexDirection dir) {
        return (corners[(int) dir] + corners[(int) dir + 1]) * blendFactor;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step) {
        var h = step * horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        var v = ((step + 1) / 2) * verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;

        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step) {
        var h = step * horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2) {
        if (elevation1 == elevation2) {
            return HexEdgeType.Flat;
        }

        var delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1) {
            return HexEdgeType.Slope;
        }

        return HexEdgeType.Cliff;
    }

    public static Vector4 SampleNoise(Vector3 pos) {
        return noiseSource.GetPixelBilinear(pos.x * noiseScale, pos.z * noiseScale);
    }
    
    public static Vector3 Perturb(Vector3 pos) {
        var sample = SampleNoise(pos);
        pos.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        pos.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return pos;
    }
}
