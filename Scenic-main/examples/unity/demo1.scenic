from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))

behavior opponent1Behavior(pt):
    first_possession = False
    try:
        do InterceptBall(ball)
        do Idle() 

    interrupt when (not first_possession and self.gameObject.ballPossession and ego.gameObject.pause == False):
        print("1st interrupt")
        do Idle() for 1 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 0.5 seconds
        first_possession = True
        do ApproachGoal(pt)

    interrupt when (first_possession and self.gameObject.ballPossession and ego.gameObject.pause == False):
        print("2nd interrupt")
        do ShootBall(goal.position, "center-middle")
        abort
    
behavior opponent2Behavior(pt):
    try:
        do Idle()
    interrupt when (self.gameObject.ballPossession):
        do Idle() for 0.5 seconds
        do GroundPassFast(pt.position)
        do Idle() for 0.5 seconds
        abort

behavior coachBehavior():
    opponent1_first_ball_possession = False
    opponent2_first_ball_possession = False
    self_possession = False

    do Idle() until closeToBall(opponent1)
    # do Idle() until hasBallPosession(opponent1)
    do Pause()
    do Speak("Say \"" + "Once the opponent takes the ball, don't rush out. position yourself in between the player and the goal post." + "\"")
    do Idle() for 7 seconds
    do Unpause()
    do Idle() until hasBallPosession(opponent1)
    do moveToLookAtBall(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, goal]).weighted({opponent1: 1, goal: 1}), [opponent1, goal], Speed(SpeedInit.MAGNITUDE)) until hasBallPosession(opponent2)


    do Pause()
    do Speak("Say \"" + "When the opponent passes the ball to its teammate, again position yourself in between the player and the goal to close the shot angle." + "\"")
    do Idle() for 7 seconds
    do Unpause()
    # do Pause()
    # do Speak("Say \"" + "When the opponent passes the ball to its teammate, again position yourself in between the player and the goal to close the shot angle." + "\"")
    # do Idle() for 7 seconds
    # do Unpause()
    do moveToLookAtBall(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent2, goal]).weighted({opponent2: 0.5, goal: 1}), [opponent2, goal], Speed(SpeedInit.MAGNITUDE)) until (hasBallPosession(opponent2) and distance from opponent1 to goal < 5)
    # do moveToLookAtBall(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, opponent2, goal]).weighted({opponent1: 0.05, opponent2: 0.9, goal: 1}), [opponent1, opponent2, goal], Speed(SpeedInit.MAGNITUDE)) until (hasBallPosession(opponent2) and distance from opponent1 to goal < 5)

    # do Pause()
    # do Speak("Say \"" + "" + "\"")
    # do Idle() for 7 seconds
    # do Unpause()
    dest = new Point ahead of goal by Range(-1,-1.5)
    do getTo(dest)
    do Idle()

    # try:
    #     do Idle()
    # interrupt when (hasBallPosession(opponent1)):
    #     # do Pause()
    #     # do Speak("once the opponent takes the ball, position yourself ahead of the player to defend the goal.")
    #     # do Unpause()
    #     print("1st")
    #     do moveTo(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, opponent2, goal]).weighted({opponent1: 1, opponent2: 1, goal: 1}), MovingStyle.RUN, Speed(SpeedInit.MAGNITUDE))
    #     opponent1_first_ball_possession = True
    # interrupt when (opponent1_first_ball_possession and not hasBallPosession(opponent1)):
    #     print("2nd")
    #     do moveTo(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, opponent2, goal]).weighted({opponent1: 0.2, opponent2: 0.9, goal: 1}), MovingStyle.RUN, Speed(SpeedInit.MAGNITUDE))
    # interrupt when ((opponent2_first_ball_possession or hasBallPosession(opponent2)) and opponent1.speed > 1):
    #     print("3rd")
    #     opponent2_first_ball_possession = True
    #     do moveTo(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, opponent2, goal]).weighted({opponent1: 0.9, opponent2: 0.2, goal: 1}), MovingStyle.RUN, Speed(SpeedInit.MAGNITUDE))
    # interrupt when (hasBallPosession(self) or self_possession):
    #     self_possession = True
    #     print("final")
    #     do WaitFor(30)
    #     do Idle()

def closeToBall(player: Player) -> bool:
    if (distance from player to ball < 1.5):
        return True

ego = new DefensePlayer at (5, Range(0,0.1), 0), 
        with behavior coachBehavior()

goal = new Goal behind ego by Range(2.9,3), facing away from ego
pt = new Point offset by (Range(-3,3), Range(-1,0))

opponent1 = new Player offset by (Range(-4,0), Range(6,10)),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent1"

pt1 = new Point offset by (Range(3,5), Range(0,2))
pt2 = new Point offset by (Range(-5,-3), Range(0,2))
op2_pos = Uniform(pt1, pt2)

opponent2 = new Player at op2_pos, 
                    facing toward opponent1,
                    with behavior opponent2Behavior(pt),
                    with name "opponent2"

ball = new Ball ahead of opponent1 by Normal(2,1)

require (distance from op2_pos to opponent1.position) > 10
terminate when (ego.gameObject.stopButton)