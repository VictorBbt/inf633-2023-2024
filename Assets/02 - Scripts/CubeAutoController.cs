using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeAutoController : MonoBehaviour {

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
        // Almost same as the CapsuleAutoController
        Predator toUpdate = GetComponent<Predator>();

        Vector3 scale = terrain.terrainData.heightmapScale;
        Transform tfm = transform;

        // Set forward vector
        //Vector3.RotateTowards(tfm.up, cterrain.getNormal(tfm.position.x/ scale.x, tfm.position.z/ scale.z), 1000, 1000);
        Vector3 v = tfm.rotation * Vector3.forward * toUpdate.speed;
        Vector3 loc = tfm.position + v;

        // Change localization
        //Vector3 loc = tfm.position + toUpdate.velocity * Time.deltaTime;

        if (loc.x < 0)
            loc.x += width;
        else if (loc.x > width)
            loc.x -= width;
        if (loc.z < 0)
            loc.z += height;
        else if (loc.z > height)
            loc.z -= height;

        float xCoord = loc.x / scale.x;
        float zCoord = loc.z / scale.z;
        loc.y = cterrain.getInterp(xCoord, zCoord); // Add an offset on y ?

        tfm.position = loc;
        // Align with terrain normal
        Quaternion alignRotation = Quaternion.FromToRotation(transform.up, cterrain.getNormal(xCoord, zCoord)) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, alignRotation, Time.deltaTime * 5f);

        //if(toUpdate.target.transform.position != null)
        //{
        //transform.LookAt(toUpdate.target.transform.position);
        //}

        // Move towards target
        //transform.position = Vector3.MoveTowards(transform.position, target.transform.position, Time.deltaTime * speed);
        // Rotate towards target


    }
}
