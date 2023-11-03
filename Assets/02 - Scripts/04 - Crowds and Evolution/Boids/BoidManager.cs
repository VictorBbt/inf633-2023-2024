using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoidManager : MonoBehaviour
{
    //const int threadGroupSize = 1024;
    //public BoidSettings settings;
    //public ComputeShader compute;
    //Prey[] boids;

    //bool debug = false;


    ////void Start()
    ////{
    ////    if (debug)
    ////    {
    ////        Debug.Log("5 - Start of Boid Manager");
    ////    }

    ////    boids = FindObjectsOfType<Prey>();
    ////    if (debug)
    ////    {
    ////        Debug.Log("N preys found:" + boids.Length.ToString());
    ////    }

    ////    foreach (Prey b in boids)
    ////    {
    ////        b.Initialize(settings);
    ////    }

    ////}

    //void Update()
    //{
    //    //ga = GetComponent<GeneticAlgo>();
    //    //boids = new Prey[ga.animals.Count];
    //    //if (debug)
    //    //{
    //    //    //Debug.Log("In BoidManager Update");
    //    //    Debug.Log("N animals=" + ga.animals.Count);
    //    //}
    //    //Debug.Log("N animals=" + ga.animals.Count);
    //    //for (int i=0; i < ga.animals.Count; i++)
    //    //{


    //    //    GameObject animal = ga.animals[i];
    //    //    Prey p = animal.GetComponent<Prey>();

    //    //    boids[i] = p;
    //    //    // Will destroy the animal if it has not enough energy
    //    //    boids[i].UpdatePositionAndEnergy();
    //    //}

    //    ga = GetComponent<GeneticAlgo>();
    //    boids = new Prey[ga.animals.Count];
    //    if (debug)
    //    {
    //        //Debug.Log("In BoidManager Update");
    //        Debug.Log("N animals=" + ga.animals.Count);
    //    }
    //    Debug.Log("N animals=" + ga.animals.Count);
    //    for (int i = 0; i < ga.animals.Count; i++)
    //    {


    //        GameObject animal = ga.animals[i];
    //        Prey p = animal.GetComponent<Prey>();

    //        boids[i] = p;
    //        // Will destroy the animal if it has not enough energy
    //        boids[i].UpdatePositionAndEnergy();
    //    }


    //    Prey[] survivorBoids = boids.Where(item => item != null).ToArray();
    //    Debug.Log("N survivors:" + survivorBoids.Length.ToString());
    //    // While here, we do not take in account the boids that were killed, or those that spawn because we are in the same frame
    //    // So the number of boids is still, so we compute the info below as if they were still here
    //    // It is ok as we only compute vectors, and coefficents and we don't access the transform, or gameObject that we potentially destroyed at the previous line
    //    if ((survivorBoids != null) && (survivorBoids.Length !=0))
    //    {

    //        int numBoids = survivorBoids.Length;
    //        var boidData = new BoidData[numBoids];

    //        for (int i = 0; i < survivorBoids.Length; i++)
    //        {
    //            boidData[i].position = survivorBoids[i].position;
    //            boidData[i].direction = survivorBoids[i].forward;
    //        }

    //        var boidBuffer = new ComputeBuffer(numBoids, BoidData.Size);
    //        boidBuffer.SetData(boidData);

    //        compute.SetBuffer(0, "boids", boidBuffer);
    //        compute.SetInt("numBoids", survivorBoids.Length);
    //        compute.SetFloat("viewRadius", settings.perceptionRadius);
    //        compute.SetFloat("avoidRadius", settings.avoidanceRadius);

    //        int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
    //        compute.Dispatch(0, threadGroups, 1, 1);

    //        boidBuffer.GetData(boidData);

    //        for (int i = 0; i < survivorBoids.Length; i++)
    //        {
    //            survivorBoids[i].avgFlockHeading = boidData[i].flockHeading;
    //            survivorBoids[i].centreOfFlockmates = boidData[i].flockCentre;
    //            survivorBoids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
    //            survivorBoids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

    //            survivorBoids[i].UpdateBoid();
    //        }

    //        boidBuffer.Release();
    //    }
    //}

    //public struct BoidData
    //{
    //    public Vector3 position;
    //    public Vector3 direction;

    //    public Vector3 flockHeading;
    //    public Vector3 flockCentre;
    //    public Vector3 avoidanceHeading;
    //    public int numFlockmates;

    //    public static int Size
    //    {
    //        get
    //        {
    //            return sizeof(float) * 3 * 5 + sizeof(int);
    //        }
    //    }
    //}
}