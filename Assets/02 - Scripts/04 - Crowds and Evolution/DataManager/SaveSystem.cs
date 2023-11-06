using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Writes the data declared in the class simulation data in a text file
/// </summary>
public static class SaveSystem
{
    public static void SaveSimulationData(GeneticAlgo algo)
    {
        SimulationData simData = new SimulationData(algo);
        string textPath = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/results-latest.txt"; // TO CHANGE
        int simSize = simData.targetWeights.Length;
        StreamWriter writer = new StreamWriter(textPath, true);
        writer.WriteLine("resultsTargetWeights;resultsAlignWeights;resultsCohesionWeights;resultsSeparateWeights;resultsNumPreys;resultsNumPredators"); // Manually change the name of columns
        for (int i=0; i< simSize; i++)
        {
            writer.WriteLine(simData.targetWeights[i].ToString() + ";" + simData.alignWeights[i].ToString() + ";" + simData.cohesionWeights[i].ToString() + ";" + simData.separateWeights[i].ToString() + ";" + simData.numPreys[i].ToString() + ";" + simData.numPredators[i].ToString());
        }
        writer.Close();
        Debug.Log("Results stored in " + textPath);
    }
    //TODO Load sim data (e.g the weights of NN to resume a simulation
}
