using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Predator : MonoBehaviour
{
    public PredatorSettings settings;

    // Terrain.
    protected CustomTerrain terrain = null;
    protected int[,] details = null;
    protected Vector2 detailSize;
    protected Vector2 terrainSize;

    // Genetic alg.
    protected GeneticAlgo genetic_algo = null;

    // Renderer.
    protected Material mat = null;

    //[HideInInspector]
    //public Vector3 velocity; // Vector3 but we will project the direction on the terrain
    [HideInInspector]
    public float speed;
    [HideInInspector]
    public float energy;
    [HideInInspector]
    public float urgeToReproduce;

    // Animal.
    [HideInInspector]
    public Transform tfm;

    [HideInInspector]
    public float[] vision;
    [HideInInspector]
    public GameObject target;
    bool debug = false;


    void Start()
    {
        if (debug)
        {
            Debug.Log("Start Predator");
        }
        // Network: 1 input per receptor, 1 output per actuator.
        vision = new float[settings.nEyes];

        energy = settings.maxEnergy;
        urgeToReproduce = settings.maxReproduce;

        tfm = transform;
        speed = (settings.minSpeed + settings.maxSpeed) / 2;
        target = null;
        //velocity = tfm.forward * (settings.minSpeed + settings.maxSpeed) / 2;

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
        float reproduceState = GetUrgeToReproduce();
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

        // 1. Update receptor:  searches for top priority objective --> Maybe NN or search for 2nd priority objective if doesn't find
        Vector3 directionToObjective = tfm.forward;

        // If we have a target, we won't go into this
        if (wantToEat)
        {
            UpdateVision(settings.preyMask); // if wants to eat, only searches for preys
        }
        else
        {
            UpdateVision(settings.predatorMask); // if wants to mate, only searches for partners
        }

        if(target  != null)
        {
            if(!TestReachedTarget()) // if we are close to the target, we keep going forward, else we go towards the objective
                directionToObjective = target.transform.position - tfm.position; // We have a target, we follow it
        }
        else
        {
            Quaternion randomRot = Quaternion.Euler(0f, (2f * UnityEngine.Random.value - 1f), 0); //Random rotation between +-30 degrees
            directionToObjective = randomRot*tfm.forward;
        }

        if (debug)
        {
            Debug.DrawRay(tfm.position, directionToObjective * 10, Color.yellow);
        }
        

        // Collision Avoidance : if possible collision found, find free direction, but not changing our target
        if (IsHeadingForCollision())
        {
            directionToObjective = AvoidCollisionDir();
        }

        // Compute speed
        if (wantToEat)
        {
            // Linear interpolation of speed: HS=1 : speed = minSpeed (tired), HS=0, speed = maxSpeed (urge to eat)
            speed = settings.maxSpeed + (settings.minSpeed -settings.maxSpeed )* healthState; // if has eaten, tired so speed is low, else high
        }
        else
        {
            speed = settings.maxSpeed + (settings.minSpeed - settings.maxSpeed) * reproduceState; // same for reproduction
        }

        speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
        if (debug)
        {
            Debug.Log("Speed = " + speed.ToString());
        }

        //Debug.DrawRay(transform.position, directionToObjective * 10f, Color.red);
        float angle = Vector3.Angle(tfm.forward, directionToObjective);
        tfm.Rotate(0f, angle, 0f);
        // then fetched by CapsuleController to update the position of our BOID
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
        // Potential pb: if too much preys, can collide with them and eat them whereas it was not intentional
        if((tfm.position - target.transform.position).magnitude < 2f)
        {
            if (debug)
            {
                Debug.Log("Reached target");
            }
            if (target.CompareTag("Prey"))
            {
                Prey toBeEat = target.gameObject.GetComponent<Prey>();
                if(toBeEat != null)
                {
                    if (debug)
                    {
                        Debug.Log("Predator Ate Prey");
                    }
                    Debug.Log("Attempting eating prey: name " + target.name);
                    genetic_algo.removePrey(target.gameObject.GetComponent<Prey>());
                    energy = Mathf.Max(energy + settings.gainEnergy, settings.maxEnergy);
                    target = null;
                    Debug.Log("End of the loop for eating");
                }
            }
            else if (target.CompareTag("Predator")) // Maybe compare IDs if predator collides with itself
            {
                if (debug)
                {
                    Debug.Log("Predator gave birth");
                }
                Debug.Log("Predator gave birth");
                genetic_algo.addPredatorOffspring(this);
                target.gameObject.GetComponent<Predator>().urgeToReproduce = Mathf.Max(urgeToReproduce + settings.gainReproduce, settings.maxReproduce);
                urgeToReproduce = Mathf.Max(urgeToReproduce + settings.gainReproduce, settings.maxReproduce);
                target = null;
            }
            return true;
        }
        return false;
    }

    public void UpdateVision(LayerMask priorityMask)
    {
        if(target == null)
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
                Vector3 forwardAnimal = rotAnimal * Vector3.forward;

                if (debug)
                {
                    Debug.DrawRay(tfm.position + new Vector3(0f, 1f, 0f), forwardAnimal * 10f);
                }
                
                vision[i] = 0;

                RaycastHit hit;
                if (Physics.Raycast(tfm.position+new Vector3(0f, 1f, 0f), forwardAnimal, out hit, settings.maxVision, priorityMask))
                {
                    if (debug)
                    {
                        Debug.Log("Predator found objective within vision");
                    }
                    target = hit.collider.gameObject;
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

    Vector3 AvoidCollisionDir()  // SAME AS PREY
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

    public float GetHealth()
    {
        return energy / settings.maxEnergy;
    }

    public float GetUrgeToReproduce()
    {
        return urgeToReproduce / settings.maxReproduce;
    }
}
