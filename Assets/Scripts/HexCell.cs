using System;
using System.Linq;
using UnityEngine;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;
    public RectTransform uiRect;

    public HexGridChunk chunk;
    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    [SerializeField] private bool[] roads = new bool[6];

    [SerializeField] private HexCell[] neighbors = new HexCell[6];

    public bool HasIncomingRiver => hasIncomingRiver;
    public bool HasOutgoingRiver => hasOutgoingRiver;

    public HexDirection IncomingRiver => incomingRiver;
    public HexDirection OutgoingRiver => outgoingRiver;
    public HexDirection RiverBeginOrEndDirection => hasIncomingRiver ? incomingRiver : outgoingRiver;

    public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;
    public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

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

            ValidateRivers();

            for (var i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection) i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    private int waterLevel;

    public int WaterLevel {
        get => waterLevel;
        set {
            if (waterLevel == value) return;

            waterLevel = value;
            ValidateRivers();
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

    private bool walled;

    public bool Walled {
        get => walled;
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }


    #region Features

    private int urbanLevel, farmLevel, plantLevel;

    public int UrbanLevel {
        get => urbanLevel;
        set {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel {
        get => farmLevel;
        set {
            if (farmLevel != value) {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel {
        get => plantLevel;
        set {
            if (plantLevel != value) {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    
    public bool IsSpecial => specialIndex > 0;
    
    private int specialIndex;

    public int SpecialIndex {
        get => specialIndex;
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    #endregion


    public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

    public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

    public Vector3 Position => transform.localPosition;

    public bool HasRoads => roads.Any(b => b);

    public bool IsUnderwater => WaterLevel > Elevation;

    public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

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

    private bool IsValidRiverDestination(HexCell neighbor) {
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
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

        var neighbor = GetNeighbor(dir);
        if (!IsValidRiverDestination(neighbor)) {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == dir) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = dir;
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = dir.Opposite();
        neighbor.specialIndex = 0;

        // Also refreshes both cells
        SetRoad((int) dir, false);
    }

    private void ValidateRivers() {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver))) {
            RemoveOutgoingRiver();
        }

        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this)) {
            RemoveIncomingRiver();
        }
    }

    #endregion


    #region Roads

    public bool HasRoadThroughEdge(HexDirection direction) {
        return roads[(int) direction];
    }

    public void AddRoad(HexDirection dir) {
        if (!roads[(int) dir] && !HasRiverThroughEdge(dir) && !IsSpecial 
            && !GetNeighbor(dir).IsSpecial && GetElevationDifference(dir) <= 1) {
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
