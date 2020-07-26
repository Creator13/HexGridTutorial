using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class HexMapGenerator : MonoBehaviour {
    private struct MapRegion {
        public int xMin, xMax, zMin, zMax;
    }

    private struct ClimateData {
        public float clouds, moisture;
    }

    private struct Biome {
        public int terrain, plant;

        public Biome(int terrain, int plant) {
            this.terrain = terrain;
            this.plant = plant;
        }
    }

    public enum HemisphereMode { Both, North, South }

    private static readonly float[] temperatureBands = {0.1f, 0.3f, 0.6f};
    private static readonly float[] moistureBands = {.12f, .28f, .85f};

    private static readonly Biome[] biomes = {
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
        new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3),
    };

    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int seed;
    [SerializeField, Range(0, .5f)] private float jitterProbability = .25f;
    [SerializeField, Range(20, 200)] private int chunkSizeMin = 30;
    [SerializeField, Range(20, 200)] private int chunkSizeMax = 100;
    [SerializeField, Range(0, 1)] private float highriseProbability = .25f;
    [SerializeField, Range(0, .4f)] private float sinkProbability = .2f;
    [SerializeField, Range(0, 100)] private int landPercentage = 50;
    [SerializeField, Range(1, 5)] private int waterLevel = 3;
    [SerializeField, Range(-4, 0)] private int elevationMin = -2;
    [SerializeField, Range(6, 10)] private int elevationMax = 8;
    [SerializeField, Range(0, 10)] private int mapBorderX = 5;
    [SerializeField, Range(0, 10)] private int mapBorderZ = 5;
    [SerializeField, Range(0, 10)] private int regionBorder = 5;
    [SerializeField, Range(1, 4)] private int regionCount = 1;
    [SerializeField, Range(0, 100)] private int erosionPercentage = 50;
    [SerializeField, Range(0, 1)] private float startingMoisture = .1f;
    [SerializeField, Range(0, 1)] private float evaporationFactor = .5f;
    [SerializeField, Range(0, 1)] private float precipitationFactor = .25f;
    [SerializeField, Range(0, 1)] private float runoffFactor = .25f;
    [SerializeField, Range(0, 1)] private float seepageFactor = .125f;
    [SerializeField] private HexDirection windDirection = HexDirection.NW;
    [SerializeField, Range(1, 10)] private float windStrength = 4f;
    [SerializeField, Range(0, 20)] private int riverPercentage = 10;
    [SerializeField, Range(0, 1)] private float extraLakeProbability = .25f;
    [SerializeField, Range(0, 1)] private float lowTemperature = 0;
    [SerializeField, Range(0, 1)] private float highTemperature = 1;
    [SerializeField] private HemisphereMode hemisphere;
    [SerializeField, Range(0, 1)] private float temperatureJitter = .1f;

    public HexGrid grid;

    private int cellCount, landCells;
    private PriorityQueue<HexCell> searchFrontier;
    private int searchFrontierPhase;
    private int temperatureJitterChannel;

    private List<MapRegion> regions;
    private List<ClimateData> climate = new List<ClimateData>();
    private List<ClimateData> nextClimate = new List<ClimateData>();
    private List<HexDirection> flowDirections = new List<HexDirection>();

    public void GenerateMap(int x, int z, bool wrapping) {
        var originalRandomState = Random.state;
        if (!useFixedSeed) {
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int) System.DateTime.Now.Ticks;
            seed ^= (int) Time.unscaledTime;
            seed &= int.MaxValue;
        }

        Random.InitState(seed);

        cellCount = x * z;

        grid.CreateMap(x, z, wrapping);

        if (searchFrontier == null) {
            searchFrontier = new PriorityQueue<HexCell>();
        }

        for (var i = 0; i < cellCount; i++) {
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        CreateRegions();
        CreateLand();
        ErodeLand();
        CreateClimate();
        CreateRivers();
        SetTerrainType();

        for (var i = 0; i < cellCount; i++) {
            grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    private void CreateRegions() {
        if (regions == null) {
            regions = new List<MapRegion>();
        }
        else {
            regions.Clear();
        }

        var borderX = grid.wrapping ? regionBorder : mapBorderX;
        MapRegion region;
        switch (regionCount) {
            default:
                if (grid.wrapping) {
                    borderX = 0;
                }
                region.xMin = borderX;
                region.xMax = grid.cellCountX - borderX;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                break;
            case 2:
                if (Random.value < 0.5f) {
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - borderX;
                    regions.Add(region);
                }
                else {
                    if (grid.wrapping) {
                        borderX = 0;
                    }
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX - borderX;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ / 2 - regionBorder;
                    regions.Add(region);
                    region.zMin = grid.cellCountZ / 2 + regionBorder;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                }

                break;
            case 3:
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 3 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = grid.cellCountX / 3 + regionBorder;
                region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                regions.Add(region);
                break;
            case 4:
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ / 2 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX / 2 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                regions.Add(region);
                region.zMin = grid.cellCountZ / 2 + regionBorder;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                regions.Add(region);
                break;
        }
    }


    #region Land creation

    private void CreateLand() {
        var landBudget = Mathf.RoundToInt(cellCount * landPercentage * .01f);
        landCells = landBudget;

        for (var guard = 0; guard < 10000; guard++) {
            var sink = Random.value < sinkProbability;
            for (var i = 0; i < regions.Count; i++) {
                var region = regions[i];
                var chunkSize = Random.Range(chunkSizeMin, chunkSizeMax + 1);
                if (sink) {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0) {
                        return;
                    }
                }
            }
        }

        if (landBudget > 0) {
            Debug.LogWarning($"Failed to used up {landBudget} land budget.");
            landCells -= landBudget;
        }
    }

    private int RaiseTerrain(int chunkSize, int budget, MapRegion region) {
        searchFrontierPhase++;
        var firstCell = GetRandomCell(region);
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

    private int SinkTerrain(int chunkSize, int budget, MapRegion region) {
        searchFrontierPhase++;
        var firstCell = GetRandomCell(region);
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

    #endregion


    #region Erosion

    private void ErodeLand() {
        var erodibleCells = ListPool<HexCell>.Get();
        for (var i = 0; i < cellCount; i++) {
            var cell = grid.GetCell(i);
            if (IsErodible(cell)) {
                erodibleCells.Add(cell);
            }
        }

        var targetErodibleCount = (int) (erodibleCells.Count * (100 - erosionPercentage) * .01f);

        while (erodibleCells.Count > targetErodibleCount) {
            var index = Random.Range(0, erodibleCells.Count);
            var cell = erodibleCells[index];
            var targetCell = GetErosionTarget(cell);

            cell.Elevation -= 1;
            targetCell.Elevation += 1;

            if (!IsErodible(cell)) {
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                var neighbor = cell.GetNeighbor(d);
                if (neighbor && neighbor.Elevation == cell.Elevation + 2 && !erodibleCells.Contains(neighbor)) {
                    erodibleCells.Add(neighbor);
                }
            }

            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell)) {
                erodibleCells.Add(targetCell);
            }

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                var neighbor = targetCell.GetNeighbor(d);
                if (neighbor && neighbor != cell && neighbor.Elevation == targetCell.Elevation + 1 &&
                    !IsErodible(neighbor)) {
                    erodibleCells.Remove(neighbor);
                }
            }
        }

        ListPool<HexCell>.Add(erodibleCells);
    }

    private bool IsErodible(HexCell cell) {
        var erodibleElevation = cell.Elevation - 2;
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
            var neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation) {
                return true;
            }
        }

        return false;
    }

    private HexCell GetErosionTarget(HexCell cell) {
        var candidates = ListPool<HexCell>.Get();
        var erodibleElevation = cell.Elevation - 2;
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
            var neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation) {
                candidates.Add(neighbor);
            }
        }

        var target = candidates[Random.Range(0, candidates.Count)];
        ListPool<HexCell>.Add(candidates);
        return target;
    }

    #endregion


    #region Climate

    private void CreateClimate() {
        climate.Clear();
        nextClimate.Clear();
        var initialData = new ClimateData {moisture = startingMoisture};
        var clearData = new ClimateData();
        for (var i = 0; i < cellCount; i++) {
            climate.Add(initialData);
            nextClimate.Add(clearData);
        }

        for (var cycle = 0; cycle < 40; cycle++) {
            for (var i = 0; i < cellCount; i++) {
                EvolveClimate(i);
            }

            var swap = climate;
            climate = nextClimate;
            nextClimate = swap;
        }
    }

    private void EvolveClimate(int cellIndex) {
        var cell = grid.GetCell(cellIndex);
        var cellClimate = climate[cellIndex];

        if (cell.IsUnderwater) {
            cellClimate.moisture = 1f;
            cellClimate.clouds += evaporationFactor;
        }
        else {
            var evaporation = cellClimate.moisture * evaporationFactor;
            cellClimate.moisture = evaporation;
            cellClimate.clouds += evaporation;
        }

        var precipitation = cellClimate.clouds * precipitationFactor;
        cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;

        var cloudMaximum = 1f - cell.ViewElevation / (elevationMax + 1f);
        if (cellClimate.clouds > cloudMaximum) {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }

        var mainDispersalDirection = windDirection.Opposite();
        var cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
        var runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
        var seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
            var neighbor = cell.GetNeighbor(d);
            if (!neighbor) {
                continue;
            }

            var neighborClimate = nextClimate[neighbor.Index];
            if (d == mainDispersalDirection) {
                neighborClimate.clouds += cloudDispersal * windStrength;
            }
            else {
                neighborClimate.clouds += cloudDispersal;
            }

            var elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
            if (elevationDelta < 0) {
                cellClimate.moisture -= runoff;
                neighborClimate.moisture += runoff;
            }
            else if (elevationDelta == 0) {
                cellClimate.moisture -= seepage;
                neighborClimate.moisture += seepage;
            }

            nextClimate[neighbor.Index] = neighborClimate;
        }

        cellClimate.clouds = 0;

        var nextCellClimate = nextClimate[cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;
        if (nextCellClimate.moisture > 1f) nextCellClimate.moisture = 1f;
        nextClimate[cellIndex] = nextCellClimate;
        climate[cellIndex] = new ClimateData();
    }

    private float DetermineTemperature(HexCell cell) {
        var latitude = (float) cell.coordinates.Z / grid.cellCountZ;

        if (hemisphere == HemisphereMode.Both) {
            latitude *= 2f;
            if (latitude > 1) {
                latitude = 2 - latitude;
            }
        }
        else if (hemisphere == HemisphereMode.North) {
            latitude = 1 - latitude;
        }

        var temp = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);
        temp *= 1f - (cell.ViewElevation - waterLevel) / (elevationMax - waterLevel + 1f);
        var jitter = HexMetrics.SampleNoise(cell.Position * .1f)[temperatureJitterChannel];
        temp += (jitter * 2f - 1f) * temperatureJitter;
        return temp;
    }

    #endregion


    #region Rivers

    private void CreateRivers() {
        var riverOrigins = ListPool<HexCell>.Get();

        for (var i = 0; i < cellCount; i++) {
            var cell = grid.GetCell(i);
            if (cell.IsUnderwater) continue;

            var data = climate[i];
            var weight = data.moisture * (cell.Elevation - waterLevel) / (elevationMax - waterLevel);

            if (weight > .75) {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }

            if (weight > .5) {
                riverOrigins.Add(cell);
            }

            if (weight > .25) {
                riverOrigins.Add(cell);
            }
        }

        var riverBudget = Mathf.RoundToInt(landCells * riverPercentage * .01f);

        while (riverBudget > 0 && riverOrigins.Count > 0) {
            var index = Random.Range(0, riverOrigins.Count);
            var origin = riverOrigins[index];

            var lastIndex = riverOrigins.Count - 1;
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if (!origin.HasRiver) {
                var isValidOrigin = true;
                for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                    var neighbor = origin.GetNeighbor(d);
                    if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater)) {
                        isValidOrigin = false;
                        break;
                    }
                }

                if (isValidOrigin) {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0) {
            Debug.LogWarning("Failed to use up river budget.");
        }

        ListPool<HexCell>.Add(riverOrigins);
    }

    private int CreateRiver(HexCell origin) {
        var length = 1;
        var cell = origin;
        var dir = HexDirection.NE;
        while (!cell.IsUnderwater) {
            var minNeighborElevation = int.MaxValue;
            flowDirections.Clear();
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                var neighbor = cell.GetNeighbor(d);

                if (!neighbor) {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation) {
                    minNeighborElevation = neighbor.Elevation;
                }

                if (neighbor == origin || neighbor.HasIncomingRiver) {
                    continue;
                }

                var delta = neighbor.Elevation - cell.Elevation;
                if (delta > 0) {
                    continue;
                }

                if (neighbor.HasOutgoingRiver) {
                    cell.SetOutgoingRiver(d);
                    return length;
                }

                if (delta < 0) {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }

                if (length == 1 || (d != dir.Next2() && d != dir.Previous2())) {
                    flowDirections.Add(d);
                }

                flowDirections.Add(d);
            }

            if (flowDirections.Count == 0) {
                if (length == 1) {
                    return 0;
                }

                if (minNeighborElevation >= cell.Elevation) {
                    cell.WaterLevel = minNeighborElevation;
                    if (minNeighborElevation == cell.Elevation) {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }

                break;
            }

            dir = flowDirections[Random.Range(0, flowDirections.Count)];
            cell.SetOutgoingRiver(dir);
            length += 1;

            if (minNeighborElevation >= cell.Elevation && Random.value < extraLakeProbability) {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(dir);
        }

        return length;
    }

    #endregion


    private void SetTerrainType() {
        temperatureJitterChannel = Random.Range(0, 4);
        var rockDesertElevation = elevationMax - (elevationMax - waterLevel) / 2;
        
        for (var i = 0; i < cellCount; i++) {
            var cell = grid.GetCell(i);
            var moisture = climate[i].moisture;
            var temperature = DetermineTemperature(cell);
            if (!cell.IsUnderwater) {
                var t = 0;
                for (; t < temperatureBands.Length; t++) {
                    if (temperature < temperatureBands[t]) {
                        break;
                    }
                }

                var m = 0;
                for (; m < moistureBands.Length; m++) {
                    if (moisture < moistureBands[m]) {
                        break;
                    }
                }

                var cellBiome = biomes[t * 4 + m];

                if (cellBiome.terrain == 0) {
                    if (cell.Elevation >= rockDesertElevation) {
                        cellBiome.terrain = 3;
                    }
                }
                else if (cell.Elevation == elevationMax) {
                    cellBiome.terrain = 4;
                }

                if (cellBiome.terrain == 4) {
                    cellBiome.plant = 0;
                }
                else if (cellBiome.plant < 3 && cell.HasRiver) {
                    cellBiome.plant += 1;
                }
                
                cell.TerrainTypeIndex = cellBiome.terrain;
                cell.PlantLevel = cellBiome.plant;
            }
            else {
                int terrain;
                if (cell.Elevation == waterLevel - 1) {
                    int cliffs = 0, slopes = 0;
                    for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                        var neighbor = cell.GetNeighbor(d);
                        if (!neighbor) {
                            continue;
                        }

                        var delta = neighbor.Elevation - cell.WaterLevel;
                        if (delta == 0) {
                            slopes += 1;
                        }
                        else if (delta > 0) {
                            cliffs += 1;
                        }
                    }

                    if (cliffs + slopes > 3) {
                        terrain = 1;
                    }
                    else if (cliffs > 0) {
                        terrain = 3;
                    }
                    else if (slopes > 0) {
                        terrain = 0;
                    }
                    else {
                        terrain = 1;
                    }
                }
                else if (cell.Elevation >= waterLevel) {
                    terrain = 1;
                }
                else if (cell.Elevation < 0) {
                    terrain = 3;
                }
                else {
                    terrain = 2;
                }

                if (terrain == 1 && temperature < temperatureBands[0]) {
                    terrain = 2;
                }
                cell.TerrainTypeIndex = terrain;
            }

            var data = temperature;
            cell.SetMapData(data);
        }
    }

    private HexCell GetRandomCell(MapRegion region) {
        return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMin, region.zMax));
    }
}
