import time
from types import MappingProxyType
import zmq
import json
from scenic.core.vectors import Vector
from scenic.core.vectors import Orientation
from numpy.linalg import norm
from typing import Optional, Any, List, TypeVar, Type, cast, Callable
from scipy.spatial.transform import Rotation

# Language: Python 3
# Holds client information for Scenic Unity communication
# See gameObject below for adding actions

def StartMessageServer(ip, port, timestep):
    return UnityMessageServer(ip, port, timestep)

class UnityMessageServer:
    def __init__(self, ip, port, timestep, timeout=10):
        self.ip = ip
        self.port = port
        self.timestep = timestep
        self.timeout = timeout
        self.timestepNumber = 0
        self.sendData = SendData()
        self.isClient = True
        self.HUD = HUD()
        self.ball = None
        self.HumanPlayers = dict()
        self.humanSavedControllerData = [None] * 2
        self.ScenicPlayers = []
        self.objects = []
        self.socket_address = ""
        self.start()
    def start(self):
        self.context = zmq.Context()
        self.socket_address = "tcp://127.0.0.1:5555"
        if self.isClient:
            self.socket = self.context.socket(zmq.REQ)
            #self.socket.setsockopt(zmq.RCVTIMEO, self.timeout * 100)
            self.socket.setsockopt(zmq.HANDSHAKE_IVL, 0)
            self.socket.connect(self.socket_address)
        else:
            self.socket = self.context.socket(zmq.REP)
            self.socket.setsockopt(zmq.RCVTIMEO, self.timeout * 100)
            self.socket.setsockopt(zmq.HANDSHAKE_IVL, 0)
            self.socket.bind(self.socket_address)
        print("Started Unity messenging client @ " + self.socket_address
                +  " at a timestep of " + str(self.timestep))
    def json_deconstructor(self, data):
        a = json.loads(data)
        if type(a) == dict:
            a = unity_json_from_dict(a)
        else:
            a = ""
        return a
    def json_constructor(self):
        self.sendData.timestepNumber = self.timestepNumber
        data = self.to_json(self.sendData)
        self.sendData.clearControl()
        return data
    def to_json(self, obj):
        def defaultMap(x):
            if isinstance(x, Vector):
                return x.coordinates
            elif isinstance(x, list):
                return tuple(x)
            elif isinstance(x, MappingProxyType):
                pass
            else:
                return x.__dict__
        return json.dumps(obj, default=defaultMap)

    def step(self):
        #send this data, then sleep then receive .
        if self.isClient:
            # print("Sending @ " + self.socket_address
            #     +  " at timestepNumber " + str(self.timestepNumber))
            # time.sleep(self.timestep)
            out_data = self.json_constructor()
            self.socket.send_json(out_data)
            self.timestepNumber += 1
            received = False
            inData = None
            while not received:
                try:
                    inData = self.socket.recv(flags=zmq.NOBLOCK)
                except zmq.ZMQError:
                    received = False
                else:
                    received = True
            incoming_data = self.json_deconstructor(str(inData, 'utf-8'))
            self.extractReceivedData(incoming_data)
        else:
            incoming_data = self.json_deconstructor(str(self.socket.recv(), 'utf-8'))
            self.extractReceivedData(incoming_data)
            time.sleep(self.timestep)
            out_data = self.json_constructor()
            self.socket.send_json(out_data)
            self.timestepNumber += 1
        #time.sleep(self.timestep)
    def terminate(self):
        if self.timestepNumber < 2:
            # If we did not find another server and scenic barely simulated
            return
        success = -1
        while success != 0:
            success = self.socket.disconnect(self.socket_address)
        print("Successfully disconnected")       
    def destroy_all(self):
        for obj in self.objects:
            obj.destroyObj()
        for p in self.ScenicPlayers:
            if p:
                p.destroyObj()
        self.step()
        self.objects = []
        self.ScenicPlayers = []
        self.sendData.clearObjects()
        self.step()
        self.sendData.clearControl()
        self.step()
    def resetData(self):
        self.sendData = SendData()
    def reset(self):
        #set control true and reset match
        raise NotImplementedError
    def spawnObject(self, obj, position, rotation):
        if obj.gameObjectType == "player":
            game_object = gameObject(position, rotation)
            obj.gameObject = game_object
            obj.gameObject.model = Model(3,1, (255,255,255,1), "player.scenic")
            if obj.team == "orange":
                #Color to light orange
                game_object.ChangeColor((254,216,177,1))
            elif obj.team == "blue":
                game_object.ChangeColor((145,224,255,255))
            self.sendData.addToQueue(obj.gameObject)
            self.sendData.control, self.sendData.addObject = True, True
            self.ScenicPlayers.append(game_object)
            game_object.tag = len(self.ScenicPlayers) - 1
            return game_object
        elif obj.gameObjectType == "human":
            #I will just use indices to mark players. This will only work for one human player
            #position and rotation should not do anything
            tag = "ego"
            if tag in self.HumanPlayers.keys():
                #raise NotImplementedError
                pass
            game_object = gameObject(position, rotation)
            obj.gameObject = game_object
            #We will only have one human for now, call it 'ego' in the dict
            obj.gameObject.model = Model(1,1, (255,255,255,1), "player.human")
            self.sendData.addToQueue(obj.gameObject)
            self.sendData.control, self.sendData.addObject = True, True
            self.HumanPlayers[tag] = game_object
            obj.rightController = ControllerInputData(False,False,False,False,False,False,False)
            obj.leftController = ControllerInputData(False,False,False,False,False,False,False)
            return game_object
        elif obj.gameObjectType == "ball":
            game_object = gameObject(position, rotation)
            obj.gameObject = game_object
            obj.gameObject.model = Model(1,1, (255,255,255,1), "Ball")
            self.sendData.addToQueue(obj.gameObject)
            self.sendData.control, self.sendData.addObject = True, True
            self.ball = game_object
            return self.ball
        elif obj.isVrObject:
            game_object = gameObject(position, rotation)
            obj.gameObject = game_object
            if obj.gameObjectType is None or obj.gameObjectType == "":
                obj.gameObjectType = "empty"
            obj.gameObject.model = Model(1,1, (255,255,255,1), obj.gameObjectType)
            self.sendData.addToQueue(obj.gameObject)
            self.sendData.control, self.sendData.addObject = True, True
            self.objects.append(obj.gameObject)
            obj.gameObject.tag = len(self.objects) - 1
            return obj.gameObject
    def extractReceivedData(self, data):
        if isinstance(data, UnityJSON):
            human_players = data.tick_data.human_players
            scenic_players = data.tick_data.scenic_players
            ball = data.tick_data.ball
            scenic_objects = data.tick_data.scenic_objects
            if self.ball is not None:
                self.ball.ConvertFromJson(ball)
            if (len(scenic_players) == len(self.ScenicPlayers)):
                k = 0
                while k < len(scenic_players):
                    unity_player, player = scenic_players[k], self.ScenicPlayers[k]
                    player.ConvertFromJson(unity_player)
                    k += 1
            #change if more than 1 human player implemented
            if "ego" in self.HumanPlayers and len(human_players) > 0:
                self.HumanPlayers["ego"].ConvertFromJson(human_players[0])
                humanLeftController = data.tick_data.human_players[0].leftController
                humanRightController = data.tick_data.human_players[0].rightController
                self.humanSavedControllerData[0] = humanLeftController
                self.humanSavedControllerData[1] = humanRightController
            if self.objects:
                k = 0
                while k < len(scenic_objects):
                    unity_obj = scenic_objects[k]
                    obj = self.objects[k]
                    obj.ConvertFromJson(unity_obj)
                    k += 1

    def getProperties(self, obj, properties):
        if obj.gameObjectType == "ball":
            if self.ball is not None:
                obj.gameObject = self.ball
            else:
                values = dict(
                    position=(0,0,0),
                    velocity=(0,0,0),
                    speed = 0.0,
                    angularSpeed = 0.0,
                    pitch = 0,
                    roll = 0,
                    yaw = 0,
                    region = None,
                    emptySpace = None,
                    topSurface = None
                )
                return values
            game_object = obj.gameObject
            stored_game_object = self.ball
            position = stored_game_object.position
            rotation = stored_game_object.rotation
            velocity = stored_game_object.velocity
            angularVelocity = stored_game_object.angularVelocity
            speed=stored_game_object.speed
            obj.gameObject = stored_game_object
            if rotation[3] == 0:
                yaw, pitch, roll = 0, 0, 0
            else:
                r = Rotation.from_quat([rotation[0], rotation[1], rotation[2], rotation[3]])
                simOrientation = Orientation(r)
                simYaw, simPitch, simRoll = simOrientation.eulerAngles   # global Euler angles
                yaw, pitch, roll = obj.parentOrientation.globalToLocalAngles(simYaw, simPitch, simRoll)   # local Euler angles

            #print(properties)
            #print(yaw)

            values = dict(
                position = position,
                speed = speed,
                velocity = velocity,
                angularSpeed = speed,
                angularVelocity = angularVelocity,
                pitch = pitch,
                roll = roll,
                yaw = yaw,
            )
            #print(set(values))
            return values
