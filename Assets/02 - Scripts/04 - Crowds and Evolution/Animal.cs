using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public abstract class Animal : MonoBehaviour
{

    //// Terrain.
    //protected CustomTerrain terrain = null;
    //protected int[,] details = null;
    //protected Vector2 detailSize;
    //protected Vector2 terrainSize;

    //// Genetic alg.
    //protected GeneticAlgo genetic_algo = null;

    //// Brain
    //public int[] networkStruct;
    //protected SimpleNeuralNet brain = null;

    //// Renderer.
    //protected Material mat = null;

    //public abstract void Initialize(ScriptableObject settings);
    //public abstract void InitializeChildren(Animal parent);

    /// <summary>
    /// Calculate distance to the nearest food resource, if there is any.
    /// </summary>
    public abstract void UpdateVision();

    //public void Setup(CustomTerrain ct, GeneticAlgo ga)
    //{
    //    terrain = ct;
    //    genetic_algo = ga;
    //    UpdateSetup();
    //}

    public abstract void UpdateSetup();

    public abstract void InheritBrain(SimpleNeuralNet other, bool mutate);

    //public SimpleNeuralNet GetBrain()
    //{
    //    return brain;
    //}
    public abstract float GetHealth();

    public float InverseLerp(float min, float max, float val)
    {
        return Mathf.Clamp01((val - min) / (max - min));
    }

}
