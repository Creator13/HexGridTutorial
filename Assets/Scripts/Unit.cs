using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Unit : MonoBehaviour {
    private const float travelSpeed = 4;
    
    public static Unit unitPrefab;

    private List<HexCell> pathToTravel;

    private HexCell location;
    private float orientation;

    public HexCell Location {
        get => location;
        set {
            if (location) {
                location.Unit = null;
            }

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

    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    public void Die() {
        location.Unit = null;
        Destroy(gameObject);
    }

    public bool IsValidDestination(HexCell cell) {
        return !cell.IsUnderwater && !cell.Unit;
    }

    public void Travel(List<HexCell> path) {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    private IEnumerator TravelPath() {
        for (var i = 1; i < pathToTravel.Count; i++) {
            var a = pathToTravel[i - 1].Position;
            var b = pathToTravel[i].Position;
            for (float t = 0; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Vector3.Lerp(a, b, t);
                yield return null;
            }
        }
    }

    private void OnEnable() {
        if (location) {
            transform.localPosition = location.Position;
        }
    }


    #region Saving

    public void Save(BinaryWriter writer) {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid) {
        var coordinates = HexCoordinates.Load(reader);
        var orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }

    #endregion


    private void OnDrawGizmos() {
        if (pathToTravel == null || pathToTravel.Count == 0) return;

        for (var i = 1; i < pathToTravel.Count; i++) {
            var a = pathToTravel[i - 1].Position;
            var b = pathToTravel[i].Position;
            for (float t = 0; t < 1f; t += .1f) {
                Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 2f);
            }
        }
    }
}
