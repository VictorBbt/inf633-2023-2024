using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CloudyMoistureBrush : TerrainBrush {

    public float InitialScale = 6f;
    public float InitialAmplitude = 30f;
    public float Persistence = 0.4f;
    public int Octaves = 9;
    public float Lacunarity = 1.92f;
    public int OffsetX = 100;
    public int OffsetZ = 100;
    [Range(0, 10)]
    public float flattenValleys = 1f;

    public override void draw(int x, int z)
    {
        Vector3 gridSize = terrain.gridSize();
        for (int zi = 0; zi <= gridSize.z; zi++)
        {
            for (int xi = 0; xi <= gridSize.x; xi++)
            {
                float Amplitude = InitialAmplitude;
                float Scale = InitialScale;
                float Sum = 0;

                for (int l = 0; l < Octaves; l++)
                {
                    (float xCoord, float zCoord) = Normalize(xi, zi, Scale, gridSize);
                    float perlinNoise = Mathf.PerlinNoise(xCoord, zCoord);
                    if (l == 0)
                    {
                        perlinNoise = Mathf.Pow(perlinNoise, flattenValleys);
                    }
                    Sum += Amplitude * perlinNoise;
                    Scale *= Lacunarity;
                    Amplitude *= Persistence;
                }
                terrain.moisture_data[x, z] = Sum;
            }
        }
    }

    public (float, float) Normalize(float x, float z, float scale, Vector3 gridSize)
    {

        float coordX = x / gridSize.x * scale + OffsetX;
        float coordZ = z / gridSize.z * scale + OffsetZ;

        return (coordX, coordZ);
    }
}
