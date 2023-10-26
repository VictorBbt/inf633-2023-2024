using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Brush learning from example distribution (real-life scenario)

public class ClusterInstanceBrush : InstanceBrush {


    public int ClusterNumber = 1;
    public int ClusterDensity = 3;
    public float ClusterRadius;

    // store then an array of game object
    public override void draw(float x, float z) {
        System.Random rand = new System.Random();
        int i = 0;

        while(i < ClusterNumber)
        {
            int j = 0;
            float signX = rand.Next(2);
            float signZ = rand.Next(2);
            float Centerx = (float)(rand.NextDouble()) * radius*(2*signX-1);
            float Centerz = (float)(rand.NextDouble()) * radius*(2*signZ -1);
            while (j < ClusterDensity)
            {
                float xtree = (float)(rand.NextDouble()) * ClusterRadius;
                float ztree = (float)(rand.NextDouble()) * ClusterRadius;
                if(new Vector2(xtree, ztree).magnitude <= ClusterRadius)
                {
                    spawnObject(x+Centerx + xtree, z +Centerz + ztree);
                    j += 1;
                    terrain.debug.text = "Put tree";
                }

            }
            i += 1;

        }
    }
}
