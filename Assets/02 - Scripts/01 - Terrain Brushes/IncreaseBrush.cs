using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IncreaseBrush : TerrainBrush {

    public float IncreaseHeight = 0;
    public bool circle = true;
    private float CurrentHeight;

    public override void draw(int x, int z)
    {
        if (!circle)
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    CurrentHeight = terrain.get(x+xi,z+ zi);
                    terrain.debug.text = CurrentHeight.ToString();
                    terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + IncreaseHeight, 0));
                }
            }
        } else
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    float dist = new Vector2(xi, zi).magnitude;
                    if (dist < radius) // more coherent to use a circle brush shape
                    {
                        CurrentHeight = terrain.get(x+xi,z+ zi);
                        terrain.debug.text = CurrentHeight.ToString();
                        terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + IncreaseHeight, 0));
                    }
                }
            }
        }
    }
}
