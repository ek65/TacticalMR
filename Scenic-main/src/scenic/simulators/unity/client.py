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
        self.socket_address = "tcp://"+ str(self.ip) +":" + str(self.port)
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
            # should never enter here in our case 
            # since our scenic side is always client and never server
            incoming_data = self.json_deconstructor(str(self.socket.recv(), 'utf-8'))
            self.extractReceivedData(incoming_data)
            time.sleep(self.timestep)
            out_data = self.json_constructor()
            self.socket.send_json(out_data)
            self.timestepNumber += 1
        # time.sleep(self.timestep)
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

        self.HumanPlayers["ego"].destroyObj()
        self.ball.destroyObj()
        self.step()
        self.objects = []
        self.ScenicPlayers = []
        self.HumanPlayers = dict()
        self.ball = None
        self.sendData.clearObjects()
        self.step()
        self.sendData.clearControl()
        self.resetData()
        self.step()
    def resetData(self):
        self.timestepNumber = 0
        self.sendData = SendData()
    def reset(self):
        #set control true and reset match
        raise NotImplementedError
    def spawnObject(self, obj, position, rotation):
        if obj.gameObjectType == "player":
            # print(position)
            game_object = gameObject(position, rotation)
            obj.gameObject = game_object
            obj.gameObject.model = Model(3,1, (255,255,255,1), "Player")
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
            obj.gameObject.model = Model(1,1, (255,255,255,1), "Human")
            self.sendData.addToQueue(obj.gameObject)
            self.sendData.control, self.sendData.addObject = True, True
            self.HumanPlayers[tag] = game_object
            # obj.rightController = ControllerInputData(False,False,False,False,False,False,False)
            # obj.leftController = ControllerInputData(False,False,False,False,False,False,False)
            return game_object
        elif obj.gameObjectType == "ball":
            # need to add position offset here so ball doesn't fall through ground
            # print(position)
            pos = Vector(position.x, position.y, 0.25)
            game_object = gameObject(pos, rotation)
            obj.gameObject = game_object
            obj.gameObject.model = Model(1,1, (255,255,255,1), "Ball")
            self.sendData.addToQueue(obj.gameObject)
            self.sendData.control, self.sendData.addObject = True, True
            self.ball = game_object
            return self.ball
        elif obj.isUnityObject:
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
                    # if (len(human_players) > 0 and human_players[0].movement_data.stopButton):
                    #     print(unity_player)
                    player.ConvertFromJson(unity_player)
                    k += 1
            #change if more than 1 human player implemented
            if "ego" in self.HumanPlayers and len(human_players) > 0:
                self.HumanPlayers["ego"].ConvertFromJson(human_players[0])
                # humanLeftController = data.tick_data.human_players[0].leftController
                # humanRightController = data.tick_data.human_players[0].rightController
                # self.humanSavedControllerData[0] = humanLeftController
                # self.humanSavedControllerData[1] = humanRightController
            if self.objects:
                k = 0
                while k < len(scenic_objects):
                    unity_obj = scenic_objects[k]
                    obj = self.objects[k]
                    obj.ConvertFromJson(unity_obj)
                    k += 1

    def getProperties(self, obj, properties):
        if obj.gameObjectType == "player":
            game_object = obj.gameObject
            stored_game_object = self.ScenicPlayers[int(game_object.tag)]
            position = stored_game_object.position
            rotation = stored_game_object.rotation
            velocity = stored_game_object.velocity
            angularVelocity = stored_game_object.angularVelocity
            speed=stored_game_object.speed
            if rotation[3] == 0:
                yaw, pitch, roll = 0, 0, 0
            else:
                r = Rotation.from_quat([rotation[0], rotation[1], rotation[2], rotation[3]])
                simOrientation = Orientation(r)
                simYaw, simPitch, simRoll = simOrientation.eulerAngles   # global Euler angles
                yaw, pitch, roll = obj.parentOrientation.globalToLocalAngles(simYaw, simPitch, simRoll)   # local Euler angles

            values = dict(
                position = position,
                velocity = velocity,
                speed = speed,
                angularSpeed = speed,
                angularVelocity = angularVelocity,
                pitch = pitch,
                roll = roll,
                yaw = yaw
            )

            return values
        elif obj.gameObjectType == "human":
            game_object = obj.gameObject
            #change when we implement more human players
            stored_game_object = self.HumanPlayers["ego"]
            # print(stored_game_object.position)
            position = stored_game_object.position
            rotation = stored_game_object.rotation
            velocity = stored_game_object.velocity
            angularVelocity = stored_game_object.angularVelocity
            speed=stored_game_object.speed
            if rotation[3] == 0:
                yaw, pitch, roll = 0, 0, 0
            else:
                r = Rotation.from_quat([rotation[0], rotation[1], rotation[2], rotation[3]])
                simOrientation = Orientation(r)
                simYaw, simPitch, simRoll = simOrientation.eulerAngles   # global Euler angles
                yaw, pitch, roll = obj.parentOrientation.globalToLocalAngles(simYaw, simPitch, simRoll)   # local Euler angles

            #add controller data here
            # savedLeftController = self.humanSavedControllerData[0]
            # savedRightController = self.humanSavedControllerData[1]
            # obj.leftController = savedLeftController
            # obj.rightController = savedRightController
            # obj.gameObject = stored_game_object

            values = dict(
                position = position,
                velocity = velocity,
                speed = speed,
                angularSpeed = speed,
                angularVelocity = angularVelocity,
                pitch = pitch,
                roll = roll,
                yaw = yaw
            )

            return values
        elif obj.gameObjectType == "ball": 
            if self.ball is not None:
                obj.gameObject = self.ball
            else:
                print("in here")
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
            # print(stored_game_object.position)
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
        elif obj.isUnityObject:
            #It must be a scenicObj
            game_object = obj.gameObject
            stored_game_object = self.objects[int(game_object.tag)]
            position = stored_game_object.position
            rotation = stored_game_object.rotation
            values = dict(
                    position=position,
                    velocity=Vector(0,0,0),
                    speed = 0.0,
                    angularSpeed = 0.0,
                    angularVelocity = Vector(0,0,0),
                    pitch = 0,
                    roll = 0,
                    yaw = 0
                )
            return values

