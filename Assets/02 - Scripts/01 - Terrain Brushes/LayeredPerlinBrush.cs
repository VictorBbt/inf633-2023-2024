using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Allows to generate Perlin noise with a specified number of octaves, specified scale/lacunarity/persistency/amplitude
/// This affects only the terrain locally (within the brush radius)
/// </summary>
public class LayeredPerlinBrush : TerrainBrush
{
    [Header("Perlin Noise Parameters")]
    public float InitialScale = 6f;
    public float InitialAmplitude = 30f;
    public float Persistence = 0.4f;
    public int Octaves = 9;
    public float Lacunarity = 1.92f;
    public bool circle = true;
    private float CurrentHeight;
    public int OffsetX = 100;
    public int OffsetZ = 100;

    public override void draw(int x, int z)
    {
        Vector3 gridSize = terrain.gridSize();

        if (!circle)
        {
            for (int zi = -radius; zi <= radius; zi++)
            {
                for (int xi = -radius; xi <= radius; xi++)
                {
                    if(isInMap(x + xi, z + zi)){
                        float Amplitude = InitialAmplitude;
                        float Scale = InitialScale;
                        float Sum = 0; // to normalize the sum of Perlin noises
                        
                        for (int l = 0; l < Octaves; l++)
                        {
                            (float xCoord, float zCoord) = normalize(x + xi, z + zi, Scale, gridSize);
                            float perlinNoise = Mathf.PerlinNoise(xCoord, zCoord);
                            
                            Sum += Amplitude * perlinNoise;
                            Scale *= Lacunarity;
                            terrain.debug.text = Scale.ToString();
                            Amplitude *= Persistence;
                        }

                        terrain.set(x + xi, z + zi, Mathf.Max(Sum, 0));
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
                    float dist = new Vector2(xi, zi).magnitude;
                    if ((dist < radius) && (isInMap(x+xi, z+zi)))// more coherent to use a circle brush shape
                    {
                        float Amplitude = InitialAmplitude;
                        float Scale = InitialScale;
                        float Sum = 0; // to normalize the sum of Perlin noises
                        
                        for (int l = 0; l < Octaves; l++)
                        {
                            (float xCoord, float zCoord) = normalize(x + xi, z + zi, Scale, gridSize);
                            float perlinNoise = Mathf.PerlinNoise(xCoord, zCoord);

                            Sum += Amplitude * perlinNoise;
                            Scale *= Lacunarity;
                            Amplitude *= Persistence;
                        }

                        terrain.set(x + xi, z + zi, Mathf.Max(Sum, 0));
                    }
                }
            }
        }
    }

    public (float, float) normalize(float x, float z, float Scale, Vector3 gridSize)
    {
        float coordX = (float) x /gridSize.x  * Scale + OffsetX;
        float coordZ = (float) z /gridSize.z * Scale + OffsetZ;

        return (coordX, coordZ);
    }

    private bool isInMap(float x, float z)
    {
        Vector3 gridSize = terrain.gridSize();
        bool xInRange = (x > 0) && (x < (float)(gridSize.x));
        bool zInRange = (z > 0) && (z < (float)(gridSize.z));

        return xInRange && zInRange;
    }
}