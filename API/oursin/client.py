"""Client for communicating with the echo server"""
import socketio
import uuid
import asyncio

from . import camera
from . import volumes
from . import meshes

from vbl_aquarium.models.logging import *

class bcolors:
    WARNING = '\033[93m'
    FAIL = '\033[91m'

ID = str(uuid.uuid1())[:8]

sio = socketio.Client()
@sio.event
def connect():
	print("(URN) connected to server")
	change_id(ID)

@sio.event
def disconnect():
    print("(URN) disconnected from server")

@sio.on('log')
def message(data):
	print(data)
	out = Log.model_validate_json(data)
	print('(Renderer) ' + out.msg)

@sio.on('log-warning')
def message(data):
	out = LogWarning.model_validate_json(data)
	print('(Renderer) ' + bcolors.WARNING + out.msg)

@sio.on('log-error')
def message(data):
	out = LogError.model_validate_json(data)
	print('(Renderer) ' + bcolors.FAIL + out.msg)

###### CALLBACKS #######

@sio.on('CameraImgMeta')
def receive_camera_img_meta(data):
	camera.on_camera_img_meta(data)
	
@sio.on('CameraImg')
def receive_camera_img(data):
	camera.on_camera_img(data)

@sio.on('VolumeClick')
def receive_volume_click(data):
	volumes._volume_click(data)

@sio.on('NeuronCallback')
def receive_neuron_callback(data):
	meshes._neuron_callback(data)
	
# Helper functions
def connected():
	return sio.connected

def close():
	"""Disconnect from the echo server
	"""
	sio.disconnect()

def change_id(newID):
	"""Change the ID used to connect to the echo server

	Parameters
	----------
	newID : string
		New ID to connect with
	"""
	sio.emit('ID',[newID,"send"])
	print(f'Login sent with ID: {newID}, copy this ID into the renderer to connect.')
