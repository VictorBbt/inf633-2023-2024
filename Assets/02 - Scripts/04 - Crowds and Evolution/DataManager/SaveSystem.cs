using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void SaveSimulationData(GeneticAlgo algo)
    {


        SimulationData simData = new SimulationData(algo);

        BinaryFormatter formatter = new BinaryFormatter();

        string path = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsTargetWeights.bin";
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, simData.targetWeights); 
        stream.Close();
        

        path = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsAlignWeights.bin";
        stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, simData.alignWeights);
        stream.Close();

        path = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsCohesionWeights.bin";
        stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, simData.cohesionWeights);
        stream.Close();

        path = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsSeparateWeights.bin";
        stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, simData.separateWeights);
        stream.Close();

        path = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsNumPreys.bin";
        stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, simData.numPreys);
        stream.Close();

        path = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsNumPredators.bin";
        stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, simData.numPredators);
        stream.Close();

        string textPath = "/Users/victorbarberteguy/Desktop/Master2/INF633/SimulationResults/resultsNoPredators.txt";
        int simSize = simData.targetWeights.Length;
        StreamWriter writer = new StreamWriter(textPath, true);
        writer.WriteLine("resultsTargetWeights;resultsAlignWeights;resultsCohesionWeights;resultsSeparateWeights;resultsNumPreys;resultsNumPredators");
        for (int i=0; i< simSize; i++)
        {
            writer.WriteLine(simData.targetWeights[i].ToString() + ";" + simData.alignWeights[i].ToString() + ";" + simData.cohesionWeights[i].ToString() + ";" + simData.separateWeights[i].ToString() + ";" + simData.numPreys[i].ToString() + ";" + simData.numPredators[i].ToString());
        }
        writer.Close();

    }
    //TODO Load sim data and plot it
}
