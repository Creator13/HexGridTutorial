﻿using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class HexMapEditor : MonoBehaviour {
    private enum OptionalToggle { Ignore, Yes, No }

    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private Material terrainMaterial;

    private int activeElevation;
    private int activeWaterLevel;
    private int activeTerrainTypeIndex;
    private int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
    private int brushSize;
    private OptionalToggle riverMode, roadMode, walledMode;

    private bool applyElevation;
    private bool applyWaterLevel;
    private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;

    private bool isDrag;
    private HexDirection dragDir;
    private HexCell previousCell;

    private void Awake() {
        ShowGrid(false);
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        SetEditMode(true);
    }

    private void Update() {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButton(0)) {
                HandleInput();
                return;
            }

            if (Input.GetKeyDown(KeyCode.C)) {
                if (Input.GetKey(KeyCode.LeftShift)) {
                    DestroyUnit();
                }
                else {
                    CreateUnit();
                }

                return;
            }
        }

        previousCell = null;
    }

    public void SetEditMode(bool toggle) {
        enabled = toggle;
    }

    private HexCell GetCellUnderCursor() {
        return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    private void HandleInput() {
        var currentCell = GetCellUnderCursor();

        if (currentCell) {
            if (previousCell && currentCell != previousCell) {
                ValidateDrag(currentCell);
            }
            else {
                isDrag = false;
            }

            EditCells(currentCell);

            previousCell = currentCell;
        }
        else {
            previousCell = null;
        }
    }

    private void ValidateDrag(HexCell currentCell) {
        for (dragDir = HexDirection.NE; dragDir <= HexDirection.NW; dragDir++) {
            if (previousCell.GetNeighbor(dragDir) == currentCell) {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
    }

    private void EditCells(HexCell center) {
        var centerX = center.coordinates.X;
        var centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
            for (var x = centerX - r; x <= centerX + brushSize; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
            for (var x = centerX - brushSize; x <= centerX + r; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell cell) {
        if (cell) {
            if (activeTerrainTypeIndex >= 0) {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }

            if (applyElevation) {
                cell.Elevation = activeElevation;
            }

            if (applyWaterLevel) {
                cell.WaterLevel = activeWaterLevel;
            }

            if (applyUrbanLevel) {
                cell.UrbanLevel = activeUrbanLevel;
            }

            if (applyFarmLevel) {
                cell.FarmLevel = activeFarmLevel;
            }

            if (applyPlantLevel) {
                cell.PlantLevel = activePlantLevel;
            }

            if (applySpecialIndex) {
                cell.SpecialIndex = activeSpecialIndex;
            }

            if (walledMode != OptionalToggle.Ignore) {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }

            if (riverMode == OptionalToggle.No) {
                cell.RemoveRiver();
            }

            if (roadMode == OptionalToggle.No) {
                cell.RemoveRoads();
            }

            if (isDrag) {
                var otherCell = cell.GetNeighbor(dragDir.Opposite());
                if (otherCell) {
                    if (riverMode == OptionalToggle.Yes) {
                        otherCell.SetOutgoingRiver(dragDir);
                    }

                    if (roadMode == OptionalToggle.Yes) {
                        otherCell.AddRoad(dragDir);
                    }
                }
            }
        }
    }

    public void SetElevation(float elevation) {
        activeElevation = (int) elevation;
    }

    public void SetApplyElevation(bool toggle) {
        applyElevation = toggle;
    }

    public void SetApplyWaterLevel(bool toggle) {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level) {
        activeWaterLevel = (int) level;
    }

    public void SetApplyUrbanLevel(bool toggle) {
        applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level) {
        activeUrbanLevel = (int) level;
    }

    public void SetApplyFarmLevel(bool toggle) {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level) {
        activeFarmLevel = (int) level;
    }

    public void SetApplyPlantLevel(bool toggle) {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level) {
        activePlantLevel = (int) level;
    }

    public void SetApplySpecialIndex(bool toggle) {
        applySpecialIndex = toggle;
    }

    public void SetSpecialIndex(float index) {
        activeSpecialIndex = (int) index;
    }

    public void SetTerrainTypeIndex(int index) {
        activeTerrainTypeIndex = index;
    }

    public void SetBrushSize(float size) {
        brushSize = (int) size;
    }

    public void SetRiverMode(int mode) {
        riverMode = (OptionalToggle) mode;
    }

    public void SetRoadMode(int mode) {
        roadMode = (OptionalToggle) mode;
    }

    public void SetWalledMode(int mode) {
        walledMode = (OptionalToggle) mode;
    }

    public void ShowGrid(bool isVisible) {
        if (isVisible) {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    private void CreateUnit() {
        var cell = GetCellUnderCursor();
        if (cell && !cell.Unit) {
            hexGrid.AddUnit(Instantiate(Unit.unitPrefab), cell, Random.Range(0, 360));
        }
    }

    private void DestroyUnit() {
        var cell = GetCellUnderCursor();
        if (cell && cell.Unit) {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }
}
