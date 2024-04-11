"""Lines"""

from . import client
import warnings
from . import utils

from vbl_aquarium.models.urchin import LineModel
from vbl_aquarium.models.generic import IDData

counter = 0

def clear():
    """Clear all custom meshes
    """
    client.sio.emit('Clear','lines')

class Line:
  def __init__(self, positions= [[0.0,0.0,0.0]], color= [1, 1, 1]):
    global counter
    counter += 1

    self.data = LineModel(
      id = f'l{counter}',
      positions = positions,
      color=utils.formatted_color(color)
    )

    self._update()
    self.in_unity = True

  def _update(self):
    client.sio.emit('urchin-line-update', self.data.to_string())

  def delete(self):
    """Deletes lines

    Examples
    >>>l1.delete()
    """
    client.sio.emit('urchin-line-delete', IDData(id=self.data.id).to_string())
    self.in_unity = False

  def set_positions(self, positions):
    """Set the positions of line renderer
    
    Parameters
    ---------- 
    position : list of vector3 [[ap,ml,dv],[ap,ml,dv]]
        vertex positions of the line in the ReferenceAtlas space (um)

    Examples
    --------
    >>>l1.set_position([[0, 0, 0],[13200,11400,8000]])
    """
    if self.in_unity == False:
      raise Exception("Line does not exist in Unity, call create method first.")
    
    for i, vec3 in enumerate(positions):
      positions[i] = utils.formatted_vector3(utils.sanitize_vector3(vec3))
    self.data.positions = positions

    self._update()

  def set_color(self, color):
    """Set the color of line renderer
    
    Parameters
    ---------- 
    color : string hex color
        new color of the line

    Examples
    --------
    >>>l1.set_color('#000000')
    """
    if self.in_unity == False:
      raise Exception("Line does not exist in Unity, call create method first.")
    
    self.data.color = utils.formatted_color(utils.sanitize_color(color))

    self._update()

def create (n):
  """Create Line objects

  Parameters
  ----------
  n : int
      Number of objects to create
  """
  lines_list = []
  
  for i in range(n):
    line = Line()
    lines_list.append(line)

  return lines_list

def delete (lines_list):
  """Deletes lines
  
  Parameters
  ---------- 
  lines_list : list of Line objects
      list of lines to be deleted

  Examples
  --------
  >>> lines.delete()
  """
  
  for line in lines_list:
    line.delete()
