using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Unit : MonoBehaviour {
    private const float travelSpeed = 4;
    private const float rotationSpeed = 180;

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
        transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);

        var t = Time.deltaTime * travelSpeed;
        for (var i = 1; i < pathToTravel.Count; i++) {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * .5f;
            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                var d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }

            t -= 1;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * travelSpeed) {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            var d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }

        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    private IEnumerator LookAt(Vector3 point) {
        point.y = transform.localPosition.y;
        var fromRotation = transform.localRotation;
        var toRotation = Quaternion.LookRotation(point - transform.localPosition);
        var angle = Quaternion.Angle(fromRotation, toRotation);
        var speed = rotationSpeed / angle;

        if (angle > 0) {
            for (var t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
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
}
