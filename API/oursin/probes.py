from . import client
import warnings
from . import utils

from vbl_aquarium.models.urchin import ProbeModel
from vbl_aquarium.models.generic import IDData, IDListVector3List, IDListColorList, IDListStringList

##Probes Renderer
counter = 0
probes = []

def clear():
		"""Clear all Probe objects
		"""
		for probe in probes:
			probe.delete()
		
		probes = []

class Probe:
	def __init__(self, color = 'FFFFFF', position = [0,0,0], angle = [0,0,0], style = 'line', scale = [0.070, 3.840, 0.020]):
		
		global counter, probes
		counter +=1
		
		self.data = ProbeModel(
			id = f'p{counter}',
			position= utils.formatted_vector3(position),
			color= utils.formatted_color(color),
			angles = utils.formatted_vector3(angle),
			style = style,
			scale = utils.formatted_vector3(scale)
		)

		self._update()
		self.in_unity = True
		
		probes.append(self)

	def _update(self):
		client.sio.emit('urchin-probe-update', self.data.to_string())

	def delete(self):
		"""Delete probe objects

		Parameters
		----------
		references probe being deleted

		Examples
		--------
		>>> p1.delete()
		"""
		client.sio.emit('urchin-probe-delete', IDData(id=self.data.id).to_string())
		self.in_unity = False

	def set_color(self,color):
		"""Set colors of probe objects

		Parameters
		----------
		color : string
			string is hex color

		Examples
		--------
		>>> p1.set_color('#FFFFFF')
		"""
		if self.in_unity == False:
			raise Exception("Object does not exist in Unity, call create method first.")
		
		self.data.color = utils.formatted_color(color)
		self._update()

	def set_position(self, position):
		"""Set probe tip position in AP/ML/DV coordinates in um relative to the zero coordinate (front, top, left)

		Parameters
		----------
		probe_positions : vector3
			value is list of floats in AP/ML/DV in um

		Examples
		--------
		>>> p1.set_position([500,1500,2500])
		"""
		if self.in_unity == False:
			raise Exception("Object does not exist in Unity, call create method first.")
		
		self.data.position = utils.formatted_vector3(position)
		self._update()

	def set_angle(self, probe_angles):
		"""Set probe azimuth/elevation/spin angles in degrees

		Azimuth 0 = has the probe facing the AP axis, positive values rotate clockwise
		Elevation 0 = probe is vertical, 90 = horizontal

		Parameters
		----------
		probe_angles : float list
			value is list of floats in az/elev/spin
			
		Examples
		--------
		>>> p1.set_angle([-90,0,0])
		"""
		if self.in_unity == False:
			raise Exception("Object does not exist in Unity, call create method first.")
		
		self.data.angles = utils.formatted_vector3(probe_angles)

		self._update()

	# def set_probe_style(self,probe_data):
	# 	"""Set probe rendering style

	# 	Style options are:
	# 		"line"
	# 		"probe-tip"
	# 		"probe-silicon"
	# 		"probe"

	# 	Inputs:
	# 	probe_data -- dictionary of probe names and string {'p1':'line'}
	# 	"""
	# 	if self.in_unity == False:
	# 		raise Exception("Object does not exist in Unity, call create method first.")
		
	# 	probe_data = utils.sanitize_string(probe_data)
	# 	self.style = probe_data
	# 	client.sio.emit('SetProbeStyle', {self.id:probe_data})

	def set_scale(self, probe_scale):
		"""Set probe scale in mm units, by default probes are scaled to 70 um wide x 20 um deep x 3840 um tall which is the size of a NP 1.0 probe.

		Parameters
		----------
		probe_scale: float list
			list of floats for width, height, depth
			
		Examples
		--------
		>>> p1.set_scale([0.070, 3.840, 0.020])
		"""
		if self.in_unity == False:
			raise Exception("Object does not exist in Unity, call create method first.")
		
		self.data.scale = utils.formatted_vector3(probe_scale)

		self._update()

	
def create(num_objects):
	"""Create probe objects

	Parameters
	----------
	num_objects: int
		number of probe objects to be created

	Examples
	--------
	>>> probes = urchin.probes.create(2)
	"""
	probe_list = []
	for i in range(num_objects):
		probe_list.append(Probe())
	return probe_list

