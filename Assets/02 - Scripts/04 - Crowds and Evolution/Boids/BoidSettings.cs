using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attached to the BOID manager
[CreateAssetMenu]
public class BoidSettings : ScriptableObject
{
    // These parameters are defined for each new animal as some can not be implemented for several animals,
    // and must be changeable from the unity inspector
    [Header("Animal parameters")]
    public float swapRate = 0.01f;
    public float mutateRate = 0.01f;
    public float swapStrength = 10.0f;
    public float mutateStrength = 0.5f;
    public float maxAngle = 10.0f;
    public float minSpeed = 2;
    public float maxSpeed = 5;
    public float perceptionRadius = 2.5f; // Radius where the preys can view their peers
    public float avoidanceRadius = 1;
    public float maxSteerForce = 3;

    [Header("Energy parameters")] // Equivalent to "urge to eat - Add the same for water and reproduction
    public float maxEnergy = 10.0f;
    public float lossEnergy = 0.1f;
    public float gainEnergy = 10.0f;

    [Header("Sensor - Food Vision")]
    public float maxVision = 20.0f;
    public float stepAngle = 10.0f;
    public int nEyes = 5;

    [Header("Collisions")]
    public LayerMask obstacleMask;
    public float boundsRadius = .27f;
    public float avoidCollisionWeight = 10;
    public float collisionAvoidDst = 5;

    // TO ADD POTENTIALLY
    // private int gender
    // [Range(0,1])
    // private float BabyScale
    // Male colour
    // Female colour

}