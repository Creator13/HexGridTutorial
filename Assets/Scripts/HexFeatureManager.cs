using UnityEngine;

public class HexFeatureManager : MonoBehaviour {
    public Transform featurePrefab;

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
        if (hash.a > cell.UrbanLevel * .25f) return;
        
        var instance = Instantiate(featurePrefab, container, false);
        pos.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(pos);
        instance.localRotation = Quaternion.Euler(0, 360f * hash.b, 0);
    }
}
