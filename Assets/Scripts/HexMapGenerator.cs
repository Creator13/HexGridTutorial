using UnityEngine;

public class HexMapGenerator : MonoBehaviour {
    public HexGrid grid;

    public void GenerateMap(int x, int z) {
        grid.CreateMap(x, z);
        for (var i = 0; i < z; i++) {
            grid.GetCell(x / 2, i).TerrainTypeIndex = 1;
        }
    }
}
