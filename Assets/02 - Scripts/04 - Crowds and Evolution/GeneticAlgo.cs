using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneticAlgo : MonoBehaviour
{

    [Header("Genetic Algorithm parameters")]
    public int popSize = 100;
    public AnimalsSpawnZone[] AnimalParameters;

    [Header("Dynamic elements")]
    public float vegetationGrowthRate = 1.0f;
    [Range(0,1)]
    public float minFoodHeightThreshold;
    [Range(0, 1)]
    public float maxFoodHeightThreshold;
    [Range(0, 90f)]
    public float minFoodSteepnessThreshold;
    [Range(0, 90f)]
    public float maxFoodSteepnessThreshold;
    public float currentGrowth;
    

    private List<GameObject> animals; // Make a list of GameObject List<GameObject>[] where the index is the index of the AnimalParameters
    protected Terrain terrain;
    protected CustomTerrain customTerrain;
    protected float width;
    protected float height;

    void Start()
    {
        // Retrieve terrain.
        terrain = Terrain.activeTerrain;
        customTerrain = GetComponent<CustomTerrain>();
        width = terrain.terrainData.size.x;
        height = terrain.terrainData.size.z;

        // Initialize terrain growth.
        currentGrowth = 0.0f;

        // Initialize animals array.
        animals = new List<GameObject>();
        for (int i = 0; i < popSize; i++)
        {
            GameObject animal = makeAnimal(0);
            animals.Add(animal);
        }
    }

    void Update()
    {
        // Keeps animal to a minimum.
        while (animals.Count < popSize / 2)
        {
            animals.Add(makeAnimal(0));
        }
        customTerrain.debug.text = "N? animals: " + animals.Count.ToString();

        // Update grass elements/food resources.
        updateResources();
    }

    /// <summary>
    /// Method to place grass or other resource in the terrain.
    /// </summary>
    public void updateResources()
    {
        Vector2 detail_sz = customTerrain.detailSize();
        int[,] details = customTerrain.getDetails();
        customTerrain.debug.text = detail_sz.ToString();
        //int objectCount = customTerrain.getObjectCount();
        currentGrowth += vegetationGrowthRate;
        while (currentGrowth > 1.0f)
        {

            int x = (int)(UnityEngine.Random.value * detail_sz.x);
            int y = (int)(UnityEngine.Random.value * detail_sz.y);
            
            // Render the objects on the scene (grass, ...)

            // Tweak the grass spawns based on steepness annd height
            if(FoodisInZone(x/2f, y / 2f))
            {
                details[y, x] = 1;
                currentGrowth -= 1.0f;
            }

        }
        customTerrain.saveDetails();
    }

    /// <summary>
    /// Method to instantiate an animal prefab. It must contain the animal.cs class attached.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public GameObject makeAnimal(Vector3 position, int index)
    {
        Vector3 scale = terrain.terrainData.heightmapScale; // Must have the scale to compare positions within the heightmap

        GameObject animal = Instantiate(AnimalParameters[index].animalPrefab, transform);
        animal.GetComponent<Animal>().Setup(customTerrain, this);
        animal.transform.position = position;
        animal.transform.Rotate(0.0f, UnityEngine.Random.value * 360.0f, 0.0f);
        return animal;
    }

    /// <summary>
    /// If makeAnimal() is called without position, we randomize it on the terrain.
    /// </summary>
    /// <returns></returns>
    public GameObject makeAnimal(int index)
    {
        Vector3 scale = terrain.terrainData.heightmapScale;
        float x = UnityEngine.Random.value * width;
        float z = UnityEngine.Random.value * height;
        float y = customTerrain.getInterp(x / scale.x, z / scale.z);
        while(!isInSpawnZone(x / scale.x, z / scale.z, index)){
            x = UnityEngine.Random.value * width;
            z = UnityEngine.Random.value * height;
            y = customTerrain.getInterp(x / scale.x, z / scale.z);
        }
        return makeAnimal(new Vector3(x, y, z), index);
    }

    /// <summary>
    /// Method to add an animal inherited from another. It spawns where the parent was.
    /// </summary>
    /// <param name="parent"></param>
    public void addOffspring(Animal parent, int parent_index)
    {
        GameObject animal = makeAnimal(parent.transform.position, parent_index);
        animal.GetComponent<Animal>().InheritBrain(parent.GetBrain(), true);
        animals.Add(animal);
    }

    /// <summary>
    /// Remove instance of an animal.
    /// </summary>
    /// <param name="animal"></param>
    public void removeAnimal(Animal animal)
    {
        animals.Remove(animal.transform.gameObject);
        Destroy(animal.transform.gameObject);
    }

    public bool FoodisInZone(float x, float z)
    {

        float y = InverseLerp(customTerrain.getMinHeight(), customTerrain.getMaxHeight(), customTerrain.getInterp(x, z));
        float steep = customTerrain.getSteepness(x, z);
        if ((y > minFoodHeightThreshold) && (y < maxFoodHeightThreshold) && (steep > minFoodSteepnessThreshold) && (steep < maxFoodSteepnessThreshold))
        {
            return true;
        }
        else { return false; }
    }

    public bool isInSpawnZone(float x, float z, int animal_index)
    {

        float y = InverseLerp(customTerrain.getMinHeight(), customTerrain.getMaxHeight(), customTerrain.getInterp(x, z));
        float steep = customTerrain.getSteepness(x, z);
        if ((y > AnimalParameters[animal_index].minHeight) && (y < AnimalParameters[animal_index].maxHeight) && (steep > AnimalParameters[animal_index].minSteepness) && (steep < AnimalParameters[animal_index].maxSteepness))
        {
            return true;
        }
        else { return false; }
    }

    private float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }

    [System.Serializable]
    public class AnimalsSpawnZone
    {
        public GameObject animalPrefab;
        public int animal_index;
        [Range(0, 1)]
        public float minHeight;
        [Range(0, 1)]
        public float maxHeight;
        [Range(0, 90f)]
        public float minSteepness;
        [Range(0, 90f)]
        public float maxSteepness;

        public int getIndexFromPrefab(GameObject prefab)
        {
            return animal_index;
        }
    }

}
