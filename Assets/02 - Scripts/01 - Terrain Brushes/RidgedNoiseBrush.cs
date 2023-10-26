using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RidgedNoiseBrush : TerrainBrush {

    public float Frequency = 1;
    public float Persistence = 0.5f;
    public int Layers = 1;
    public float exponent; // positive number
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
                    CurrentHeight = terrain.get(x + xi, z + zi);
                    float Sum = 0; // to normalize the sum of Perlin noises
                    float Ratio = Frequency;
                    float newHeight = Ratio * ridgedNoise((x + xi) / Ratio, (z + zi) / Ratio);
                    for (int l = 1; l < Layers; l++)
                    {
                        newHeight += Ratio * ridgedNoise((x + xi) / Ratio, (z + zi) / Ratio)*newHeight;
                        Sum += Ratio;
                        Ratio *= Persistence;
                    }
                    newHeight = Mathf.Pow((newHeight / Sum), exponent);
                    //terrain.debug.text = CurrentHeight.ToString();
                    terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + newHeight, 0));
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
                        CurrentHeight = terrain.get(x + xi, z + zi);
                        float Sum = 0; // to normalize the sum of Perlin noises
                        float Ratio = Frequency;
                        float newHeight = Ratio * ridgedNoise((x + xi) / Ratio, (z + zi) / Ratio);
                        for (int l = 1; l < Layers; l++)
                        {
                            newHeight += Ratio * ridgedNoise((x + xi) / Ratio, (z + zi) / Ratio) * newHeight;
                            Sum += Ratio;
                            Ratio *= Persistence;
                        }
                        newHeight = Mathf.Pow((newHeight / Sum), exponent);
                        //terrain.debug.text = CurrentHeight.ToString();
                        terrain.set(x + xi, z + zi, Mathf.Max(CurrentHeight + newHeight, 0));
                    }
                }
            }
        }
    }

    public float ridgedNoise(float x, float y)
    {
        return 2 * (0.5f - Mathf.Abs(0.5f - Mathf.PerlinNoise(x, y)));
    }
}
