using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class HexGrid : MonoBehaviour {
    public int cellCountX = 20, cellCountZ = 15;
    private int chunkCountX, chunkCountZ;

    [SerializeField] private HexCell cellPrefab;
    [SerializeField] private Text cellLabelPrefab;
    [SerializeField] private HexGridChunk chunkPrefab;

    [SerializeField] private Texture2D noiseSource;
    public int seed;
    [SerializeField] private Unit unitPrefab;

    private HexCell[] cells;
    private HexGridChunk[] chunks;
    private HexCellShaderData cellShaderData;

    private PriorityQueue<HexCell> searchFrontier;
    private int searchFrontierPhase;
    private HexCell currentPathFrom, currentPathTo;
    private bool currentPathExists;

    public bool HasPath => currentPathExists;

    private void Awake() {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        Unit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        cellShaderData.Grid = this;
        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable() {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            Unit.unitPrefab = unitPrefab;
            ResetVisibility();
        }
    }

    public bool CreateMap(int x, int z) {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0) {
            Debug.LogError("Unsupported Map Size");
            return false;
        }

        ClearPath();
        ClearUnits();

        // Destroy existing chunks
        if (chunks != null) {
            foreach (var chunk in chunks) {
                Destroy(chunk.gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        return true;
    }

    public HexCell GetCell(Vector3 position) {
        position = transform.InverseTransformPoint(position);
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    public HexCell GetCell(HexCoordinates coords) {
        var z = coords.Z;
        if (z < 0 || z >= cellCountZ) {
            return null;
        }

        var x = coords.X + z / 2;
        if (x < 0 || x >= cellCountX) {
            return null;
        }

        return cells[x + z * cellCountX];
    }

    public HexCell GetCell(Ray ray) {
        if (Physics.Raycast(ray, out var hit)) {
            return GetCell(hit.point);
        }

        return null;
    }

    public HexCell GetCell(int xOffset, int zOffset) {
        return cells[xOffset + zOffset * cellCountX];
    }

    public HexCell GetCell(int cellIndex) {
        return cells[cellIndex];
    }

    public void ShowUI(bool visible) {
        foreach (var chunk in chunks) {
            chunk.ShowUI(visible);
        }
    }

    private void CreateCells() {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++) {
            for (var x = 0; x < cellCountX; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateChunks() {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++) {
            for (var x = 0; x < chunkCountX; x++) {
                var chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCell(int x, int z, int i) {
        Vector3 position;
        // Integer division is wanted here
        position.x = (x + z * .5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        var cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = cellShaderData;

        cell.Explorable = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
        
        if (x > 0) {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }

        if (z > 0) {
            if ((z & 1) == 0) {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0) {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1) {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        var label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell) {
        var chunkX = x / HexMetrics.chunkSizeX;
        var chunkZ = z / HexMetrics.chunkSizeZ;
        var chunk = chunks[chunkX + chunkZ * chunkCountX];

        var localX = x - chunkX * HexMetrics.chunkSizeX;
        var localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }


    #region Units

    private List<Unit> units = new List<Unit>();

    private void ClearUnits() {
        foreach (var unit in units) {
            unit.Die();
        }

        units.Clear();
    }

    public void AddUnit(Unit unit, HexCell location, float orientation) {
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(Unit unit) {
        units.Remove(unit);
        unit.Die();
    }

    #endregion


    #region Pathfinding

    public void FindPath(HexCell fromCell, HexCell toCell, Unit unit) {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        ShowPath(unit.Speed);
        stopwatch.Stop();
        Debug.Log($"Path calculated in {stopwatch.ElapsedMilliseconds}ms");
    }

    public void ClearPath() {
        if (currentPathExists) {
            var current = currentPathTo;
            while (current != currentPathFrom) {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }

            current.DisableHighlight();
            currentPathExists = false;
        }
        else if (currentPathFrom) {
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }

        currentPathFrom = null;
        currentPathTo = null;
    }

    private void ShowPath(int speed) {
        if (currentPathExists) {
            var current = currentPathTo;
            while (current != currentPathFrom) {
                var turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }

        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    public List<HexCell> GetPath() {
        if (!currentPathExists) {
            return null;
        }

        var path = ListPool<HexCell>.Get();
        for (var c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
            path.Add(c);
        }

        path.Add(currentPathFrom);
        path.Reverse();

        return path;
    }

    private bool Search(HexCell fromCell, HexCell toCell, Unit unit) {
        int speed = unit.Speed;
        searchFrontierPhase += 2;

        if (searchFrontier == null) {
            searchFrontier = new PriorityQueue<HexCell>();
        }
        else {
            searchFrontier.Clear();
        }

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0) {
            var current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell) {
                return true;
            }

            var currentTurn = (current.Distance - 1) / speed;

            for (var dir = HexDirection.NE; dir <= HexDirection.NW; dir++) {
                var neighbor = current.GetNeighbor(dir);

                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase) {
                    continue;
                }

                if (neighbor.IsUnderwater || neighbor.Unit) {
                    continue;
                }

                if (!unit.IsValidDestination(neighbor)) {
                    continue;
                }

                var moveCost = unit.GetMoveCost(current, neighbor, dir);
                if (moveCost < 0) {
                    continue;
                }

                var distance = current.Distance + moveCost;
                var turn = (distance - 1) / speed;
                if (turn > currentTurn) {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance) {
                    var oldPriority = neighbor.Priority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return false;
    }

    #endregion


    #region Visibility

    private List<HexCell> GetVisibleCells(HexCell fromCell, int range) {
        var visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;

        if (searchFrontier == null) {
            searchFrontier = new PriorityQueue<HexCell>();
        }
        else {
            searchFrontier.Clear();
        }

        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        var fromCoords = fromCell.coordinates;
        while (searchFrontier.Count > 0) {
            var current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (var dir = HexDirection.NE; dir <= HexDirection.NW; dir++) {
                var neighbor = current.GetNeighbor(dir);

                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase || !neighbor.Explorable) {
                    continue;
                }

                var distance = current.Distance + 1;
                if (distance + neighbor.ViewElevation > range ||
                    distance > fromCoords.DistanceTo(neighbor.coordinates)) {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance) {
                    var oldPriority = neighbor.Priority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return visibleCells;
    }

    public void IncreaseVisibility(HexCell fromCell, int range) {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var cell in cells) {
            cell.IncreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range) {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var cell in cells) {
            cell.DecreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

    public void ResetVisibility() {
        foreach (var cell in cells) {
            cell.ResetVisibility();
        }

        foreach (var unit in units) {
            IncreaseVisibility(unit.Location, unit.VisionRange);
        }
    }

    #endregion


    #region Saving

    public void Save(BinaryWriter writer) {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        foreach (var t in cells) {
            t.Save(writer);
        }

        writer.Write(units.Count);
        foreach (var unit in units) {
            unit.Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header) {
        ClearPath();
        ClearUnits();

        int x = 20, z = 15;
        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (!(x == cellCountX && z == cellCountZ)) {
            if (!CreateMap(x, z)) {
                return;
            }
        }

        var originalImmediateMode = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;

        foreach (var cell in cells) {
            cell.Load(reader, header);
        }

        foreach (var chunk in chunks) {
            chunk.Refresh();
        }

        if (header >= 2) {
            var unitCount = reader.ReadInt32();
            for (var i = 0; i < unitCount; i++) {
                Unit.Load(reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
    }

    #endregion
}
