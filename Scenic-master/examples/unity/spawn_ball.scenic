model scenic.simulators.unity.model
# Z is Y axis in Unity

x1 = Range(4., 6.)
y1 = Range(-10., 10.)
z1 = Range(0., 10.)

x2 = Range(-4., 6.)
y2 = Range(-10., 10.)
z2 = Range(0., 20.)

ego = new Ball at (x1,y1,z1)
# ball2 = new Ball at (x2,y2,z2)

# behavior egoBehavior(ball):
#    while (distance from self to ball) < 0.5:
#        take MoveTo(ball.position)
#    take Shoot()

# ego = new Player at (0,0,0), with behavior egoBehavior()
# ball = new Ball at (0,5,0)

