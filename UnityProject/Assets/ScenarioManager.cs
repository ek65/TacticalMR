using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    public enum ScenarioType
    {
        Soccer,
        Factory,
    }

    public ScenarioType currentScenario;

    void Start()
    {
        SwitchScenario(currentScenario);
    }

    public void SwitchScenario(ScenarioType newScenario)
    {
        currentScenario = newScenario;

        switch (currentScenario)
        {
            case ScenarioType.Soccer:
                EnableSoccerScenario();
                break;

            case ScenarioType.Factory:
                EnableFactoryScenario();
                break;

            default:
                Debug.LogWarning("Unknown scenario selected!");
                break;
        }
    }

    private void EnableSoccerScenario()
    {
        Debug.Log("Soccer scenario activated!");
    }

    private void EnableFactoryScenario()
    {
        Debug.Log("Factory scenario activated!");
    }
    
    private void EnableCustomScenario2()
    {
        Debug.Log("Custom scenario 2 activated!");
    }
}