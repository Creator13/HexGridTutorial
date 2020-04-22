using System;
using System.Linq;
using UnityEngine;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;
    public RectTransform uiRect;

    public HexGridChunk chunk;

    private int elevation = int.MinValue;
    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    [SerializeField] private bool[] roads = new bool[6];

    [SerializeField] private HexCell[] neighbors = new HexCell[6];

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

            if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) {
                RemoveOutgoingRiver();
            }

            if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation) {
                RemoveIncomingRiver();
            }

            for (var i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection) i) > 1) {
                    SetRoad(i, false);
                }
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

    public bool HasRoads => roads.Any(b => b);

    public HexCell GetNeighbor(HexDirection dir) {
        return neighbors[(int) dir];
    }

    public void SetNeighbor(HexDirection dir, HexCell cell) {
        neighbors[(int) dir] = cell;
        cell.neighbors[(int) dir.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection dir) {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int) dir].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell other) {
        return HexMetrics.GetEdgeType(elevation, other.elevation);
    }

    public int GetElevationDifference(HexDirection dir) {
        var diff = elevation - GetNeighbor(dir).elevation;
        return Math.Abs(diff);
    }


    #region Rivers

    public bool HasRiverThroughEdge(HexDirection dir) {
        return hasIncomingRiver && incomingRiver == dir || hasOutgoingRiver && outgoingRiver == dir;
    }

    public void RemoveOutgoingRiver() {
        if (!hasOutgoingRiver) {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        var neighbour = GetNeighbor(outgoingRiver);
        neighbour.hasIncomingRiver = false;
        neighbour.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver() {
        if (!hasIncomingRiver) {
            return;
        }

        hasIncomingRiver = false;
        RefreshSelfOnly();

        var neighbour = GetNeighbor(incomingRiver);
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

        var neighbour = GetNeighbor(dir);
        if (!neighbour || elevation < neighbour.elevation) {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == dir) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = dir;

        neighbour.RemoveIncomingRiver();
        neighbour.hasIncomingRiver = true;
        neighbour.incomingRiver = dir.Opposite();

        // Also refreshes both cells
        SetRoad((int) dir, false);
    }

    #endregion


    #region Roads

    public bool HasRoadThroughEdge(HexDirection direction) {
        return roads[(int) direction];
    }

    public void AddRoad(HexDirection dir) {
        if (!roads[(int) dir] && !HasRiverThroughEdge(dir) && GetElevationDifference(dir) <= 1) {
            SetRoad((int) dir, true);
        }
    }

    public void RemoveRoads() {
        for (var i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    private void SetRoad(int i, bool state) {
        roads[i] = state;
        // Set state of the corresponding neighbor
        neighbors[i].roads[(int) ((HexDirection) i).Opposite()] = state;
        neighbors[i].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    #endregion


    private void RefreshSelfOnly() {
        chunk.Refresh();
    }

    private void Refresh() {
        if (chunk) {
            chunk.Refresh();
            foreach (var neighbor in neighbors) {
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}
