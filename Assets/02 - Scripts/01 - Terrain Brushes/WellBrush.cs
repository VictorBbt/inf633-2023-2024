using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Digs a well on the terrain
/// </summary>
public class WellBrush : TerrainBrush {

    public float IncreaseHeight = 0;
    public float sigma = 0;

    private float CurrentHeight;
    public override void draw(int x, int z)
    {
        for (int zi = -radius; zi <= radius; zi++)
        {
            for (int xi = -radius; xi <= radius; xi++)
            {
                float dist = new Vector2(xi, zi).magnitude;
                if (dist < radius) // more coherent to use a circle brush shape
                {
                    float Squareddist = Mathf.Pow(xi, 2) + Mathf.Pow(zi, 2);
                    CurrentHeight = terrain.get(x + xi, z + zi);
                    // Basically the symmetry of a Gaussian wrt to the (x, z) plan
                    terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + IncreaseHeight/(Mathf.Sqrt(2 * Mathf.PI) * sigma) - Gaussian2D(IncreaseHeight, Squareddist, sigma), 0));
                }
            }
        }
    }

    private float Gaussian2D(float A, float x,  float sig)
    {
        //xi and zi are already relative coordinates so do not substract with x and z
        return (A / (Mathf.Sqrt(2 * Mathf.PI) * sig))* Mathf.Exp(- ( x / (2 * Mathf.Pow(sig, 2)) ));

    }

}
