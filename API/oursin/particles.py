"""Particles"""

from . import client
import warnings
from . import utils

from vbl_aquarium.models.generic import IDListFloatList

## Particle system
counter = 0
class Particle:
	"""Particles should not be directly instantiated, use urchin.particles.create(n) and urchin.clear_particles()
	"""
	def __init__(self):
		pass

	def set_position(self, position):
		"""Set the position of a particle in ap/ml/dv coordinates relative to the origin (0,0,0)
		
		Parameters
		---------- 
		position : list of three floats
			(ap, ml, dv) coordinates in um

		Examples
		--------
		>>>p1.set_position([5200,5700,330]) # set a particle to Bregma, in CCF space
		"""
		if self.in_unity == False:
			raise Exception("Particle does not exist in Unity, call create method first.")
		
		self.position = utils.sanitize_vector3(position)
		client.sio.emit('SetParticlePos', {self.id: [self.position[0]/1000, self.position[1]/1000, self.position[2]/1000]})

	def set_size(self, size):
		"""Set the size of a particle
		
		Parameters
		---------- 
		size : float

		Examples
		--------
		>>>p1.set_size(0.02) # 20 um 
		"""
		if self.in_unity == False:
			raise Exception("Particle does not exist in Unity, call create method first.")
		
		size = utils.sanitize_float(size)
		self.size = size
		client.sio.emit('SetParticleSize', {self.id: size})
	
	def set_color(self, color):
		"""Set the color of a particle
		
		Parameters
		---------- 
		color : string hex color

		Examples
		--------
		>>>p1.set_color('#FFFFFF')
		"""
		if self.in_unity == False:
			raise Exception("Particle does not exist in Unity, call create method first.")
		
		color = utils.sanitize_color(color)
		self.color = color
		client.sio.emit('SetParticleColor', {self.id: color})

	# todo: re-implement shape and material when we move to ParticleGroup instead of single Particle objects

	# def set_shape(self, shape):
	# 	"""Set the shape of a particle
	# 	Options are
	#  	 - 'sphere' (default)
	#  	 - 'cube' better performance when rendering tens of thousands of neurons

	# 	Parameters
	# 	---------- 
	# 	shape : string
	# 		new shape of the neuron

	# 	Examples
	# 	--------
	# 	>>>p1.set_shape('sphere')
	# 	"""
	# 	if self.in_unity == False:
	# 		raise Exception("Particle does not exist in Unity, call create method first.")
		
	# 	self.shape = shape
	# 	client.sio.emit('SetNeuronShape', {self.id: shape})

	# def set_material(self, material):
	# 	"""Set the material of neuron renderer
	# 	Options are
	#  	- 'lit-transparent' (default)
	#  	- 'lit'
	#  	- 'unlit'

	# 	Parameters
	# 	---------- 
	# 	material : string
	# 		new material of the neuron

	# 	Examples
	# 	--------
	# 	>>>p1.set_material('lit-transparent')
	# 	"""
	# 	if self.in_unity == False:
	# 		raise Exception("Particle does not exist in Unity, call create method first.")
		
	# 	self.material = material
	# 	client.sio.emit('SetNeuronMaterial', {self.id: material})

def create(num_particles):
	"""Create particles

	Note: particles must be created before setting other values

	Parameters
	----------
	num_particles : int

	Examples
	--------  
	>>> neurons = urchin.particles.create(3)
	"""
	global counter
	neurons = []
	for i in range(num_particles):
		counter += 1
		particle = Particle()
		particle.id = f'n{str(counter)}'
		particle.in_unity = True
		neurons.append(particle)

	client.sio.emit('CreateParticles', [x.id for x in neurons])
	return neurons

def clear():
	"""Clear all particles

	Note that there is no delete method for individual particles, they must all be cleared at once.
	"""
	client.sio.emit('Clear', 'particle')

def set_positions(particles_list, positions_list):
	"""Set the position of particles in ap/ml/dv coordinates relative to the origin

	Parameters
	----------
	particles_list : list of Particle
	positions_list : list of list of three floats
		list of positions of neurons (ap, ml, dv) in um

	Examples
	--------
	>>> urchin.particles.set_positions([p1,p2,p3], [[1000,1000,1000],...,...])
	"""
	particles_list = utils.sanitize_list(particles_list)
	positions_list = utils.sanitize_list(positions_list)

	neuron_positions = {}
	for i in range(len(particles_list)):
		neuron = particles_list[i]
		if neuron.in_unity:
			pos = utils.sanitize_vector3(positions_list[i])
			neuron_positions[neuron.id] = [pos[0]/1000, pos[1]/1000, pos[2]/1000]
		else:
			warnings.warn(f"Particle with id {neuron.id} does not exist in Unity, call create method first.")
	client.sio.emit('SetParticlePos', neuron_positions)

def set_sizes(particles_list, sizes_list):
	"""Set particles sizes

	Parameters
	----------
	particles_list : list of Particle
	sizes_list : list of float

	Examples
	--------
	>>> urchin.particles.set_sizes([p1,n2,n3], [0.01,0.02,0.03])
	"""
	particles_list = utils.sanitize_list(particles_list)
	sizes_list = utils.sanitize_list(sizes_list)

	data = IDListFloatList(
		ids = [],
		values= []
	)

	for i in range(len(particles_list)):
		neuron = particles_list[i]
		if neuron.in_unity:
			data.ids.append(neuron.id)
			data.values.append(utils.sanitize_float(sizes_list[i]))
		else:
			warnings.warn(f"Particle with id {neuron.id} does not exist in Unity, call create method first.")
	
	_set_sizes(data)

def _set_sizes(data):
	"""Set particles sizes when already wrapped
	
	Parameters
	----------
	neurons_sizes: IDListFloatList
	
	Examples
	--------
	>>> urchin.particles._set_sizes(IDListFloatList(ids=[0,1,2], value=[1, 1, 1]))
	"""   
	client.sio.emit('SetParticleSize', data)

def set_colors(particles_list, colors_list):
	"""Set neuron colors

	Parameters
	----------
	particles_list : list of Particle
	colors_list : list of string hex colors
		list of colors of neurons

	Examples
	--------
	>>> urchin.particles.set_colors([p1,n2,n3], ['#FFFFFF','#000000','#FF0000'])
	"""
	particles_list = utils.sanitize_list(particles_list)
	colors_list = utils.sanitize_list(colors_list)

	neurons_colors = {}
	for i in range(len(particles_list)):
		neuron = particles_list[i]
		if neuron.in_unity:
			neurons_colors[neuron.id] = utils.sanitize_color(colors_list[i])
		else:
			warnings.warn(f"Particle with id {neuron.id} does not exist in Unity, call create method first.")
	client.sio.emit('SetParticleColor', neurons_colors)

def set_material(material_name):
	"""Change the material used to render neurons

 	Options are
 	- 'gaussian' (default)
 	- 'circle'
 	- 'circle-lit'
 	- 'square'
 	- 'square-lit'
 	- 'diamond'
 	- 'diamond-lit'

	Parameters
	----------
	material_name: string

	Examples
	--------
	>>> urchin.neurons.set_material('circle')
	"""
	material_name = utils.sanitize_string(material_name)

	client.sio.emit('SetParticleMaterial', material_name)