# gameObject class holds information Unity needs to generate all gameObject
class gameObject:
    position : Vector
    rotation : tuple
    
    #tag is to keep track of the scenic objects spawned
    tag : str
    #this is used to both record human players and disc at RUNTIME
    clientID : int

    # ballHeading : Vector

    velocity : Vector
    angularVelocity : Vector
    speed : float
    # velocityStop : bool

    # doTransform : bool

    # transformPosition : Vector

    # destroy : bool
    # catchRadius : float

    def __init__(self, position, rotation):
        self.position = position
        self.rotation = (rotation.x, rotation.y, rotation.z, rotation.w)
        # self.transformPosition = Vector(0,0,0)
        # self.velocityStop = False
        self.velocity = Vector(0,0,0)
        self.angularVelocity = Vector(0,0,0)
        self.speed = 0.0
        self.tag = ""
        self.clientID = 0

        # self.doTransform = False
        # self.destroy = False
        # self.path = list()

        # Initialize added variables
        # self.heldByHuman = False
        # self.heldByScenic = False
    #############################################################################################
    # Functions that take in values from the actions and update the variables of the gameObject #
    # Call, within actions.py, using "obj.gameObject.func()" to access                          #
    #############################################################################################

    # For demo purpose, might need to update later 
    def DoPunch(self, punch):
        self.doPunch = punch
    def destroyObj(self):
        print("Destroying object")
        self.destroy = True

    def Idle(self, idle):
        pass
    # def Thrust(self, thrust, heading = Vector(0,0,0)):
    #     self.thrustHeading = heading
    #     self.thrustOn = thrust
    def Brake(self, brake):
        self.brake = brake
    def SetStopVelocity(self, velocityStop):
        self.velocityStop = velocityStop
    def SetPosition(self, position, apply=True):
        self.transformPosition = position
        self.doTransform = apply
    def GrabWall(self, holdingWall):
        self.holdingWall = holdingWall
    def ReleaseWall(self, holdingWall, wallHeading, pushMagnitude):
        self.holdingWall = holdingWall
        self.wallHeading = wallHeading
        self.pushMagnitude = pushMagnitude
    def GrabDisc(self, holdingDisc):
        self.holdingDisc = holdingDisc
    def ThrowDisc(self, holdingDisc, discHeading, throwMagnitude):
        self.holdingDisc = holdingDisc
        self.discHeading = discHeading
        self.throwMagnitude = throwMagnitude

    def MoveToPosition(self, pos):
        self.mercunaPosition = pos
        self.doMercunaMove = True
    def MoveToObject(self, objectID):
        self.mercunaID = objectID
        self.doMercunaMove = True
    def StopMercunaMove(self, mercunaMove):
        self.doMercunaMove = mercunaMove
    def Follow(self, followID, distance):
        self.mercunaID = followID
        self.mercunaDistance = distance
        self.doMercunaFollow = True
    def ChangeColor(self, color):
        self.model.color = color
    def ToggleThBo(self, active):
        self.thBoActive = active
    def ToggleLine(self, active, dest):
        self.doLineDraw = active
        self.lineDestination = dest
    def setTopSpeed(self, topSpeed : float):
        self.topSpeed = topSpeed
    def setCatchRadius(self, catchRadius : float):
        self.catchRadius = catchRadius

    # Utility/Helper Functions
    def toVector3(self,unity_v3):
        return Vector(unity_v3.x, unity_v3.y, unity_v3.z)
    def toQuaternion(self,unity_q):
        return (unity_q.x, unity_q.y, unity_q.z, unity_q.w)
    def ConvertFromJson(self, data):
        self.position = self.toVector3(data.movement_data.transform)
        self.velocity = self.toVector3(data.movement_data.velocity)
        self.angularVelocity = self.toVector3(data.movement_data.angular_velocity)
        self.speed = data.movement_data.speed
        self.clientID = data.clientID
        self.rotation = self.toQuaternion(data.movement_data.rotation)
        self.path = list()
        self.trigger = data.movement_data.trigger
        self.stopButton = data.movement_data.stopButton
        for v in data.movement_data.path:
            v = self.toVector3(v)
            self.path.append(v)
        self.heldByHuman = data.movement_data.heldByHuman
        self.heldByScenic = data.movement_data.heldByScenic
            

