"""Camera"""

from . import client
from . import utils

import PIL
from PIL import Image

import numpy as np

import io
import json
import asyncio

from vbl_aquarium.models.urchin import CameraRotationModel, CameraModel
from vbl_aquarium.models.generic import FloatData, IDData, Vector2Data
			
receive_totalBytes = {}
receive_bytes = {}
receive_camera = {}

main = []

PIL.Image.MAX_IMAGE_PIXELS = 22500000

# Handle receiving camera images back as screenshots
def on_camera_img_meta(data_str):
	"""Handler for receiving metadata about incoming images

	Parameters
	----------
	data_str : string
		JSON with two fields {"name":"", "totalBytes":""}
	"""
	global receive_totalBytes

	data = json.loads(data_str)

	name = data["name"]
	totalBytes = data["totalBytes"]

	receive_totalBytes[name] = totalBytes
	receive_bytes[name] = bytearray()

def on_camera_img(data_str):
	"""Handler for receiving data about incoming images

	Parameters
	----------
	data_str : string
		JSON with two fields {"name":"", "bytes":""}
	"""
	global receive_totalBytes, receive_bytes, receive_camera

	data = json.loads(data_str)

	name = data["name"]
	byte_data = bytes(data["data"])

	receive_bytes[name] = receive_bytes[name] + byte_data
	
	if len(receive_bytes[name]) == receive_totalBytes[name]:
		print(f'(Camera receive) Camera {name} received an image')
		receive_camera[name].image_received = True

## Camera renderer
counter = 0

