
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
    isHuman: False
    explained1: False
    explained2: False
    explained3: False
    team : "blue"

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
        do Idle() for Range(2, 3) seconds
        if self.gameObject.ballPossession:
            available = Uniform(*self.close)
            print(f"decided to pass ball to {available.name}")
            do GroundPassFast(available.position)

balanceExplanation = "Alright, let's talk about balancing on the field. When you see your teammate covering another player, that's your cue to balance things out. It's like maintaining equilibrium in the defense. So, when you're balancing, your role is to position yourself behind the player who's covering and in front of any teammates of the attacker who might receive a potential pass. This setup helps us anticipate and intercept any long passes that could come our way. It's about staying in control and ready to react to whatever the opposition throws at us. Got it?"
coverExplanation = "Alright, here's the scoop on covering. When you're on the field and you see your teammate pressing another player, that's your signal to step in and cover. Imagine it like a safety net for your teammate's move. So, when you're covering, your job is to position yourself behind your teammate who is pressing and between the two closest attackers. This setup ensures you're ready if the attacker tries to bypass your teammate who's pressing, or if they decide to pass the ball to a teammate instead. It's all about being that extra layer of defense, ready to intercept or block any moves they make. Makes sense?"
pressExplanation = "Alright, here's the deal. When you're on the field and you notice you're the one closest to the opponent with the ball, that's your cue to press. It's like seeing an opening and seizing it. You move in, keeping yourself within a meter of them, and position yourself at a 45-degree angle. This setup allows you to react quickly if they try to make a move past you. Pressing is all about seizing the right moment and disrupting their play. Clear enough?"

#TODO: Add is humna to enable talk, pasue and resume
behavior defendantBehavior(front: Opponent):
    try: 
        self.action = "idle"
        do SetPlayerSpeed(5.0)
        do MoveTo(self.tacticalPosition, "Idling")
        do Idle()
    interrupt when (self.closeRight and self.closeRight.action == "cover") or (self.closeLeft and self.closeLeft.action ==  "cover"): # Balance condition
        if self.isHuman and self.explained1 == False: 
            do explain(40, balanceExplanation)
            self.explained1 = True
        self.action = "balance"
        do SetPlayerSpeed(5.0)
        do MoveTo(self.tacticalPosition + Vector((football.position.x - self.position.x) * 0.6, -2 - abs(football.position.x - self.position.x) * 0.2, 0), "Balancing") for 0.1 seconds
    interrupt when (self.closeRight and self.closeRight.action == "press") or (self.closeLeft and self.closeLeft.action ==  "press"): # Cover condition
        if self.isHuman and self.explained2 == False:
            do explain(40,coverExplanation)
            self.explained2 = True
        self.action = "cover"
        do SetPlayerSpeed(7.5)
        do MoveTo(self.tacticalPosition + Vector((football.position.x - self.position.x) * 0.6, 0, 0), "Covering") for 0.1 seconds
    interrupt when pressCondition(self, [front, football]):
        if self.isHuman and self.explained3 == False:
            do explain(35, pressExplanation)
            self.explained3 = True
        final = lambda x: self.tacticalPosition + Vector((x[1].position.x - self.position.x) * 0.6, x[0].position.y - 2.5, 0)
        do SetPlayerSpeed(20.0)
        do positionAction(final, 10, [front, football])

behavior explain(time, string):
    do Idle() for 1 seconds
    ego.gameObject.pause = True
    do Speak("Say \"" + string + "\"")
    do Idle() for time seconds
    ego.gameObject.pause = False
    do Idle() for 1 seconds

def pressCondition(self, targets) -> bool:
    # Targets as a list of scenic obj with format [frontOpponent, football]
    # TODO: Check args
    return targets[0] == closerTo(targets[1], Opponent)

# This would be a generic action
behavior positionAction(final, speed, targets):
    self.action = "press"
    do MoveTo(final(targets), "Pressing") for 0.1 seconds

def coverCondition(self, targets) -> bool:
    # Targets as a list of scenic obj with fromat [leftDefendant, rightDefendant]
    #TODO: Check args
    return (self.closeRight and self.closeRight.action == "press") or (self.closeLeft and self.closeLeft.action ==  "press")

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
defendant_b.isHuman = True
defendant_b.team = "self"

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