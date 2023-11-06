using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NoiseBrush : TerrainBrush {

    public float Frequency = 1;
    public float Amplitude = 10f;
    public float scale = 1f;
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
                    CurrentHeight = terrain.get(x+xi,z+ zi);
                    (float coordX, float coordZ) = normalize(x + xi, z + zi, gridSize);
                    float newHeight = Amplitude*Mathf.PerlinNoise(coordX, coordZ);
                    terrain.set(x + xi, z + zi, Mathf.Max(newHeight, 0));
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
                        (float coordX, float coordZ) = normalize(x + xi, z + zi, gridSize);
                        float newHeight =Amplitude* Mathf.PerlinNoise(coordX, coordZ);
                        terrain.set(x + xi, z + zi, Mathf.Max(newHeight, 0));
                    }
                }
            }
        }
    }

    public (float, float) normalize(float x, float z, Vector3 gridSize)
    {

        float coordX = x / gridSize.x * scale + OffsetX;
        float coordZ = z / gridSize.z * scale + OffsetZ;

        return (coordX, coordZ);
    }
}
