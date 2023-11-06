using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Brush based on a Poisson disc sampling with a moisture map (made with Perlin noise) in background to have dry and moist zones
/// </summary>
public class MoisturePoissonInstanceBrush : InstanceBrush {

    [Header("Poisson Settings")]
    public int[] DryFamilyTreeIndexes;
    public int[] MoistFamilyTreeIndexes;
    // Moisture is normalized between 0 and 1, and thus the dryFamily will have (1-moisture) probability to appear
    // And the dry trees will appear with a chance moisture
    public float minFreeDistance;

    [Header("Moisture Map Generator")]
    public float InitialScale = 6f;
    public float InitialAmplitude = 10f;
    public float Persistence = 0.4f;
    public int Octaves = 9;
    public float Lacunarity = 1.92f;
    public int OffsetX = 500;
    public int OffsetZ = 500;
    [Range(0, 10)]
    public float flattenValleys = 1f;

    private float[,] moisture_data;

    public override void draw(float x, float z) {
        Vector3 gridSize = terrain.gridSize();
        moisture_data = MoistureMapGenerator();
        float minMoisture = getMinMoisture(moisture_data, gridSize);

        float maxMoisture = getMaxMoisture(moisture_data, gridSize);

        // The samples are generated in a rectangle but have no coordinates in our terrain
        // x and z are the middle of our rectangle, so the origin of the rectangle must be at (x - radius, z - radius)
        float terrainXPos = Mathf.Max(0, x - radius);
        float terrainZPos = Mathf.Max(0, z - radius);
        Vector2 terrainPos = new Vector2(terrainXPos, terrainZPos);

        PoissonDiscSampler PoissonSampler = new PoissonDiscSampler((float)radius*2f, (float)radius*2f, minFreeDistance);
        foreach(Vector2 sample in PoissonSampler.Samples())
        {
            Vector3 terrainSamplePos = new Vector3(terrainXPos + sample.x, terrain.getInterp(sample.x, sample.y), terrainZPos+ sample.y);
            
            float interpolatedMoisture = InterpMoisture(terrainSamplePos.x, terrainSamplePos.z);

            float moisture = InverseLerp(minMoisture, maxMoisture, interpolatedMoisture);

            float probaMoisture = Random.value;
            if(probaMoisture < 1 - moisture)
            {
                // We spawn a dry tree, randomly between the index prefabs of DryFamilyTreeIndex
                if(DryFamilyTreeIndexes.Length == 0)
                {
                    terrain.debug.text = "No prefabs specified for the Dry Family Trees";
                    return;
                }

                int prefabToSpawn = DryFamilyTreeIndexes[Random.Range(0, DryFamilyTreeIndexes.Length - 1)];
                float scale = terrain.PrefabParameters[prefabToSpawn].min_scale + Random.value * (terrain.PrefabParameters[prefabToSpawn].max_scale - terrain.PrefabParameters[prefabToSpawn].min_scale);
                terrain.spawnObject(terrainSamplePos, scale, prefabToSpawn);
            } else
            {
                // We spawn a moist tree, randomly between the index prefabs of DryFamilyTreeIndex
                if (MoistFamilyTreeIndexes.Length == 0)
                {
                    terrain.debug.text = "No prefabs specified for the Moist Family Trees";
                    return;
                }

                int prefabToSpawn = MoistFamilyTreeIndexes[Random.Range(0, MoistFamilyTreeIndexes.Length - 1)];
                float scale = terrain.PrefabParameters[prefabToSpawn].min_scale + Random.value * (terrain.PrefabParameters[prefabToSpawn].max_scale - terrain.PrefabParameters[prefabToSpawn].min_scale);
                terrain.spawnObject(terrainSamplePos, scale, prefabToSpawn);
            }

        }
    }

    private float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }

    private float InterpMoisture(float x, float z)
    {
        float XOffset = x - (int)x;
        float ZOffset = z - (int)z;

        float moistureSW = moisture_data[Mathf.Max(0, (int)x), Mathf.Max(0, (int)z)];
        //Debug.Log("Moisture SW = " + moistureSW.ToString());
        float moistureNW = moisture_data[Mathf.Min((int)terrain.gridSize().x, (int)(x + 1)), Mathf.Max(0, (int)z)];
        //Debug.Log("Moisture NW = " + moistureNW.ToString());
        float moistureNE = moisture_data[Mathf.Min((int)terrain.gridSize().x, (int)(x + 1)), Mathf.Min((int)terrain.gridSize().z, (int)(z + 1))];
        //Debug.Log("Moisture NE = " + moistureNE.ToString());
        float moistureSE = moisture_data[Mathf.Max(0, (int)(x)), Mathf.Min((int)terrain.gridSize().z, (int)(z + 1))];
        //Debug.Log("Moisture SE = " + moistureSE.ToString());
        return (1 - XOffset) * (1 - ZOffset) * moistureSW + ZOffset * (1 - XOffset) * moistureNW * XOffset * ZOffset * moistureNE + (1 - ZOffset) * XOffset * moistureSE;
    }

    public float[,] MoistureMapGenerator()
    {
        Vector3 gridSize = terrain.gridSize();
        moisture_data = new float[(int)gridSize.x, (int)gridSize.z];

        for (int zi = 0; zi < gridSize.z; zi++)
        {
            for (int xi = 0; xi < gridSize.x; xi++)
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
                moisture_data[xi, zi] = Sum;
            }
        }
        terrain.debug.text = "Successfully generated a Perlin-based Moisture Map";
        return moisture_data;
    }

    public (float, float) Normalize(float x, float z, float scale, Vector3 gridSize)
    {
        float coordX = x / gridSize.x * scale + OffsetX;
        float coordZ = z / gridSize.z * scale + OffsetZ;
        return (coordX, coordZ);
    }

    public float getMaxMoisture(float[,] moisture_data, Vector3 gridSize)
    {
        float maxMoisture = 0f;
        for (int xi = 0; xi < gridSize.x; xi++)
        {
            for (int zi = 0; zi < gridSize.z; zi++)
            {
                float curM = moisture_data[xi, zi];
                if (curM > maxMoisture)
                {
                    maxMoisture = curM;
                }
            }
        }
        return maxMoisture;
    }

    public float getMinMoisture(float[,] moisture_data, Vector3 gridSize)
    {
        float minMoisture = moisture_data[0, 0];
        for (int xi = 0; xi < gridSize.x; xi++)
        {
            for (int zi = 0; zi < gridSize.z; zi++)
            {
                float curM = moisture_data[xi, zi];
                if (curM < minMoisture)
                {
                    minMoisture = curM;
                }
            }
        }
        return minMoisture;
    }
}
