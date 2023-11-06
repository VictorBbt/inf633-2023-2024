using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// GLOBAL BRUSH
/// This brush allow to generate random realistic terrain (Perlin, Ridged Noise, Terrace) with custom parameters
/// </summary>
public class TerrainCreationBrush : TerrainBrush
{
    System.Random rand = new System.Random();
    // Perlin parameters
    public float InitialScale = 6f;
    public float InitialAmplitude = 30f;
    public float Persistence = 0.4f;
    public int Octaves = 9;
    public float Lacunarity = 1.92f;
    public int OffsetX = 100;
    public int OffsetZ = 100;

    // Change the shape of Perlin Noise:
    public bool StandardPerlin = true;
    public bool Ridged = false;
    public bool Terrace = false;

    [Range(0,500)] // Circular Filtering to add peaks and valleys
    public float CircularFiltering = 364;
    public int NumberOfPeaks = 1;

    [Range(0, 10)]
    public float flattenValleys = 1f;
    public float TerraceHeight = 3f;
    public override void draw(int x, int z)
    {
        Vector3 gridSize = terrain.gridSize();

        // Standard Perlin
        if (StandardPerlin)
        {
            for (int xi = 0; xi <= gridSize.x; xi++)
            {
                for (int zi = 0; zi <= gridSize.x; zi++)
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

                    terrain.set(xi, zi,Mathf.Max(Sum, 0));
                }
            }
        }

        // Ridged noise
        else if (Ridged)
        {
            for (int xi = 0; xi <= gridSize.x; xi++)
            {
                for (int zi = 0; zi <= gridSize.x; zi++)
                {
                    float Amplitude = InitialAmplitude;
                    float Scale = InitialScale;
                    float Sum = 0;
                    float SumOfNoises = 0;
                    float perlinNoise;

                    for (int l = 0; l < Octaves; l++)
                    {
                        (float xCoord, float zCoord) = Normalize(xi, zi, Scale, gridSize);
                        if (l == 0)
                        {
                            perlinNoise = 2f * (0.5f - Mathf.Abs(0.5f - Mathf.PerlinNoise(xCoord, zCoord))) ;
                        }
                        else
                        {
                            perlinNoise = 2f * (0.5f - Mathf.Abs(0.5f - Mathf.PerlinNoise(xCoord, zCoord))) * SumOfNoises;
                        }

                        SumOfNoises += perlinNoise;

                        flattenValleys *= Persistence;
                        Sum += Amplitude * perlinNoise;
                        Scale *= Lacunarity;
                        Amplitude *= Persistence;
                    }

                    terrain.set(xi, zi, Mathf.Max(Sum, 0));
                }
            }
        }

        // Terraces
        else if (Terrace)
        {
            for (int xi = 0; xi <= gridSize.x; xi++)
            {
                for (int zi = 0; zi <= gridSize.x; zi++)
                {
                    float Amplitude = InitialAmplitude;
                    float Scale = InitialScale;
                    float Sum = 0;
                    float SumOfNoises = 0;
                    float perlinNoise;

                    for (int l = 0; l < Octaves; l++)
                    {
                        (float xCoord, float zCoord) = Normalize(xi, zi, Scale, gridSize);
                        if (l == 0)
                        {
                            perlinNoise = 2f * (0.5f - Mathf.Abs(0.5f - Mathf.PerlinNoise(xCoord, zCoord)));
                        }
                        else
                        {
                            perlinNoise = 2f * (0.5f - Mathf.Abs(0.5f - Mathf.PerlinNoise(xCoord, zCoord))) * SumOfNoises;
                        }

                        SumOfNoises += perlinNoise;

                        flattenValleys *= Persistence;
                        Sum += Amplitude * perlinNoise;
                        Scale *= Lacunarity;
                        Amplitude *= Persistence;
                    }

                    terrain.set(xi, zi, TerraceHeight * (int)(Sum / TerraceHeight));
                }
            }
        }

