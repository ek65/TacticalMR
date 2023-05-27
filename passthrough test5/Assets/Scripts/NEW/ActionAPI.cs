using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionAPI : MonoBehaviour
{
    [SerializeField] Animator animator;

    // Bollean 
    //-> Dribble 6 
    //-> Movement 2 
    
    // Method with Paramenter
    // Movmeent (Gameobject ,init , final) -> Fromn One pos to another
    // Dribble (Gameobject ,init , final) -> Fromn One pos to another Dribble 
    
    void Ground_Pass_Slow()
    {
        animator.SetBool("GroundPassSlow", true);
    }

    void CloseAnimation(string setFalse)
    {
        animator.SetBool(setFalse, false);
    }

    // Start is called before the first frame update
    void Start()
    {
        Ground_Pass_Slow();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
