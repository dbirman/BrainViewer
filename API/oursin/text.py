from . import client
import warnings
from . import utils

from vbl_aquarium.models.urchin import TextModel
from vbl_aquarium.models.generic import IDData, IDListStringList, IDListColorList, IDListFloatList, IDListVector2List

## Text renderer

counter = 0
texts = []

def clear():
    """Clear all custom meshes
    """
    global texts
    for text in texts:
      text.delete()

    texts = []

class Text:
  def __init__(self, text = "", color = [1, 1, 1], font_size = 12, position = [0,0]):
    global counter, texts
    counter +=1

    self.data = TextModel(
      id = f't{counter}',
      text = text,
      color = utils.formatted_color(color),
      font_size=font_size,
      position=utils.formatted_vector2(position)
    )

    self._update()
    self.in_unity = True

    texts.append(self)

  def _update(self):
    """Send serialized data to update this text object in Urchin
    """
    client.sio.emit('urchin-text-update', self.data.to_json_string())
  
  def delete(self):
    """Delete a text object
        
    Examples
    --------
    >>> t1.delete()
    """
    client.sio.emit('urchin-text-delete', IDData(id=self.data.id).to_json_string())
    self.in_unity = False

  def set_text(self, text):
    """Set the text in a set of text objects

    Parameters
    ----------
    text : string
      text to be displayed

    Examples
    --------
    >>> t1.set_text('test text')
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    self.data.text = utils.sanitize_string(text)
    self._update()

  def set_color(self,color):
    """Set the color of a set of text objects

    Parameters
    ----------
    color : color
        hex code or [R,G,B]
        
    Examples
    --------
    >>> t1.set_color('#FF0000')
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    self.data.color = utils.formatted_color(color)
    self._update()

  def set_font_size(self, font_size):
    """Set the font size of a set of text objects

    Parameters
    ----------
    text_sizes : int
        font sizes
        
    Examples
    --------
    >>> t1.set_size(12)
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    self.data.font_size = font_size
    self._update()

  def set_position(self,position):
    """Set the positions of a set of text objects in UI canvas space
    Bottom left corner is [-1,-1], top right [1,1]

    Text is anchored at the top left corner of its text box.

    Parameters
    ----------
    text_pos : list of two floats
        canvas positions relative to the center
        
    Examples
    --------
    >>> t1.set_position([400, 300])
    """
    if self.in_unity == False:
      raise Exception("Object does not exist in Unity, call create method first.")
    
    self.data.position = utils.formatted_vector2(position)
    self._update()


def create(n):
  """Create n text objects with default parameters

  Parameters
  ----------
  n : int
      number of text objects
  """
  text_list = []
  for i in range(n):
    text_list.append(Text())
  return text_list

def set_texts(text_list, str_list):
  """Set the string value of multiple text objects

  Parameters
  ----------
  text_list : list of Text
      Text objects
  str_list : _type_
      _description_
  """
  str_list = utils.sanitize_list(str_list, len(text_list))

  for text, str, in zip(text_list, str_list):
    text.data.text = str

  data = IDListStringList(
    ids = [text.data.id for text in text_list],
    values= [string for string in str_list]
  )

  client.sio.emit('urchin-text-texts', data.to_json_string())

def set_positions(text_list, pos_list):
  """Set the positions of multiple text objects

  Positions are [0,1] relative to the edges of the screen

  Parameters
  ----------
  text_list : list of Text
      Text objects
  pos_list : list of float
      [0,0] top left [1,1] bottom right
  """
  pos_list = utils.sanitize_list(pos_list, len(text_list))
  
  for text, pos, in zip(text_list, pos_list):
    text.data.position = utils.formatted_vector2(pos)

  data = IDListVector2List(
    ids = [text.data.id for text in text_list],
    values = [text.data.position for text in text_list]
  )

  client.sio.emit('urchin-text-positions', data.to_json_string())

def set_font_sizes(text_list, font_size_list):
  """_summary_

  Parameters
  ----------
  text_list : list of Text
      Text objects
  font_size_list : _type_
      _description_
  """

  font_size_list = utils.sanitize_list(font_size_list, len(text_list))
  
  for text, font_size, in zip(text_list, font_size_list):
    text.data.font_size = font_size

  data = IDListFloatList(
    ids = [text.data.id for text in text_list],
    values= [text.data.font_size for text in text_list]
  )
  
  client.sio.emit('urchin-text-sizes', data.to_json_string())

def set_colors(text_list, color_list):
  """_summary_

  Parameters
  ----------
  text_list : list of Text
      Text objects
  color_list : _type_
      _description_
  """
  color_list = utils.sanitize_list(color_list, len(text_list))

  for text, color, in zip(text_list, color_list):
    text.data.color = utils.formatted_color(color)

  data = IDListColorList(
    ids = [text.data.id for text in text_list],
    values= [text.data.color for text in text_list]
  )
  
  client.sio.emit('urchin-text-colors', data.to_json_string())