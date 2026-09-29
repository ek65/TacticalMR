using System.Collections;
using Fusion;
using UnityEngine.SceneManagement;

/*
 * Scene manager for laptop mode (GameMode.Single). The desktop scene is not in Build Settings, so its build index is -1,
 * which Fusion treats as "no scene" and never registers the scene's NetworkObjects (their RPCs then throw
 * "Behaviour not initialized"). This adopts the scene that is already open instead of loading one by build index.
 */
public class LaptopModeSceneManager : NetworkSceneManagerDefault
{
    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
    {
        yield return null;
        finished(FindNetworkObjects(SceneManager.GetActiveScene(), disable: true));
    }
}
