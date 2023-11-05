using UnityEngine;
using UnityEditor;

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
    }
}