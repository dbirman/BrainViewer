from .. import client
from .. import utils
from pathlib import Path

import json
from vbl_aquarium.models.urchin import AtlasModel, StructureModel, ColormapModel, CustomAtlasModel
from vbl_aquarium.models.generic import *

class CustomAtlas:
    def __init__(self, atlas_name, atlas_dimensions, atlas_resolution):
        self.atlas_name = atlas_name

        data = CustomAtlasModel(
            name = atlas_name,
            dimensions= utils.formatted_vector3(atlas_dimensions),
            resolution= utils.formatted_vector3(atlas_resolution)
        )

        client.sio.emit('CustomAtlas', data.to_string())

class Atlas:
    def __init__(self, atlas_name):
        # load the ontology structure file
        self.loaded = False
        self.data = AtlasModel(
            name = atlas_name,
            areas = [],
            colormap=ColormapModel()
        )

        current_script_directory = Path(__file__).resolve().parent
        
        data_file_path = f'{current_script_directory}/data/{self.data.name}.structures.json'
        with open(data_file_path,'r') as f:
            temp = json.load(f)

        for i, structure_data in enumerate(temp):
            # {"acronym": "root", "id": 997, "name": "root", "structure_id_path": [997], "rgb_triplet": [255, 255, 255]}
            rgb_normalized = [x/255 for x in structure_data['rgb_triplet']]
            area_data = StructureModel(
                name = structure_data['name'],
                acronym = structure_data['acronym'],
                atlas_id = structure_data['id'],
                color= utils.formatted_color(rgb_normalized)
            )
            self.data.areas.append(area_data)

            area = Structure(
                data=area_data,
                index = i,
                update_callback=self._update
            )

            setattr(self, structure_data['acronym'], area)

    def _update(self):
        """Internal helper function, push data to Unity and update all values
        """
        client.sio.emit('urchin-atlas-update', self.data.to_string())

    def load(self):
        """Load this atlas
        """
        if self.loaded:
            print("(Warning) Atlas was already loaded, the renderer can have issues if you try to load an atlas twice.")
        
        self.loaded = True
        client.sio.emit('urchin-atlas-load', self.data.to_string())

    def clear(self):
        """Clear all visible areas
        """

        # update all areas to be not visible
        for area in self.data.areas:
            area.visible = False

        client.sio.emit('Clear', 'area')

    def load_defaults(self):
        """Load the left and right areas

        Note that this function is not stateful, if you save the scene it will not be reloaded.
        """
        client.sio.emit('urchin-atlas-defaults', "")

    def set_reference_coord(self, reference_coord):
        """Set the reference coordinate for the atlas (Bregma by default)

        Parameters
        ----------
        reference_coord : list of float
        """
        self.data.reference_coord = utils.formatted_vector3(utils.sanitize_vector3(reference_coord))
        self._update

    def get_areas(self, area_list):
        """Get the area objects given a list of area acronyms

        Parameters
        ----------
        area_list : list of string
            List of acronyms to get objects for

        Returns
        -------
        list of Structure

        Examples
        --------
        >>> area_list = urchin.get_areas(["root", "VISp"])
        """
        areas = []
        for name in area_list:
            try:
                area = getattr(self, name)
                areas.append(area)
            except:
                print(f'(Warning): Area {name} couldn''t be found in this atlas!')
        return areas

    def set_visibilities(self, area_list, area_visibility, side = utils.Side.FULL, push = True):
        """Set visibility of multiple areas at once

        Parameters
        ----------
        area_list : list of Structure
        area_visibility : list of bool
        sided : string, optional
            Brain area side to load, default = FULL

        Examples
        --------
        >>> urchin.ccf25.set_visibilities(urchin.ccf25.get_areas(["root", "VISp"]), [True, False])
        >>> urchin.ccf25.set_visibilities(urchin.ccf25.get_areas(["root", "VISp"]), True)
        """
        area_visibility = utils.sanitize_list(area_visibility, len(area_list))

        for i, area in enumerate(area_list):
            self.data.areas[area.index].visible = area_visibility[i]
            self.data.areas[area.index].side = side.value

        if push:
            self._update()

    def set_colors(self, area_list, area_colors, push = True):
        """Set color of multiple areas at once.

        Parameters
        ----------
        area_list : list of Structure
        area_colors : list of colors (hex string or RGB triplet)

        Examples
        --------
        >>> urchin.ccf25.set_visibilities(urchin.ccf25.get_areas(["root", "VISp"]), ['#ff0000', '#00ff00'])
        >>> urchin.ccf25.set_visibilities(urchin.ccf25.get_areas(["root", "VISp"]), [255, 255, 255])
        """
        area_colors = utils.sanitize_list(area_colors, len(area_list))
        
        for i, area in enumerate(area_list):
            self.data.areas[area.index].color = utils.formatted_color(area_colors[i])

        if push:
            self._update()
        
    def set_colormap(self, colormap_name, min = 0, max = 1):
        """Set colormap used for mapping area *intensity* values to colors


        Options are
        - cool (default, teal 0 -> magenta 1)
        - grey (black 0 -> white 1)
        - grey-green (grey 0, light 1/255 -> dark 1)
        - grey-purple (grey 0, light 1/255 -> dark 1)
        - grey-red (grey 0, light 1/255 -> dark 1)
        - grey-rainbow (grey 0, rainbow colors from 1/255 -> 1)

        Parameters
        ----------
        colormap_name : string
            colormap name
        """
        self.data.colormap = ColormapModel(
            name = colormap_name,
            min = min,
            max = max
        )
        self._update()

    def set_color_intensity(self, area_list, area_intensities, push = True):
        """Set intensity values, colors will be set according to the active colormap

        Parameters
        ----------
        area_intensities : list of float
            0->1

        Examples
        --------
        >>> urchin.ccf25.set_color_intensity(urchin.ccf25.get_areas(["root", "VISp"]), [0, 1])
        """
        area_intensities = utils.sanitize_list(area_intensities, len(area_list))

        for i, area in enumerate(area_list):
            self.data.areas[area.index].color_intensity = area_intensities[i]

        if push:
            self._update()

    def set_alphas(self, area_list, area_alphas, push = True):
        """Set alpha values, without changing colors

        Parameters
        ----------
        area_list : list of Structure
        area_alphas : list of float
        """
        area_alphas = utils.sanitize_list(area_alphas, len(area_list))

        for i, area in enumerate(area_list):
            self.data.areas[area.index].color.a = area_alphas[i]

        if push:
            self._update()

    def set_materials(self, area_list, area_materials, push = True):
        """Set material of multiple areas at once.

        Material options are
        - 'opaque-lit' or 'default'
        - 'opaque-unlit'
        - 'transparent-lit'
        - 'transparent-unlit'
        
        Parameters
        ----------
        area_colors : dict {string: string}
            Keys are area IDs or acronyms, Values are hex colors

        Examples
        --------
        >>> urchin.ccf25.set_materials(urchin.ccf25.get_areas(["root", "VISp"]), ['transparent-lit', 'opaque-lit'])
        >>> urchin.ccf25.set_materials(urchin.ccf25.get_areas(["root", "VISp"]), ['transparent-lit])
        """
        area_materials = utils.sanitize_list(area_materials, len(area_list))

        for i, area in enumerate(area_list):
            self.data.areas[area.index].material = area_materials[i]

        if push:
            self._update()

