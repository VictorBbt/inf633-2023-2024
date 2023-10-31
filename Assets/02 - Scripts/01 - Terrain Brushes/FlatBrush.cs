using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlatBrush : TerrainBrush {

    public override void draw(int x, int z) {
        
        terrain.debug.text = isInMap(x,z).ToString();
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                terrain.set(x + xi, z + zi, 0);
            }
        }
    }

    private bool isInMap(int x, int z)
    {
        Vector3 gridSize = terrain.gridSize();
        bool xInRange = (x > 0) && (x < gridSize.x);
        //terrain.debug.text = xInRange.ToString();
        bool zInRange = (z > 0) && (z < gridSize.z);

        return xInRange && zInRange;
    }
}
