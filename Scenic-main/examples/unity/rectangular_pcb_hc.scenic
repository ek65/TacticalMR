
# Scenario 02 (Hardcode):
# Rectangular Press-Cover-Balance formation

#    0    0.   0    0   
# /--------------------\
# |       H            |
# |  O         O       |
# |                 O  |
# |                    |
# \--------------------/
#    0    0    0    0    

#            |
#            V

#    0    0    0.   0    +8
# /--------------------\
# |            O       | +4
# |       H         O  |  0
# |  O                 | -4
# |                    | -8
# \--------------------/
#    0    0    0    0    -10
#
#    -4   0    +4   +8

# H: user (defendant)
# O: defendants
# 0: attackers
# .: football
# -, |, /, \ : defending boundary

from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

class Defendant(Player):
    friendly: True
    action: "idle"
    closeLeft: None
    closeRight: None

class Opponent(Player):
    friendly: False
    close: []

def teamPossession(self: Player) -> bool:
    objects = simulation().objects

    for obj in objects:

        if isinstance(self, Defendant):
            if (isinstance(obj, Defendant) and obj.gameObject.ballPossession):
                return True

        elif isinstance(self, Opponent):
            if (isinstance(obj, Opponent) and obj.gameObject.ballPossession):
                return True

    return False


def closerTo(origin, type: type) -> Object:
    objects = simulation().objects
    minDist = float('inf')
    closer = None

    for obj in objects:
        if not isinstance(obj, type):
            continue

        d = distance from origin to obj

        if 0 < d < minDist:
            minDist = d
            closer = obj

    return closer


behavior opponentBehavior():
    try:
        if self.debug:
            print("default")
        do MoveTo(self.tacticalPosition)
        do Idle()
    interrupt when self == closerTo(football, Opponent):
        if self.debug:
            print("going for ball")
        do InterceptBall(football)
        do Idle() 
    interrupt when teamPossession(self):
        if self.debug:
            print("team has ball")
        do MoveTo(self.tacticalPosition)
        do Idle()
    interrupt when self.gameObject.ballPossession and distance from self to self.tacticalPosition < 0.5:
        if self.debug:
            print("passing ball")
        do Idle() for Range(2, 4) seconds
        available = Uniform(*self.close)
        print(f"decided to pass ball to {available.name}")
        do GroundPassFast(available.position)


behavior defendantBehavior(front: Opponent):
    try: 
        self.action = "idle"
        do MoveTo(self.tacticalPosition)
        do Idle()
    interrupt when (self.closeRight and self.closeRight.action == "cover") or (self.closeLeft and self.closeLeft.action ==  "cover"): # Balance condition
        self.action = "balance"
        do MoveTo(self.tacticalPosition + Vector((football.position.x - self.position.x) * 0.4, -2 - abs(football.position.x - self.position.x) * 0.2, 0)) for 0.1 seconds
    interrupt when (self.closeRight and self.closeRight.action == "press") or (self.closeLeft and self.closeLeft.action ==  "press"): # Cover condition
        self.action = "cover"
        do MoveTo(self.tacticalPosition + Vector((football.position.x - self.position.x) * 0.4, 0, 0)) for 0.1 seconds
    interrupt when front == closerTo(football, Opponent): # Press condition
        self.action = "press"
        do MoveTo(self.tacticalPosition + Vector((football.position.x - self.position.x) * 0.4, 4, 0)) for 0.1 seconds

# Try interrupt while moving to (no idling after)

football = new Ball at (0, 6, 0)
ego = new Human at (-20, 20, 0)

opponent_a = new Opponent at (-8, 8, 0),
                    with behavior opponentBehavior()
opponent_a.tacticalPosition = (-8, 8, 0)
opponent_a.debug = True

opponent_b = new Opponent at (0, 8, 0),
                    with behavior opponentBehavior()
opponent_b.tacticalPosition = (0, 8, 0)

opponent_c = new Opponent at (8, 8, 0),
                    with behavior opponentBehavior()
opponent_c.tacticalPosition = (8, 8, 0)

opponent_d = new Opponent at (16, 8, 0),
                    with behavior opponentBehavior()
opponent_d.tacticalPosition = (16, 8, 0)

opponent_a.close = [opponent_b]
opponent_b.close = [opponent_a, opponent_c]
opponent_c.close = [opponent_b, opponent_d]
opponent_d.close = [opponent_c]

defendant_a = new Defendant at (-8, 0, 0),
                    with behavior defendantBehavior(opponent_a)
defendant_a.tacticalPosition = (-8, 0, 0)

defendant_b = new Defendant at (0, 0, 0),
                    with behavior defendantBehavior(opponent_b)
defendant_b.tacticalPosition = (0, 0, 0)

defendant_c = new Defendant at (8, 0, 0),
                    with behavior defendantBehavior(opponent_c)
defendant_c.tacticalPosition = (8, 0, 0)

defendant_d = new Defendant at (16, 0, 0),
                    with behavior defendantBehavior(opponent_d)
defendant_d.tacticalPosition = (16, 0, 0)

defendant_a.closeRight = defendant_b
defendant_b.closeLeft = defendant_a
defendant_b.closeRight = defendant_c
defendant_c.closeLeft = defendant_b
defendant_c.closeRight = defendant_d
defendant_d.closeLeft = defendant_c

defendant_a.name = "Defendant A"
defendant_b.name = "Defendant B"
defendant_c.name = "Defendant C"
defendant_d.name = "Defendant D"

opponent_a.name = "Opponent A"
opponent_b.name = "Opponent B"
opponent_c.name = "Opponent C"
opponent_d.name = "Opponent D"

terminate when (ego.gameObject.stopButton)