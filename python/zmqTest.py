import zmq
import time
import json
import random
class send_data:

    x1 : float
    y1 : float
    z1 : float
    x2 : float
    y2 : float
    z2 : float
    def __init__(self, x1, y1, z1, x2, y2, z2):
        self.x1 = x1
        self.y1 = y1
        self.z1 = z1
        self.x2 = x2
        self.y2 = y2
        self.z2 = z2

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
    def step(self):
        coordinates = self.make_data()
        self.socket.send_json(coordinates)
        print(coordinates)
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
        x1Bounds = [-0.35, 0.6]
        z1Bounds = [0.5, 1]
        x = random.uniform(x1Bounds)
        y = 1.277
        z = random.choice(z1Bounds)
        data = send_data(x,y,z)
        Jsondata = json.dumps(data, default=lambda o: o.__dict__)
        return Jsondata
a = VRMessageServer()
while True:
    a.step()
    time.sleep(0.1)



class send_data:
    x : float
    y : float
    z : float
    def __init__(self, x, y, z):
        self.x = x
        self.y = y
        self.z = z
    def toJson(self):
        return json.dumps(self, default=lambda o: o.__dict__)