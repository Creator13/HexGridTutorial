using UnityEngine;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;
    public RectTransform uiRect;

    public HexGridChunk chunk;

    private int elevation = int.MinValue;
    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    public bool HasIncomingRiver => hasIncomingRiver;
    public bool HasOutgoingRiver => hasOutgoingRiver;
    public HexDirection IncomingRiver => incomingRiver;
    public HexDirection OutgoingRiver => outgoingRiver;

    public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;
    public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

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

            if (hasOutgoingRiver && elevation < GetNeighbour(outgoingRiver).elevation) {
                RemoveOutgoingRiver();
            }

            if (hasIncomingRiver && elevation > GetNeighbour(incomingRiver).elevation) {
                RemoveIncomingRiver();
            }
            
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

    public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

    public float RiverSurfaceY => (elevation + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;
    
    public Vector3 Position => transform.localPosition;

    [SerializeField] private HexCell[] neighbours;

    public HexCell GetNeighbour(HexDirection dir) {
        return neighbours[(int) dir];
    }

    public void SetNeighbour(HexDirection dir, HexCell cell) {
        neighbours[(int) dir] = cell;
        cell.neighbours[(int) dir.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection dir) {
        return HexMetrics.GetEdgeType(elevation, neighbours[(int) dir].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell other) {
        return HexMetrics.GetEdgeType(elevation, other.elevation);
    }

    public bool HasRiverThroughEdge(HexDirection dir) {
        return hasIncomingRiver && incomingRiver == dir || hasOutgoingRiver && outgoingRiver == dir;
    }

    public void RemoveOutgoingRiver() {
        if (!hasOutgoingRiver) {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        var neighbour = GetNeighbour(outgoingRiver);
        neighbour.hasIncomingRiver = false;
        neighbour.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver() {
        if (!hasIncomingRiver) {
            return;
        }

        hasIncomingRiver = false;
        RefreshSelfOnly();

        var neighbour = GetNeighbour(incomingRiver);
        neighbour.hasOutgoingRiver = false;
        neighbour.RefreshSelfOnly();
    }

    public void RemoveRiver() {
        RemoveIncomingRiver();
        RemoveOutgoingRiver();
    }

    public void SetOutgoingRiver(HexDirection dir) {
        if (hasOutgoingRiver && outgoingRiver == dir) {
            return;
        }

        var neighbour = GetNeighbour(dir);
        if (!neighbour || elevation < neighbour.elevation) {
            return;
        }
        
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == dir) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = dir;
        RefreshSelfOnly();
        
        neighbour.RemoveIncomingRiver();
        neighbour.hasIncomingRiver = true;
        neighbour.incomingRiver = dir.Opposite();
        neighbour.RefreshSelfOnly();
    }
    
    private void RefreshSelfOnly() {
        chunk.Refresh();
    }
    
    private void Refresh() {
        if (chunk) {
            chunk.Refresh();
            foreach (var neighbor in neighbours) {
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}
