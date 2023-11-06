using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Predator : MonoBehaviour
{
    // PREDATOR PARAMETERS //

    // Common settings to all predators
    public PredatorSettings settings;

    // Terrain
    protected CustomTerrain terrain = null;
    protected int[,] details = null;
    protected Vector2 detailSize;
    protected Vector2 terrainSize;

    // Genetic alg.
    protected GeneticAlgo genetic_algo = null;

    // Renderer.
    protected Material mat = null;

    // Animal features
    [HideInInspector]
    public float speed;
    [HideInInspector]
    public float energy;
    [HideInInspector]
    public float urgeToReproduce;
    [HideInInspector]
    public Transform tfm;
    [HideInInspector]
    public float[] vision;
    [HideInInspector]
    public GameObject target;

    // Debugging, or info
    bool debug = false;


    void Start()
    {
        if (debug)
        {
            Debug.Log("Start Predator");
        }

        // Network
        vision = new float[settings.nEyes];

        // Set the initial rates to maximum
        energy = settings.maxEnergy;
        urgeToReproduce = settings.maxReproduce;

        tfm = transform;
        speed = (settings.minSpeed + settings.maxSpeed) / 2;
        target = null;

        // Renderer used to update animal color.
        // It needs to be updated for more complex models.
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            mat = renderer.material;
    }

    public void UpdatePredator()
    {
        if (debug)
        {
            Debug.Log("Update Predator");
        }
        // In case something is not initialized...
        if (terrain == null)
            return;

        float healthState = GetHealth();
        float reproduceState = Mathf.Clamp(GetUrgeToReproduce(), 0.1f, 1f); //Stops searching for mate if is gonna die
        bool wantToEat = healthState <= reproduceState;

        if (debug)
        {
            Debug.Log("Want to eat? " + wantToEat.ToString());
            Debug.Log("HealthState: " + healthState.ToString());
            Debug.Log("ReproduceState: " + reproduceState.ToString());
        }

        // Update the color of the animal as a function of the energy that it contains.
        if (mat != null)
            if (wantToEat)
            {
                mat.color = Color.red * healthState;
            } else
            {
                mat.color = Color.blue * reproduceState;
            }


        Vector3 directionToObjective = tfm.forward;

        // 1. Update Vision        
        if (wantToEat)
        {
            UpdateVision(settings.preyMask); // if wants to eat, only searches for preys
        }
        else
        {
            UpdateVision(settings.predatorMask); // if wants to mate, only searches for partners
        }

        // 2. Taking a direction
        if(target  != null)
        {
            if(!TestReachedTarget()) // if we are close to the target, we keep going forward, else we go towards the objective
                directionToObjective = target.transform.position + target.transform.forward - tfm.position; // We have a target, we follow it
        }
        else
        {
            Quaternion randomRot = Quaternion.Euler(0f, (2f * UnityEngine.Random.value - 1f)*settings.maxAngle, 0); //Random rotation
            directionToObjective = randomRot*tfm.forward;
            if (wantToEat)
            {
                // Linear interpolation of speed: HS=1 : speed = minSpeed (tired), HS=0, speed = maxSpeed (urge to eat)
                speed = settings.maxSpeed + (settings.minSpeed - settings.maxSpeed) * healthState; // if has eaten, tired so speed is low, else high
            }
            else
            {
                speed = settings.maxSpeed + (settings.minSpeed - settings.maxSpeed) * reproduceState; // same for reproduction
            }
        }

        if (debug)
        {
            Debug.DrawRay(tfm.position + new Vector3(0f, 1f, 0f), directionToObjective * 10, Color.yellow);
        }

        // Collision Avoidance : if possible collision found, find free direction, but not changing the target
        if (IsHeadingForCollision())
        {
            directionToObjective = AvoidCollisionDir();
        }

        // Compute speed if there is no target in view
        speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
        if (debug)
        {
            Debug.Log("Speed = " + speed.ToString());
        }

        float angle = Vector3.Angle(tfm.forward, directionToObjective);
        tfm.Rotate(0f, angle, 0f); // Rotate of the right angle
        // then fetched by CubeController to update the position of our BOID
    }

    public void UpdateReprodAndEnergy()
    {
        if (debug)
        {
            Debug.Log("Update Reprod And Energy");
        }

        // For each frame, we lose lossEnergy

        energy -= settings.lossEnergy;
        urgeToReproduce -= settings.urgeReproduceRate;


        // If the energy is below 0, the animal dies.
        if (energy < 0)
        {
            energy = 0.0f;

            genetic_algo.removePredator(this);
        }
    }

    private bool TestReachedTarget()
    {
        // if has encountered an Objective, do the appropriate action
        if((tfm.position - target.transform.position).magnitude <= 6f)
        {
            if (debug)
            {
                Debug.Log("Reached target");
            }

            if (target.CompareTag("Prey"))
            {
                if (debug)
                {
                    Debug.Log("Predator Eating Prey");
                }
                genetic_algo.removePrey(target.gameObject.GetComponentInParent<Prey>()); // Removing the prey from the simulation
                energy = Mathf.Min(energy + settings.gainEnergy, settings.maxEnergy); // Gaining energy
                target = null; 
           
            }
            else if (target.CompareTag("Predator"))
            {
                if (debug)
                {
                    Debug.Log("Predator gave birth");
                }
                genetic_algo.addPredatorOffspring(this); // Spawn a predator, possible upgrade: give brains to predators and mix them whe reproducing
                target.gameObject.GetComponentInParent<Predator>().urgeToReproduce = Mathf.Max(urgeToReproduce + settings.gainReproduce, settings.maxReproduce); // Affects the urge to reproduce of the target  also
                urgeToReproduce = Mathf.Min(urgeToReproduce + settings.gainReproduce, settings.maxReproduce);
                target = null;
            }
            return true;

        } else if ((tfm.position - target.transform.position).magnitude > settings.maxTrackingDistance)
        {
            if (debug)
            {
                Debug.Log("Target has escaped and is out of reached - Predator gives up");
            }
            target = null;
            return true;

        }
        return false; // We are still following  the target
    }

    public void UpdateVision(LayerMask priorityMask)
    {
        if(target == null) // Do not look when we have a target
        {
            float startingAngle = -((float)settings.nEyes / 2f) * settings.stepAngle;

            if(debug)
            {
                Vector3 borderLine = Quaternion.AngleAxis(startingAngle, Vector3.up) * transform.forward;
                Debug.DrawRay(transform.position + new Vector3(0f, 1f, 0f), borderLine * settings.maxVision, Color.yellow);
                borderLine = Quaternion.AngleAxis(-startingAngle, Vector3.up) * transform.forward;
                Debug.DrawRay(tfm.position + new Vector3(0f, 1f, 0f), borderLine * settings.maxVision, Color.yellow);
            }

            for (int i = 0; i < settings.nEyes; i++)
            {
                Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startingAngle + (i * settings.stepAngle), 0.0f); 
                Vector3 forwardAnimal = rotAnimal * Vector3.forward; // Direction  of the ray

                if (debug)
                {
                    Debug.DrawRay(tfm.position + new Vector3(0f, 1f, 0f), forwardAnimal * 10f);
                }
                
                vision[i] = 0;

                RaycastHit hit;
                //  Using SphereCast to spot objects
                if (Physics.SphereCast(tfm.position + new Vector3(0f, 1f, 0f), 1f, forwardAnimal, out hit, settings.maxVision, priorityMask))
                {
                    if (debug)
                    {
                        Debug.Log("Predator found objective within vision");
                    }
                    target = hit.collider.gameObject; // set target
                    speed = settings.maxSpeed; // hunting mechanics: speed is max
                }
            }
        }

    }

    // Collisions are not treated as a genetic attribute, if we detect collision, we avoid them witha fixed weight - SAME AS PREY
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

    Vector3 AvoidCollisionDir()  // SAME AS PREY.CS
    {
        int numDirections = 10;
        float startAngle = -20f;
        float stepCollisionAngle = 4f;
        for (int i = 0; i < numDirections; i++)
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

    // Helper functions to avoid non setup features 
    public void Setup(CustomTerrain ct, GeneticAlgo ga)
    {
        terrain = ct;
        genetic_algo = ga;
        UpdateSetup();
    }

    public void UpdateSetup()
    {
        Vector3 gsz = terrain.terrainSize();
        terrainSize = new Vector2(gsz.x, gsz.z);
    }

    // Getters
    public float GetHealth()
    {
        return energy / settings.maxEnergy;
    }

    public float GetUrgeToReproduce()
    {
        return urgeToReproduce / settings.maxReproduce;
    }
}
