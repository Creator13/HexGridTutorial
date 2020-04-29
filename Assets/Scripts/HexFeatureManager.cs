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

    public void AddFeature(Vector3 pos) {
        var instance = Instantiate(featurePrefab, container, false);
        pos.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(pos);
    }
}
