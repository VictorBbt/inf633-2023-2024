using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Set height to a specified value
/// </summary>
public class SimpleBrush : TerrainBrush {

    public float height = 5;
    public bool circle = true;

    public override void draw(int x, int z) {

        if (circle)
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    float dist = new Vector2(xi, zi).magnitude;
                    if (dist < radius) // circle brush shape
                    {
                        terrain.set(x + xi, z + zi, height);
                    }
                }
            }
        }
        else
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    terrain.set(x + xi, z + zi, height);
                }
            }
        }
    }
}
