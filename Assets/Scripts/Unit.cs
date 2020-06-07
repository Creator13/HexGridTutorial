using System.IO;
using UnityEngine;

public class Unit : MonoBehaviour {
    public static Unit unitPrefab;
    
    public HexCell Location {
        get => location;
        set {
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    public float Orientation {
        get => orientation;
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0, value, 0);
        }
    }
    
    private HexCell location;
    private float orientation;

    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    public void Die() {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer) {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid) {
        var coordinates = HexCoordinates.Load(reader);
        var orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }
}
