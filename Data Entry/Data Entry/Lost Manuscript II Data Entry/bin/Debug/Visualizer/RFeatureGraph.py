from includes import *

class RFeatureGraph:
	def __init__(self):
		self.nodes = []
		self.root = None
		self.base = render.attachNewNode("GraphCenter")
		self.base.setPos(0,0,0)
		self.setPlacementRange()
		
	def setPlacementRange(self, xMax = 50, yMax = 50, zMax = 50):
		self.xMax = xMax
		self.yMax = yMax
		self.zMax = zMax
		
	def addNode(self, toAdd):
		rnd = Random()
		self.nodes.append(toAdd)
		x = (rnd.random()*2-1)*self.xMax
		y = (rnd.random()*2-1)*self.yMax
		z = (rnd.random()*2-1)*self.zMax
		self.nodes[-1].setPos((x,y,z))
		
	def update(self):
		sx = 0
		sy = 0
		sz = 0
		count = len(self.nodes)
		if (count == 0):
			count += 1
		for x in range(0, len(self.nodes)):
			sx += self.nodes[x].getPos().getX()
			sy += self.nodes[x].getPos().getY()
			sz += self.nodes[x].getPos().getZ()
			for y in range(0, len(self.nodes)):
				if(x != y):
					self.nodes[x].applyForceFrom(self.nodes[y])
		ax = sx/count
		ay = sy/count
		az = sz/count
		self.base.setPos(ax, ay,az)
					
	def draw(self):
		for x in range(0, len(self.nodes)):
			if(self.nodes[x].flag == False):
				self.nodes[x].drawStrong()
		for x in range(0, len(self.nodes)):
			self.nodes[x].flag = False
			
	def link(self, nodeA, nodeB, weight = 0.0):
		self.nodeA.addNeighbor(nodeB, weight)
		
	def strongLink(self, nodeA, nodeB, weight = 0.0):
		self.nodeA.addNeighbor(nodeB, weight)
		self.nodeB.addNeighbor(nodeA, weight)
		
	def seedOffAll(self):
		for x in range(0, len(self.nodes)):
			self.nodes[x].seedOff()
			
	def setPOI(self, index):
		self.resetPOI()
		self.nodes[index].onClick()
		
	def resetPOI(self):
		for x in range(0, len(self.nodes)):
			self.nodes[x].offClick()
			
	def resetAllPOI(self):
		for x in range(0, len(self.nodes)):
			self.nodes[x].resetClick()
			if(self.nodes[x] == self.root):
				self.nodes[x].markAsRoot()