import direct.directbase.DirectStart
from pandac.PandaModules import *
from direct.gui.DirectGui import *

class FloatingTextBuilder:
	def __init__(self):
		pass
	
	@staticmethod
	def build3DText(toDisplay):
		newTextNode = TextNode('text') # Create a new TextNode
		newTextNode.setText(toDisplay) # Set the TextNode text
		newTextNode.setTextColor(0,0,0,1)
		newTextNode.setAlign(TextNode.ACenter) # Set the text align
		newTextNode.setWordwrap(16.0) # Set the word wrap
		text_generate = newTextNode.generate() # Generate a NodePath
		newTextNodePath = render.attachNewNode(text_generate) # Attach the NodePath to the render tree
		newTextNodePath.setBin('fixed', 40)
		newTextNodePath.setDepthTest(False)
		newTextNodePath.setDepthWrite(False)
		newTextNodePath.setBillboardPointEye()
		newTextNodePath.detachNode()
		return newTextNodePath