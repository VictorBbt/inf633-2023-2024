using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Smooth or healing brush
/// </summary>
public class SmoothBrush : TerrainBrush {

    public float IncreaseHeight = 0;
    public int SmoothingRadius = 2; 

    private float CurrentHeight;
    public override void draw(int x, int z)
    {
        for (int zi = -radius; zi <= radius; zi++)
        {
            for (int xi = -radius; xi <= radius; xi++)
            {
                float dist = new Vector2(xi, zi).magnitude;
                if (dist < radius)
                {

                    // Compute the mean within a range of SmoothingWindow (sliding window mean)
                    float mean = 0;
                    float cnt = 0;
                    for (int smX = -SmoothingRadius; smX <= SmoothingRadius; smX++)
                    {
                        for (int smZ = -SmoothingRadius; smZ <= SmoothingRadius; smZ++)
                        {
                            float NeighbourHeight = terrain.get(x + xi + smX, z + zi + smZ);
                            cnt += 1;
                            mean += NeighbourHeight;
                        }
                        terrain.set(x + xi, z + zi, mean/cnt);
                    }
                }

            }
        }
    }

}
