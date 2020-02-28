using UnityEngine;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;
    public RectTransform uiRect;

    public HexGridChunk chunk;

    private int elevation = int.MinValue;

    public int Elevation {
        get => elevation;
        set {
            if (elevation == value) return;

            elevation = value;

            var pos = transform.localPosition;
            pos.y = elevation * HexMetrics.elevationStep;
            pos.y += (HexMetrics.SampleNoise(pos).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
            transform.localPosition = pos;

            var uiPos = uiRect.localPosition;
            uiPos.z = -pos.y;
            uiRect.localPosition = uiPos;

            Refresh();
        }
    }

    private Color color;

    public Color Color {
        get => color;
        set {
            if (color == value) return;

            color = value;
            Refresh();
        }
    }

    public Vector3 Position => transform.localPosition;

    [SerializeField] private HexCell[] neighbors;

    public HexCell GetNeighbour(HexDirection dir) {
        return neighbors[(int) dir];
    }

    public void SetNeighbour(HexDirection dir, HexCell cell) {
        neighbors[(int) dir] = cell;
        cell.neighbors[(int) dir.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection dir) {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int) dir].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell other) {
        return HexMetrics.GetEdgeType(elevation, other.elevation);
    }

    private void Refresh() {
        if (chunk) {
            chunk.Refresh();
            for (var i = 0; i < neighbors.Length; i++) {
                var neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}