def delete(probes_list):
	"""Delete probe objects

	Parameters
	----------
	probes_list: list
		list of probe objects to be deleted

	Examples
	--------
	>>> probes.delete([p1,p2])
	"""
	probes_list = utils.sanitize_list(probes_list)
	probe_ids = [x.id for x in probes_list]
	client.sio.emit('DeleteProbes', probe_ids)

def set_colors(probes_list, colors_list):
	"""Set colors of probe objects

	Parameters
	----------
	probes_list: list of probe objects
		list of probe objects to be colored
	colors_list : list of string hex colors
		new hex colors for each probe

	Examples
	--------
	>>> urchin.probes.set_colors(probes,['#FFFFFF','#000000'])
	"""
	colors_list = utils.sanitize_list(colors_list, len(probes_list))

	data = IDListColorList(
		ids = [x.data.id for x in probes_list],
		values= [utils.formatted_color(x) for x in colors_list]
	)

	client.sio.emit('urchin-probe-colors', data.to_string())

def set_positions(probes_list, positions_list):
	"""Set probe tip positions in AP/ML/DV coordinates in um relative to the zero point (front, left, top)

	Parameters
	----------
	probes_list : list of Probe
	positions_list : list of vector3
		tip coordinate in AP/ML/DV in um
			
	Examples
	--------
	>>> urchin.probes.set_positions(probes,[[1000,2000,1000],[2000,2000,2000]])
	"""
	positions_list = utils.sanitize_list(positions_list, len(probes_list))

	data = IDListVector3List(
		ids = [x.data.id for x in probes_list],
		values= [utils.formatted_vector3([pos[0]/1000, pos[1]/1000, pos[2]/1000]) for pos in positions_list]
	)

	client.sio.emit('urchin-probe-positions', data.to_string())

def set_angles(probes_list, angles_list):
	"""Set probe azimuth/elevation/spin angles in degrees

	Azimuth 0 = has the probe facing the AP axis, positive values rotate clockwise
	Elevation 0 = probe is vertical, 90 = horizontal

	Parameters
	----------
	probes_list : list of probe objects
		list of probes being set
	probe_angles : list of list of three floats
		value is list of floats in az/elev/spin	
		
	Examples
	--------
	>>> urchin.probes.set_angles(probes,[[-90,0,0],[0,30,0]])
	"""
	angles_list = utils.sanitize_list(angles_list, len(probes_list))

	data = IDListVector3List(
		ids = [x.data.id for x in probes_list],
		values= [utils.formatted_vector3(angle) for angle in angles_list]
	)

	client.sio.emit('urchin-probe-angles', data.to_string())

# def set_probe_styles(probes_list,styles_list):
# 	"""Set probe rendering style

# 	Style options are:
# 		"line"
# 		"probe-tip"
# 		"probe-silicon"
# 		"probe"

# 	Parameters
# 	----------
# 	probes_list : list of probe objects
# 		list of probes being set
# 	styles_list :list of strings
# 		list of strings for probe style
	
# 	Examples
# 	--------
# 	>>> urchin.probes.set_probe_styles(probes,['line','probe-tip'])
# 	"""
# 	probes_list = utils.sanitize_list(probes_list)
# 	styles_list = utils.sanitize_list(styles_list)

# 	probe_styles = {}
# 	for i in range(len(probes_list)):
# 		probe = probes_list[i]
# 		if probe.in_unity:
# 			probe_styles[probe.id] = utils.sanitize_string(styles_list[i])
# 		else:
# 			warnings.warn(f"Object with id {probe.id} does not exist in Unity, Please create object {probe.id}.")
# 	client.sio.emit('SetProbeStyle', probe_styles)

def set_scales(probes_list, scales_list):
	"""Set probe scale in mm units, by default probes are scaled to 70 um wide x 20 um deep x 3840 um tall which is the size of a NP 1.0 probe.

	Parameters
	----------
	probes_list: list of probe objects
		list of probe sizes being set
	scales_list: list of list of three floats
		list of floats for width, height, depth for each probe
		
	Examples
	--------
	>>> urchin.probes.set_scales(probes,[[0.070, 3.840, 0.020],[0.070, 3.840, 0.020]])
	"""
	scales_list = utils.sanitize_list(scales_list, len(probes_list))

	data = IDListVector3List(
		ids = [x.data.id for x in probes_list],
		values= [utils.formatted_vector3(scale) for scale in scales_list]
	)

	client.sio.emit('urchin-probe-scales', data.to_string())