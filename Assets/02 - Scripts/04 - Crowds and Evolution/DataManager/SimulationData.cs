using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SimulationData
{

    public float[] targetWeights;
    public float[] alignWeights;
    public float[] cohesionWeights;
    public float[] separateWeights;
    public int[] numPreys;
    public int[] numPredators;
    public SimulationData(GeneticAlgo algo)
    {

        targetWeights = algo.targetWeights.ToArray();
        alignWeights = algo.alignWeights.ToArray();
        cohesionWeights = algo.cohesionWeights.ToArray();
        separateWeights = algo.separateWeights.ToArray();
        numPreys = algo.numPreys.ToArray();
        numPredators = algo.numPredators.ToArray();
        Debug.Log("IMPORTANT: Size s of the arrays is  " + targetWeights.Length.ToString());
        Debug.Log("     Types are: f[s], f[s], f[s], f[s], i[s], i[s]");
    }
}