class Structure:
    """Structure attributes can be accessed as

    >>> structure.name
    >>> structure.acronym
    >>> structure.id
    >>> structure.rgb_triplet
    >>> structure.path
    """
    def __init__(self, data, index, update_callback):
        self.data = data
        self.index = index
        self.update_callback = update_callback

    def set_visibility(self, visibility, side = utils.Side.FULL, push = True):
        """Set area visibility

        Parameters
        ----------
        visibility : bool
        side : utils.Side, optional
            .FULL, .LEFT, or .RIGHT

        Examples
        --------
        >>> urchin.ccf25.root.set_visibility(True)
        >>> urchin.ccf25.root.set_visibility(True, urchin.utils.Side.LEFT)
        """
        self.data.visible = visibility
        self.data.side = side.value

        if push:
            self.update_callback()

    def set_color(self, color, push = True):
        """Set area color.

        Parameters
        ----------
        color : hex string

        Examples
        --------
        >>> urchin.ccf25.root.set_color('#ff0000')
        >>> urchin.ccf25.root.set_color([255, 0, 0], "left")
        """
        self.data.color = utils.formatted_color(utils.sanitize_color(color))

        if push:
            self.update_callback()

    def set_alpha(self, alpha, push = True):
        """Set area transparency.

        Parameters
        ----------
        alpha: float

        Examples
        --------
        >>> urchin.ccf25.root.set_alpha(0.5)
        """
        self.data.color.a = utils.sanitize_float(alpha)

        if push:
            self.update_callback()


    def set_intensity(self, intensity, push = True):
        """Set color based on the intensity value through the active colormap.

        Parameters
        ----------
        intensity : float
            0->1

        Examples
        --------
        >>> urn.set_intensity(0.5)
        """
        self.data.color_intensity = utils.sanitize_float(intensity)

        if push:
            self.update_callback()

    def set_material(self, material, push = True):
        """Set material.

        Material options are
        - 'opaque-lit' or 'default'
        - 'opaque-unlit'
        - 'transparent-lit'
        - 'transparent-unlit'

        Parameters
        ----------
        material: string

        Examples
        ----------
        >>> urchin.ccf25.root.set_material('transparent-lit')
        """
        self.data.material = utils.sanitize_string(material)

        if push:
            self.update_callback()

    # def set_data(area_data):
    #     """Set the data array for each CCF area model

    #     Data arrays work the same as the set_area_intensity() function but are controlled by the area_index value, which can be set in the renderer or through the API.

    #     Parameters
    #     ----------
    #     area_data : dict {string: float list}
    #         keys area IDs or acronyms, values are a list of floats
    #     """
    #     client.sio.emit('SetAreaData', area_data)

    # def set_data_index(area_index):
    #     """Set the data index for the CCF area models

    #     Parameters
    #     ----------
    #     area_index : int
    #         data index
    #     """
    #     client.sio.emit('SetAreaIndex', area_index)