"""Primitive meshes"""

from . import client
import warnings
from . import utils

from vbl_aquarium.models.urchin import *
from vbl_aquarium.models.generic import *

callback = None

def _neuron_callback(callback_data):
  if callback is not None:
    callback(callback_data)

counter = 0
meshes = []

## Primitive Mesh Renderer
class Mesh:
  """Mesh Object in Unity
  """

  def __init__(self, position= [0.0,0.0,0.0], scale= [1,1,1], color=[1,1,1],
               material = 'default', interactive = False):
    """Create a mesh object

    Note: this function should be avoided, use `urchin.meshes.create()`

    Parameters
    ----------
    position : list, optional
        position in ap/ml/dv Atlas coordinates, by default [0.0,0.0,0.0]
    scale : list, optional
        neuron scale, by default [1,1,1]
    color : str, optional
        by default '#FFFFFF'
    material : str, optional
        by default 'default'
    """
    global counter

    self.data = MeshModel(
      id = str(counter),
      shape = 'sphere',
      position = utils.formatted_vector3([x/1000 for x in position]),
      color = utils.formatted_color(color),
      scale = utils.formatted_vector3(scale),
      material = material,
      interactive = interactive
    )

    counter += 1

    self._update()
    self.in_unity = True

    meshes.append(self)

  def _update(self):
    """Serialize and update the data in the Urchin Renderer
    """
    client.sio.emit('urchin-meshes-update', self.data.to_string())

  def delete(self):
    """Deletes meshes

    Parameters
    ----------
    references object being deleted
      
	  Examples
	  --------
	  >>> cube_obj.delete() 
    """

    data = IDData
    data.id = self.data.id

    client.sio.emit('urchin-meshes-delete', data.to_string())
    self.in_unity = False
  
  def set_position(self, position):
    """Set the position of a mesh object. Position shoul dbe in AP/ML/DV coordinates in um

    Parameters
    ----------
    position : list of three floats
      (ap, ml, dv) in um
        
    Examples
    --------
    >>> cube_obj.set_position([2,2,2])
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    position_sanitized = utils.sanitize_vector3([position[0]/1000, position[1]/1000, position[2]/1000])
    self.data.position = utils.formatted_vector3(position_sanitized)

    self._update()
  
  def set_scale(self, scale):
    """Set the scale of mesh renderer

    Parameters
    ----------
    scale : list of three floats
      new scale of mesh
        
    Examples
    --------
    >>> cube_obj.set_scale([3,2,3])
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    scale_sanitized = utils.sanitize_vector3(scale)
    self.data.scale = utils.formatted_vector3(scale_sanitized)

    self._update()
  
  def set_color(self, color):
    """Set the color of mesh renderer

    Parameters
    ----------
    mesh_color : string hex color
      new hex color of mesh
        
    Examples
    --------
    >>> cube_obj.set_color("#000000")
    
    """ 
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    # color_sanitized = utils.sanitize_color(color)
    self.data.color = utils.formatted_color(color)

    self._update()

  def set_material(self, material):
    """Set the material of mesh renderer

    Parameters
    ----------
    mesh_material : string
      name of new material
        
    Examples
    --------
    >>> cube_obj.set_material('opaque-lit')
    
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    material_sanitized = utils.sanitize_material(material)
    self.data.material = material_sanitized

    self._update()


def clear():
  """Clear all Mesh objects that have been created
  """
  global meshes

  for mesh in meshes:
    mesh.delete()

  meshes = []

def create(num_objects, position= [0.0,0.0,0.0], scale= [1,1,1], color=[1,1,1],
               material = 'default', interactive = False):
  """Create multiple meshes

  Parameters
  ----------
  num_objects : int
      number of mesh objects to be created
  position : list, optional
      default position nullspace (ap, ml, dv), by default [0.0,0.0,0.0]
  scale : list, optional
      default scale, by default [1,1,1]
  color : list, optional
      default color, by default [1,1,1]
  material : str, optional
      default material, by default 'default'
  interactive : bool, optional
      default interactive state, by default False

	Examples
	--------
	>>> meshes = urchin.meshes.create(2)
  """
  mesh_objects = []
  for i in range(num_objects):
    mesh_objects.append(Mesh(position=position, color=color, scale=scale,
                             material=material, interactive=interactive))
  return(mesh_objects)

def delete(meshes_list):
  """Deletes meshes

  Parameters
  ----------
  meshes_list : list of mesh objects
	  list of meshes being deleted
      
	Examples
	--------
	>>> urchin.meshes.delete(meshes)
  """
  meshes_list = utils.sanitize_list(meshes_list)

  data = IDList(
    ids = [x.data.id for x in meshes_list]
  )

  client.sio.emit('urchin-meshes-deletes', data.to_string())

def set_positions(meshes_list, positions_list):
  """Set the positions of mesh renderers

  Parameters
  ----------
  meshes_list : list of mesh objects
	  list of meshes being set
  positions_list : list of list of three floats
    vertex positions of each mesh in um
      
	Examples
	--------
	>>> urchin.primitives.set_positions(cubes,[[3,3,3],[2,2,2]])
  """
  meshes_list = utils.sanitize_list(meshes_list)
  positions_list = utils.sanitize_list(positions_list)

  data = IDListVector3List(
    ids = [x.data.id for x in meshes_list],
    values = [utils.formatted_vector3(utils.sanitize_vector3([x[0]/1000, x[1]/1000, x[2]/1000])) for x in positions_list]
  )

  client.sio.emit('urchin-meshes-positions', data.to_string())

def set_scales(meshes_list, scales_list):
  """Set scale of mesh renderers

  Parameters
  ----------
  meshes_list : list of mesh objects
	  list of meshes being scaled
  scales_list : list of list of three floats
    new scales of each mesh
      
	Examples
	--------
	>>> urchin.primitives.set_scales(cubes,[[3,3,3],[2,2,2]])
  """
  meshes_list = utils.sanitize_list(meshes_list)
  scales_list = utils.sanitize_list(scales_list)

  data = IDListVector3List(
    ids = [x.data.id for x in meshes_list],
    values = [utils.formatted_vector3(utils.sanitize_vector3(x)) for x in scales_list]
  )

  client.sio.emit('urchin-meshes-scales', data.to_string())

def set_colors(meshes_list, colors_list):
  """Sets colors of mesh renderers

  Parameters
  ----------
  meshes_list : list of mesh objects
	  list of meshes undergoing color change
  colors_list : list of string hex colors
      new hex colors for each mesh
      
	Examples
	--------
	>>> urchin.primitives.set_colors(cubes,["#000000","#000000"])
	
  """
  meshes_list = utils.sanitize_list(meshes_list)
  colors_list = utils.sanitize_list(colors_list)

  data = IDListColorList(
    ids = [x.data.id for x in meshes_list],
    values = [utils.formatted_color(x) for x in colors_list]
  )

  client.sio.emit('urchin-meshes-colors', data.to_string())

def set_materials(meshes_list, materials_list):
  """Sets materials of mesh renderers

  Parameters
  ----------
  meshes_list : list of mesh objects
	  list of meshes undergoing material change
  materials_list : list of strings
      name of new materials
      
	Examples
	--------
	>>> urchin.primitives.set_material(cubes,['opaque-lit','opaque-lit'])
	
  """
  meshes_list = utils.sanitize_list(meshes_list)
  materials_list = utils.sanitize_list(materials_list)

  data = IDListStringList(
    ids = [x.data.id for x in meshes_list],
    values = [utils.sanitize_material(x) for x in materials_list]
  )
      
  client.sio.emit('urchin-meshes-materials', data.to_string()) 