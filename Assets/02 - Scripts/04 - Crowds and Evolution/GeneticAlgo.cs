using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GeneticAlgo : MonoBehaviour
{

    [Header("Genetic Algorithm parameters")]
    public int preyPopulationSize = 100;
    public int predatorPopulationSize = 5;

    [Header("Prey settings")]
    public GameObject preyPrefab;
    public BoidSettings preySettings;
    public ComputeShader computeBoid;
    const int threadGroupSize = 1024;

    [Header("Prey settings")]
    public GameObject predatorPrefab;
    public PredatorSettings predSettings;

    Prey[] boids;

    [Header("Dynamic elements")]
    public float vegetationGrowthRate = 1.0f;
    [Range(0, 1)]
    public float minFoodHeightThreshold;
    [Range(0, 1)]
    public float maxFoodHeightThreshold;
    [Range(0, 90f)]
    public float minFoodSteepnessThreshold;
    [Range(0, 90f)]
    public float maxFoodSteepnessThreshold;
    [HideInInspector]
    public float currentGrowth;

    // make this public to get the list of animals in the Boid manager
    [HideInInspector]
    public List<Prey> preys; // Make a list of GameObject List<GameObject>[] where the index is the index of the AnimalParameters, when using the System.Serializable feature
    [HideInInspector]
    public List<Predator> predators;
    protected Terrain terrain;
    protected CustomTerrain customTerrain;
    protected float width;
    protected float height;

    // Unity crashes if we pre compute this.
    //protected List<(int, int)> possibleGrassCoords;
    //protected List<(int, int)> possiblePreySpawnCoords;

    bool debug = false;

    [HideInInspector]
    public List<int> numPredators;
    [HideInInspector]
    public List<int> numPreys;
    [HideInInspector]
    public List<float> targetWeights;
    [HideInInspector]
    public List<float> alignWeights;
    [HideInInspector]
    public List<float> cohesionWeights;
    [HideInInspector]
    public List<float> separateWeights;
    [HideInInspector]
    private float meanTargetWeight;
    [HideInInspector]
    private float meanAlignWeight;
    [HideInInspector]
    private float meanCohesionWeight;
    [HideInInspector]
    private float meanSeparateWeight;

    void Start()
    {
        // Retrieve terrain.
        terrain = Terrain.activeTerrain;
        customTerrain = GetComponent<CustomTerrain>();
        width = terrain.terrainData.size.x;
        height = terrain.terrainData.size.z;

        // Initialize terrain growth.
        currentGrowth = 0.0f;

        // Initialize animals array. Only prey for the moment
        if (debug)
        {
            Debug.Log("2 - Spawning " + preyPopulationSize.ToString() + " preys in GeneticAlgo ");
            Debug.Log("     Spawning " + predatorPopulationSize.ToString() + " predators in GeneticAlgo");
        }

        preys = new List<Prey>();
        for (int i = 0; i < preyPopulationSize; i++)
        {
            Prey prey = makePrey();
            preys.Add(prey);
            //animal.GetComponent<Prey>().Initialize(Gsettings);
        }

        predators = new List<Predator>();
        for (int i = 0; i < predatorPopulationSize; i++)
        {
            Predator pred = makePredator();
            predators.Add(pred);
        }
        // Maybe in a static class ?
        //possibleGrassCoords = getFoodZone(); // To save computing time, we precompute the possible indexes where grass can appear (coordinates of heightmap)
        //                                     // instead of sampling at random on the terrain and then looking if it is in Zone
        //possiblePreySpawnCoords = getSpawnZone(AnimalParameters[0].animal_index);
    }

    void Update()
    {
        if (debug)
        {
            Debug.Log("In Genetic Algo update");
        }
        
        // Keeps animal to a minimum.
        while (preys.Count < preyPopulationSize / 2f)
        {
            Prey newPrey = makePrey();
            preys.Add(newPrey);
        }

        while (predators.Count < predatorPopulationSize / 2f)
        {
            Predator newPred = makePredator();
            predators.Add(newPred);
        }

        // Update grass elements/food resources.
        updateResources();

        // Update position of all the boids by computing their relative positions
        updateBoids();

        // Update position of all the predators
            // Predators are updated after boids, so the target can sometimes die by hunger and is reached at the same time by the predator
        updatePredators();


        int numAliveBoids = preys.Count;
        int numAlivePredators = predators.Count;
        numPreys.Add(numAliveBoids);
        numPredators.Add(numAlivePredators);
        customTerrain.debug.text = "N? preys: " + numAliveBoids.ToString() + " / N? predators: " + numAlivePredators.ToString();
    }

    /// <summary>
    /// Method to place grass or other resource in the terrain.
    /// </summary>
    public void updateResources()
    {
        Vector2 detail_sz = customTerrain.detailSize();
        int[,] details = customTerrain.getDetails();

        //int objectCount = customTerrain.getObjectCount();
        currentGrowth += vegetationGrowthRate;
        while (currentGrowth > 1.0f)
        {
            int x = (int)(UnityEngine.Random.value * detail_sz.x);
            int y = (int)(UnityEngine.Random.value * detail_sz.y);
            details[y, x] = 1;
            currentGrowth -= 1.0f;
        }
        customTerrain.saveDetails();
    }

    public void updateBoids()
    {
        meanTargetWeight = 0f;
        meanAlignWeight = 0f;
        meanCohesionWeight =0f;
        meanSeparateWeight = 0f;

        boids = preys.ToArray();
        int numBoids = boids.Length;
        // Compute Buffer approach
        if ((boids != null) && (boids.Length != 0))
        {
            var boidData = new BoidData[numBoids];
            for (int i = 0; i < boids.Length; i++)
            {
                boidData[i].position = boids[i].tfm.position;
                boidData[i].direction = boids[i].tfm.forward;
            }
            var boidBuffer = new ComputeBuffer(numBoids, BoidData.Size);
            boidBuffer.SetData(boidData);
            computeBoid.SetBuffer(0, "boids", boidBuffer);
            computeBoid.SetInt("numBoids", boids.Length);
            computeBoid.SetFloat("viewRadius", preySettings.perceptionRadius);
            computeBoid.SetFloat("avoidRadius", preySettings.avoidanceRadius);
            //computeBoid.SetFloat("maxVisionCos", Mathf.Cos(settings.stepAngle));
            /* In buffer
            // float maxVisionCos;
            // float scalarProduct = dot(normalize(boids[id.x].position), normalize(boidB.direction));
            // float cosAngle = cos(scalarProduct);
            // && (scalarProduct > 0) && (cosAngle < maxVisionCos) )
            */
            int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
            computeBoid.Dispatch(0, threadGroups, 1, 1);
            boidBuffer.GetData(boidData);
            for (int i = 0; i < boids.Length; i++)
            {
                boids[i].avgFlockHeading = boidData[i].flockHeading;
                boids[i].centreOfFlockmates = boidData[i].flockCentre;
                boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

                boids[i].UpdateBoid();
                meanTargetWeight += boids[i].targetWeight;
                meanAlignWeight += boids[i].alignWeight;
                meanCohesionWeight += boids[i].cohesionWeight;
                meanSeparateWeight += boids[i].separateWeight;
            }
            boidBuffer.Release();

            if (debug)
            {
                Debug.Log("mean: targetW: " + (meanTargetWeight / numBoids).ToString());
                Debug.Log("mean alignW: " + (meanAlignWeight / numBoids).ToString());
                Debug.Log("mean cohesionW: " + (meanCohesionWeight / numBoids).ToString());
                Debug.Log("mean separateW: " + (meanSeparateWeight / numBoids).ToString());
                Debug.Log("______________________________________");
            }

            Debug.Log("mean: targetW: " + (meanTargetWeight / numBoids).ToString());
            Debug.Log("mean alignW: " + (meanAlignWeight / numBoids).ToString());
            Debug.Log("mean cohesionW: " + (meanCohesionWeight / numBoids).ToString());
            Debug.Log("mean separateW: " + (meanSeparateWeight / numBoids).ToString());
            Debug.Log("______________________________________");
            targetWeights.Add(meanTargetWeight / numBoids);
            alignWeights.Add(meanAlignWeight / numBoids);
            cohesionWeights.Add(meanCohesionWeight / numBoids);
            separateWeights.Add(meanSeparateWeight / numBoids);
        }





        //for (int id = 0; id < numBoids; id++)
        //{
        //    for (int indexB = 0; indexB < numBoids; indexB++)
        //    {
        //        if (id != indexB)
        //        {
        //            Prey boidB = boids[indexB];
        //            Vector3 offset = boidB.tfm.position - boids[id].tfm.position;
        //            float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;

        //            if (sqrDst < settings.perceptionRadius * settings.perceptionRadius) //Add also if is visible, i.e angle entre foirward et position relative < maxVision
        //            {
        //                boids[id].numPerceivedFlockmates += 1;
        //                boids[id].avgFlockHeading += boidB.tfm.forward;
        //                boids[id].centreOfFlockmates += boidB.tfm.position;

        //                if (sqrDst < settings.avoidanceRadius * settings.avoidanceRadius)
        //                {
        //                    if (sqrDst != 0)
        //                    {
        //                        boids[id].avgAvoidanceHeading -= offset / sqrDst;
        //                    }
        //                    else { boids[id].avgAvoidanceHeading -= offset / 0.0001f; }

        //                }
        //            }
        //        }
        //        boids[id].UpdateBoid();
        //    }

        //}

        for (int i = 0; i < boids.Length; i++)
        {

            // Will destroy the animal if it has not enough energy
            bool isAlive = boids[i].UpdatePositionAndEnergy();
            //if (isAlive)
            //{
                
            //}
            
        }

        //No need to do this as boids will be updated at the beginning of the next update
        //Prey[] survivorBoids = boids.Where(item => item != null).ToArray();
    }

    public void updatePredators()
    {
        for(int i = 0; i<predators.Count; i++)
        {
            predators[i].UpdatePredator();
            predators[i].UpdateReprodAndEnergy();
        }
    }

    public Prey makePrey(Vector3 position)
    {
        if (debug)
        {
            Debug.Log("3 - Instantiating prey in makePrey at position: " + position.ToString());

        }
        GameObject animal = Instantiate(preyPrefab, transform);
        Prey newPrey = animal.GetComponent<Prey>();
        newPrey.transform.position = position;
        newPrey.Setup(customTerrain, this);
        SetupPreyCharac(newPrey);
        newPrey.transform.Rotate(0.0f, UnityEngine.Random.value * 360.0f, 0.0f);
        return newPrey;
    }

    public Predator makePredator(Vector3 position)
    {
        if (debug)
        {
            Debug.Log("3 - Instantiating predator in makePredator at position: " + position.ToString());
        }

        GameObject animal = Instantiate(predatorPrefab, transform);
        Predator newPredator = animal.GetComponent<Predator>();
        newPredator.transform.position = position;
        newPredator.Setup(customTerrain, this);
        SetupPredatorCharac(newPredator);
        newPredator.transform.Rotate(0.0f, UnityEngine.Random.value * 360.0f, 0.0f);
        return newPredator;
    }

    public Prey makePreyFromParent(Vector3 position, Quaternion rotation)
    {
        if (debug)
        {
            Debug.Log("3 - Instantiating Animal in makeAnimal at position: " + position.ToString());
        }
        GameObject animal = Instantiate(preyPrefab, transform);
        Prey newPrey = animal.GetComponent<Prey>();
        newPrey.transform.position = position;
        newPrey.Setup(customTerrain, this);
        SetupPreyCharac(newPrey);
        newPrey.transform.rotation = rotation;
        return newPrey;
    }

    public Prey makePrey()
    {
        Vector3 scale = terrain.terrainData.heightmapScale;
        float x = UnityEngine.Random.value * width;
        float z = UnityEngine.Random.value * height;
        float y = customTerrain.getInterp(x / scale.x, z / scale.z);
        return makePrey(new Vector3(x, y, z));
    }

    public Predator makePredator()
    {
        Vector3 scale = terrain.terrainData.heightmapScale;
        float x = UnityEngine.Random.value * width;
        float z = UnityEngine.Random.value * height;
        float y = customTerrain.getInterp(x / scale.x, z / scale.z);
        return makePredator(new Vector3(x, y, z));
    }

    public void addPreyOffspring(Prey parent)
    {
        if (debug)
        {
            Debug.Log("Parent Prey is spawning a child");
        }
        //Debug.Log("Parent is spawning a child");
        Prey newPrey = makePreyFromParent(parent.transform.position, parent.transform.rotation);
        newPrey.InheritFoodBrain(parent.GetFoodBrain(), true);
        newPrey.InheritReactionBrain(parent.GetReactionBrain(), true);
        //newPrey.InitializeChildren(parent);
        preys.Add(newPrey);
    }

    public void addPredatorOffspring(Predator parent)
    {
        if (debug)
        {
            Debug.Log("Parent Predator is spawning a child");
        }
        //Debug.Log("Parent is spawning a child");
        Predator newPredator = makePredator(parent.transform.position); // Not necessarily the same rotation as in preys
        predators.Add(newPredator);
    }

    public void removePrey(Prey p)
    {
        p.willBeDestroyed = true; // Avoid being eaten twice in one Update loop, or create exception
        preys.Remove(p);
        GameObject correspondingObj = p.gameObject;
        Destroy(correspondingObj);
        if (debug)
        {
            Debug.Log("Destroyed Prey");
        }
        Debug.Log("Destroyed Prey");
    }

    public void removePredator(Predator pred)
    {
        predators.Remove(pred);
        GameObject correspondingObj = pred.gameObject;
        Destroy(correspondingObj);
        if (debug)
        {
            Debug.Log("Destroyed Predator");
        }
    }

    public void SetupPreyCharac(Prey prey)
    {
        prey.foodVision = new float[preySettings.nEyes];
        prey.FoodNetworkStruct = new int[] { preySettings.nEyes, 5, 1 };
        prey.predVision = new float[preySettings.nPredEyes + 1];
        prey.ReactionNetworkStruct = new int[] { preySettings.nPredEyes+1, 5, 5, 4};
        prey.energy = preySettings.maxEnergy;
        prey.tfm = prey.transform;
        prey.velocity = prey.tfm.forward * (preySettings.minSpeed + preySettings.maxSpeed) / 2;
    }

    public void SetupPredatorCharac(Predator predator)
    {
        predator.vision = new float[predSettings.nEyes];

        predator.energy = predSettings.maxEnergy;
        predator.urgeToReproduce = predSettings.maxReproduce;

        predator.tfm = transform;
        predator.speed = (predSettings.minSpeed + predSettings.maxSpeed) / 2;
        //predator.velocity = predator.tfm.forward * (predSettings.minSpeed + predSettings.maxSpeed) / 2;
    }


    // Setup for ComputeBuffer
    public struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public static int Size
        {
            get
            {
                return sizeof(float) * 3 * 5 + sizeof(int);
            }
        }
    }

    /// FUNCTIONS THAT CAN UPGRADE THE MODEL BUT THAT ARE NOT USED TO SAVE COMPUTATION AND MEMORY ///

    /// <summary>
    /// Method to instantiate an animal prefab. It must contain the animal.cs class attached.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    //public GameObject makeAnimal(Vector3 position, int index)
    //{
    //    if (debug)
    //    {
    //        Debug.Log("3 - Instantiating Animal in makeAnimal at position: " + position.ToString());
    //    }

    //    GameObject animal = Instantiate(AnimalParameters[index].animalPrefab, transform);
    //    animal.GetComponent<Animal>().Setup(customTerrain, this);
    //    animal.transform.position = position;
    //    animal.transform.Rotate(0.0f, UnityEngine.Random.value * 360.0f, 0.0f);
    //    return animal;
    //}

    /// <summary>
    /// If makeAnimal() is called without position, we randomize it on the terrain.
    /// </summary>
    /// <returns></returns>
    //public GameObject makeAnimal(int index)
    //{
    //    Vector3 scale = terrain.terrainData.heightmapScale;
    //    float x = UnityEngine.Random.value * width;
    //    float z = UnityEngine.Random.value * height;

    //    float y = customTerrain.getInterp(x / scale.x, z / scale.z);

    //    // Saving computation time
    //    //while (!isInSpawnZone(x / scale.x, z / scale.z,y, index))
    //    //{
    //    //    x = UnityEngine.Random.value * width;
    //    //    z = UnityEngine.Random.value * height;
    //    //    y = customTerrain.getInterp(x / scale.x, z / scale.z);
    //    //}
    //    return makeAnimal(new Vector3(x, y, z), index);
    //}

    /// <summary>
    /// Method to add an animal inherited from another. It spawns where the parent was.
    /// </summary>
    /// <param name="parent"></param>
    //public void addOffspring(Animal parent, int parent_index)
    //{
    //    if (debug)
    //    {
    //        Debug.Log("Parent is spawning a child");
    //    }
    //    Debug.Log("Parent is spawning a child");
    //    GameObject animal = makeAnimal(parent.transform.position, parent_index);
    //    animal.GetComponent<Animal>().InheritBrain(parent.GetBrain(), true);
    //    animal.GetComponent<Animal>().InitializeChildren(parent);
    //    animals.Add(animal);
    //}

    /// <summary>
    /// Remove instance of an animal.
    /// </summary>
    /// <param name="animal"></param>
    //public void removeAnimal(Animal animal)
    //{

    //    animals.Remove(animal.transform.gameObject);
    //    Destroy(animal.transform.gameObject);
    //    if (debug)
    //    {
    //        Debug.Log("Destroyed Animal");
    //    }

    //}

    //public bool FoodisInZone(float x, float z)
    //{

    //    float y = InverseLerp(customTerrain.getMinHeight(), customTerrain.getMaxHeight(), customTerrain.getInterp(x, z));
    //    float steep = customTerrain.getSteepness(x, z);
    //    if ((y > minFoodHeightThreshold) && (y < maxFoodHeightThreshold) && (steep > minFoodSteepnessThreshold) && (steep < maxFoodSteepnessThreshold))
    //    {
    //        return true;
    //    }
    //    else { return false; }
    //}

    //public bool isInSpawnZone(float x, float z, float h, int animal_index)
    //{
    //    float y = InverseLerp(customTerrain.getMinHeight(), customTerrain.getMaxHeight(), h);
    //    float steep = customTerrain.getSteepness(x, z);
    //    if ((y > AnimalParameters[animal_index].minHeight) && (y < AnimalParameters[animal_index].maxHeight) && (steep > AnimalParameters[animal_index].minSteepness) && (steep < AnimalParameters[animal_index].maxSteepness))
    //    {
    //        return true;
    //    }
    //    else { return false; }
    //}

    //private float InverseLerp(float min, float max, float val)
    //{
    //    return Mathf.Clamp01((val - min) / (max - min));
    //}

    //[System.Serializable]
    //public class AnimalsSpawnZone
    //{
    //    public GameObject animalPrefab;
    //    public int animal_index;
    //    [Range(0, 1)]
    //    public float minHeight;
    //    [Range(0, 1)]
    //    public float maxHeight;
    //    [Range(0, 90f)]
    //    public float minSteepness;
    //    [Range(0, 90f)]
    //    public float maxSteepness;

    //    public ScriptableObject settings;


    //}
}
