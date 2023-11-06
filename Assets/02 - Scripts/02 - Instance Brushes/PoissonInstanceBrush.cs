using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Poisson distribution based on Poisson disc sampler
/// </summary>
public class PoissonInstanceBrush : InstanceBrush {

    public int CorrespondingIndex;
    public float minFreeDistance;

    public override void draw(float x, float z) {
        // The samples are ge,erated in a rectangle but have no coordinates in our terrain
        // x and z are the middle of our rectangle, so the origin of the rectangle must be at (x - radius, z - radius)
        float terrainXPos = Mathf.Max(0, x - radius);
        float terrainZPos = Mathf.Max(0, z - radius);
        Vector2 terrainPos = new Vector2(terrainXPos, terrainZPos);
        Debug.Log("True coords: (" + x.ToString() + "," + z.ToString() + ")");
        Debug.Log("Poisson sampling origin: " + terrainPos.ToString());

        PoissonDiscSampler PoissonSampler = new PoissonDiscSampler((float)radius*2f, (float)radius*2f, minFreeDistance);
        foreach(Vector2 sample in PoissonSampler.Samples())
        {
            Vector3 terrainSamplePos = new Vector3(terrainXPos + sample.x, terrain.getInterp(sample.x, sample.y), terrainZPos+ sample.y);
            float scale = terrain.PrefabParameters[CorrespondingIndex].min_scale + Random.value * (terrain.PrefabParameters[CorrespondingIndex].max_scale - terrain.PrefabParameters[CorrespondingIndex].min_scale);
            terrain.spawnObject(terrainSamplePos, scale, CorrespondingIndex);
            terrain.debug.text = "Spawned object at:" + terrainSamplePos.ToString();
        }
    }
}
