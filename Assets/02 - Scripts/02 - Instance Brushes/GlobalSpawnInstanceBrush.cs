using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GlobalSpawnInstanceBrush : InstanceBrush {

    public int[] NumberOfObjects;

    public override void draw(float x, float z) {
        int maxTry = 100;
        int nb_prefabs = terrain.PrefabParameters.Length;
        int[,] spawnZones = terrain.getAllZones();
        int i = 0;
        while(i < Mathf.Min(NumberOfObjects.Length, nb_prefabs))
        {
            int cnt = 0;
            int tryCount = 0;
            while((cnt <= NumberOfObjects[i]) && (tryCount < maxTry))
            {
                int Centerx = Random.Range(0,(int)terrain.gridSize().x);
                int Centerz = Random.Range(0, (int)terrain.gridSize().z);

                if (spawnZones[Centerx, Centerz] == i)
                {
                    Vector3 pos = new Vector3(Centerx, terrain.getInterp(Centerx, Centerz), Centerz);
                    float scale = terrain.PrefabParameters[i].min_scale + Random.value * (terrain.PrefabParameters[i].max_scale - terrain.PrefabParameters[i].min_scale);
                    terrain.spawnObject(pos, scale, i);
                    //terrain.debug.text = "Spawned object " + i.ToString() + " at pos: " + pos.ToString();
                    cnt += 1;
                    tryCount = 0;
                } else
                {
                    tryCount += 1;
                }
            }
            i += 1;
        }
    }
}
