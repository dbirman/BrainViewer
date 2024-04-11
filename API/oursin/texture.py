"""Calcium"""
from . import client
import warnings
from . import utils
from PIL import Image
from typing import List
import io

receive_fname = ''
receive_count = 0
receive_data = []

CHUNK_SIZE = 100000000

counter = 0

def clear():
    """Clear all custom meshes
    """
    client.sio.emit('Clear','texture')

class Texture:
    def __init__(self, position=None, offset=None, texture_file=None):
        self.create()

        if position: self.set_position(position)
        if offset: self.set_offset(offset)
        if texture_file: self.set_image(texture_file)

    def create(self):
        """Creates Textures

        Parameters
        ---------- 
        none

        Examples
        >>> tex = urchin.texture.Texture()
        """
        global counter
        counter += 1
        self.id = 'tex' + str(counter)
        client.sio.emit('CreateFOV',[self.id])
        self.in_unity = True

    def delete(self):
        """Deletes Textures

        Parameters
        ---------- 
        references object being deleted

        Examples
        >>> tex.delete()
        """
        client.sio.emit('DeleteFOV',[self.id])
        self.in_unity = False

    def set_position(self,positions):
        """Set the position of fov in ap/ml/dv coordinates relative to the CCF (0,0,0) point

        Parameters
        ---------- 
        position : list of list of three floats
        	vertex positions of the four corners of the texture relative to the CCF origin

        Examples
        --------
        >>> tex.set_position([[9.2611688 , 7.94252654, 1.3904818 ],
                [8.75443871, 7.94252654, 1.08008647],
                [9.24259926, 8.52829154, 1.43690564],
                [8.73586918, 8.52829154, 1.12651031]])
        """
        if self.in_unity == False:
            raise Exception("Texture does not exist in Unity, call create method first.")

        for i, pos in enumerate(positions):
            positions[i] = utils.sanitize_vector3(pos)

        self.position = utils.sanitize_list(positions)
        client.sio.emit('SetFOVPos',{self.id: positions})
    
    def set_image(self, array):
        """Set the image data for texture

        Parameters
        ----------
        array : numpy array
            luminance data for texture

        Examples
        --------
        >>> tex.set_texture(array)
        """
        if self.in_unity == False:
            raise Exception("Texture does not exist in Unity, call create method first.")

        # texture_file = utils.sanitize_string(texture_file)
        # self.texture_file=texture_file

        # img = Image.open(texture_file)
        # Convert img to bytes
        img_bytes = array.tobytes()
        # img_bytes.seek(0)
        # Split bytes into chunks
        chunks = [img_bytes]
            
        client.sio.emit('SetFOVTextureDataMetaInit', [self.id, len(chunks), array.shape[0], array.shape[1], 'array'])

        # Send img by chunk
        # [TODO: Replace with a data structure]
        for i,chunk in enumerate(chunks):
            immediate_apply = True if i==len(chunks)-1 else False
            client.sio.emit('SetFOVTextureDataMeta', [self.id,i,immediate_apply])
            client.sio.emit('SetFOVTextureData',chunk)

    def set_offset(self, offset):
        """Set the vertical offset for this texture

        Parameters
        ----------
        offset : float
            Vertical offset in mm
        """
        client.sio.emit('SetFOVOffset', {self.id: offset})


def create(N):
    """Create Texture objects

    Note: textures must be created before setting other values

    Parameters
    ----------
    N : int
        number of new Texture objects

    Examples
    --------
    >>> textures_list = urchin.textures.create(3)
    """
    textures = []
    for _ in range(N):
        textures.append(Texture())
    return textures

def delete(textures_list):
    """Delete Texture objects

    Parameters
    ----------
    textures_list : list of Texture

    Examples
    --------
    >>> urchin.fovs.delete(textures_list)
    """
    textures_list = utils.sanitize_list(textures_list)
    for tex in textures_list:
        if tex.in_unity:
            tex.delete()
        else:
            warnings.warn(f"fov with id {tex.id} does not exist in Unity, call create method first.")

    fovs_ids = [x.id for x in textures_list]
    client.sio.emit('DeleteFOVs', fovs_ids)

def set_positions(textures_list, positions_list):
    """Set the positions of textures in ap/ml/dv coordinates relative to the CCF (0,0,0) point

    Note: this plural function has no efficiency advantage of Texture.set_position, for readable
    code we recommend using the former.

    Parameters
    ----------
    textures_list : list of fov objects
        list of fovs being moved
    positions : list of list of list of three floats
        list of four vertex corners for each texture

    Examples
    --------
    >>> urchin.fovs.set_positions([[...],[...],[...]]) # see set_position for position example
    """
    textures_list = utils.sanitize_list(textures_list)
    positions_list = utils.sanitize_list(positions_list)

    for fov,position in zip(textures_list, positions_list):
        if fov.in_unity:
            fov.set_position(position)
        else:
            warnings.warn(f"fov with id {fov.id} does not exist in Unity, call create method first.")

def set_images(textures_list, images_list):
    """Set the position of fov in ml/ap/dv coordinates relative to the CCF (0,0,0) point

    Parameters
    ----------
    fovs_list : list of fov objects
        list of fovs being moved
    positions : list of list of three floats
        list of positions of fovs

    Examples
    --------
    >>> img0 = fov.
    >>> urchin.fovs.set_textures([f1,f2,f3], ['fov1.png','fov'])
    """
    textures_list = utils.sanitize_list(textures_list)
    images_list = utils.sanitize_list(images_list, len(textures_list))

    for tex,img in zip(textures_list,images_list):
        if tex.in_unity:
            tex.set_texture(img)
        else:
            warnings.warn(f"Texture with id {tex.id} does not exist in Unity, call create method first.")