﻿using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {
    public int cellCountX = 20, cellCountZ = 15;
    private int chunkCountX, chunkCountZ;

    [SerializeField] private HexCell cellPrefab;
    [SerializeField] private Text cellLabelPrefab;
    [SerializeField] private HexGridChunk chunkPrefab;

    [SerializeField] private Texture2D noiseSource;
    public int seed;

    private HexCell[] cells;
    private HexGridChunk[] chunks;

    private void Awake() {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable() {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

    public bool CreateMap(int x, int z) {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0) {
            Debug.LogError("Unsupported Map Size");
            return false;
        }

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


    #region Saving

    public void Save(BinaryWriter writer) {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        foreach (var t in cells) {
            t.Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header) {
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

        foreach (var t in cells) {
            t.Load(reader);
        }

        foreach (var chunk in chunks) {
            chunk.Refresh();
        }
    }

    #endregion
}
