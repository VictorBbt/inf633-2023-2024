using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Allows to put textures on the terrain
/// The texture indexes are the corresponding indexes in the Terrain GUI inspector
/// We are forced to manually set the splatmaps, as we can't wriite a Custom Terrain shader (the Terrain tool already has its own custom shader)
/// </summary>
public class TextureBrush : TerrainBrush {

    public AdditionalLayerSettings[] additionalLayerSettings; // Corresponding indexes in the Terrain GUI inspector (first layer is 0 (the one set at low height)
    public bool GrassAndCliffSteepness = true;
    public Vector3 sunDirection;

    private float minHeight;
    private float maxHeight;

    int layerCount;
    public override void draw(int x, int z)
    {
        // Get the attached terrain component
        Terrain terrainA = Terrain.activeTerrain;

        // Get a reference to the terrain data
        TerrainData terrainData = terrainA.terrainData;
        layerCount = terrainData.alphamapLayers;

        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, layerCount];

        minHeight = terrain.getMinHeight();
        maxHeight = terrain.getMaxHeight();
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int q = 0; q < terrainData.alphamapWidth; q++)
            {

                // Normalise q/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float q_01 = (float)q / (float)terrainData.alphamapWidth;

                // Get the coordinates for the heightMap
                float xH = Mathf.RoundToInt(y_01 * terrainData.heightmapResolution);
                float zH = Mathf.RoundToInt(q_01 * terrainData.heightmapResolution);

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrain.get(xH, zH);
                float normalizedHeight = InverseLerp(minHeight, maxHeight, height); // Normalize the height to be coherent with the thresholds

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrain.getNormal(xH, zH);

                // Calculate the steepness of the terrain
                float steepness = terrain.getSteepness(xH, zH);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[layerCount];
                for (int i = 0; i < splatWeights.Length; i++) { splatWeights[i] = 0; } // init to 0

                // Creating array based on the feature on additionalLayreSettings
                float[] startHeights = additionalLayerSettings.Select(x => x.startHeight).ToArray();
                float[] blendStrengths = additionalLayerSettings.Select(x => x.blendStrength).ToArray();

                // SET CUSTOM RULES:

                // 1. Blending based on heights with linear blending between the adjacent textures
                for (int i = 0; i < layerCount - 1; i++)
                {
                    // if normalizedHeight is too big, we clamp it
                    float drawStrength = LinearBlending(i, normalizedHeight, startHeights[i], startHeights[i + 1], blendStrengths[i]);
                    splatWeights[i] = drawStrength;
                }
                // Setting up last layer
                splatWeights[layerCount - 1] = LinearBlending(layerCount - 1, normalizedHeight, startHeights[layerCount - 1], maxHeight, blendStrengths[layerCount - 1]);


                //// 2. Taking steepness into account
                //    // texture[2] Grass stronger on flatter terrain
                //    // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                //    // Subtract result from 1.0 to give greater weighting to flat surfaces //splatWeights[3] = height * Mathf.Clamp01(normal.z);
                if (GrassAndCliffSteepness)
                {
                    if ((normalizedHeight > startHeights[2]) && (normalizedHeight < startHeights[4])) // We are on a hill
                    {
                        splatWeights[2] += 0.2f * (1 - InverseLerp(0f, 90f, steepness)); // Steepness is around 0 and 90 on the Terrain class, so we interpolate and add greater weight if steepness is low, while saturating at 0.2
                    }

                    if (normalizedHeight > startHeights[5])// We are on a mountain
                    {
                        splatWeights[3] += 0.2f * InverseLerp(0f, 90f, steepness); // Cliffs are more present if it is very steep
                    }
                }

                //// 3. Higher weight for grass and snow on surfaces facing positive sunDirection axis
                //        // i.e. if the pixel is in the shadow
                if(sunDirection != Vector3.zero)
                {
                    if ((normalizedHeight > startHeights[2]) && (normalizedHeight < startHeights[4])) // We are on a hill
                    {
                        splatWeights[2] += 0.3f*Mathf.Clamp01(Vector3.Dot(normal, sunDirection));
                    }
                    if (normalizedHeight > startHeights[4]) // We are on a mountain
                    {
                        splatWeights[layerCount-1] += 0.3f*Mathf.Clamp01(Vector3.Dot(normal, sunDirection));
                    }
                }

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float s = splatWeights.Sum();
                if (s == 0)
                {
                    splatWeights[0] = 1;
                }

                // Loop through each terrain texture
                for (int i = 0; i < layerCount; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= s;

                    // Assign this point to the splatmap array
                    splatmapData[q, y, i] = splatWeights[i];
                }

                }
            }
            // Finally assign the new splatmap to the terrainData:
            terrainData.SetAlphamaps(0, 0, splatmapData);
        }

    private float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }

    private float LinearBlending(int index, float h, float startHi, float startHNexti, float BlendRate)
    {
        if (BlendRate == 0) // No blending, we return 1 within the zone and 0 elsewhere
        {
            if ((h > startHi) && (h < startHNexti))
            {
                return 1f;
            }
            else { return 0f; }
        }

        else
        {
            if (index == 0) // we start at height 0
            {
                if (startHNexti != 0f)
                {
                    float a = (BlendRate - 1) / startHNexti;
                    return Mathf.Clamp01(1 + a*h);
                }
                else
                {
                    return 0f;
                }

            }

            if (index == layerCount) 
            {
                if(startHi == startHNexti)
                {
                    return 0f;
                } else
                {
                    float a = (1 - BlendRate) / (startHNexti - startHi);
                    float b = 1 - a * startHNexti;
                    return Mathf.Clamp01(a * h + b);
                }

            }
            else
            {
                // triangle shaped function which equals to one at midZone and linearly decreases to BlendRate at the adjacent junctions of textures (startHi, startNextHi)
                float midZone = (startHi + startHNexti) / 2;
                if(h <= midZone)
                {
                    float a = (1 - BlendRate) / (midZone - startHi);
                    float b = 1 - a * midZone;
                    return Mathf.Clamp01(a * h + b);
                }
                else
                {
                    float a = (BlendRate - 1) / (startHNexti - midZone);
                    float b = 1 - a * midZone;
                    return Mathf.Clamp01(a * h + b);
                }
            }
        }
    }

    [System.Serializable]
    public class AdditionalLayerSettings
    {
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 0.2f)]
        public float blendStrength;
    }
}
