using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour {
    [SerializeField] private Text menuLabel, actionButtonLabel;
    [SerializeField] private InputField nameInput;
    
    [SerializeField] private RectTransform listContent;
    [SerializeField] private SaveLoadItem itemPrefab;
    
    public HexGrid hexGrid;
    
    private bool saveMode;
    
    public void Open(bool mode) {
        saveMode = mode;

        if (saveMode) {
            menuLabel.text = "Save map";
            actionButtonLabel.text = "Save";
        }
        else {
            menuLabel.text = "Load map";
            actionButtonLabel.text = "Load";
        }
        
        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close() {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void Action() {
        var path = GetSelectedPath();
        if (path == null) {
            return;
        }

        if (saveMode) {
            Save(path);
        }
        else {
            Load(path);
        }
        
        Close();
    }

    public void Delete() {
        var path = GetSelectedPath();
        if (path == null) return;
        
        if (File.Exists(path)) {
            File.Delete(path);
        }

        // Update UI
        nameInput.text = "";
        FillList();
    }

    public void SelectItem(string name) {
        nameInput.text = name;
    }
    
    private string GetSelectedPath() {
        var mapName = nameInput.text;
        if (mapName.Length == 0) return null;

        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    private void FillList() {
        foreach (RectTransform item in listContent) {
            Destroy(item.gameObject);
        }
        
        var paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        foreach (var map in paths) {
            var item = Instantiate(itemPrefab, listContent, false);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(map);
        }
    }
    
    private void Save(string path) {
        using (var writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
            writer.Write(2);
            hexGrid.Save(writer);
        }
    }

    private void Load(string path) {
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist!");
            return;
        }
        
        using (var reader = new BinaryReader(File.OpenRead(path))) {
            var header = reader.ReadInt32();
            if (header <= 2) {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
}
