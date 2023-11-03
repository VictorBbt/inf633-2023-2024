using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Prey : MonoBehaviour
{
    public BoidSettings settings;

    // Terrain.
    protected CustomTerrain terrain = null;
    protected int[,] details = null;
    protected Vector2 detailSize;
    protected Vector2 terrainSize;

    // Genetic alg.
    protected GeneticAlgo genetic_algo = null;

    // Brain
    public int[] networkStruct;
    protected SimpleNeuralNet brain = null;

    // Renderer.
    protected Material mat = null;

    // State
    //[HideInInspector]
    //public Vector3 position;
    //[HideInInspector]
    //public Vector3 forward;
    [HideInInspector]
    Vector2 velocity; // always a Vector2 because the y component is always terrain.getInterp(float x, float z)
    [HideInInspector]
    public float energy;

    // To update:
    Vector3 acceleration;
    [HideInInspector]
    public Vector3 avgFlockHeading;
    [HideInInspector]
    public Vector3 avgAvoidanceHeading;
    [HideInInspector]
    public Vector3 centreOfFlockmates;
    [HideInInspector]
    public int numPerceivedFlockmates;

    // Animal.
    [HideInInspector]
    public Transform tfm;
    // Used by SLague
    //Transform cachedTransform;
    //Material material;
    [HideInInspector]
    public float[] vision;
    bool debug = false;

    // Weights to optimize with NN
    public float TargetWeight = 1; // Weight that characterizes the force with which the prey will be steered towards the output of its vision sensor
                                       // i.e, how strong it wants to go to eat (he has to learn that when he is hungry, he has to give a
    public float alignWeight = 1;
    public float cohesionWeight = 1;
    public float separateWeight = 1;


    /* Other parameters accessible with the Animal.cs inheritance
      // Terrain.
        protected CustomTerrain terrain = null;
        protected int[,] details = null;
        protected Vector2 detailSize;
        protected Vector2 terrainSize;
      // Genetic alg.
        protected GeneticAlgo genetic_algo = null;
      // Brain
        protected int[] networkStruct;
        protected SimpleNeuralNet brain = null;
        // Renderer.
        private Material mat = null;
    */

    void Start()
    {
        if (debug)
        {
            Debug.Log("Start Prey");
        }
        // Network: 1 input per receptor, 1 output per actuator.
        vision = new float[settings.nEyes];
        networkStruct = new int[] { settings.nEyes, 5, 1 };
        energy = settings.maxEnergy;
        tfm = transform;

        // Used by S Lague
        //position = cachedTransform.position;
        //forward = cachedTransform.forward;
        //position = tfm.position;
        //forward = tfm.forward;

        // Renderer used to update animal color.
        // It needs to be updated for more complex models.
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            mat = renderer.material;
    }

    public void UpdateBoid()
    {
        if (debug)
        {
            Debug.Log("Update Boid");
        }
        // In case something is not initialized...
        if (brain == null)
            brain = new SimpleNeuralNet(networkStruct);
        if (terrain == null)
            return;
        if (details == null)
        {
            UpdateSetup();
            return;
        }

        // Update the color of the animal as a function of the energy that it contains.
        if (mat != null)
            mat.color = Color.white * (energy / settings.maxEnergy);

        // 1. Update receptor.
        UpdateVision();
        //TODO ADD VISION FOR PREDATORS
        // 2. Use brain for direction to get to reach the target with the vision sensor (for the moment, just spots details (food))

        float[] output = brain.getOutput(vision); // For the moment, the output is a value between 0 and 1
                                                  //Debug.Log("Output[0]: " +  output[0].ToString());
        //TODO ADD BRAIN PREDATORS+ENERGY --> NEW COEFFS FOR TARGET, COHESION, ALIGNMENT, ...
        Vector3 acceleration = Vector3.zero;

        //Offset to target computed with the UpdateVision (we have angle +distance)

        //if (numPerceivedFlockmates != 0)
        //{
        //    centreOfFlockmates /= numPerceivedFlockmates;

        //    Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - tfm.position);

        //    Vector3 alignmentForce = SteerTowards(avgFlockHeading) * alignWeight;
        //    Vector3 cohesionForce = SteerTowards(offsetToFlockmatesCentre) * cohesionWeight;
        //    Vector3 seperationForce = SteerTowards(avgAvoidanceHeading) * separateWeight;

        //    acceleration += alignmentForce;
        //    acceleration += cohesionForce;
        //    acceleration += seperationForce;
        //}

        // 3. Act using actuators.
        float angle = (output[0] * 2.0f - 1.0f) * settings.maxAngle; // How much it turns
        tfm.Rotate(0.0f, angle, 0.0f); // Change with UpdateBoid()
    }

    public bool UpdatePositionAndEnergy()
    {
        if (debug)
        {
            Debug.Log("Update Pos And Energy");
        }

        // Retrieve animal location in the heighmap
        int dx = (int)(( tfm.position.x / terrainSize.x) * detailSize.x);
        int dy = (int)(( tfm.position.z / terrainSize.y) * detailSize.y);

        // For each frame, we lose lossEnergy

        energy -= settings.lossEnergy;

        // If the animal is located in the dimensions of the terrain and over a grass position (details[dy, dx] > 0), it eats it, gain energy and spawn an offspring.
        if ((dx >= 0) && dx < (details.GetLength(1)) && (dy >= 0) && (dy < details.GetLength(0)) && details[dy, dx] > 0)
        {
            // Eat (remove) the grass and gain energy.
            details[dy, dx] = 0;


            energy += settings.gainEnergy;
            if (energy > settings.maxEnergy)
            {
                energy = settings.maxEnergy;
            }

            // Spawns a new prey when has eaten grass
            //Debug.Log("Has eaten");
            genetic_algo.addPreyOffspring(this);
        }

        // If the energy is below 0, the animal dies.
        if (energy < 0)
        {
            energy = 0.0f;

            genetic_algo.removePrey(this);
            return false;
        }
        return true;
    }



    /// <summary>
    /// Calculate distance to the nearest food resource, if there is any.
    /// </summary>
    public void UpdateVision()
    {
        float startingAngle = -((float)settings.nEyes / 2.0f) * settings.stepAngle;
        Vector2 ratio = detailSize / terrainSize;

        //Vector3 borderLine = Quaternion.AngleAxis(startingAngle, Vector3.up) * transform.forward;
        //Debug.DrawRay(transform.position, borderLine*10f);
        //borderLine = Quaternion.AngleAxis(-startingAngle, Vector3.up) * transform.forward;
        //Debug.DrawRay(tfm.position, borderLine*10f);

        for (int i = 0; i < settings.nEyes; i++)
        {
            Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startingAngle + (settings.stepAngle * i), 0.0f);
            Vector3 forwardAnimal = rotAnimal * Vector3.forward;
            float sx = tfm.position.x * ratio.x;
            float sy = tfm.position.z * ratio.y;

            vision[i] = 1.0f;

            // Interate over vision length.
            for (float distance = 1.0f; distance < settings.maxVision; distance += 0.5f)
            {
                // Position where we are looking at.
                float px = (sx + (distance * forwardAnimal.x * ratio.x));
                float py = (sy + (distance * forwardAnimal.z * ratio.y));

                // Sees at the other size of the map if close to the border
                if (px < 0)
                    px += detailSize.x;
                else if (px >= detailSize.x)
                    px -= detailSize.x;
                if (py < 0)
                    py += detailSize.y;
                else if (py >= detailSize.y)
                    py -= detailSize.y;
                // if we are well on a pixel of the terrain, and  there is food
                if ((int)px >= 0 && (int)px < details.GetLength(1) && (int)py >= 0 && (int)py < details.GetLength(0) && details[(int)py, (int)px] > 0)
                {
                    vision[i] = distance / settings.maxVision;
                    break;
                }
            }
        }
    }

    /* Inherited for the moment but might not be (if we have to keep track of predators, preys,...
    public void Setup(CustomTerrain ct, GeneticAlgo ga)
    {
        terrain = ct;
        genetic_algo = ga;
        UpdateSetup();
    }
    */

    public void Setup(CustomTerrain ct, GeneticAlgo ga)
    {
        terrain = ct;
        genetic_algo = ga;
        UpdateSetup();
    }

    public void UpdateSetup()
    {
        detailSize = terrain.detailSize();
        Vector3 gsz = terrain.terrainSize();
        terrainSize = new Vector2(gsz.x, gsz.z);
        details = terrain.getDetails();
    }

    public void InheritBrain(SimpleNeuralNet other, bool mutate)
    {
        brain = new SimpleNeuralNet(other);
        if (mutate)
            brain.mutate(settings.swapRate, settings.mutateRate, settings.swapStrength, settings.mutateStrength);
    }

    public float GetHealth()
    {
        return energy / settings.maxEnergy;
    }

    public SimpleNeuralNet GetBrain()
    {
        return brain;
    }

    Vector2 SteerTowards(Vector2 vector)
    {
        Vector2 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector2.ClampMagnitude(v, settings.maxSteerForce);
    }

    // Previous framework
    //void Awake()
    //{
    //    if (debug)
    //    {
    //        Debug.Log("4 - Set cached data in Awake Prey");
    //    }
    //    material = transform.GetComponentInChildren<MeshRenderer>().material;
    //    cachedTransform = transform;
    //}

    //public void Initialize(BoidSettings Gsettings)
    //{
    //    if (debug)
    //    {
    //        Debug.Log("5 - In prey Initialize (called from GeneticAlgo)");
    //    }

    //    this.settings = Gsettings;

    //    position = cachedTransform.position;
    //    forward = cachedTransform.forward;

    //    float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
    //    velocity = transform.forward * startSpeed;

    //    // Network: 1 input per receptor, 1 output per actuator.
    //    vision = new float[settings.nEyes];
    //    networkStruct = new int[] { settings.nEyes, 5, 1 };
    //    settings.energy = settings.maxEnergy;
    //    tfm = transform;

    //    // Renderer used to update animal color.
    //    // It needs to be updated for more complex models.
    //    MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
    //    if (renderer != null)
    //        mat = renderer.material;
    //}

    //public override void InitializeChildren(Animal parentAnimal)
    //{
    //    if (debug)
    //    {
    //        Debug.Log("Initializing children");
    //    }
    //    Prey parent = parentAnimal as Prey;

    //    this.settings = parent.settings;

    //    float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
    //    velocity = transform.forward * startSpeed;

    //    // Network: 1 input per receptor, 1 output per actuator.
    //    vision = parent.vision;
    //    networkStruct = parent.networkStruct;
    //    settings.energy = settings.maxEnergy;
    //    tfm = parent.tfm;

    //    // Renderer used to update animal color.
    //    // It needs to be updated for more complex models.
    //    MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
    //    if (renderer != null)
    //        mat = renderer.material;
    //}

}
