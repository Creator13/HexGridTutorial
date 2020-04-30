﻿using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {
    private enum OptionalToggle { Ignore, Yes, No }

    [SerializeField] private Color[] colors;
    [SerializeField] private HexGrid hexGrid;

    private int activeElevation;
    private int activeWaterLevel;
    private int activeUrbanLevel, activeFarmLevel, activePlantLevel;
    private Color activeColor;
    private int brushSize;
    private OptionalToggle riverMode, roadMode, walledMode;

    private bool applyColor;
    private bool applyElevation;
    private bool applyWaterLevel;
    private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;

    private bool isDrag;
    private HexDirection dragDir;
    private HexCell previousCell;

    private void Awake() {
        SelectColor(0);
    }

    private void Update() {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        }
        else {
            previousCell = null;
        }
    }

    private void HandleInput() {
        var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(inputRay, out var hit)) {
            var currentCell = hexGrid.GetCell(hit.point);

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
            if (applyColor) {
                cell.Color = activeColor;
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

    public void SelectColor(int i) {
        applyColor = i >= 0;
        if (applyColor) {
            activeColor = colors[i];
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

    public void SetBrushSize(float size) {
        brushSize = (int) size;
    }

    public void ShowUI(bool visible) {
        hexGrid.ShowUI(visible);
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
}
