using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {
    private int cellCountX, cellCountZ;
    [SerializeField] public int chunkCountX = 4;
    [SerializeField] public int chunkCountZ = 3;

    [SerializeField] private Color defaultColor = Color.white;

    [SerializeField] private HexCell cellPrefab;
    [SerializeField] private Text cellLabelPrefab;
    [SerializeField] private HexGridChunk chunkPrefab;

    [SerializeField] private Texture2D noiseSource;

    private HexCell[] cells;
    private HexGridChunk[] chunks;

    private void Awake() {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    private void OnEnable() {
        HexMetrics.noiseSource = noiseSource;
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
        cell.Color = defaultColor;

        if (x > 0) {
            cell.SetNeighbour(HexDirection.W, cells[i - 1]);
        }

        if (z > 0) {
            if ((z & 1) == 0) {
                cell.SetNeighbour(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0) {
                    cell.SetNeighbour(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else {
                cell.SetNeighbour(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1) {
                    cell.SetNeighbour(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        var label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

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
}
