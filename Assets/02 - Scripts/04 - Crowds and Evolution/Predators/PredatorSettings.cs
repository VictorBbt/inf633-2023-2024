using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attached to the BOID manager
[CreateAssetMenu]
public class PredatorSettings : ScriptableObject
{
    // These parameters are defined for each new animal as some can not be implemented for several animals,
    // and must be changeable from the unity inspector
    [Header("Animal parameters")]
    public float maxAngle = 10.0f;
    public float minSpeed = 10f; 
    public float maxSpeed = 20f;

    [Header("Energy parameters")] // Equivalent to "urge to eat - Add the same for water and reproduction
    public float maxEnergy = 10.0f;
    public float lossEnergy = 0.1f;
    public float gainEnergy = 10.0f;

    [Header("Reproduce parameters")] // Equivalent to "urge to eat - Add the same for water and reproduction
    public float maxReproduce = 20.0f;
    public float urgeReproduceRate = 0.1f;
    public float gainReproduce = 10.0f;

    [Header("Sensor - Vision")]
    public LayerMask predatorMask;
    public LayerMask preyMask;
    public float maxVision = 30f;
    public float stepAngle = 5.0f;
    public int nEyes = 20;

    [Header("Collisions")]
    public LayerMask obstacleMask;
    public float boundsRadius = 2f;
    public float avoidCollisionWeight = 10;
    public float collisionAvoidDst = 5;

    // TO ADD POTENTIALLY
    // private int gender
    // [Range(0,1])
    // private float BabyScale
    // Male colour
    // Female colour

}