class Camera:
	def __init__(self, main = False):	
		global counter
		counter += 1	

		self.data = CameraModel(
			id = 'CameraMain' if main else f'Camera{counter}',
			controllable= True if main else False
		)
		
		self._update()

		self.in_unity = True
		self.image_received_event = asyncio.Event()
		self.loop = asyncio.get_event_loop()

	def _update(self):
		client.sio.emit('urchin-camera-update', self.data.to_string())

	def delete(self):
		"""Deletes camera
		
		Examples
		--------
		>>>c1.delete()
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
			
		client.sio.emit('urchin-camera-delete', IDData(id = self.data.id).to_string())
		self.in_unity = False

	def set_target_coordinate(self,camera_target_coordinate):
		"""Set the camera target coordinate in CCF space in um relative to CCF (0,0,0), without moving the camera. Coordinates can be negative or larger than the CCF space (11400,13200,8000)

		Parameters
		----------
		camera_target_coordinate : float list
			list of coordinates in ap, ml, dv in um

		Examples
		--------
		>>>c1.set_target_coordinate([500,1500,1000])
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
			
		self.data.target= utils.formatted_vector3(utils.sanitize_vector3(camera_target_coordinate))
		self._update()

	def set_rotation(self, rotation):
		"""Set the camera rotation (pitch, yaw, roll). The camera is locked to a target, so this rotation rotates around the target.

		Rotations are applied in order: roll, yaw, pitch. This can cause gimbal lock.

		Parameters
		----------
		rotation : float list OR string
			list of euler angles to set the camera rotation in (pitch, yaw, roll)
			OR
			string: "axial", "coronal", "sagittal", or "angled"

		Examples
		--------
		>>> c1.set_rotation([0,0,0])
		>>> c1.set_rotation("angled")
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
		
		if rotation == 'axial':
			rotation = [0,0,0]
		elif rotation == 'sagittal':
			rotation = [0,90,-90]
		elif rotation == 'coronal':
			rotation = [-90,0,0]
		elif rotation == 'angled':
			rotation = [22.5,22.5,225]
		
		rotation = utils.sanitize_vector3(rotation)
		self.data.rotation= utils.formatted_vector3(rotation)
		
		self._update()

	def set_zoom(self,zoom):
		"""Set the camera zoom. 

		Parameters
		----------
		zoom : float	
			camera zoom parameter

		Examples
		--------
		>>> c1.set_zoom(1.0)
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
		
		self.data.zoom=utils.sanitize_float(zoom)
		self._update()

	# def set_target_area(self, camera_target_area):
	# 	"""Set the camera rotation to look towards a target area

	# 	Note: some long/curved areas have mesh centers that aren't the 'logical' center. For these areas, calculate a center yourself and use set_camera_target.

	# 	Parameters
	# 	----------
	# 	camera_target_area : string
	# 		area ID or acronym, append "-lh" or "-rh" for one-sided meshes
	# 	Examples
	# 	--------
	# 	>>> c1.set_target_area("grey-l") 
	# 	"""
	# 	if self.in_unity == False:
	# 		raise Exception("Camera is not created. Please create camera before calling method.")
		
	# 	camera_target_area
	# 	self.target = camera_target_area
	# 	client.sio.emit('SetCameraTargetArea', {self.data.id: camera_target_area})

	def set_pan(self,pan_x, pan_y):
		"""Set camera pan coordinates

		Parameters
		----------
		pan_x : float
			x coordinate
		pan_y : float
			y coordinate
		
		Examples
		--------
		>>> c1.set_pan(3.0, 4.0)
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
		
		self.data.pan= utils.formatted_vector2([pan_x, pan_y])
		self._update()

	def set_mode(self, mode):
		"""Set camera perspective mode

		Parameters
		----------
		mode : string
			"perspective" or "orthographic" (default)
		
		Examples
		--------
		>>> c1.set_mode('perspective')
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
		
		if mode == 'orthographic':
				self.data.mode= CameraModel.CameraMode.orthographic
		else:
				self.data.mode= CameraModel.CameraMode.perspective
				
		self._update()

	def set_background_color(self, background_color):
		"""Set camera background color

		Parameters
		----------
		background_color : hex color string

		Examples
		--------
		>>> c1.set_background_color('#000000') # black background
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
		
		self.data.background_color= utils.formatted_color(background_color)

		self._update()

	def set_controllable(self):
		"""Sets camera to controllable
		
		Examples
		--------
		>>> c1.set_controllable()
		"""
		if self.in_unity == False:
			raise Exception("Camera is not created. Please create camera before calling method.")
		
		self.data.controllable=True
		self._update()
		
	async def screenshot(self, size=[1024,768], filename = 'return'):
		"""Capture a screenshot, must be awaited

		Parameters
		----------
		size : list, optional
			Size of the screenshot, by default [1024,768]
		filename: string, optional
			Filename to save to, relative to local path
			
		Examples
		--------
		>>> await urchin.camera.main.screenshot()
		"""
		global receive_totalBytes, receive_bytes, receive_camera
		self.image_received_event.clear()
		self.image_received = False
		receive_camera[self.data.id] = self

		if size[0] > 15000 or size[1] > 15000:
			raise Exception('(urchin.camera) Screenshots can''t exceed 15000x15000')
			
		data = Vector2Data(
			id = self.data.id,
			value= utils.formatted_vector2(size)
		)
		
		client.sio.emit('urchin-camera-screenshot-request', data.to_string())

		while not self.image_received:
			await asyncio.sleep(0.1)
		# await self.image_received_event.wait()

		# image is here, reconstruct it
		img = Image.open(io.BytesIO(receive_bytes[self.data.id]))
		
		print(f'(Camera receive) {self.data.id} complete')
		del receive_totalBytes[self.data.id]
		del receive_bytes[self.data.id]
		del receive_camera[self.data.id]

		if not filename == 'return':
			img.save(filename)
		else:
			return img
		
	async def capture_video(self, file_name, callback = None,
						 start_rotation = None, end_rotation = None,
						 frame_rate = 30, duration = 5,
						 size = (1024,768),
						 test = False):
		"""Capture a video and save it to a file, must be awaited

		Can be used in two modes, either by specifying a callback(frame#) or a start/end_rotation

		Parameters
		----------
		file_name : string
			_description_
		callback : function, optional
			callback to execute *prior* to each frame, by default None
		start_rotation : [yaw, pitch, roll], optional
		end_rotation : [yaw, pitch, roll], optional
		frame_rate : int, optional
			frames per second, by default 30
		duration : int, optional
			seconds, by default 5
		size : tuple, optional
			screenshot size, by default (1024,768)

		Examples
		--------
		>>> await urchin.camera.main.capture_video('output.mp4', callback=my_callback_function)
		>>> await urchin.camera.main.capture_video('output.mp4', start_rotation=[22.5, 22.5, 225], end_rotation=[22.5, 22.5, 0])

		"""
		try:
			import cv2
		except:
			raise Exception('Please install cv2 by running `pip install opencv-python` in your terminal to use the Video features')
		
		fourcc = cv2.VideoWriter_fourcc(*'mp4v')
		out = cv2.VideoWriter(file_name, fourcc, frame_rate, size)

		n_frames = frame_rate * duration

		if start_rotation is not None:
			client.sio.emit('urchin-camera-lerp-set', CameraRotationModel(
				start_rotation=utils.formatted_vector3(start_rotation),
				end_rotation=utils.formatted_vector3(end_rotation)
			).to_string())

		for frame in range(n_frames):

			if callback is not None:
				callback(frame)

			if start_rotation is not None:
				perc = frame / n_frames

				client.sio.emit('urchin-camera-lerp', FloatData(
					id=self.data.id,
					value=perc
				).to_string())
			
			if not test:
				img = await self.screenshot([size[0], size[1]])
				image_array = np.array(img)
				image_array = cv2.cvtColor(image_array, cv2.COLOR_RGB2BGR)
				out.write(image_array)
		
		out.release()
		print(f'Video captured on {self.data.id} saved to {file_name}')


def set_light_rotation(angles):
	"""Override the rotation of the main camera light

	Parameters
	----------
	angles : vector3
		Euler angles of light
	"""
	angles = utils.sanitize_vector3(angles)
	print(angles)
	print(isinstance(angles,list))
	client.sio.emit('SetLightRotation', angles)

def set_light_camera(camera_name = None):
	"""Change the camera that the main light is linked to (the light will rotate the camera)

	Parameters
	----------
	camera_name : string, optional
		Name of camera to attach light to, by default None
	"""
	if (camera_name is None):
		client.sio.emit('ResetLightLink')
	else:
		client.sio.emit('SetLightLink', camera_name)

def set_brain_rotation(yaw):
	"""Set the brain's rotation, independent of the camera. This is useful when you want to animate
	a brain rotating for a video. You can set the camera angle you want, and then on each frame
	update the brain rotation, as needed.

	Parameters
	----------
	yaw : float
		Yaw angle for the brain, independent of the camera
	"""

	client.sio.emit('urchin-brain-yaw', FloatData(id='', value=yaw).to_string())

def setup():
	global main
	main = Camera(main = True)