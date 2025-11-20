using System.Collections;
using UnityEngine;
using Fusion;

public class TestAction : MonoBehaviour
{
    public ActionAPI actionAPI;

    public GameObject target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // wait for 1 seconds
        StartCoroutine(Speak());
    }

    // Update is called once per frame
    void Update()
    {
        // actionAPI.MoveToPos(new Vector3(0,0,1));
    }
    
    // public override void FixedUpdateNetwork() {
    //     if (!Object.HasStateAuthority) return; // host-only writes in Shared/Host mode
    //     targetChild.position = new Vector3(0f, 0f, 1f);
    //     // If you’re moving relative to the parent, use localPosition instead.
    //     // targetChild.localPosition = new Vector3(0f, 0f, 1f);
    // }

    public IEnumerator TakeAction()
    {
        yield return new WaitForSeconds(1);
        actionAPI.MoveToPos(new Vector3(0,0,1));
    }
    
    public IEnumerator Speak()
    {
        yield return new WaitForSeconds(1);
        actionAPI.Speak("Wait until Coach raises hand to signal package complete");
    }
}
