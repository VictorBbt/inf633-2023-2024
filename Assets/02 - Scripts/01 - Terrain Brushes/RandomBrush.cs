using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Generates pseudo random noise on the terrain
/// </summary>
public class RandomBrush : TerrainBrush {

    public float MaxIncreaseHeight = 0;
    public bool circle = true;
    private float CurrentHeight;

    public override void draw(int x, int z)
    {
        System.Random rand = new System.Random();
        if (!circle)
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    float randNum = (float)(rand.NextDouble());
                    CurrentHeight = terrain.get(x+xi,z+ zi);
                    terrain.debug.text = CurrentHeight.ToString();
                    terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + MaxIncreaseHeight*randNum, 0));
                }
            }
        }
        else
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    float dist = new Vector2(xi, zi).magnitude;
                    if (dist < radius) // more coherent to use a circle brush shape
                    {
                        float randNum = (float)(rand.NextDouble());
                        CurrentHeight = terrain.get(x+xi,z+ zi);
                        terrain.debug.text = CurrentHeight.ToString();
                        terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + MaxIncreaseHeight * randNum, 0));
                    }
                }
            }
        }
    }
}
