using UnityEngine;

public class HexMapCamera : MonoBehaviour {
    [SerializeField] private float stickMinZoom, stickMaxZoom;
    [SerializeField] private float swivelMinZoom, swivelMaxZoom;
    [SerializeField] private float moveSpeedMinZoom, moveSpeedMaxZoom;
    [SerializeField] private float rotationSpeed;

    [SerializeField] private HexGrid grid;

    private Transform swivel, stick;

    private float zoom = 1f;
    private float rotationAngle;

    private void Awake() {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    private void Update() {
        var zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0) {
            AdjustZoom(zoomDelta);
        }

        var rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0) {
            AdjustRotation(rotationDelta);
        }

        var xDelta = Input.GetAxis("Horizontal");
        var zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0 || zDelta != 0) {
            AdjustPosition(xDelta, zDelta);
        }
    }

    private void AdjustZoom(float delta) {
        zoom = Mathf.Clamp01(zoom + delta);

        var distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0, 0, distance);

        var angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0, 0);
    }

    private void AdjustPosition(float xDelta, float zDelta) {
        var direction = transform.localRotation * new Vector3(xDelta, 0, zDelta).normalized;
        var damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        var distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;

        var position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    private void AdjustRotation(float delta) {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        
        if (rotationAngle < 0) {
            rotationAngle += 360;
        }
        else if (rotationAngle >= 360f) {
            rotationAngle -= 360f;
        }

        transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
    }

    private Vector3 ClampPosition(Vector3 pos) {
        var xMax = (grid.chunkCountX * HexMetrics.chunkSizeX - 0.5f) * (2f * HexMetrics.innerRadius);
        pos.x = Mathf.Clamp(pos.x, 0, xMax);

        var zMax = (grid.chunkCountZ * HexMetrics.chunkSizeZ - 1) * (1.5f * HexMetrics.outerRadius);
        pos.z = Mathf.Clamp(pos.z, 0, zMax);

        return pos;
    }
}
