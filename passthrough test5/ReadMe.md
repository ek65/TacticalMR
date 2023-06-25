## Public APIs

### Translation using Blend Trees

| API | Description |
|-----|-------------|
| MoveFromOnePositionToAnother(GameObject selfPlayer, Vector3 destinationPosition, bool lookAt = true) | Turns and moves the selfPlayer towards the destinationPosition. |
| DribbleFromOnePositionToAnother(GameObject selfPlayer, Vector3 destinationPosition) | Turns and moves the selfPlayer towards the destinationPosition, while dribbling. |
| BallHeaderShoot(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for heading the ball towards destinationPosition. ballProjectileHeight defines the height of ball projectile. |

### Movment Singleton Actions

| API | Description |
|-----|-------------|
| ReceiveBall(GameObject selfPlayer, Vector3 receiveFrom) | Action for receiving the ball coming from the recieveFrom position. |
| TackleBall(GameObject selfPlayer, Vector3 tackleFrom) | Action for tackling the ball at the tackleFrom position. |

### Dribbling Singleton Actions

| API | Description |
|-----|-------------|
| GroundPassSlow(GameObject selfPlayer, Vector3 destinationPosition) | Action for a slow grounded pass. |
| GroundPassFast(GameObject selfPlayer, Vector3 destinationPosition) | Action for a fast grounded pass. | 
| AirPass(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for air pass. |
| ChipLeft(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for chipping left. |
| ChipRight(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for chipping right. |
| ChipFront(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for chipping in front. |
| Shoot(GameObject selfPlayer, Vector3 destinationPosition, string destinationZone) | Action for shooting the ball towards an empty space, or into the goal post. |
| BallThrow(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for throwing the ball. |

### GoalKeeper Singleton Actions

| API | Description |
|-----|-------------|
| BodyBlockLeftSide(GameObject selfPlayer) | Body Block action by goalkeeper towards left. |
| BodyBlockRightSide(GameObject selfPlayer) | Body Block action by goalkeeper towards right. |
| CatchGroundBall(GameObject selfPlayer) | Catching the ball from the ground. |
| CatchBallInTheAir(GameObject selfPlayer) | Catch ball in the air. |
| CatchBallInFront(GameObject selfPlayer) | Catch ball coming from front. |
| DropKickShot(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Kick ball towards the destinationPosition after dropping it. |
| OverHandThrow(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Action for throwing the ball. |
| RollingBallPass(GameObject selfPlayer, Vector3 destinationPosition) | Pass the ball by rolling. |
| PlacingAndLongPass(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight) | Place the ball and then kick strongly. |
| PlacingAndShortPass(GameObject selfPlayer, Vector3 destinationPosition) | Place the ball and kick slowly. |

## Variables and Parameters

### Constant Parameters

| Variable Name | Data Type | Description |
|---------------|-----------|-------------|
| playerRunningSpeed | float | Specifies speed of the playe during movement or dribbling. |
| timeDuration | float | To calculate the time taken for player to move to the target position. |
| goalWidth | float | the bradness of the goal post. This is used to calculate the coordinated of the destinationZone. |
| soccerBall | GameObject | A reference to the ball in the scene. |
| stopMovement | bool | To specify whether the player should move or stop. |
| transitionTo | string | Used as a helper string to increase code interpretability, whit transitions in animations. |
| forceFactor | float | The universal value governing the strength of the force for ball. |
| weakPassForce | float | Relative force value for slow pass. |
| strongPassForce | float | Relative force value for strong pass. |
| airPassForce | float | Relative force value for air pass. |
| chipForce | float | Relative force value for chip pass. |
| shootForce | float | Relative force value for shooting. |
| rotationDuration | float | Used for LookTowards() function. Specifies how fast the player turn towards the destination. |

### Variables used in public functions as parameters

| Variable Name | Data Type | Description |
|---------------|-----------|-------------|
| selfPlayer | GameObject | Reference the the player gameobject on whom the action is targeted |
| destinationPosition | Vector3 | Specifies the destination position for the player or the ball |
| lookAt | bool | Determines whether the player should look at the destination position. Ture by default, of not passed explicitly. |
| ballProjectileHeight | string | Specifies the desired height for the ball trajectory. |
| destinationZone | string | Specifies the target zone for shooting the ball. The nine zones are : { empty, left-top, left-middle, left-bottom, center-top, center-middle, center-bottom, right-top, right-middle, right-bottom };
| receiveFrom | Vector3 | The position from where the ball is being passed to the current player. |
| tackleFrom | Vector3 | The position where the current player is supposed to tackle the ball. |

# Framework for accessing the APIs.

1. The APIs are present in the script _**ActionAPIs.cs**_ as the public functions. All the private functions are the helper functions for these APIs.
2. This script is referenced through an empty gameobject in the scene - _**APIManager**_. No other instance of this script should be made.
3. A script _**SceneHandler.cs**_ is created to manage the behaviours in the scene. This script is attached to the empty gameobject _**SceneHandler**_ in the scene.
4. The variables in this script can be increased to take the reference of the players, golakeepers, and other assets (if any) in the scene.
5. SceneHandler.cs also takes reference to the ActionAPI.cs script through APIManager gameobject.
6. No script is attached to player, ball, or goalkeeper to access the APIs.
7. Finally, the reference to gameobjects and ActionAPIs.cs can be used to call the APIs.

8. Sample Code:

          public class SceneHandler : MonoBehaviour
          {
              public GameObject apiManager;
              public GameObject goalPost;
              public GameObject playerOne;
              public GameObject goalKeeper;
              public GameObject soccerBall;
          
              ActionAPI actionAPIs;
          
              void Start()
              {
                  actionAPIs = apiManager.GetComponent<ActionAPI>();
          
                  // example for calling the APIs
                  actionAPIs.Shoot(playerOne, goalPost.transform.position, "left-bottom");
              }
          }

















