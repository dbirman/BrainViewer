"""
urchin.

Python library for connecting to and sending data to a Universal Renderer for Neuroscience renderer.
"""

# load the client
from . import client
from .renderer import *

# load sanitization
from . import utils

# load the scene controls
from . import camera

# load the object controls
from . import lines
from . import meshes
from . import particles
from . import probes
from . import text
from . import volumes
from . import texture
from . import custom
from . import dock
from . import ui
# load the colors
from . import colors

# load the atlases
from .atlas import *
