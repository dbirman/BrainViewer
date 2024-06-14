"""Sanitizing inputs to send through API"""
import numpy as np
from enum import Enum

from vbl_aquarium.models.unity import *

### ENUMS
class Side(Enum):
    LEFT = -1
    FULL = 0
    RIGHT = 1
    ALL = 3

### SANITIZING FUNCTIONS

def sanitize_vector3(vector):
    """Guarantee that a vector is a vector 3, or raise an exception

    Parameters
    ----------
    input : any
        arbitrary input parameter

    Returns
    -------
    list
        vector3 as a list [x,y,z]

    Raises
    ------
    Exception
        Failed to coerce input to a length 3 list
    """
    try:
        vector_list = list(map(float, vector))
    except (TypeError, ValueError):
        raise ValueError("Input vector must be convertible to a list of three floats.")

    # Check if the length is exactly three
    if len(vector_list) != 3:
        raise ValueError("Input vector must have exactly three elements.")

    return vector_list


def sanitize_color(color):
    """
    Convert input color to a list of r/g/b/a values in the range 0->1.

    Parameters
    ----------
    color : str or list
        Hex code or list of floats representing color values.

    Returns
    -------
    list
        List of r/g/b/a values in the range 0->1.
    """
    if isinstance(color, list) or isinstance(color, tuple):
        if len(color) == 3 or len(color) == 4:
            try:
                if max(color) > 1:
                    return [x / 255 for x in color]
                else:
                    return color
            except (TypeError, ValueError):
                raise ValueError("Input list must contain three or four floats.")

    elif isinstance(color, str):
        return hex_to_rgb(color)

    else:
        raise TypeError("Input type not recognized.")

def sanitize_float(value):
    if isinstance(value, float):
        return value
    else:
        try:
            return float(value)
        except:
            raise Exception("Value could not be coerced to a float.")

def sanitize_material(material):
    if isinstance(material, str):
        return(material)
    else:
        raise Exception("Material is not properly passed in as a string. Please pass in material as a string.")

def sanitize_list(input, length=0):
    """Guarantee that a list is of at least size length, or try to broadcast to that size

    Parameters
    ----------
    input : list
    length : int, optional
        length to broadcast to, by default 0

    Returns
    -------
    list
    """
    if length > 0 and not isinstance(input, list):
        input = [input] * length

    if not isinstance(input, list):
        raise Exception("List parameter needs to be a list.")

    return input
    
def sanitize_string(string):
    if isinstance(string, str):
        return(string)
    else:
        raise Exception("Input is not properly passed in as a string. Please pass in input as a string.")
    
def sanitize_side(acronym, sided):
    if sided == "full":
        return acronym
    elif sided == "left":
        return f'{acronym}-lh'
    elif sided == "right":
        return f'{acronym}-rh'
    else:
        raise Exception(f'Sided enum {sided} not properly defined, should be full/left/right')
    
def rgb_to_hex(rgb):
    return '#%02x%02x%02x' % rgb
    
def rgba_to_hex(rgba):
    return '#%02x%02x%02x%02x' % rgba

def hex_to_rgb(hex_code):
    # Remove '#' if present
    hex_code = hex_code.lstrip('#')
    # Convert hexadecimal to RGB
    r = int(hex_code[0:2], 16)
    g = int(hex_code[2:4], 16)
    b = int(hex_code[4:6], 16)
    return (r/255, g/255, b/255)

def list_of_list2vector3(list_of_list):
    """Convert a list of lists to a list of Vector3 objects

    Parameters
    ----------
    list_of_list : list of length 3 lists
        _description_
    """
    return [formatted_vector3(data) for data in list_of_list]

def formatted_vector3(list_of_float):
    """Convert a list of floats to a Vector3

    Parameters
    ----------
    list_of_float : list
    """

    list_of_float = sanitize_vector3(list_of_float)

    return Vector3(
        x = list_of_float[0],
        y = list_of_float[1],
        z = list_of_float[2]
    )

def formatted_vector2(list_of_float):
  """Convert a list of floats to a Vector2

  Parameters
  ----------
  list_of_float : list
  """
  return Vector2(
      x = list_of_float[0],
      y = list_of_float[1]
  )

def formatted_color(color):
    """Converts a color, either a hex or list of floats, to a Color object

    Parameters
    ----------
    color : list/str
        Length 3 for RGB, 4 for RGBA, or a hex color string
    """

    color = sanitize_color(color)

    if len(color) == 3:
        return Color(
            r = color[0],
            g = color[1],
            b = color[2]
        )
    elif len(color) == 4:
        return Color(
            r = color[0],
            g = color[1],
            b = color[2],
            a = color[3]
        )
    else:
        raise Exception('Colors should be length 3 or 4')