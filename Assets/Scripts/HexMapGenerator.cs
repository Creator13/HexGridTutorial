using UnityEngine;

public class HexMapGenerator : MonoBehaviour {
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int seed;
    [SerializeField, Range(0, .5f)] private float jitterProbability = .25f;
    [SerializeField, Range(20, 200)] private int chunkSizeMin = 30;
    [SerializeField, Range(20, 200)] private int chunkSizeMax = 100;
    [SerializeField, Range(0, 1f)] private float highriseProbability = .25f;
    [SerializeField, Range(0, .4f)] private float sinkProbability = .2f;
    [SerializeField, Range(0, 100)] private int landPercentage = 50;
    [SerializeField, Range(1, 5)] private int waterLevel = 3;
    [SerializeField, Range(-4, 0)] private int elevationMin = -2;
    [SerializeField, Range(6, 10)] private int elevationMax = 8;
    [SerializeField, Range(0, 10)] private int mapBorderX = 5;
    [SerializeField, Range(0, 10)] private int mapBorderZ = 5;

    public HexGrid grid;

    private int cellCount;
    private PriorityQueue<HexCell> searchFrontier;
    private int searchFrontierPhase;

    private int xMin, xMax, zMin, zMax;

    public void GenerateMap(int x, int z) {
        var originalRandomState = Random.state;
        if (!useFixedSeed) {
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int) System.DateTime.Now.Ticks;
            seed ^= (int) Time.unscaledTime;
            seed &= int.MaxValue;
        }

        Random.InitState(seed);

        cellCount = x * z;

        grid.CreateMap(x, z);

        if (searchFrontier == null) {
            searchFrontier = new PriorityQueue<HexCell>();
        }

        for (var i = 0; i < cellCount; i++) {
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        xMin = mapBorderX;
        xMax = x - mapBorderX;
        zMin = mapBorderZ;
        zMax = z - mapBorderZ;

        CreateLand();
        SetTerrainType();

        for (var i = 0; i < cellCount; i++) {
            grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    private void CreateLand() {
        var landBudget = Mathf.RoundToInt(cellCount * landPercentage * .01f);

        for (var guard = 0; landBudget > 0 && guard < 10000; guard++) {
            var chunkSize = Random.Range(chunkSizeMin, chunkSizeMax + 1);
            if (Random.value < sinkProbability) {
                landBudget = SinkTerrain(chunkSize, landBudget);
            }
            else {
                landBudget = RaiseTerrain(chunkSize, landBudget);
            }
        }

        if (landBudget > 0) {
            Debug.LogWarning($"Failed to used up {landBudget} land budget.");
        }
    }

    private int RaiseTerrain(int chunkSize, int budget) {
        searchFrontierPhase++;
        var firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        var center = firstCell.coordinates;

        var rise = Random.value < highriseProbability ? 2 : 1;
        var size = 0;
        while (size < chunkSize && searchFrontier.Count > 0) {
            var current = searchFrontier.Dequeue();

            var originalElevation = current.Elevation;
            var newElevation = originalElevation + rise;
            if (newElevation > elevationMax) {
                continue;
            }

            current.Elevation = newElevation;

            if (originalElevation < waterLevel && newElevation >= waterLevel && --budget == 0) {
                break;
            }

            size += 1;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                var neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }

        searchFrontier.Clear();
        return budget;
    }

    private int SinkTerrain(int chunkSize, int budget) {
        searchFrontierPhase++;
        var firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        var center = firstCell.coordinates;

        var sink = Random.value < highriseProbability ? 2 : 1;
        var size = 0;
        while (size < chunkSize && searchFrontier.Count > 0) {
            var current = searchFrontier.Dequeue();

            var originalElevation = current.Elevation;
            var newElevation = current.Elevation - sink;
            if (newElevation < elevationMin) {
                continue;
            }

            current.Elevation = newElevation;

            if (originalElevation >= waterLevel && newElevation < waterLevel) {
                budget += 1;
            }

            size += 1;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                var neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }

        searchFrontier.Clear();
        return budget;
    }

    private void SetTerrainType() {
        for (var i = 0; i < cellCount; i++) {
            var cell = grid.GetCell(i);
            if (!cell.IsUnderwater) {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }

    private HexCell GetRandomCell() {
        return grid.GetCell(Random.Range(xMin, xMax), Random.Range(zMin, zMax));
    }
}
