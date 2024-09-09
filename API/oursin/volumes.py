"""Volumetric datasets (x*y*z matrix)"""

from . import client
from . import utils
import numpy as np
import zlib
import json
import csv
import base64

from vbl_aquarium.models.urchin import VolumeMetaModel, VolumeDataChunk

counter = 0
volumes = []

CHUNK_LIMIT = 1000000

click_list = []
verbose = False

def _volume_click(data):
	"""Internal callback function
	"""
	click_pos = json.loads(data)
	if verbose:
		print(click_pos)
	
	click_list.append(click_pos)

def clear_clicks():
	"""Clear the volumes click list
	"""
	global click_list
	click_list = []

def save_clicks(fpath):
	"""Save the current click list to a CSV file.

	Use `urchin.volumes.clear_clicks()` before starting your click sequence.

	Parameters
	----------
	fpath : string
		Relative filepath
	"""
	global click_list
	# Extract headers and data
	headers = ['ap', 'ml', 'dv']
	data = [[entry[header] for header in headers] for entry in click_list]
	
	with open(fpath, mode='w', newline='') as file:
		writer = csv.writer(file)
		
		# Write headers
		writer.writerow(headers)
		
		# Write data
		writer.writerows(data)

def clear():
		"""Clear all custom meshes
		"""
		global volumes
		
		for volume in volumes:
			volume.delete()

		volumes = []

class Volume:
	"""Volumetric dataset represented in a compressed format by using a colormap to translate
	uint8 x/y/z data into full RGB color.

	Volumes should be created in (AP, ML, DV)
	"""
	def __init__(self, volume_data, colormap = None):
		"""_summary_

		Parameters
		----------
		volume_data : _type_
			_description_
		colormap : _type_, optional
			_description_, by default None
		"""
		global counter, volumes
		self.id = f'volume{counter}'
		counter += 1

		volume_data[np.isnan(volume_data)] = 255

		flattened_data = volume_data.flatten().astype(np.uint8).tobytes()
		compressed_data = zlib.compress(flattened_data)
		compressed_data = base64.b64encode(compressed_data).decode('utf-8')

		if colormap is None:
			colormap = ['#000000'] * 255
		
		self.data = VolumeMetaModel(
			name = f'volume{counter}',
			n_bytes = len(compressed_data),
			colormap = [utils.formatted_color(color) for color in colormap],
			visible = True
		)

		self.update()

		# send data packets
		# split data into chunks
		n_chunks = int(np.ceil(self.data.n_bytes / CHUNK_LIMIT))
		print(f'Data fits in {n_chunks} chunks of 1MB or less')
		offset = 0
		for chunk in range(n_chunks):
			# get the data
			chunk_size = np.min((self.data.n_bytes - offset, CHUNK_LIMIT))

			chunk_data = VolumeDataChunk(
				name = self.data.name,
				# bytes = base64.b64encode(compressed_data[offset : offset + chunk_size]).decode('utf-8')
				bytes = compressed_data[offset : offset + chunk_size]

			)
			client.sio.emit('SetVolumeData', chunk_data.to_json_string())

			offset += chunk_size
			
		volumes.append(self)

	def update(self):
		client.sio.emit('UpdateVolume', self.data.to_json_string())

	def delete(self):
		client.sio.emit('DeleteVolume', self.id)

def compress_volume(volume_data, n_colors=254):
	"""Compress a volume of float data into a uint8 volume by quantiles.

	NaN values are mapped to 255 (transparent) for Urchin.

	This is required for use with the urchin.volume.Volume object type.

	Parameters
	----------
	volume_data : float volume
		3D matrix of float data
	n_colors : int (optional)
		Default to 254, number of un-reserved colors. 255 must always be reserved for NaN / transparency

	Returns
	-------
	(uint8 volume, float[] map)
	"""
	valid_values = volume_data[~np.isnan(volume_data)]
	quantiles = np.quantile(valid_values.flatten(), np.linspace(0,1,n_colors))

	out = np.digitize(volume_data, quantiles, right=True).astype(np.uint8)
	out[np.isnan(volume_data)] = 255

	return out.astype(np.uint8), quantiles

def colormap(colormap_name='greens', reserved_colors=[], datapoints=None):
	"""Build a colormap

	This function has two parts:
	1. It builds a standard colormap in indexes 0->n_colors
	2. It leaves "reserved" colors at the end, by default this is just 255 which becomes 
	transparent in Urchin. But you can add a list of additional colors which will be added
	to the end of the colormap. The order will match the list you pass in, so e.g.
	3. If you pass in datapoints, it will generate a non-uniform colormap going from the min
	to maximum value.

	indexes: 	[0->252, 	253->254, 				255]
	colors: 	[greens, 	your reserved colors, 	transparent]

	Colormap options
	----------
		reds: 0->255 R channel
		greens: 0->255 G channel
		blues: 0->255 B channel

	Parameters
	----------
	colormap_name : str, optional
		_description_, by default 'greens'
	reserved_colors : _type_, optional
		_description_, by default None

	Returns
	-------
	list of string
		List of colormap hex colors in Urchin-compatible format
	"""
	colormap = []

	n_unreserved = 254 - len(reserved_colors)

	datapoints = (datapoints - np.min(datapoints)) / (np.max(datapoints) - np.min(datapoints))
	datapoints = datapoints / np.max(datapoints)

	for i in range(0, n_unreserved):
		if datapoints is not None:
			v = int(np.round(datapoints[i] * 255))
		else:
			v = int(np.round(i / n_unreserved * 255))

		if colormap_name == 'reds':
			colormap.append(utils.rgba_to_hex((v,0,0,255)))
		elif colormap_name == 'greens':
			colormap.append(utils.rgba_to_hex((0,v,0,255)))
		elif colormap_name == 'blues':
			colormap.append(utils.rgba_to_hex((0,0,v,255)))
		else:
			raise Exception(f'{colormap_name} is not a valid colormap option')

	colormap.extend(reserved_colors)

	return colormap