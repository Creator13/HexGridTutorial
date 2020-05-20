using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour {
    public SaveLoadMenu menu;

    private string mapName;

    public string MapName {
        get => mapName;
        set {
            mapName = value;
            transform.GetChild(0).GetComponent<Text>().text = value;
        }
    }

    public void Select() {
        menu.SelectItem(MapName);
    }
}
