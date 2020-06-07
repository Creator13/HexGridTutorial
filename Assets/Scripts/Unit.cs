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
        Vector3 a, b, c = pathToTravel[0].Position;

        var t = Time.deltaTime * travelSpeed;
        for (var i = 1; i < pathToTravel.Count; i++) {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * .5f;
            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                yield return null;
            }

            t -= 1;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * travelSpeed) {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            yield return null;
        }

        transform.localPosition = location.Position;
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

        Vector3 a, b, c = pathToTravel[0].Position;
        
        for (var i = 1; i < pathToTravel.Count; i++) {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * .5f;
            for (float t = 0; t < 1f; t += .1f) {
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }
        }
        
        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (float t = 0; t < 1f; t += .1f) {
            Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
        }
    }
}
