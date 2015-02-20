from includes import *

class camControl(DirectObject):
	def __init__(self, newLookAtTarget):
		self.canMoveCam = False
		self.canLookCam = False
		self.zPos = 0
		self.moveIn = 75
		self.oldX = 0
		self.oldY = 0
		self.x = 0
		self.y = 0
		self.xTot = 0
		self.yTot = 0
		self.heading = 0
		self.pitch = 0
		self.InvertPitch = 0
		self.lookAtTarget = newLookAtTarget
		
		self.camControlNode = render.attachNewNode("camControlNode")
		base.cam.reparentTo(self.camControlNode)
		
		self.accept("shift",    self.setCamMovement, [True])
		self.accept("shift-up", self.setCamMovement, [False])
		self.accept("control",    self.setCamLook, [True])
		self.accept("control-up", self.setCamLook, [False])
		self.accept("shift-wheel_down", self.moveCameraInOut, [True])
		self.accept("shift-wheel_up", self.moveCameraInOut, [False])
		
		self.__initToStart()
		
	def __initToStart(self):
		self.canMoveCam = True
		self.update()
		self.canMoveCam = False
		
	def update(self):
		if base.mouseWatcherNode.hasMouse():
			self.oldX = self.x
			self.oldY = self.y
			self.x = base.mouseWatcherNode.getMouseX()
			self.y = base.mouseWatcherNode.getMouseY()
		dx = self.x-self.oldX
		dy = self.y-self.oldY
		if(self.canMoveCam):
			self.xTot -= dx
			self.yTot -= dy
			self.moveCam(self.xTot, self.yTot)
		elif(self.canLookCam):
			self.pitch   += dy*100
			self.heading -= dx*100
			if(self.heading > 25):
				self.heading = 25
			elif(self.heading < -25):
				self.heading = -25              
			base.cam.setH(self.heading)
			base.cam.setP(self.pitch)
		self.camControlNode.lookAt(self.lookAtTarget) 
		
	def setCamMovement(self, tmp):
		self.canMoveCam = tmp
	def setCamLook(self, tmp):
		self.canLookCam = tmp
		
	def moveCam(self, xPos, yPos):
		xAngleDegrees = xPos * 120.0
		xAngleRadians = xAngleDegrees * (pi / 180.0)
		yAngleDegrees = yPos * 120.0
		yAngleRadians = yAngleDegrees * (pi / 180.0)
		self.moveIn += (self.y-self.oldY)*100
		self.camControlNode.setPos(self.moveIn * sin(xAngleRadians), -self.moveIn * cos(xAngleRadians), self.zPos)
		
	def moveCameraInOut(self, inOut):
		if(inOut == False):
			self.zPos += 10
		else:
			self.zPos -= 10