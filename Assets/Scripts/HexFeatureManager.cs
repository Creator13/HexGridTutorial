using UnityEngine;
using UnityEngine.Serialization;

public class HexFeatureManager : MonoBehaviour {
    public HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

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

        var prefab = PickPrefab(urbanCollections, cell.UrbanLevel, hash.a, hash.d);
        
        var otherPrefab = PickPrefab(farmCollections, cell.FarmLevel, hash.b, hash.d);
        var usedHash = hash.a;
        if (prefab) {
            if (otherPrefab && hash.b < hash.a) {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab) {
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(plantCollections, cell.PlantLevel, hash.c, hash.d);
        if (prefab) {
            if (otherPrefab && hash.c < usedHash) {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab) {
            prefab = otherPrefab;
        }
        else return;

        var instance = Instantiate(prefab, container, false);
        pos.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(pos);
        instance.localRotation = Quaternion.Euler(0, 360f * hash.e, 0);
    }

    private Transform PickPrefab(HexFeatureCollection[] collection, int lvl, float hash, float choice) {
        if (lvl > 0) {
            var thresholds = HexMetrics.GetFeatureThresholds(lvl - 1);
            for (var i = 0; i < thresholds.Length; i++) {
                if (hash < thresholds[i]) {
                    return collection[i].Pick(choice);
                }
            }
        }

        return null;
    }
}
