using UnityEngine;

public class HexMapGenerator : MonoBehaviour {
    [SerializeField, Range(0, .5f)] private float jitterProbability = .25f;
    [SerializeField, Range(20,200)] private int chunkSizeMin = 30;
    [SerializeField, Range(20,200)] private int chunkSizeMax = 100;
    [SerializeField, Range(0, 100)] private int landPercentage = 50;
    [SerializeField, Range(1, 5)] private int waterLevel = 3;
    
    public HexGrid grid;

    private int cellCount;
    private PriorityQueue<HexCell> searchFrontier;
    private int searchFrontierPhase;

    public void GenerateMap(int x, int z) {
        cellCount = x * z;

        grid.CreateMap(x, z);

        if (searchFrontier == null) {
            searchFrontier = new PriorityQueue<HexCell>();
        }

        for (var i = 0; i < cellCount; i++) {
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        CreateLand();
        SetTerrainType();

        for (var i = 0; i < cellCount; i++) {
            grid.GetCell(i).SearchPhase = 0;
        }
    }

    private void CreateLand() {
        var landBudget = Mathf.RoundToInt(cellCount * landPercentage * .01f);

        while (landBudget > 0) {
            landBudget = RaiseTerrain(Random.Range(chunkSizeMin, chunkSizeMax + 1), landBudget);
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

        var size = 0;
        while (size < chunkSize && searchFrontier.Count > 0) {
            var current = searchFrontier.Dequeue();
            current.Elevation += 1;
            if (current.Elevation == waterLevel && --budget == 0) {
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

    private void SetTerrainType() {
        for (var i = 0; i < cellCount; i++) {
            var cell = grid.GetCell(i);
            if (!cell.IsUnderwater) {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }

    private HexCell GetRandomCell() {
        return grid.GetCell(Random.Range(0, cellCount));
    }
}
