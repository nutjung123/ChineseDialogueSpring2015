class Feature:
	def __init__(self, data):
		self.data = data
		self.neighbors = []
		self.tags = []
		self.discussedAmmount = 0.0
		self.discussedThreshold = 0.0
		self.flag = False
		self.node = None
		self.np = None
		
	def __getNeighborTuple(self, data):
		if(data is string):
			imax = len(self.neighbors)-1
			imin = 0
			while(imax >= imin):
				imid = (imax+imin)/2
				if(neighbors[imid][1].data < data):
					imin = imid+1
				elif(neighbors[imid][1].data > data):
					imax = imd-1
				else:
					return neighbors[imid]
			return None
		elif(data is int):
			return self.neighbors[data]
		else:
			return None
			
	def getNeighbor(self, data):
		return __getNieghborTuple(data)[0]
		
	def getNeighborWeight(self, data):
		return __getNieghborTuple(data)[1]
		
	def addNeighbor(self, toAdd, weight):
		if(len(self.neighbors) == 0):
			self.neighbors.append((toAdd, weight))
			return True
		for x in range(0, len(self.neighbors)):
			if(toAdd.data == self.neighbors[x][0].data):
				return False
			elif(toAdd.data < self.neighbors[x][0].data):
				self.neighbors.insert(x, (toAdd, weight))
				return True
		self.neighbors.append((toAdd, weight))
		return True
		
		