        // Gaussian 2D filter on several random places at the terrain (or at the center if the number of peaks is set to one
        if (CircularFiltering > 0) // Setting circular filtering to O won't filter anything
        {
            float[,] hMap;
            if (NumberOfPeaks == 1)
            {
                int Centerx = (int)(terrain.gridSize().x / 2);
                int Centerz = (int)(terrain.gridSize().z / 2);
                hMap = circularFiltering(Centerx, Centerz);
            }
            else
            {
                hMap = new float[(int)(terrain.gridSize().x) + 1, (int)(terrain.gridSize().z) + 1];
                for (int i = 0; i <= terrain.gridSize().x; i++)
                {
                    for (int j = 0; j <= terrain.gridSize().z; j++)
                    {
                        hMap[i, j] = 0.0f; // You can set initial values here
                    }
                }

                for (int n = 0; n < NumberOfPeaks; n++)
                {
                    // if you want to have the peaks at chosen posiitions, change opsX and posZ here
                    int posX = rand.Next(0, (int)terrain.gridSize().x - 1);
                    int posZ = rand.Next(0, (int)terrain.gridSize().z - 1);
                    float[,] map1 = circularFiltering(posX, posZ);
                    for (int i = 0; i <= terrain.gridSize().x; i++)
                    {
                        for (int j = 0; j <= terrain.gridSize().z; j++)
                        {
                            hMap[i, j] = hMap[i, j] + map1[i, j]; // You can set initial values here
                        }
                    }
                }

            }

            // Setting the values of the heightmap on the terrain
            for (int xi = 0; xi <= terrain.gridSize().x; xi++)
            {
                for (int zi = 0; zi <= terrain.gridSize().z; zi++)
                {
                    terrain.set(xi, zi, hMap[xi, zi]);
                }
            }
        }
    }

    public (float, float) Normalize(float x, float z, float Scale, Vector3 gridSize)
    {
        float coordX = (float) x /gridSize.x  * Scale + OffsetX;
        float coordZ = (float) z /gridSize.z * Scale + OffsetZ;

        return (coordX, coordZ);
    }

    private bool IsInMap(float x, float z)
    {
        Vector3 gridSize = terrain.gridSize();
        bool xInRange = (x > 0) && (x < gridSize.x);
        bool zInRange = (z > 0) && (z < gridSize.z);

        return xInRange && zInRange;
    }

    private float GaussianF(float r, float R)
    {
        return Mathf.Exp(-Mathf.Pow((3*r / R), 2)/2); // with sigma = 1, the gaussian in 3 is almost 0
    }


    private float[,] circularFiltering(int Centerx,int Centerz)
    {
        float[,] hMap = new float[(int)(terrain.gridSize().x)+1 ,(int)(terrain.gridSize().z)+1]; // init heightmap
        for (int xi = 0; xi <= terrain.gridSize().x; xi++)
        {
            for (int zi = 0; zi <= terrain.gridSize().z; zi++)
            {
                Vector2 CenteredOrigin = new Vector2(xi - Centerx, zi - Centerz); // Center of the gaussian
                float dist = CenteredOrigin.magnitude;
                float CurrentHeight = terrain.get(xi, zi);

                float minCoef = GaussianF(CircularFiltering, CircularFiltering);
                if (dist <= CircularFiltering) 
                {
                    float r = Mathf.Sqrt(CenteredOrigin.x * CenteredOrigin.x + CenteredOrigin.y * CenteredOrigin.y);
                    float penCoef = GaussianF(r, CircularFiltering); // Compute by how much it is decreased (gaussian 2D of radius CircularFiltering)
                    hMap[xi, zi] = penCoef * CurrentHeight;
                    
                }
                else
                {
                    hMap[xi, zi] = minCoef * CurrentHeight;// if we are not in the range, we set to the values on the radius of the filter (ensures continuity)
                }
            }
        }
        return hMap;
    }

}