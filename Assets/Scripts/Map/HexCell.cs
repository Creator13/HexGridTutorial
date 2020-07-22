using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HexCell : MonoBehaviour, IPriorityQueueItem {
    public HexCoordinates coordinates;
    public RectTransform uiRect;
    public HexGridChunk chunk;

    [SerializeField] private HexCell[] neighbors = new HexCell[6];

    public HexCell PathFrom { get; set; }
    public int SearchHeuristic { get; set; }
    public int SearchPhase { get; set; }
    public int Priority => distance + SearchHeuristic;
    public IPriorityQueueItem NextWithSamePriority { get; set; }

    public Unit Unit { get; set; }

    public HexCellShaderData ShaderData { get; set; }
    public int Index { get; set; }

    private bool explored;
    public bool IsExplored {
        get => explored && Explorable;
        private set => explored = value;
    }

    public bool Explorable { get; set; }

    private int elevation = int.MinValue;
    private int waterLevel;
    private int terrainTypeIndex;
    private bool walled;
    private int distance;

    public int Elevation {
        get => elevation;
        set {
            if (elevation == value) return;

            var originalViewElevation = ViewElevation;
            elevation = value;

            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }

            RefreshPosition();

            ValidateRivers();

            for (var i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection) i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public int WaterLevel {
        get => waterLevel;
        set {
            if (waterLevel == value) return;

            var originalViewElevation = ViewElevation;
            waterLevel = value;

            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }

            ValidateRivers();
            Refresh();
        }
    }

    public int TerrainTypeIndex {
        get => terrainTypeIndex;
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    public bool Walled {
        get => walled;
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }

    public int Distance {
        get => distance;
        set { distance = value; }
    }


    #region Features

    private int urbanLevel, farmLevel, plantLevel, specialIndex;

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

    public bool IsSpecial => specialIndex > 0;

    #endregion


    public Vector3 Position => transform.localPosition;

    public bool IsUnderwater => WaterLevel > Elevation;

    public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

    public int ViewElevation => elevation >= waterLevel ? elevation : waterLevel;

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

    private void RefreshPosition() {
        var pos = transform.localPosition;
        pos.y = elevation * HexMetrics.elevationStep;
        pos.y += (HexMetrics.SampleNoise(pos).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = pos;

        var uiPos = uiRect.localPosition;
        uiPos.z = -pos.y;
        uiRect.localPosition = uiPos;
    }

    private void RefreshSelfOnly() {
        chunk.Refresh();

        if (Unit) {
            Unit.ValidateLocation();
        }
    }

    private void Refresh() {
        if (chunk) {
            chunk.Refresh();
            foreach (var neighbor in neighbors) {
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }

            if (Unit) {
                Unit.ValidateLocation();
            }
        }
    }

    public void SetLabel(string text) {
        var label = uiRect.GetComponent<Text>();
        label.text = text;
    }

    public void EnableHighlight(Color tint) {
        var highlight = uiRect.GetComponentInChildren<Image>();
        highlight.enabled = true;
        highlight.color = tint;
    }

    public void DisableHighlight() {
        var highlight = uiRect.GetComponentInChildren<Image>();
        highlight.enabled = false;
    }

    public void SetMapData(float data) {
        ShaderData.SetMapData(this, data);
    }


    #region Visibility

    private int visibility;

    public bool IsVisible => visibility > 0 && Explorable;

    public void IncreaseVisibility() {
        visibility++;
        if (visibility == 1) {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility() {
        visibility--;
        if (visibility == 0) {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void ResetVisibility() {
        if (visibility > 0) {
            visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }

    #endregion


    #region Rivers

    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    public bool HasIncomingRiver => hasIncomingRiver;
    public bool HasOutgoingRiver => hasOutgoingRiver;

    public HexDirection IncomingRiver => incomingRiver;
    public HexDirection OutgoingRiver => outgoingRiver;
    public HexDirection RiverBeginOrEndDirection => hasIncomingRiver ? incomingRiver : outgoingRiver;

    public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;
    public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

    public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
    public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

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

    [SerializeField] private bool[] roads = new bool[6];

    public bool HasRoads => roads.Any(b => b);

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


    #region Saving

    public void Save(BinaryWriter writer) {
        writer.Write((sbyte) terrainTypeIndex);
        writer.Write((sbyte) elevation);
        writer.Write((byte) waterLevel);
        writer.Write((byte) urbanLevel);
        writer.Write((byte) farmLevel);
        writer.Write((byte) plantLevel);
        writer.Write((byte) specialIndex);

        writer.Write(walled);

        if (hasIncomingRiver) {
            writer.Write((byte) (incomingRiver + 128));
        }
        else {
            writer.Write((byte) 0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte) (outgoingRiver + 128));
        }
        else {
            writer.Write((byte) 0);
        }

        var roadFlags = 0;
        for (var i = 0; i < roads.Length; i++) {
            if (roads[i]) {
                roadFlags |= 1 << i;
            }
        }

        writer.Write((byte) roadFlags);
        writer.Write(IsExplored);
    }

    public void Load(BinaryReader reader, int header) {
        terrainTypeIndex = reader.ReadSByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadSByte();
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();

        walled = reader.ReadBoolean();

        var riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection) (riverData - 128);
        }
        else {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection) (riverData - 128);
        }
        else {
            hasOutgoingRiver = false;
        }

        var roadFlags = reader.ReadByte();
        for (var i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshTerrain(this);
    }

    #endregion
}
