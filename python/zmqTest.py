import zmq
import time
import json
import random

class Vector:
	def __init__(self, x, y, z=0):
		self.vector3 = (x, y, z)

	@property
	def x(self) -> float:
		return self.vector3[0]

	@property
	def y(self) -> float:
		return self.vector3[1]

	@property
	def z(self) -> float:
		return self.vector3[2]

class send_data:
    d : list
    def __init__(self, d):
        self.d = d
    def toJson(self):
        return json.dumps(self, default=lambda o: o.__dict__)


class gameObject:
    timestep : int

    # list types should be max 3 elements because they are converted to Vector3 in Unity
    position : list
    rotation : list
    
    #tag is to keep track of the scenic objects spawned
    tag : str

    doMove : bool
    movePosition : list

    doKick : bool
    kickPosition : list

    team : bool

    def __init__(self, position, rotation):
        self.timestep = 0

        self.position = position
        self.rotation = rotation
        self.tag = ""

        self.doMove = False
        # self.movePosition = []

        self.doKick = False
        self.kickPosition = [0,0,0]

        self.team = False
    
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


class VRMessageServer:
    def __init__(self):
        self.start()
        self.timestepNumber = 0
    def start(self):
        self.context = zmq.Context()
        self.socket_address = "tcp://127.0.0.1:5555"
        self.socket = self.context.socket(zmq.REQ)
        self.socket.setsockopt(zmq.HANDSHAKE_IVL, 0)
        self.socket.connect(self.socket_address)
    def spawn_object(self):
        if (player_obj.tag == "player"):
            player_obj.team = False
            data = player_obj # send_data(player_obj) when more than 1 obj
            Jsondata = json.dumps(data, default=lambda o: o.__dict__)
            self.socket.send_json(Jsondata)
            print(Jsondata)
            received = False
            while not received:
                try:
                    inData = self.socket.recv(flags=zmq.NOBLOCK)
                except zmq.ZMQError:
                    received = False
                else:
                    received = True
    def step(self):
        out_data = self.make_data()
        self.socket.send_json(out_data)
        print(out_data)
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
        #print(inData)
    def make_data(self):
        if (player_obj.tag == "player"):
            self.player_1_behavior()
            data = player_obj
            Jsondata = json.dumps(data, default=lambda o: o.__dict__)
        return Jsondata
    def player_1_behavior(self):
        data = None
        player_obj.doMove = False
        player_obj.doKick = False
        player_obj.kickPosition = [0,0,0]
        player_obj.timestep = self.timestepNumber

        # move to pos for 3 seconds
        if self.timestepNumber < 30:
             player_obj.doMove = True

        # kick ball for 3 seconds
        elif self.timestepNumber < 60:
             player_obj.doKick = True
             player_obj.kickPosition = [10,0,10]

        # move to pos for 6 seconds
        elif self.timestepNumber < 120:
             player_obj.doMove = True

        # # kick ball for 3 seconds
        elif self.timestepNumber < 150:
             player_obj.doKick = True
             player_obj.kickPosition = [0,0,20]


player_obj = gameObject([0,0,-13], Vector(0, 0, 0))
player_obj.tag = "player"

a = VRMessageServer()
a.spawn_object()
while True:
    a.step()
    time.sleep(0.1) # 10 updates/second





