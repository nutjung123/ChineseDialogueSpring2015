from Feature import *
from includes import *

class RepresentedFeature(Feature):
	def __init__(self, data, world, pos = (0,0,0)):
		Feature.__init__(self, data)
		self.world        = world
		self.flag         = False
		self.vRep         = None
		self.vText        = None
		self.physBaseNode = None
		self.physBaseNp   = None
		self.mySegsNode   = None
		self.mySegs       = None
		
		self.initVisuals()
		self.initPhysBase(pos)
		self.initSegs()
		
		self.vRep.reparentTo(self.physBaseNp)
		self.vText.reparentTo(self.physBaseNp)
		
		self.makePickable()
		
	def drawStrong(self):
		if(self.mySegsNode != None):
			self.mySegsNode.removeNode()
		self.drawStrongHelper(self.mySegs)
		self.mySegsNode = render.attachNewNode(self.mySegs.create())
		self.resetFlags()
		
	def drawStrongHelper(self, segs):
		self.flag = True
		self.drawWeak()
		segs.moveTo(self.getPos())
		for x in range(0, len(self.neighbors)):
			segs.drawTo(self.neighbors[x][0].getPos())
			if(self.neighbors[x][0].flag == False):
				self.neighbors[x][0].drawStrongHelper(segs)
			segs.moveTo(self.getPos())
				
	def drawWeak(self):
		self.physBaseNp.reparentTo(render)
		
	def addNeighbor(self, toAdd, weight):
		if(self.isNeighbor(toAdd)):
			return
		Feature.addNeighbor(self, toAdd, weight)
		if(not toAdd.isNeighbor(self)):
			ropeNP = self.makeRope(self.getPos(), toAdd.getPos())
			
			# Index of the last node of the rope
			idx = ropeNP.node().getNumNodes() - 1
			
			# Attach the last node of the rope with the rigid body
			ropeNP.node().appendAnchor(0  , self.physBaseNp.node())
			ropeNP.node().appendAnchor(idx, toAdd.physBaseNp.node())
		
	def resetFlags(self):
		self.flag = False
		for x in range(0, len(self.neighbors)):
			if(self.neighbors[x][0].flag):
				self.neighbors[x][0].resetFlags()
		
	def seedOff(self):
		x = (random.random()*2)-1
		y = (random.random()*2)-1
		z = (random.random()*2)-1
		self.physBaseNode.applyCentralForce(Vec3(x*5000,y*5000,z*5000))
		
	def getPhysNode(self):
		return self.physBaseNode
		
	def getPhysNodePath(self):
		return self.physBaseNp
		
	def getPos(self):
		return self.physBaseNp.getPos()
	def setPos(self, newPos):
		self.physBaseNp.setPos(newPos)
		
	def getForceFrom(self, dest):
		dx  = (self.getPos().getX() - dest.getPos().getX())
		dy  = (self.getPos().getY() - dest.getPos().getY())
		dz  = (self.getPos().getZ() - dest.getPos().getZ())
		dxy = math.sqrt(math.pow(dx, 2) + math.pow(dy, 2))
		
		x = math.pow(dx, 2)
		y = math.pow(dy, 2)
		z = math.pow(dz, 2)
		dist = math.sqrt(x + y + z)
		
		Gconst = 1000
		f = Gconst*((self.getMass() * dest.getMass())/dist)
		
		fx = f*math.cos(math.atan2(dy, dx))
		fy = f*math.sin(math.atan2(dy, dx))
		fz = f*math.sin(math.atan2(dz, dxy))
		
		return Vec3(fx, fy, fz)
		
	def applyForceFrom(self, dest):
		self.physBaseNode.applyCentralForce(self.getForceFrom(dest))
		
	def getMass(self):
		return self.physBaseNode.getMass()
		
	def isNeighbor(self, toCheck):
		for x in range(0, len(self.neighbors)):
			if(self.neighbors[x][0] == toCheck):
				return True
		return False 
		
	def initVisuals(self):
		self.vRep = loader.loadModel("Cube.egg")
		self.vRep.setPos(0,0,-.5)
		self.vRep.setScale(1.0/12.0)
		
		self.vText = FloatingTextBuilder.build3DText(self.data)
		self.vText.setPos(0,0,0)
		
	def initPhysBase(self, pos):
		self.physBaseNode = BulletRigidBodyNode("FeatureNode")
		self.physBaseNode.setMass(1.0)
		self.physBaseNode.setLinearDamping(0.5)
		
		self.physBaseNp = render.attachNewNode(self.physBaseNode)
		self.physBaseNp.setPos(pos)
		
		self.physBaseNp.detachNode()
		
		self.world.attachRigidBody(self.physBaseNode)
		
	def initSegs(self):
		self.mySegs = LineSegs( )
		self.mySegs.setThickness( 1.0 )
		self.mySegs.setColor( Vec4(0,0,0,1) )
		self.mySegs.moveTo(self.vRep.getPos())
		
	def makeRope(self, p1, p2):
		#INIT THE ROPE
		info = self.world.getWorldInfo()
		n = 1 #This is the number of segments in the rope
		bodyNode = BulletSoftBodyNode.makeRope(info, p1, p2, n, 0) 
		bodyNode.setTotalMass(0.75)
		#MAKE THE ROPES ELASTIC
		tmp = bodyNode.getMaterials()
		for x in range(0, len(tmp)):
			bodyNode.getMaterial(x).setLinearStiffness(.25)
		#ADD THE ROPE TO THE SCENE AND TO THE PHYSICS WORLD
		bodyNP = render.attachNewNode(bodyNode)
		self.world.attachSoftBody(bodyNode)
		#DONE
		return bodyNP
		
	def onClick(self):
		self.vRep.setColor(1,0,0,1)
		self.vText.reparentTo(self.physBaseNp)
		for x in range(0, len(self.neighbors)):
			self.neighbors[x][0].vRep.setColor(1,1,0,1)
			self.neighbors[x][0].vText.reparentTo(self.neighbors[x][0].physBaseNp)
			
			
	def offClick(self):
		self.vRep.setColor(1,1,1,1)
		self.vText.detachNode()
			
	def resetClick(self):
		self.offClick()
		self.vText.reparentTo(self.physBaseNp)
		
	def makePickable(self):
		self.physBaseNp.setTag('pickable','true')
		
	def markAsRoot(self):
		self.vRep.setColor(0,1,0,1)