class Model:
    length : float
    width : float
    color : tuple
    type : str
    def __init__(self, length=0, width=0, color=(0,0,0,1), type=""):
        self.length = length
        self.width = width
        self.color = color
        self.type = type
#This are the classes that the json will be inserted into
#We might want to put this into another .py file, or make its definition static

# HUD class is responsible for passing and displaying text on Unity side
# see setText for more information
class HUD:
    #message : str
    # The HUD message is a list of strings that will be played one at a time in scene
    # This should be the order of strings:
    # 1. Feedback of the previous scenario (first one leave empty string)
    # 2. Ask if the player chooses to skip to next skill training (branching path?)
    # 3. Display prompt for the scenario
    # 4. No description but prompt player to press button to start 
    message : list
    # 

    enabled : bool
    location : str
    thBoActive : bool
    brakeActive : bool
    def __init__(self, message=["noAction"], enabled=True, location="center"):
        self.message = message
        self.enabled = enabled
        self.location = location
        self.thBoActive = True
        self.brakeActive = True
    def setText(self, new_message: list):
        self.message = new_message
    def enableHUD(self):
        self.enabled = True
    def reposition(self, position: str):
        self.position = position
    def toggleHumanThBo(self, thBoActive : bool):
        self.thBoActive = thBoActive
    def toggleHumanBrake(self, brakeActive : bool):
        self.brakeActive = brakeActive

