using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomInstanceBrush : InstanceBrush {

    public bool circle = true;
    public int ObjectNumber = 1;
    // store then an array of game object
    public override void draw(float x, float z) {


        int i = 0;

        while(i < ObjectNumber)
        {
            System.Random rand = new System.Random();
            float signX = rand.Next(1);
            float signZ = rand.Next(1);
            float randx = (float)(rand.NextDouble())*radius*(2*signX-1);
            float randz = (float)(rand.NextDouble())*radius*(2 * signZ - 1);
            Vector2 TreeCoord = new Vector2(randx, randz);

            if(circle)
            {
                if(TreeCoord.magnitude <= radius)
                {
                    spawnObject(x + randx, z + randz);
                    i += 1;
                }
            }

            if(!circle)
            {
                spawnObject(x + randx, z + randz);
                i += 1;
            }

        }
    }
}