class actionParameters:
    intVals : list
    floatVals : list
    stringVals : list
    tupleVals : list
    boolVals : list

    def __init__(self):
        self.intVals = []
        self.floatVals = []
        self.stringVals = []
        self.tupleVals = []
        self.boolVals = []

    def addParameter(self, intVal: int = None, floatVal : float = None, stringVal : str = None, tupleVal : tuple = None, boolVal : bool = None):
        tmpList = []
        tmpList.extend([intVal, floatVal, stringVal, tupleVal, boolVal])
        parameter = [x for x in tmpList if x is not None][0]
        if type(parameter) is int:
            self.intVals.append(parameter)
        elif type(parameter) is float:
            self.floatVals.append(parameter)
        elif type(parameter) is str:
            self.stringVals.append(parameter)
        elif type(parameter) is tuple:
            self.tupleVals.append(parameter)
        elif type(parameter) is bool:
            self.boolVals.append(parameter)

# gameObject class holds information Unity needs to generate all gameObject
class gameObject:
    position : Vector
    rotation : tuple
    
    #tag is to keep track of the scenic objects spawned
    tag : str
    #this is used to both record human players and ball at RUNTIME
    clientID : int

    velocity : Vector
    angularVelocity : Vector
    speed : float
    stopButton : bool
    pause:bool
    ballPossession : bool
    
    actionDict : dict

    def __init__(self, position, rotation):
        self.position = position
        self.rotation = (rotation.x, rotation.y, rotation.z, rotation.w)
        self.velocity = Vector(0,0,0)
        self.angularVelocity = Vector(0,0,0)
        self.speed = 0.0
        self.tag = ""
        self.clientID = 0
        self.stopButton = False
        self.pause = False
        self.ballPossession = False
        self.actionDict = {}
        self.model = Model()

    #############################################################################################
    # Functions that take in values from the actions and update the variables of the gameObject #
    # Call, within actions.py, using "obj.gameObject.func()" to access                          #
    #############################################################################################        

    def DoAction(self, actionName : str, *args):
        if (actionName == "Idle"):
            # clear action dict
            self.actionDict = {}
        else:
            self.actionDict = {}
            params = actionParameters()
            for i in list(args):
                params.addParameter(i)
            self.actionDict[actionName] = params
    
    def StopAction(self):
        self.actionDict = {}
 
    def destroyObj(self):
        print("Destroying object")
        self.destroy = True

    def ChangeColor(self, color):
        self.model.color = color

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
        self.stopButton = data.movement_data.stopButton
        self.ballPossession = data.movement_data.ballPossession
        # self.heldByHuman = data.movement_data.heldByHuman
        # self.heldByScenic = data.movement_data.heldByScenic
            

    

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
# CURRENTLY UNUSED
class HUD:
    #message : str
    # The HUD message is a list of strings that will be played one at a time in scene
    # This should be the order of strings:
    # 1. Feedback of the previous scenario (first one leave empty string)
    # 2. Ask if the player chooses to skip to next skill training (branching path?)
    # 3. Display prompt for the scenario
    # 4. No description but prompt player to press button to start 
    message : list

    enabled : bool
    location : str
    def __init__(self, message=["noAction"], enabled=True, location="center"):
        self.message = message
        self.enabled = enabled
        self.location = location
    def setText(self, new_message: list):
        self.message = new_message
    def enableHUD(self):
        self.enabled = True
    def reposition(self, position: str):
        self.position = position


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
            self.destroy = False
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
    stopButton: bool
    ballPossession: bool
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
        stopButton = from_bool(obj.get("stopButton"))
        ballPossession = from_bool(obj.get("ballPossession"))
        heldByHuman = from_bool(obj.get("heldByHuman"))
        heldByScenic = from_bool(obj.get("heldByScenic"))
        return MovementData(transform, speed, velocity, angular_velocity, rotation, stopButton, ballPossession, heldByHuman, heldByScenic)

    def to_dict(self) -> dict:
        result: dict = {}
        result["transform"] = to_class(UnityVector3, self.transform)
        result["speed"] = to_float(self.speed)
        result["velocity"] = to_class(UnityVector3, self.velocity)
        result["UnityVector3"] = to_class(UnityVector3, self.angular_velocity)
        result["rotation"] = to_class(UnityVector3, self.rotation)
        result["stopButton"] = from_bool(self.stopButton)
        result["ballPossession"] = from_bool(self.ballPossession)
        result["heldByHuman"] = from_bool(self.heldByHuman)
        result["heldByScenic"] = from_bool(self.heldByScenic)
        return result

# CURRENTLY UNUSED
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

    @staticmethod
    def from_dict(obj: Any) -> 'ScenicPlayer':
        assert isinstance(obj, dict)
        movement_data = MovementData.from_dict(obj.get("movementData"))
        clientID = from_int(obj.get("clientID"))
        return ScenicPlayer(movement_data, clientID)

    def to_dict(self) -> dict:
        result: dict = {}
        result["movementData"] = to_class(MovementData, self.movement_data)
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