class SendData:
    control : bool
    timestepNumber : int
    objects : list
    spawnQueue : list
    addObject : bool
    destroy : bool
    def __init__(self):
        self.control = False
        self.addObject = False
        self.timestepNumber = 0
        self.destroy = False
        self.objects = []
        self.spawnQueue = []
    def addToQueue(self, player : gameObject):
        self.objects.append(player)
        self.spawnQueue.append(player)
    def clearQueue(self):
        self.spawnQueue = []
    def clearControl(self):
        if self.control:
            self.clearQueue()
            self.control = False
            #self.addToQueue = False
    def clearObjects(self):
        self.control = True
        self.destroy = True
        self.objects = []

from dataclasses import dataclass

T = TypeVar("T")

def from_int(x: Any) -> int:
    assert isinstance(x, int) and not isinstance(x, bool)
    return x

def from_float(x: Any) -> float:
    assert isinstance(x, (float, int)) and not isinstance(x, bool)
    return float(x)

def from_none(x: Any) -> Any:
    assert x is None
    return x

def from_union(fs, x):
    for f in fs:
        try:
            return f(x)
        except:
            pass
    assert False

def to_float(x: Any) -> float:
    assert isinstance(x, float)
    return x

def to_class(c: Type[T], x: Any) -> dict:
    assert isinstance(x, c)
    return cast(Any, x).to_dict()

