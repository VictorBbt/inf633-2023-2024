using UnityEngine;
using UnityEditor;

/// <summary>
/// Ths add buttons in the inspector GUI for the genetic algorithm to save the data and to spawn predators suddenly in the simulation
/// </summary>
[CustomEditor(typeof(GeneticAlgo))]
public class SaveSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GeneticAlgo algo = (GeneticAlgo)target;
        if (GUILayout.Button("Save Data"))
        {
            SaveSystem.SaveSimulationData(algo);
            Debug.Log("Data of the running simulation has been saved");
        }

        if (GUILayout.Button("Spawn Predators"))
        {
            for(int i = 0; i < 30; i++)
            {
                Predator p = algo.makePredator();
                algo.predators.Add(p);
                
            }
            Debug.Log("Spawned 30 predators");
        }
    }
}