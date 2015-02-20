from panda3d.core import *

# # loadPrcFileData("", "fullscreen 1") 
# loadPrcFileData("", "multisapmles 4")
# loadPrcFileData("", "framebuffer-multisample 1")
# loadPrcFileData("", "prefer-parasite-buffer #f")
# loadPrcFileData("", "parallax-mapping-samples 3")
# loadPrcFileData("", "parallax-mapping-scale 0.1")
# loadPrcFileData("", "audio-library-name p3fmod_audio")
# loadPrcFileData("", "sync-video 1")

# loads from files and textualy replaces includes at compile time
from math import *
from random import *
from direct.showbase.DirectObject import DirectObject
from direct.showbase.InputStateGlobal import inputState
from direct.gui.DirectGui import *
from direct.gui.OnscreenImage import OnscreenImage
from direct.gui.OnscreenText import OnscreenText
from direct.task import *
from direct.interval.IntervalGlobal import *
from direct.interval.MetaInterval import Sequence
from direct.interval.LerpInterval import LerpFunc
from direct.interval.FunctionInterval import Func 
from direct.actor.Actor import Actor

from panda3d.core import GeomNode

from pandac.PandaModules import *
from ctypes.wintypes import *
from direct.particles.ParticleEffect import ParticleEffect
from direct.gui.DirectGui import *
from direct.gui.OnscreenImage import *
from direct.filter.CommonFilters import CommonFilters

# networking mods
from direct.distributed.PyDatagram import PyDatagram 
from direct.distributed.PyDatagramIterator import PyDatagramIterator 

#from panda3d.bullet import *
from panda3d.core import BitMask32

from panda3d.bullet import *

import random
import os
import sys
import time
import math
import datetime
import hashlib
import picker 
from xml.dom import minidom

from FloatingTextBuilder import *
from Feature import *
from RepresentedFeature import *
from RFeatureGraph import *
from camControl import *
from XMLReader import *


import direct.directbase.DirectStart 