def from_bool(x: Any) -> bool:
    assert isinstance(x, bool)
    return x

def from_list(f: Callable[[Any], T], x: Any) -> List[T]:
    assert isinstance(x, list)
    return [f(y) for y in x]

@dataclass
class UnityVector3:
    x: float
    y: float
    z: float
    w: Optional[float] = None

    @staticmethod
    def from_dict(obj: Any) -> 'UnityVector3':
        assert isinstance(obj, dict)
        x = from_float(obj.get("x"))
        y = from_float(obj.get("y"))
        z = from_float(obj.get("z"))
        w = from_union([from_float, from_none], obj.get("w"))
        return UnityVector3(x, y, z, w)

    def to_dict(self) -> dict:
        result: dict = {}
        result["x"] = to_float(self.x)
        result["y"] = to_float(self.y)
        result["z"] = to_float(self.z)
        result["w"] = from_union([from_int, from_none], self.w)
        return result

@dataclass
class MovementData:
    transform: UnityVector3
    speed: float
    velocity: UnityVector3
    angular_velocity: UnityVector3
    rotation: UnityVector3
    path: List[UnityVector3]
    trigger: bool
    stopButton: bool
    heldByHuman: bool
    heldByScenic: bool
    @staticmethod
    def from_dict(obj: Any) -> 'MovementData':
        assert isinstance(obj, dict)
        transform = UnityVector3.from_dict(obj.get("transform"))
        speed = from_float(obj.get("speed"))
        velocity = UnityVector3.from_dict(obj.get("velocity"))
        angular_velocity = UnityVector3.from_dict(obj.get("angularVelocity"))
        rotation = UnityVector3.from_dict(obj.get("rotation"))
        path = from_list(UnityVector3.from_dict, obj.get("path"))
        trigger = from_bool(obj.get("trigger"))
        stopButton = from_bool(obj.get("stopButton"))
        heldByHuman = from_bool(obj.get("heldByHuman"))
        heldByScenic = from_bool(obj.get("heldByScenic"))
        return MovementData(transform, speed, velocity, angular_velocity, rotation, path, trigger, stopButton, heldByHuman, heldByScenic)

    def to_dict(self) -> dict:
        result: dict = {}
        result["transform"] = to_class(UnityVector3, self.transform)
        result["speed"] = to_float(self.speed)
        result["velocity"] = to_class(UnityVector3, self.velocity)
        result["UnityVector3"] = to_class(UnityVector3, self.angular_velocity)
        result["rotation"] = to_class(UnityVector3, self.rotation)
        result["path"] = from_list(lambda x: to_class(UnityVector3, x), self.path)
        result["trigger"] = from_bool(self.trigger)
        result["stopButton"] = from_bool(self.stopButon)
        result["heldByHuman"] = from_bool(self.heldByHuman)
        result["heldByScenic"] = from_bool(self.heldByScenic)
        return result
@dataclass
class ControllerInputData:
    primary2DAxis: bool
    primaryButton: bool
    secondaryButton: bool
    gripButton: bool
    triggerButton: bool
    menuButton: bool
    primary2DAxisClick: bool
    @staticmethod
    def from_dict(obj: Any) -> 'ControllerInputData':
        assert isinstance(obj, dict)
        primary2DAxis = from_bool(obj.get("primary2DAxis"))
        primaryButton = from_bool(obj.get("primaryButton"))
        secondaryButton = from_bool(obj.get("secondaryButton"))
        gripButton = from_bool(obj.get("gripButton"))
        triggerButton = from_bool(obj.get("triggerButton"))
        menuButton = from_bool(obj.get("menuButton"))
        primary2DAxisClick = from_bool(obj.get("primary2DAxisClick"))
        return ControllerInputData(primary2DAxis,primaryButton,secondaryButton,
                                gripButton,triggerButton,menuButton,primary2DAxisClick)
    def to_dict(self) -> dict:
        result: dict = {}
        result["primary2DAxis"] = from_bool(self.primary2DAxis)
        result["primaryButton"] = from_bool(self.primaryButton)
        result["secondaryButton"] = from_bool(self.secondaryButton)
        result["gripButton"] = from_bool(self.gripButton)
        result["triggerButton"] = from_bool(self.triggerButton)
        result["menuButton"] = from_bool(self.menuButton)
        result["primary2DAxisClick"] = from_bool(self.primary2DAxisClick)
        return result

