from scenic.core.simulators import SimulationCreationError, Simulator, Simulation
from scenic.syntax.veneer import verbosePrint
from scenic.simulators.unity import client

# print('Connecting to Unity Server...')
# msgClient = client.StartMessageServer('127.0.0.1', 5555, 1)
# while(True):
#     msgClient.step()


class UnitySimulator(Simulator):
    def __init__(self, ip='127.0.0.1', port=5555, timeout=10, render=True, timestep=1):
        super().__init__()
        verbosePrint('Connecting to Unity Server...')
        self.messageClient = client.StartMessageServer(ip, port, timestep)
        self.scenario_number = 0
        self.timestep = timestep
    def createSimulation(self, scene, verbosity=0):
        self.scenario_number += 1
        return UnitySimulation(scene, self.messageClient, self.timestep)
    def destroy(self):
        print("Destroying Simulator")
        #self.messageClient.terminate()
        #Sever socket and try to reconnect to unity


class UnitySimulation(Simulation):
    def __init__(self, scene, client, timestep=0.1, verbosity=0):
        super().__init__(scene, timestep=timestep, verbosity=verbosity)
        self.client = client
        self.ego = None
        for obj in self.objects:
            unityActor = self.createObjectInSimulator(obj)
            if obj is self.objects[0]:
                self.ego = obj
    def step(self):
        self.client.step()
    def executeActions(self, allActions):
        '''
        Buffers allActions before sending. Sending happens in step:
        getProperties -> Information from unity is parsed / action is picked 
        -> executeActions -> step()
        '''
        super().executeActions(allActions)
    def createObjectInSimulator(self, obj):
        gameObject = self.client.spawnObject(obj, obj.position, obj.orientation)
        obj.gameObject = gameObject
        return gameObject
    def getProperties(self, obj, properties):
        unityActor = obj.gameObject
        if not obj.gameObjectType == "ball" and unityActor is None:
            values = dict(
            position=(0,0,0),
            #heading= float(0),
            velocity=(0,0,0),
            #angularVelocity=angularVelocity,
            speed = 0.0,
            angularSpeed = 0.0,
            pitch = 0,
            roll = 0,
            yaw = 0
            )
            return values
        values = self.client.getProperties(obj, properties)
        return values
    def destroy(self):
        print("Destroying Simulation")
        self.client.destroy_all()
        super().destroy()