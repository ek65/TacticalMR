from scenic.simulators.unity.actions import *

# Language: scenic (python)
# This file defines all shared scenic behaviors. In order to use any behavior defined
# here, add "from scenic.simulators.vr.behaviors import *" to the top of the scenic file

behavior Idle():
    while True:
        # print(distance from ego to self)
        take IdleAction()

behavior ShootBall(vec : Vector, string : str):
    take ShootAction(vec, string, "Shoot Ball")
    take StopAction()

behavior InterceptBall(ball):
    while (distance from self to ball) > 0.5:
        # print(distance from self to ball)
        take MoveToAction(ball.position, "Intercept Ball")
    take StopAction()

behavior GroundPassFast(vec : Vector):
    take GroundPassFastAction(vec, "Pass Ball")
    take StopAction()

behavior MoveTo(v, status=""):
    dist = 1000
    while not (dist < 0.5):
        take MoveToAction(v, status)
        dist = distance from self to v

behavior ApproachGoal(v):
    dist = 1000
    while not (dist < 0.5):
        take MoveToAction(v, "Approach Goal")
        dist = distance from self to v

behavior DribbleTo(v):
    dist = 1000
    while not (dist < 0.5):
        take DribbleToAction(v)
        dist = distance from self to v

behavior SetPlayerSpeed(s):
    take SetPlayerSpeedAction(s)

behavior Print(o):
    take PrintAction(o)

behavior Speak(input : str):
    take SpeakAction(input)
    take StopAction()

behavior Pause():
    take PauseAction()
    take StopAction()