@dataclass
class Ball:
    movement_data: MovementData
    clientID: int
    @staticmethod
    def from_dict(obj: Any) -> 'Ball':
        assert isinstance(obj, dict)
        movement_data = MovementData.from_dict(obj.get("movementData"))
        clientID = from_int(obj.get("clientID"))
        return Ball(movement_data, clientID)

    def to_dict(self) -> dict:
        result: dict = {}
        result["movementData"] = to_class(MovementData, self.movement_data)
        return result

@dataclass
class ScenicPlayer:
    movement_data: MovementData
    clientID : int
    leftController : ControllerInputData
    rightController : ControllerInputData

    @staticmethod
    def from_dict(obj: Any) -> 'ScenicPlayer':
        assert isinstance(obj, dict)
        movement_data = MovementData.from_dict(obj.get("movementData"))
        clientID = from_int(obj.get("clientID"))
        leftController = ControllerInputData.from_dict(obj.get("leftController"))
        rightController = ControllerInputData.from_dict(obj.get("rightController"))
        return ScenicPlayer(movement_data, clientID, leftController, rightController)

    def to_dict(self) -> dict:
        result: dict = {}
        result["movementData"] = to_class(MovementData, self.movement_data)
        result["leftController"] = to_class(ControllerInputData, self.leftController)
        result["rightController"] = to_class(ControllerInputData, self.rightController)
        return result

@dataclass
class TickData:
    num_players: int
    ball: Ball
    human_players: List[ScenicPlayer]
    scenic_players: List[ScenicPlayer]
    scenic_objects: List[ScenicPlayer]

    @staticmethod
    def from_dict(obj: Any) -> 'TickData':
        assert isinstance(obj, dict)
        num_players = from_int(obj.get("numPlayers"))
        ball = Ball.from_dict(obj.get("Ball"))
        human_players = from_list(ScenicPlayer.from_dict, obj.get("HumanPlayers"))
        scenic_players = from_list(ScenicPlayer.from_dict, obj.get("ScenicPlayers"))
        scenic_objects = from_list(ScenicPlayer.from_dict, obj.get("ScenicObjects"))
        return TickData(num_players, ball, human_players, scenic_players, scenic_objects)

    def to_dict(self) -> dict:
        result: dict = {}
        result["numPlayers"] = from_int(self.num_players)
        result["Ball"] = to_class(Ball, self.ball)
        result["HumanPlayers"] = from_list(lambda x: to_class(ScenicPlayer, x), self.human_players)
        result["ScenicPlayers"] = from_list(lambda x: to_class(ScenicPlayer, x), self.scenic_players)
        result["ScenicObjects"] = from_list(lambda x: to_class(ScenicPlayer, x), self.scenic_objects)
        return result

@dataclass
class UnityJSON:
    tick_data: TickData

    @staticmethod
    def from_dict(obj: Any) -> 'UnityJSON':
        assert isinstance(obj, dict)
        tick_data = TickData.from_dict(obj.get("TickData"))
        return UnityJSON(tick_data)

    def to_dict(self) -> dict:
        result: dict = {}
        result["TickData"] = to_class(TickData, self.tick_data)
        return result

def unity_json_from_dict(s: Any) -> UnityJSON:
    return UnityJSON.from_dict(s)

def unity_json_to_dict(x: UnityJSON) -> Any:
    return to_class(UnityJSON, x)
