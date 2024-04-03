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
    """Does nothing right now

    Parameters
    ----------
    color : _type_
        _description_

    Returns
    -------
    _type_
        _description_
    """
    return color
    

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

def formatted_color(list_of_float):
    """Converts a list of floats to a Color. Values should be 0->1

    Parameters
    ----------
    list_of_float : list
        Length 3 for RGB, 4 for RGBA
    """

    if len(list_of_float) == 3:
        return Color(
            r = list_of_float[0],
            g = list_of_float[1],
            b = list_of_float[2]
        )
    elif len(list_of_float) == 4:
        return Color(
            r = list_of_float[0],
            g = list_of_float[1],
            b = list_of_float[2],
            a = list_of_float[3]
        )
    else:
        raise Exception('Colors should be length 3 or 4')