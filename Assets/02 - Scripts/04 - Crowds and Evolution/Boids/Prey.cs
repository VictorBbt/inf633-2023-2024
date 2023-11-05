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
    [HideInInspector]
    public int[] FoodNetworkStruct;
    [HideInInspector]
    public int[] ReactionNetworkStruct;
    [HideInInspector]
    protected SimpleNeuralNet foodBrain = null;
    [HideInInspector]
    protected SimpleNeuralNet reactionBrain = null;


    // Renderer.
    protected Material mat = null;

    // State
    //[HideInInspector]
    //public Vector3 position;
    //[HideInInspector]
    //public Vector3 forward;
    [HideInInspector]
    public Vector3 velocity; // Vector3 but we will project the direction on the terrain
    [HideInInspector]
    public float energy;

    // To update:
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
    public float[] foodVision;
    [HideInInspector]
    public float[] predVision;
    [HideInInspector]
    public bool willBeDestroyed = false; // Used to see if the target of predators that was previously updated are not already destroyed
    bool debug = false;

    // Weights to optimize with NN
    public float targetWeight = 1; // Weight that characterizes the force with which the prey will be steered towards the output of its vision sensor
                                       // i.e, how strong it wants to go to eat (he has to learn that when he is hungry, he has to give a
    public float alignWeight = 1;
    public float cohesionWeight = 1;
    public float separateWeight = 2;

    void Start()
    {
        if (debug)
        {
            Debug.Log("Start Prey");
        }
        // Network: 1 input per receptor, 1 output per actuator.
        foodVision = new float[settings.nEyes];
        FoodNetworkStruct = new int[] { settings.nEyes, 5, 1 };
        predVision = new float[settings.nPredEyes+1];
        ReactionNetworkStruct = new int[] { settings.nPredEyes + 1 , 5, 5, 4 };

        energy = settings.maxEnergy;
        tfm = transform;
        velocity = tfm.forward * (settings.minSpeed + settings.maxSpeed)/2;

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
        if (foodBrain == null)
            foodBrain = new SimpleNeuralNet(FoodNetworkStruct);
        if (reactionBrain == null)
            reactionBrain = new SimpleNeuralNet(ReactionNetworkStruct);
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
        UpdateFoodVision();
        UpdatePredatorVision();
        // 2. Use brain for direction to get to reach the target with the vision sensor, and detect predators + self-hunger to change its behavior

        float[] foodOutput = foodBrain.getOutput(foodVision);

        predVision[predVision.Length - 1] = GetHealth();// We add the sense of hunger to the equation, to choose between go toward food or behave in group
        float[] reactionOutput = reactionBrain.getOutput(predVision);
        targetWeight = reactionOutput[0];
        alignWeight = reactionOutput[1];
        cohesionWeight = reactionOutput[2];
        separateWeight = reactionOutput[3];

        //TODO ADD BRAIN PREDATORS+ENERGY --> NEW COEFFS FOR TARGET, COHESION, ALIGNMENT, ...
        Vector3 acceleration = Vector3.zero;

        // 3. Act using actuators - All the forces are normalized and only the weights give them more importance (maybe to change ?)
        //Debug.Log("Position before moving: " + tfm.position);

        // Compute angle to go towards food
        float angle = (foodOutput[0] * 2.0f - 1.0f) * settings.maxAngle; // How much it turns, limited in +-maxAngle
        //Debug.Log("Angle: " + angle.ToString());
        tfm.Rotate(0.0f, angle, 0.0f); // I want to go there if I were alone (not an external force), but the group influences me (as an external force).
        

        // Add the group influence
        if (numPerceivedFlockmates != 0)
        {
            centreOfFlockmates /= numPerceivedFlockmates;

            Vector3 offsetToFlockmatesCentre = (validTerrainPosition(centreOfFlockmates) - tfm.position);

            Vector3 alignmentForce = SteerTowards(avgFlockHeading) * alignWeight;
            Vector3 cohesionForce = SteerTowards(offsetToFlockmatesCentre) * cohesionWeight; // We want it to be driven only on x and z coordinates, as y is constrained
            Vector3 separationForce = SteerTowards(avgAvoidanceHeading) * separateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += separationForce;
        }

        // Collision Avoidance : if possible collision found, find free direction (no NN, fixed weight for collision avoidance)
        if (IsHeadingForCollision())
        {
            Vector3 collisionAvoidDir = AvoidCollisionDir();
            Vector3 collisionAvoidForce = SteerTowards(collisionAvoidDir) * settings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
        //Debug.Log("Speed: " + speed.ToString());
        velocity = dir * speed; // then fetched by CapsuleController to update the position of our BOID

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
    public void UpdateFoodVision()
    {
        float startingAngle = -((float)settings.nEyes / 2.0f) * settings.stepAngle;
        Vector2 ratio = detailSize / terrainSize;

        //Vector3 borderLine = Quaternion.AngleAxis(startingAngle, Vector3.up) * transform.forward;
        //Debug.DrawRay(transform.position, borderLine * 10f);
        //borderLine = Quaternion.AngleAxis(-startingAngle, Vector3.up) * transform.forward;
        //Debug.DrawRay(tfm.position, borderLine * 10f);

        for (int i = 0; i < settings.nEyes; i++)
        {
            Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startingAngle + (settings.stepAngle * i), 0.0f);
            Vector3 forwardAnimal = rotAnimal * Vector3.forward;
            float sx = tfm.position.x * ratio.x;
            float sy = tfm.position.z * ratio.y;

            foodVision[i] = 1.0f;

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
                    foodVision[i] = distance / settings.maxVision;
                }
            }
        }
    }

    public void UpdatePredatorVision()
    {
        float startingAngle = -((float)settings.nPredEyes/2f) * settings.stepAngle; // the number of eyes to detect preys is 2 times higher

        if (debug)
        {
            Vector3 borderLine = Quaternion.AngleAxis(startingAngle, Vector3.up) * transform.forward;
            Debug.DrawRay(transform.position + new Vector3(0f, 1f, 0f), borderLine * settings.maxVision, Color.blue);
            borderLine = Quaternion.AngleAxis(-startingAngle, Vector3.up) * transform.forward;
            Debug.DrawRay(tfm.position + new Vector3(0f, 1f, 0f), borderLine * settings.maxVision, Color.blue);
        }

        for (int i = 0; i < settings.nPredEyes; i++)
        {
            Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startingAngle + (settings.stepAngle * i), 0.0f);
            Vector3 forwardAnimal = rotAnimal * Vector3.forward;
            if (debug)
            {
                Debug.DrawRay(tfm.position + new Vector3(0f, 1f, 0f), forwardAnimal * 10f);
            }
            
            predVision[i] = 1;

            RaycastHit hit;
            if (Physics.Raycast(tfm.position + new Vector3(0f, 1f, 0f), forwardAnimal, out hit, settings.maxVision, settings.predatorMask))
            {
                predVision[i] = hit.distance / settings.maxVision;
                Debug.Log("Found Predator");
            }
            //Debug.Log("Predvision[i]: " + predVision[i].ToString());
        }
    }

    // Collisions are not treated as a genetic attribute, if we detect collision, we avoid them witha fixed weight
    bool IsHeadingForCollision() 
    {
        RaycastHit hit;
        if (Physics.SphereCast(tfm.position + new Vector3(0f, 1f, 0f), settings.boundsRadius, tfm.forward, out hit, settings.collisionAvoidDst, settings.obstacleMask))
        {
            if (debug)
            {
                Debug.Log("Obstacle in view");
            }
            return true;
        }
        else { }
        return false;
    }

    Vector3 AvoidCollisionDir()
    {
        int numDirections = 10;
        float startAngle = -20f;
        float stepCollisionAngle = 4f;
        for (int i =0; i < numDirections; i++)
        {
            Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startAngle + (stepCollisionAngle * i), 0.0f);
            Vector3 lookDirection = rotAnimal * Vector3.forward;
            Vector3 dir = tfm.TransformDirection(lookDirection);
            Ray ray = new Ray(tfm.position + new Vector3(0f, 1f, 0f), dir);
            if (!Physics.SphereCast(ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask))
            {
                return dir;
            }
        }

        return tfm.forward;
    }

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

    public void InheritFoodBrain(SimpleNeuralNet other, bool mutate)
    {
        foodBrain = new SimpleNeuralNet(other);
        if (mutate)
            foodBrain.mutate(settings.swapRate, settings.mutateRate, settings.swapStrength, settings.mutateStrength);
    }

    public void InheritReactionBrain(SimpleNeuralNet other, bool mutate)
    {
        reactionBrain = new SimpleNeuralNet(other);
        if (mutate)
            foodBrain.mutate(settings.swapRate, settings.mutateRate, settings.swapStrength, settings.mutateStrength);
    }

    public float GetHealth()
    {
        return energy / settings.maxEnergy;
    }

    public SimpleNeuralNet GetFoodBrain()
    {
        return foodBrain;
    }

    public SimpleNeuralNet GetReactionBrain()
    {
        return reactionBrain;
    }

    /// <summary>
    /// y is constrained by the position on x and 
    /// So we project this Vector on the (x, z) plan, normalize it and multiply it by the distance of the previous Vector3
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    Vector3 SteerTowards(Vector3 vector)
    {
        Vector2 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude(v, settings.maxSteerForce);
    }

    Vector3 validTerrainPosition(Vector3 vector)
    {
        float xCoord = vector.x;
        float zCoord = vector.z;
        return new Vector3(xCoord, terrain.getInterp(xCoord, zCoord), zCoord);
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
