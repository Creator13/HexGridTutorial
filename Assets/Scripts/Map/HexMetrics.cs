using UnityEngine;

public enum HexEdgeType {
    Flat, Slope, Cliff
}

public static class HexMetrics {
    public const int hashGridSize = 256;
    public const float hashGridScale = .25f;
    private static HexHash[] hashGrid;

    public const int chunkSizeX = 5, chunkSizeZ = 5;

    private static float[][] featureThresholds = {
        new[] {0.0f, 0.0f, 0.4f},
        new[] {0.0f, 0.4f, 0.6f},
        new[] {0.4f, 0.6f, 0.8f}
    };
    
    public const float outerToInner = .866025404f;
    public const float innerToOuter = 1f / outerToInner;
    
    public const float outerRadius = 10;
    public const float innerRadius = outerRadius * outerToInner;

    public const float solidFactor = .8f;
    public const float blendFactor = 1 - solidFactor;
    public const float waterFactor = .6f;
    public const float waterBlendFactor = 1 - waterFactor;

    public const float elevationStep = 3;

    public const int terracesPerSlope = 2;
    public const int terraceSteps = terracesPerSlope * 2 + 1;

    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    public static Texture2D noiseSource;

    public const float cellPerturbStrength = 4;
    public const float noiseScale = 0.003f;
    public const float elevationPerturbStrength = 1.5f;

    public const float streamBedElevationOffset = -1.75f;

    public const float waterElevationOffset = -.5f;

    public const float wallHeight = 4;
    public const float wallThickness = .75f;
    public const float wallElevationOffset = verticalTerraceStepSize;
    public const float wallYOffset = -1;
    public const float wallTowerThreshold = .5f;

    public const float bridgeDesignLength = 7f;

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

    public static Vector3 GetFirstWaterCorner(HexDirection dir) {
        return corners[(int) dir] * waterFactor;
    }
    
    public static Vector3 GetSecondWaterCorner(HexDirection dir) {
        return corners[(int) dir + 1] * waterFactor;
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

    public static Vector3 GetWaterBridge(HexDirection dir) {
        return (corners[(int) dir] + corners[(int) dir + 1]) * waterBlendFactor;
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

    public static void InitializeHashGrid(int seed) {
        hashGrid = new HexHash[hashGridSize * hashGridSize];

        var currentState = Random.state;
        
        Random.InitState(seed);
        for (var i = 0; i < hashGrid.Length; i++) {
            hashGrid[i] = HexHash.Create();
        }

        Random.state = currentState;
    }

    public static HexHash SampleHashGrid(Vector3 pos) {
        var x = (int) (pos.x * hashGridScale) % hashGridSize;
        if (x < 0) x += hashGridSize; 
        var z = (int) (pos.z * hashGridScale) % hashGridSize;
        if (z < 0) z += hashGridSize; 
        return hashGrid[x + z * hashGridSize];
    }

    public static float[] GetFeatureThresholds(int level) {
        return featureThresholds[level];
    }

    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far) {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * .5f);
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far) {
        near.x += (far.x - near.x) * .5f;
        near.z += (far.z - near.z) * .5f;
        var v = near.y < far.y ? wallElevationOffset : 1 - wallElevationOffset;
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }
}
