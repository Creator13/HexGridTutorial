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

    private HexCell location, currentTravelLocation;
    private float orientation;

    public int Speed => 24;
    public int VisionRange { get; } = 3;

    public HexGrid Grid { get; set; }

    public HexCell Location {
        get => location;
        set {
            if (location) {
                Grid.DecreaseVisibility(Location, VisionRange);
                location.Unit = null;
            }

            location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(Location, VisionRange);
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
        if (location) {
            Grid.DecreaseVisibility(Location, VisionRange);
        }

        location.Unit = null;
        Destroy(gameObject);
    }

    public bool IsValidDestination(HexCell cell) {
        return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
    }

    public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection dir) {
        var edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff) {
            return -1;
        }

        int moveCost;
        if (fromCell.HasRoadThroughEdge(dir)) {
            moveCost = 1;
        }
        else if (fromCell.Walled != toCell.Walled) {
            return -1;
        }
        else {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }

        return moveCost;
    }

    public void Travel(List<HexCell> path) {
        location.Unit = null;
        location = path[path.Count - 1];
        location.Unit = this;
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    private IEnumerator TravelPath() {
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);
        Grid.DecreaseVisibility(currentTravelLocation ? currentTravelLocation : pathToTravel[0], VisionRange);

        var t = Time.deltaTime * travelSpeed;
        for (var i = 1; i < pathToTravel.Count; i++) {
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + currentTravelLocation.Position) * .5f;
            Grid.IncreaseVisibility(pathToTravel[i], VisionRange);

            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                var d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }

            Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
            t -= 1;
        }

        currentTravelLocation = null;

        a = c;
        b = Location.Position;
        c = b;
        Grid.IncreaseVisibility(location, VisionRange);
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
            if (currentTravelLocation) {
                Grid.IncreaseVisibility(location, VisionRange);
                Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                currentTravelLocation = null;
            }
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
