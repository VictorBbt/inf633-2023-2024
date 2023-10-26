using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConstantVolumeBrush : TerrainBrush {

    public float IncreaseHeight = 0;
    public float sigma = 0;
    public float ExtrudingDepth = -1f; // how much we want to take off the nearby surface, negative number 

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
                    terrain.set(x + xi, z + zi, CurrentHeight + Mathf.Max(Gaussian2D(IncreaseHeight, Squareddist, sigma), 0));
                }
            }


        }

        // approximation of the displaced volume when increasing the terrain with a Gaussian
        float DisplacedVol = (Mathf.Sqrt(2f * Mathf.PI) * IncreaseHeight * sigma) * (1 - Mathf.Exp(-Mathf.Pow(radius, 2)) / (2*Mathf.Pow(sigma, 2)));
        
        // we want to get this volume off the nearby terrain, so we compute the radius2 where the volume between r1 and r2 is equal to V when considering
        // that we take a constant volume off all around the circle
        int radius2 = 1 + (int)(Mathf.Sqrt((DisplacedVol / Mathf.PI * (-ExtrudingDepth)) + Mathf.Pow(radius, 2)));
        terrain.debug.text = radius2.ToString();

        for (int a = -radius2; a <= radius2; a++)
        {
            for (int b = -radius2; b <= radius2; b++)
            {
                float dist = new Vector2(a, b).magnitude;
                if ((dist > radius) && (dist<radius2))
                {
                    CurrentHeight = terrain.get(x + a, z + b);
                    terrain.set(x + a, z + b, Mathf.Max(0, CurrentHeight + ExtrudingDepth));
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
