﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomTerrain : MonoBehaviour {

    public Text debug;

    [Header("Global brushes attributes")]
    [Range(1, 100)]
    public int brush_radius = 10;

    [Header("Instance brush attributes")]
    public PrefabSettings[] PrefabParameters;


    private Brush current_brush;

    private Terrain terrain;
    private Collider terrain_collider;
    private TerrainData terrain_data;
    private Vector3 terrain_size;
    private int heightmap_width;
    private int heightmap_height;
    private float[,] heightmap_data;
    private int amap_width, amap_height;
    private int detail_width, detail_height;
    private int[,] detail_layer = null;
    private float[,,] alphamaps;

    private GameObject highlight_go;
    private Projector highlight_proj;
    public static System.Random rnd = new System.Random();

    [SerializeField] Camera cam;

    // Initialization
    void Start () { //Start
        //Debug.Log("1 - Retrieve terrain information");
        if (!terrain)
            terrain = Terrain.activeTerrain;
        terrain_collider = terrain.GetComponent<Collider>();
        terrain_data = terrain.terrainData;
        terrain_size = terrain_data.size;
        heightmap_width = terrain_data.heightmapResolution;
        heightmap_height = terrain_data.heightmapResolution;
        heightmap_data = terrain_data.GetHeights(0, 0, heightmap_width, heightmap_height);
        current_brush = null;
        highlight_go = GameObject.Find("Cursor Highlight");
        highlight_proj = highlight_go.GetComponent<Projector>();

        detail_width = terrain_data.detailWidth;
        detail_height = terrain_data.detailHeight;
        detail_layer = terrain_data.GetDetailLayer(0, 0, detail_width, detail_height, 0);
        amap_width = terrain_data.alphamapWidth;
        amap_height = terrain_data.alphamapHeight;
        alphamaps = terrain_data.GetAlphamaps(0, 0, amap_width, amap_height);

        // Reset and save textures
        // for (int y = 0; y < amap_height; y++) {
        //     for (int x = 0; x < amap_width; x++) {
        //         alphamaps[x, y, 0] = 1.0f;
        //         alphamaps[x, y, 1] = 0.0f;
        //     }
        // }
        // saveTextures();
        // Reset and save  ss
        for (int y = 0; y < detail_height; y++) {
            for (int x = 0; x < detail_width; x++) {
                detail_layer[x, y] = 0;
            }
        }
        saveDetails();

        cam = GameObject.FindGameObjectWithTag("SecondCamera").GetComponent<Camera>();
    }

    // Called once per frame
    void Update () {
        Vector3 hit_loc = Vector3.zero;
        bool do_draw_target = false;
        RaycastHit hit;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (terrain_collider.Raycast(ray, out hit, Mathf.Infinity)) {
            hit_loc = hit.point;
            if (current_brush)
                do_draw_target = true;
            if (Input.GetMouseButtonDown(0)) {
                debug.text = "Coords: " + hit_loc.ToString();
                if (current_brush)
                    current_brush.callDraw(hit_loc.x, hit_loc.z);
            }
        }
        drawTarget(hit_loc, do_draw_target);
    }

    // Draw the brush marker on the terrain
    private void drawTarget(Vector3 c, bool show) {
        if (show) {
            c.y += 100;
            highlight_go.transform.position = c;
            highlight_proj.orthographicSize = brush_radius;
        } else {
            highlight_go.transform.position = new Vector3(-10, -10, -10);
            highlight_proj.orthographicSize = 0;
        }
    }

    public int registerPrefab(GameObject go) {
        for (int i = 0; i < terrain_data.treePrototypes.Length; i++) {
            if (terrain_data.treePrototypes[i].prefab == go)
                return i;
        }

        TreePrototype proto = new TreePrototype();
        proto.bendFactor = 0.0f;
        proto.prefab = PrefabParameters[0].CorrespondingPrefab;
        List<TreePrototype> protos = new List<TreePrototype>(terrain_data.treePrototypes);
        protos.Add(proto);
        terrain_data.treePrototypes = protos.ToArray();
        return protos.Count - 1;
    }

    public Vector3 get3(int x, int z) {
        return new Vector3(x, get(x, z), z);
    }
    public Vector3 get3(float x, float z) {
        return new Vector3(x, get(x, z), z);
    }
    public Vector3 getInterp3(float x, float z) {
        return new Vector3(x, getInterp(x, z), z);
    }

    // Get grid height for a node
    public float get(int x, int z) {
        x = (x + heightmap_width) % heightmap_width;
        z = (z + heightmap_height) % heightmap_height;
        return heightmap_data[z, x] * terrain_data.heightmapScale.y;
    }
    public float get(float x, float z) {
        return get((int)x, (int)z);
    }

    public float getInterp(float x, float z) {
        return terrain_data.GetInterpolatedHeight(x / heightmap_width,
                                                  z / heightmap_height);
    }
    public float getSteepness(float x, float z) {
        return terrain_data.GetSteepness(x / heightmap_width,
                                         z / heightmap_height);
    }
    public Vector3 getNormal(float x, float z)
    {
        return terrain_data.GetInterpolatedNormal(x / heightmap_width,
                                                  z / heightmap_height);
    }

    // Set the grid height for a node
    public void set(int x, int z, float val) {
        x = (x + heightmap_width) % heightmap_width;
        z = (z + heightmap_height) % heightmap_height;
        heightmap_data[z, x] = val / terrain_data.heightmapScale.y;
    }
    public void set(float x, float z, float val) {
        set((int)x, (int)z, val);
    }

    // Spawn a new object (tree)
    public void spawnObject(Vector3 loc, float scale, int proto_idx) {
        TreeInstance obj = new TreeInstance();
        loc = new Vector3(loc.x / heightmap_width,
                          loc.y / terrain_data.heightmapScale.y,
                          loc.z / heightmap_height);
        obj.position = loc;
        obj.prototypeIndex = proto_idx;
        obj.lightmapColor =  Color.white;
        obj.heightScale = scale;
        obj.widthScale = scale;
        obj.rotation = (float)rnd.NextDouble() * 2 * Mathf.PI;
        obj.color = Color.white;
        terrain.AddTreeInstance(obj);
    }
    // Object (tree) manipulation
    public int getObjectCount() {
        return terrain_data.treeInstanceCount;
    }
    public TreeInstance getObject(int index) {
        return terrain_data.GetTreeInstance(index);
    }
    // Returns an object (tree) location in grid space
    public Vector3 getObjectLoc(int index) {
        return getObjectLoc(terrain_data.GetTreeInstance(index));
    }
    public Vector3 getObjectLoc(TreeInstance obj) {
        return new Vector3(obj.position.x * heightmap_width,
                           obj.position.y * terrain_data.heightmapScale.y,
                           obj.position.z * heightmap_height);
    }

    // Get dimensions of the heightmap grid
    public Vector3 gridSize() {
        return new Vector3(heightmap_width, 0.0f, heightmap_height);
    }
    // Get real dimensions of the terrain (world space)
    public Vector3 terrainSize() {
        return terrain_size;
    }
    // Get texture (alphamap) size
    public Vector2 textureSize() {
        return new Vector2(amap_width, amap_height);
    }
    public float[,,] getTextures() {
        return alphamaps;
    }
    // Get detail size
    public Vector2 detailSize() {
        return new Vector2(detail_width, detail_height);
    }
    public int[,] getDetails() {
        return detail_layer;
    }

    // Convert from grid space to world space
    public Vector3 grid2world(Vector3 grid) {
        return new Vector3(grid.x * terrain_data.heightmapScale.x,
                           grid.y,
                           grid.z * terrain_data.heightmapScale.z);
    }
    public Vector3 grid2world(float x, float y, float z) {
        return grid2world(new Vector3(x, y, z));
    }
    public Vector3 grid2world(float x, float z) {
        return grid2world(x, 0.0f, z);
    }

    // Convert from world space to grid space
    public Vector3 world2grid(Vector3 grid) {
        return new Vector3(grid.x / terrain_data.heightmapScale.x,
                           grid.y,
                           grid.z / terrain_data.heightmapScale.z);
    }
    public Vector3 world2grid(float x, float y, float z) {
        return world2grid(new Vector3(x, y, z));
    }
    public Vector3 world2grid(float x, float z) {
        return world2grid(x, 0.0f, z);
    }

    // Reset to flat terrain
    public void reset() {
        for (int z = 0; z < heightmap_height; z++) {
            for (int x = 0; x < heightmap_width; x++) {
                heightmap_data[z, x] = 0;
            }
        }
        save();
    }

    // Register changes made to the terrain
    public void save() {
        terrain_data.SetHeights(0, 0, heightmap_data);
    }
    public void saveTextures() {
        terrain_data.SetAlphamaps(0, 0, alphamaps);
    }
    public void saveDetails() {
        terrain_data.SetDetailLayer(0, 0, 0, detail_layer);
    }

    // Get and set active brushes
    public void setBrush(Brush brush) {
        current_brush = brush;
    }
    public Brush getBrush() {
        return current_brush;
    }

    public float getMaxHeight() {
        float maxHeight = 0f;
        for(int xi = 0; xi <= heightmap_width; xi++)
        {
            for (int zi = 0; zi <= heightmap_height; zi++)
            {
                float curH = get(xi, zi);
                if(curH > maxHeight)
                {
                    maxHeight = curH;
                }
            }
        }
        return maxHeight;
    }

    public float getMinHeight()
    {
        float minHeight = get(0, 0);
        for (int xi = 0; xi <= heightmap_width; xi++)
        {
            for (int zi = 0; zi <= heightmap_height; zi++)
            {
                float curH = get(xi, zi);
                if (curH < minHeight)
                {
                    minHeight = curH;
                }
            }
        }
        return minHeight;
    }


    public int[,] getZone(int index, float x, float z)
    {
        int[,] zone = new int[(int)gridSize().x, (int)gridSize().z];
        float y = InverseLerp(getMinHeight(), getMaxHeight(), getInterp(x, z));
        float steep = getSteepness(x, z);
        float minH = PrefabParameters[index].minHeight;
        float maxH = PrefabParameters[index].maxHeight;
        float minS = PrefabParameters[index].minSteepness;
        float maxS = PrefabParameters[index].maxSteepness;
        if((y > minH) && (y<maxH) && (steep > minS) && (steep < maxS))
        {
            zone[(int)x, (int)z] = 1;
        } else { zone[(int)x, (int)z] = 0; }

        return zone;
    }

    public int[,] getAllZones()
    {

        float minTerrainH = getMinHeight();
        float maxTerrainH = getMaxHeight();
        int[,] zone = new int[(int)gridSize().x, (int)gridSize().z];
        for (int x = 0; x < (int)gridSize().x; x++)
        {
            for (int z = 0; z < (int)gridSize().z; z++)
            {
                zone[x, z] = -1;
            }
        }

        for (int x=0; x< (int)gridSize().x; x++)
        {
            for (int z = 0; z < (int)gridSize().z; z++)
            {
                for (int i = 0; i < PrefabParameters.Length; i++)
                {
                    float y =InverseLerp(minTerrainH, maxTerrainH, getInterp(x, z));
                    float steep = getSteepness(x, z);
                    float minH = PrefabParameters[i].minHeight;
                    float maxH = PrefabParameters[i].maxHeight;
                    float minS = PrefabParameters[i].minSteepness;
                    float maxS = PrefabParameters[i].maxSteepness;
                    if ((y >= minH) && (y <= maxH) && (steep >= minS) && (steep <= maxS))
                    {
                        zone[x, z] = i;
                    }
                }
            }
        }

        return zone;

    }

    private float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }

    [System.Serializable]
    public class PrefabSettings
    {
        public GameObject CorrespondingPrefab;
        public int prefab_idx;
        [Range(0, 1)]
        public float minHeight;
        [Range(0, 1)]
        public float maxHeight;
        [Range(0, 90f)]
        public float minSteepness;
        [Range(0, 90f)]
        public float maxSteepness;
        public float min_scale = 0.8f;
        public float max_scale = 1.2f;
    }


}
