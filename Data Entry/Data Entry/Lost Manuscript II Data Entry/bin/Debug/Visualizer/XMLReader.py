from includes import *
from xml.dom import minidom

class XMLReader:
	def __init__(self, world):
		self.world = world
		
	def getNode(self, id):
		for x in range(0, len(self.nodes)):
			if(self.nodes[x][0] == id):
				return self.nodes[x][1]
		return None
		
	def buildFeatureGraph(self, fileName):
		self.rFeatureGraph = RFeatureGraph()
		self.xmldoc = minidom.parse(fileName)
		itemlist = self.xmldoc.getElementsByTagName('Feature') 
		root = self.xmldoc.getElementsByTagName('Root')[0]
		rootIndex = int(root.attributes['id'].value)
		print rootIndex
		self.nodes = []
		for x in range(0, itemlist.length):
			s = itemlist.item(x)
			id = int(s.attributes['id'].value)
			data = s.attributes['data'].value
			self.nodes.append([])
			self.nodes[-1].append(id)
			self.nodes[-1].append(RepresentedFeature(data, self.world))
		for x in range(0, itemlist.length):
			s = itemlist.item(x)
			id = int(s.attributes['id'].value)
			data = s.attributes['data'].value
			node = self.getNode(id)
			neighbors = s.childNodes
			for y in range(0, neighbors.length):
				n = neighbors.item(y)
				if(n.attributes != None and n.nodeName == "neighbor"):
					destid = int(n.attributes['dest'].value)
					weight = float(n.attributes['weight'].value)
					node.addNeighbor(self.getNode(destid), weight)
					
		for x in range(0, len(self.nodes)):
			self.rFeatureGraph.addNode(self.nodes[x][1])
		self.rFeatureGraph.root = self.getNode(rootIndex)
		return self.rFeatureGraph