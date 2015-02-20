#--------------------------------------------------------------#
#                                                              #
#     Title :  Picker                                          #
#     Owner :  NHSGA-ETC-CMU                                   #
#     Description:  Class to assist with the clicking          #
#         of objects. Makes object clickable and holds their   #
#         locations in the scene graph.                        #
#                                                              #
#--------------------------------------------------------------#

import direct.directbase.DirectStart 
#for the events 
from direct.showbase import DirectObject 
#for collision stuff 
from pandac.PandaModules import * 


class Picker(DirectObject.DirectObject): 
   def __init__(self): 
      #setup collision stuff 

      self.picker= CollisionTraverser() 
      self.queue=CollisionHandlerQueue() 

      self.pickerNode=CollisionNode('mouseRay') 
      self.pickerNP=camera.attachNewNode(self.pickerNode) 

      self.pickerNode.setFromCollideMask(GeomNode.getDefaultCollideMask()) 

      self.pickerRay=CollisionRay() 

      self.pickerNode.addSolid(self.pickerRay) 

      ## self.picker.addCollider(self.pickerNode, self.queue) 
      self.picker.addCollider(self.pickerNP, self.queue)

      #this holds the object that has been picked 
      self.pickedObj=None 

      self.accept('mouse1', self.pickMe) 

   #this function is meant to flag an object as being somthing we can pick 
   def makePickable(self,newObj): 
      newObj.setTag('pickable','true') 

   #this function finds the closest object to the camera that has been hit by our ray 
   def getObjectHit(self, mpos): #mpos is the position of the mouse on the screen 
      self.pickedObj=None #be sure to reset this 
      self.pickerRay.setFromLens(base.camNode, mpos.getX(),mpos.getY()) 
      self.picker.traverse(render) 
      if self.queue.getNumEntries() > 0: 
         self.queue.sortEntries() 
         self.pickedObj=self.queue.getEntry(0).getIntoNodePath() 

         ## parent=self.pickedObj.getParent() 
         parent=self.pickedObj
         self.pickedObj=None 

         while parent != render: 
            if parent.getTag('pickable')=='true': 
               self.pickedObj=parent 
               return parent 
            else: 
               parent=parent.getParent() 
      return None #not return nothing but return type None
    #function to return the name of the object after it has been picked
   def getPickedObj(self): 
         return self.pickedObj 
    #obtains the name of the object at the location of the mouse at click
   def pickMe(self): 
         self.getObjectHit( base.mouseWatcherNode.getMouse()) 
         messenger.send('objectPicked')
         ## print self.pickedObj 

