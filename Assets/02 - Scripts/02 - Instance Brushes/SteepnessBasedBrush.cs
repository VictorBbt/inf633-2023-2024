using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Brush based on Steepness of the terrain
/// </summary>
public class SteepnessBasedBrush : InstanceBrush {


    // O: food
    // 1: sandyRock
    // 2: conifer Tree
    // 3: big tree
    // 4: small alpine tree
    // 5: Big rocks

    // the best would be to get the texture and tp spawn objects accordingly
    [Range(0,1)]
    public float flatThreshold;
    [Range(0, 1)]
    public float littleSteepThreshold;
    [Range(0, 1)]
    public float verySteepThreshold;

    public override void draw(float x, float z) {
        float curHeight = terrain.get(x, z);
        float curSteepness = terrain.getSteepness(x, z);
        float normHeight = InverseLerp(0, 90f, curSteepness);
        Vector3 pos = new Vector3(x, curHeight, z);
        float scale = 1f;

        if ((normHeight > flatThreshold) && (normHeight < littleSteepThreshold))
        {
            terrain.debug.text = "Flat terrain";
            terrain.spawnObject(pos, scale, 0);
        }
        else if((normHeight > littleSteepThreshold) && (normHeight < verySteepThreshold))
        {
            terrain.debug.text = "Little steepness";
            terrain.spawnObject(pos, scale, 1);
        }
        else if(normHeight > verySteepThreshold) 
        {
            terrain.debug.text = "High steepness ";
            terrain.spawnObject(pos, scale, 2);
        }
    }

    private float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }
}
