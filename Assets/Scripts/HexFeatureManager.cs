using UnityEngine;

public class HexFeatureManager : MonoBehaviour {
    public HexFeatureCollection[] urbanPrefabs;

    private Transform container;

    public void Clear() {
        if (container) {
            Destroy(container.gameObject);
        }

        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply() { }

    public void AddFeature(HexCell cell, Vector3 pos) {
        var hash = HexMetrics.SampleHashGrid(pos);

        var prefab = PickPrefab(cell.UrbanLevel, hash.a, hash.b);
        if (!prefab) return;

        var instance = Instantiate(prefab, container, false);
        pos.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(pos);
        instance.localRotation = Quaternion.Euler(0, 360f * hash.c, 0);
    }

    private Transform PickPrefab(int lvl, float hash, float choice) {
        if (lvl > 0) {
            var thresholds = HexMetrics.GetFeatureThresholds(lvl - 1);
            for (var i = 0; i < thresholds.Length; i++) {
                if (hash < thresholds[i]) {
                    return urbanPrefabs[i].Pick(choice);
                }
            }
        }

        return null;
    }
}
