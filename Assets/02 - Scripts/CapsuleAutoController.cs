using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is the controller class for the preys (actuator)
/// </summary>
public class CapsuleAutoController : MonoBehaviour {

    public float max_speed = 0.5f;
    protected Terrain terrain;
    protected CustomTerrain cterrain;
    protected float width, height;

    void Start() {
        terrain = Terrain.activeTerrain;
        cterrain = terrain.GetComponent<CustomTerrain>();
        width = terrain.terrainData.size.x;
        height = terrain.terrainData.size.z;
    }

    void Update() {
        Prey toUpdate = GetComponent<Prey>(); // Prey to update

        Vector3 scale = terrain.terrainData.heightmapScale;
        Transform tfm = transform;
        Vector3 vTarget = tfm.rotation * Vector3.forward * toUpdate.targetWeight; // Update of the position based on the angle of foodBrain

        Vector3 loc = tfm.position + vTarget +  toUpdate.velocity * Time.deltaTime; // Second integration for group behaviour component

        // continues path at the other side of the terrain if goes after the limit
        if (loc.x < 0)
            loc.x += width;
        else if (loc.x > width)
            loc.x -= width;
        if (loc.z < 0)
            loc.z += height;
        else if (loc.z > height)
            loc.z -= height;
        loc.y = cterrain.getInterp(loc.x/scale.x, loc.z/scale.z);
        tfm.position = loc;
    }
}
