using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Brush learning from example distribution (real-life scenario)
// Poisson sampling to spawn objects ?
// Cluster or not ?
public class TerrainFeatureBasedBrush : InstanceBrush {


    // O: food
    // 1: sandyRock
    // 2: conifer Tree
    // 3: big tree
    // 4: small alpine tree
    // 5: Big rocks

    // the best would be to get the texture and tp spawn objects accordingly
    public int sandyRockHeightThreshold;
    public int bigTreeHeightThreshold;
    public int littleTreeRocksHeightThreshold;

    public int treeSteepnessThreshold; // 40/50 très pentu, 7 quasi rien/ 20/30 colline
    public int bushSteepnessThreshold;



    // store then an array of game object
    public override void draw(float x, float z) {
        terrain.debug.text = terrain.getSteepness(x, z).ToString();
        float curHeight = terrain.getInterp(x, z);
        float curSteep = terrain.getSteepness(x, z);
        Vector3 pos = new Vector3(x, curHeight, z);
        float scale = 1f;
        if (curHeight > sandyRockHeightThreshold)
        {
            terrain.spawnObject(pos, scale, 1);
        }
        else if ((curHeight > bigTreeHeightThreshold) && (curSteep < treeSteepnessThreshold))
        {
            terrain.spawnObject(pos, scale, 3);
        }
        else if ((curHeight > bigTreeHeightThreshold) && (curSteep > treeSteepnessThreshold))
        {
            terrain.spawnObject(pos, scale, 2);
        }


    }
}
