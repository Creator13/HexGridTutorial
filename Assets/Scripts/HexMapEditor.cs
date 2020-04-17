using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {
    private enum OptionalToggle {
        Ignore,
        Yes,
        No
    }

    [SerializeField] private Color[] colors;
    [SerializeField] private HexGrid hexGrid;

    private int activeElevation;
    private Color activeColor;
    private int brushSize;
    private OptionalToggle riverMode;

    private bool applyColor;
    private bool applyElevation = true;

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

            if (riverMode == OptionalToggle.No) {
                cell.RemoveRiver();
            }
            else if (isDrag && riverMode == OptionalToggle.Yes) {
                var otherCell = cell.GetNeighbor(dragDir.Opposite());
                if (otherCell) {
                    otherCell.SetOutgoingRiver(dragDir);
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

    public void SetBrushSize(float size) {
        brushSize = (int) size;
    }

    public void ShowUI(bool visible) {
        hexGrid.ShowUI(visible);
    }

    public void SetRiverMode(int mode) {
        riverMode = (OptionalToggle) mode;
    }
}
