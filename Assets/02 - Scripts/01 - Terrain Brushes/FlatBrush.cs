using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Set the terrain's height to zero
/// </summary>
public class FlatBrush : TerrainBrush {

    public override void draw(int x, int z) {
        
        terrain.debug.text = isInMap(x,z).ToString();
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (isInMap(xi, zi))
                {
                    terrain.set(x + xi, z + zi, 0);
                }
            }
        }
    }

    private bool isInMap(int x, int z)
    {
        Vector3 gridSize = terrain.gridSize();
        bool xInRange = (x > 0) && (x < gridSize.x);
        bool zInRange = (z > 0) && (z < gridSize.z);

        return xInRange && zInRange;
    }
}
