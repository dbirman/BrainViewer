"""Particles"""

from . import client
import warnings
from . import utils

from vbl_aquarium.models.unity import Vector3, Color
from vbl_aquarium.models.generic import IDData, FloatList, ColorList
from vbl_aquarium.models.urchin import ParticleSystemModel

## Particle system
counter = 0
class ParticleSystem:
	"""Particle system
	
	Minimize the number of particle systems you create
	
	You cannot edit the number of particles in a system after creation
	
	Create separate particle systems when you need to use different materials
	"""
	
	def __init__(self, n, material = 'circle', positions = None, sizes = None, colors = None):
		"""Initialize particle system

		Parameters
		----------
		n : int
				Number of particles
		"""
		global counter
		counter += 1

		self.data = ParticleSystemModel(
			id= f'psystem{counter}',
			n = n,
			material= material,
			positions = [Vector3()] * n if positions is None else [utils.formatted_vector3(pos) for pos in utils.sanitize_list(positions, n)],
			sizes = [0.1] * n if sizes is None else utils.sanitize_list(sizes, n),
			colors = [Color()] * n if sizes is None else utils.sanitize_list(colors, n)
		)

		self._update()
		self.in_unity = True

	def _update(self):
		"""Push data to Urchin renderer
		"""
		client.sio.emit('urchin-particles-update', self.data.to_string())

	def delete(self):
		"""Delete this particle system and all its particles
		"""
		client.sio.emit('urchin-particles-delete', IDData(id= self.data.id).to_string())
		self.in_unity = False

	def set_material(self, material):
		"""Set the material of a particle system

		Options are
	 	- 'gaussian'
		- 'circle' (default)
		- 'circle-lit'
		- 'square'
		- 'square-lit'
		- 'diamond'
		- 'diamond-lit'

		Parameters
		---------- 
		material : string
			new material for all particles

		Examples
		--------
		>>>psystem1.set_material('circle')
		"""
		if self.in_unity == False:
			raise Exception("Particle system was deleted")
		
		self.data.material = utils.sanitize_string(material)
		self._update()

	def set_positions(self, positions):
		"""Set the positions of particles relative to the reference coordinate
		
		Parameters
		---------- 
		position : list of three floats
			(ap, ml, dv) coordinates in um

		Examples
		--------
		>>>psystem1.set_positions([5200,5700,330]) # move all particles to Bregma in CCF
		"""
		if self.in_unity == False:
			raise Exception("Particle system was deleted")
		
		positions = utils.sanitize_list(positions, self.data.n)
		self.data.positions = [utils.formatted_vector3([pos[0]/1000, pos[1]/1000, pos[2]/1000]) for pos in positions]
		
		self._update()

	def _set_positions(self, positions):
		"""Efficient position setting, for real-time applications

		Make sure your positions are in mm, not um

		ID should match psystem.data.id
		
		Parameters
		----------
		positions : Vector3List
				AP/ML/DV positions in *mm*
		"""
		if self.in_unity == False:
			raise Exception("Particle system was deleted")
		
		client.sio.emit('urchin-particles-positions', positions.to_string())

	def set_sizes(self, sizes):
		"""Set the sizes of particles in um
		
		Parameters
		---------- 
		size : float

		Examples
		--------
		>>>psystem1.set_sizes([20])  
		"""
		if self.in_unity == False:
				raise Exception("Particle system was deleted")
		
		sizes = utils.sanitize_list(sizes, self.data.n)
		sizes = [utils.sanitize_float(size)/1000 for size in sizes]
		self.data.sizes = sizes
		
		if self.data.n < 100000:
				self._update()
		else:
				# use the efficient code
				data = FloatList(
					id= self.data.id,
					values= sizes
				)

				self._set_sizes(data)

	def _set_sizes(self, sizes):
		"""Efficent size setting, for real-time applications

		ID should match psystem.data.id

		Parameters
		----------
		sizes : FloatList
				Sizes of particles in *mm*
		"""
		if self.in_unity == False:
			raise Exception("Particle system was deleted")
		
		client.sio.emit('urchin-particles-sizes', sizes.to_string())
	
	def set_colors(self, colors):
		"""Set the colors of particles
		
		Parameters
		---------- 
		colors : list
			hex or [r,g,b] colors

		Examples
		--------
		>>>psystem1.set_color(['#FFFFFF'])
		"""
		if self.in_unity == False:
			raise Exception("Particle system was deleted")
		
		colors = utils.sanitize_list(colors, self.data.n)
		colors = [utils.formatted_color(color) for color in colors]
		self.data.colors = colors

		
		if self.data.n < 100000:
				self._update()
		else:
				# use the efficient code
				data = ColorList(
					id= self.data.id,
					values= colors
				)

				self._set_colors(data)
	
	def _set_colors(self, colors):
		"""Efficient color setting, for real-time applications

		ID should match psystem.data.id

		Parameters
		----------
		colors : ColorList
				Colors of particles
		"""
		if self.in_unity == False:
			raise Exception("Particle system was deleted")
		
		client.sio.emit('urchin-particles-colors', colors.to_string())

def clear():
	"""Clear all particle systems
	"""
	client.sio.emit('Clear', 'particles')