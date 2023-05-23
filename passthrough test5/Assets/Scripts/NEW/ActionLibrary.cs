using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionLibrary : MonoBehaviour
{

    [SerializeField] Animator PlayersAnimator;
    [SerializeField] float ballOffSetDistance = 0.01f;
    [SerializeField] float playerRunnimgSpeed = 2f;
    [SerializeField] float timeDuration = 5f;
    [SerializeField] AnimationClip receiveAnimationClip;

    bool BallPossesed = false;

    // Start is called before the first frame update
    void Awake()
    {
        PlayersAnimator = GetComponent<Animator>();
    }
    
    /// <summary>
    /// Moving a Player From One Point to Another
    /// </summary>
    /// <param name="init"></param>
    /// <param name="final"></param>
    public void MoveFromOnePositionToAnother(Vector3 init,Vector3 final)
    {
        StartCoroutine(Lerp(init,final, false));
    }

    /// <summary>
    /// Moving  Player from his Position to towards The Ball
    /// </summary>
    /// <param name="init"></param>
    /// <param name="final"></param>
    /// <param name="towardsBall"></param>
    public void MoveFromOnePositionToAnother(Vector3 init,Vector3 final,bool towardsBall)
    {
        Debug.Log(final);
        StartCoroutine(Lerp(init, final, towardsBall));
    }

    public void MoveFromOnePositionToAnother(Vector3 final, bool towardsBall)
    {
        Vector3 init = transform.position;
        StartCoroutine(Lerp(init, final, towardsBall));
    }

    #region Coroutines

    /// <summary>
    /// Moving Player from One Position to Another Lerping MOtion
    /// </summary>
    /// <param name="init">Initial Position</param>
    /// <param name="final">FInal Position</param>
    /// <param name="towardsBall">Movind towards Ball or Not</param>
    /// <returns></returns>
    IEnumerator Lerp(Vector3 init, Vector3 final, bool towardsBall)
    {
        final.y = init.y; // Don't Update Y Coordinate
        transform.LookAt(final);  // for Look At Ball 

        float timeElapsed = 0;
        float distance = Vector3.Distance(init, final);
        timeDuration = 2f * distance / playerRunnimgSpeed;
        Debug.Log("Distance - " + distance + " || TimeDuration - " + timeDuration);
        
        if (towardsBall)
        {
            // Update Final posion wrt Balloffset
            //final -= new Vector3(ballOffSetDistance, 0, ballOffSetDistance);

            while (timeElapsed < timeDuration)
            {
                float t = timeElapsed / timeDuration;
                t = t * t * (3f - 2f * t);
                float UpdatedDistance = Vector3.Distance(init, transform.position);

                #region For Mathematical Reference
                // parabola Equation if used (x-5)^2 = -4(25/8)(y-2) => y = -2/25(x^2 - 10x)  (in this Max Distance 10)
                // parabola Equation if used (x-(d/2))^2 = -4((d/2)^2/8)(y-2)) => y = (8 (d x - x^2))/d^2
                #endregion

                float transitionValue = (8f * (distance * (UpdatedDistance) - Mathf.Pow(UpdatedDistance, 2)) / Mathf.Pow(distance, 2));

                if (!SceneManager2v1.instance.isBallPosessed)
                {
                    PlayersAnimator.SetFloat("VelZ", transitionValue);

                    transform.position = Vector3.Lerp(init, final, t);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                    
                }
                else
                {
                    // For sync the motion, delay is given here 
                    yield return new WaitForSeconds(0.2f);
                    break;
                }
            }
            // Detect the player has ball or Not
            if (BallPossesed)
            {
                transform.LookAt(SceneManager2v1.instance.UserPlayer.transform);
                PlayersAnimator.SetBool("Pass", true);
                yield return new WaitForSeconds(0.45f);
                var SoccerBall = SceneManager2v1.instance.SoccerBall;
                Vector3 BallPassDirection = SceneManager2v1.instance.UserPlayer.transform.position - SoccerBall.transform.position;
                float speed = 0.2f;
                SoccerBall.GetComponent<Rigidbody>().AddForce(BallPassDirection.x*speed,0,BallPassDirection.z*speed,ForceMode.Impulse);
            }
            else // IF Player dont have a Ball then Player stop at its position
            {
                PlayersAnimator.SetFloat("VelZ", 0f);
            }
        }
    }
    #endregion

    #region Collision Detection

    /// <summary>
    /// Detect Player is Collide with the ball or not
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SoccerBall"))
        {
            Debug.Log("In region to pass ball");
            SceneManager2v1.instance.isBallPosessed = BallPossesed = true;
        }
    }
    #endregion
}