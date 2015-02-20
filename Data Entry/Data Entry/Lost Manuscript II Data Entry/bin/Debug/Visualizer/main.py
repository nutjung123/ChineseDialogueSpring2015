from includes import * 

class World(DirectObject):
	def __init__ (self):
		fileName = ""
		if(len(sys.argv) > 1):
			fileName = sys.argv[1]
		base.disableMouse()
		self.setupText()
		self.world = BulletWorld()
		if(fileName != ""):
			self.myXMLReader = XMLReader(self.world)
			self.rFeatureGraph = self.myXMLReader.buildFeatureGraph(fileName)
			self.camControl = camControl(self.rFeatureGraph.base)
		else:
			self.rFeatureGraph = RFeatureGraph()
			self.camControl = camControl(self.rFeatureGraph.base)
		self.paused = False
		self.POIindex = 0
		self.rFeatureGraph.resetAllPOI()
		
		taskMgr.add(self.update, "update")
		
		self.accept("escape",      sys.exit)
		self.accept("space",       self.pause)
		self.accept("r",           self.seedOffAll)
		self.accept("arrow_left",  self.incrementPOI, [True])
		self.accept("arrow_right", self.incrementPOI, [False])
		self.accept("arrow_up",    self.rFeatureGraph.resetAllPOI)
		
	def update(self, task):
		self.camControl.update()
		if(not self.paused):
			dt = globalClock.getDt()
			self.rFeatureGraph.update()
			self.world.doPhysics(dt)
			self.rFeatureGraph.draw()
		return task.cont
		
	def seedOffAll(self):
		self.rFeatureGraph.seedOffAll()
		
	def pause(self):
		self.paused = not self.paused
		
	def incrementPOI(self, upDown):
		if(upDown):
			self.POIindex += 1
		else:
			self.POIindex -= 1
		self.POIindex = self.POIindex%len(self.rFeatureGraph.nodes)
		self.rFeatureGraph.setPOI(self.POIindex)
		
	def setupText(self):
		self.inst = OnscreenText(text = 'Press "Spacebar" to pause\nPress "R" to attempt to reorder the Graph\nPress "Shift" and move the mouse to move the camera\nPress "Control" and move the mouse to look around\nUse left and right arrows to view individual nodes, use up arrow to reset\nNOTE: distance between nodes does not indicate weight',  pos = (0.0, -0.70), scale = 0.05)
		self.desc = OnscreenText(text = 'Lost Manuscript II Graph Viewer - Thomas Manzini',  pos = (0.0, 0.9), scale = 0.05)
#END OF WORLD
# creates instance of world
w = World()
# runs panda world code
run()