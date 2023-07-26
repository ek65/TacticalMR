from scenic.simulators.unity.actions import *
model scenic.simulators.unity.model
# Z is Y axis in Unity

x1 = Range(4., 6.)
y1 = Range(-10., 10.)
z1 = Range(0., 10.)

x2 = Range(-4., 6.)
y2 = Range(-10., 10.)
z2 = Range(0., 20.)

#ego = new Player at (0,0,0)
# ego = new Ball at (x1,y1,z1)
# ball2 = new Ball at (x2,y2,z2)

behavior Idle():
    while True:
        take IdleAction()

behavior GroundPassSlow(vec : Vector):
    take GroundPassSlowAction(vec)
    take StopAction()

behavior egoBehavior(ball):
    #print(distance from self to ball)
    do Idle() for 1 seconds
    while (distance from self to ball) > 0.5:
        print(distance from self to ball)
        take MoveToAction(ball.position)
    do Idle() for 1 seconds
    do GroundPassSlow(Vector(1,1,0))

       

# behavior egoBehavior(ball):
#    while (distance from self to ball) > 0.5:
#        take MoveToAction(ball.position)
#    take Shoot()

ball = new Ball at (0,5,0)
#ego = new Player at (2,0,0), facing toward ball
ego = new Player at (20,0,0), with behavior egoBehavior(ball)
p1 = new Player at (10,6,0), facing toward ball
p2 = new Player at (10,3,0), facing toward ego
p3 = new Player at (10,0,0), facing toward ego

