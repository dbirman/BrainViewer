"""Primitive meshes"""

from . import client

  ## Primitive Mesh Renderer
class Primitives:
  #Run everytime an object is created, sets the fields to defaults if one is not given, and sends the info to Unity
  def __init__(self,position, scale, color, material):
    client.sio.emit('CreateMesh', self)
    if(position == None):
       position = [0,0,0]
    self.position = position
    client.sio.emit('SetPosition', {self: position})
      
    if(scale == None):
      scale = [1,1,1]
    self.scale = scale
    client.sio.emit('SetScale', {self: scale})
      
    if(color == None):
      color = '#FFFFFF'
    self.color = color
    client.sio.emit('SetColor',{self: color})

    if(material == None):
       material = 'default'
    self.material = material
    client.sio.emit('SetMaterial',{self: material})  

  #actually initializes each object(s), does use any parameters other than how many to initialize (uses all defaults)
  def create(numObjects):
    if (numObjects == 1)
      mesh = Primitives()
      client.sio.emit('CreateMesh', mesh)
    else:
      mesh_names = []
      for(int i=1; i<= numObjects; i++)
        mesh_names[i] = Primitives()
      client.sio.emit('CreateMesh', mesh_names)


##OLD CODE BELOW
    
    

def create(mesh_names):
	"""Creates primitive mesh

  Parameters
  ----------
  mesh_names : list of strings
	IDs of meshes being created
      
	Examples
	--------
	>>> urn.create(['cube1','cube2'])
  """
	client.sio.emit('CreateMesh', mesh_names)

def delete(mesh_names):
	"""Deletes meshes

  Parameters
  ----------
  mesh_names : list of strings
	IDs of meshes being deleted
      
	Examples
	--------
	>>> urn.delete(['cube1'])
  """
	client.sio.emit('DeleteMesh', mesh_names)

def set_position(mesh_pos):
  """Set the position of mesh renderer

  Parameters
  ----------
  mesh_pos : dict {string : list of three floats}
      dictionary of IDs and vertex positions of the mesh
      
	Examples
	--------
	>>> urn.set_position({'cube1': [1, 2, 3]})
  """
  client.sio.emit('SetPosition', mesh_pos)

def set_scale(mesh_scale):
  """Set the scale of mesh renderer

  Parameters
  ----------
  mesh_scale : dict {string : list of three floats}
      dictionary of IDs and new scale of mesh
      
	Examples
	--------
	>>> urn.set_scale({'cube1': [3, 3, 3]})
  """
  client.sio.emit('SetScale', mesh_scale)

def set_color(mesh_color):
  """Set the color of mesh renderer

  Parameters
  ----------
  mesh_color : dict {string : string hex color}
      dictionary of IDs and new hex color of mesh
      
	Examples
	--------
	>>> urn.set_color({'cube1': '#FFFFFF'})
	
  """
  client.sio.emit('SetColor', mesh_color)

def set_material(mesh_material):
  """Set the material of mesh renderer

  Parameters
  ----------
  mesh_material : dict {string : string}
      dictionary of object IDs and name of new material
      
	Examples
	--------
	>>> urn.set_material({'cube1': 'unlit'})
	
  """
  client.sio.emit('SetMaterial', mesh_material)  