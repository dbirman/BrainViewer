"""Custom 3D Objects"""

from . import client
from . import utils
import json

count = 0

class CustomMesh:
    """Custom 3D object
    """
    
    def __init__(self, vertices, triangles, normals = None):
        """Create a Custom 3D object based on a set of vertices and triangle faces

        Unity can automatically calculate the normals to create a convex object, or you can pass them yourself.

        Parameters
        ----------
        vertices : list of vector3
            Vertex coordinates, the x/y/z directions will correspond to AP/DV/ML if your object was exported from Blender
        triangles : list of vector3
            Triangle vertex indexes
        normals : list of vector3, optional
            Normal directions, by default None
        """
        global count

        self.id = str(count)
        count += 1

        data = {}
        data['ID'] = self.id
        data['vertices'] = vertices
        data['triangles'] = triangles

        if not normals is None:
            data['normals'] = normals

        client.sio.emit('CustomMeshCreate', json.dumps(data))
        
        self.in_unity = True

    def delete(self):
        """Destroy this object in the renderer scene
        """
        client.sio.emit('CustomMeshDelete', self.id)
        self.in_unity = False

    def set_position(self, position = [0,0,0], use_reference = True):
        """Set the position relative to the reference coordinate

        Note that the null transform is active by default in Urchin, therefore directions are the CCF defaults:
        AP+ posterior, ML+ right, DV+ ventral

        By default objects are placed with their origin at the reference (Bregma), disabling this
        places objects relative to the Atlas origin, which is the (0,0,0) coordinate in the top, front, left
        corner of the atlas space.

        Parameters
        ----------
        position : vector3
            AP/ML/DV coordinate relative to the reference (defaults to [0,0,0] when unset)
        use_reference : bool, optional
            whether to use the reference coordinate, by default True
        """
        position = utils.sanitize_vector3(position)

        data = {}
        data['ID'] = self.id
        data['Position'] = utils.formatted_vector3(position)
        data['UseReference'] = use_reference

        client.sio.emit('CustomMeshPosition', json.dumps(data))

    def set_scale(self, scale = [1, 1, 1]):
        """_summary_

        Parameters
        ----------
        scale : list, optional
            _description_, by default [1, 1, 1]
        """

        scale = utils.sanitize_vector3(scale)

        data = {}
        data['ID'] = self.id
        data['Value'] = utils.formatted_vector3(scale)

        client.sio.emit('CustomMeshScale', json.dumps(data))

def clear():
    """Clear all custom meshes
    """
    client.sio.emit('Clear','custom')