using System.IO;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates {
    [SerializeField] private int x, z;
    public int X => x;
    public int Z => z;
    public int Y => -X - Z;

    public HexCoordinates(int x, int z) {
        if (HexMetrics.Wrapping) {
            var oX = x + z / 2;
            if (oX < 0) {
                x += HexMetrics.wrapSize;
            }
            else if (oX >= HexMetrics.wrapSize) {
                x -= HexMetrics.wrapSize;
            }
        }
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z) {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position) {
        var x = position.x / HexMetrics.innerDiameter;
        var y = -x;

        var offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        var iX = Mathf.RoundToInt(x);
        var iY = Mathf.RoundToInt(y);
        var iZ = Mathf.RoundToInt(-x - y);

        // Discard largest rounding error
        if (iX + iY + iZ != 0) {
            var dX = Mathf.Abs(x - iX);
            var dY = Mathf.Abs(y - iY);
            var dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            }
            else if (dZ > dY) {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates other) {
        // return ((X < other.X ? other.X - X : X - other.X) +
        //         (Y < other.Y ? other.Y - Y : Y - other.Y) +
        //         (Z < other.Z ? other.Z - Z : Z - other.Z)) / 2;
        var xy = (x < other.x ? other.x - x : x - other.x) + (Y < other.Y ? other.Y - Y : Y - other.Y);

        if (HexMetrics.Wrapping) {
            other.x += HexMetrics.wrapSize;
            var xyWrapped = (x < other.x ? other.x - x : x - other.x) + (Y < other.Y ? other.Y - Y : Y - other.Y);
            if (xyWrapped < xy) {
                xy = xyWrapped;
            }
            else {
                other.x -= 2 * HexMetrics.wrapSize;
                xyWrapped = (x < other.x ? other.x - x : x - other.x) + (Y < other.Y ? other.Y - Y : Y - other.Y);
                if (xyWrapped < xy) {
                    xy = xyWrapped;
                }
            }
        }
        return (xy + (z < other.z ? other.z - z : z - other.z)) / 2;
    }

    public void Save(BinaryWriter writer) {
        writer.Write(x);
        writer.Write(z);
    }

    public static HexCoordinates Load(BinaryReader reader) {
        return new HexCoordinates {
            x = reader.ReadInt32(),
            z = reader.ReadInt32()
        };
    }

    public override string ToString() {
        return $"({X}, {Y}, {Z})";
    }

    public string ToStringOnSeparateLines() {
        return $"{X}\n{Y}\n{Z}";
    }
}
