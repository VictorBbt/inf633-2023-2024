using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Brush learning from example distribution (real-life scenario)
// Poisson sampling to spawn objects ?
// Cluster or not ?
public class HeightBasedBrush : InstanceBrush {


    // O: food
    // 1: sandyRock
    // 2: conifer Tree
    // 3: big tree
    // 4: small alpine tree
    // 5: Big rocks

    // the best would be to get the texture and tp spawn objects accordingly
    [Range(0,1)]
    public float sandyRockHeightThreshold;
    [Range(0, 1)]
    public float coniferThreshold;
    [Range(0, 1)]
    public float TreeRocksHeightThreshold;
    [Range(0, 1)]
    public float mountainRocksHeightThreshold;

    //public int treeSteepnessThreshold; // 40/50 très pentu, 7 quasi rien/ 20/30 colline
    //public int bushSteepnessThreshold;

    float minHeight;
    float maxHeight;

    // store then an array of game object
    public override void draw(float x, float z) {
        minHeight = terrain.getMinHeight();
        maxHeight = terrain.getMaxHeight();
        float curHeight = terrain.getInterp(x, z);
        float normHeight = InverseLerp(minHeight, maxHeight, curHeight);
        Vector3 pos = new Vector3(x, curHeight, z);
        float scale = 1f;
        if ((normHeight > sandyRockHeightThreshold) && (normHeight < coniferThreshold))
        {
            terrain.debug.text = "Entered sandyRock";
            terrain.spawnObject(pos, scale, 0);
        }
        else if((normHeight > coniferThreshold) && (normHeight < TreeRocksHeightThreshold))
        {
            terrain.debug.text = "Entered conifer";
            terrain.spawnObject(pos, scale, 1);
        }
        else if((normHeight > TreeRocksHeightThreshold) && (normHeight < mountainRocksHeightThreshold))
        {
            terrain.debug.text = "Entered treeRocks";
            terrain.spawnObject(pos, scale, 2);
        }
        else if (normHeight > mountainRocksHeightThreshold) 
        {
            terrain.debug.text = "Entered mountainRock";
            terrain.spawnObject(pos, scale, 4);
        }
    }

    private float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }
}
