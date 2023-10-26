using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MinimalDistanceInstanceBrush : InstanceBrush {

    public int ObjectNumber= 1;
    public float MinimalDistance;
    
    public override void draw(float x, float z) {
        System.Random rand = new System.Random();

        int i = 0;
        int nb_objects = terrain.getObjectCount();
        while (i < ObjectNumber)
        {
            bool isPlaceable = true;
            int index = 0;

            float signX = rand.Next(2);
            float signZ = rand.Next(2);
            float Centerx = (float)(rand.NextDouble()) * radius*(2*signX-1);
            float Centerz = (float)(rand.NextDouble()) * radius*(2 * signZ - 1);
            while ( (index < nb_objects) && (isPlaceable))
            {
                Vector3 compObj = terrain.getObjectLoc(index);
                Vector2 CoordObj = new Vector2(compObj.x, compObj.y);

                Vector2 Center = new Vector2(x + Centerx, z+Centerz);

                Vector2 distV = Center - CoordObj;
                if(distV.magnitude <= MinimalDistance)
                {
                    isPlaceable = false;
                }

                index += 1;
            }

            if (isPlaceable)
            {
                spawnObject(x + Centerx, z + Centerz);
                i += 1;
            }
        }
    }
}
