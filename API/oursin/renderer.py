import webbrowser
import os

notebook = False
try:
	from IPython import get_ipython
	if get_ipython() is not None:
		from IPython.display import Javascript
		notebook = True
except ImportError:
	# You are not running in a Jupyter Notebook
	pass

from . import camera, client, custom, lines, meshes, particles, probes, text, texture, ui, utils, volumes

def is_running_in_colab():
	return notebook and 'google.colab' in str(get_ipython())

def setup(localhost = False, standalone = False, id = None):
	"""Connect the Unity Renderer for Neuroscience Python API to the web-based (or standalone) viewer

	Parameters
	----------
	localhost : bool, optional
		connect to a local development server rather than the remote server, by default False
	standalone : bool, optional
		connect to a standalone Desktop build rather than the web-based Brain Viewer, by default False
	"""

	if client.connected():
		print(f'(urchin) Client is already connected. Use ID: {client.ID}')
		return
	
	if id is not None:
		client.ID = id

	if localhost:
		client.sio.connect('http://localhost:5000')
	else:
		client.sio.connect('https://urchin-commserver.herokuapp.com/')

	if not standalone:
		#To open browser window:
		url = f'https://data.virtualbrainlab.org/Urchin/?ID={client.ID}'
		if not is_running_in_colab():
			webbrowser.open(url)
		else:
			# Specify window features
			window_features = "width=1200,height=800,toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes"
			# Use the window.open function with window features
			javascript_code = f'window.open("{url}", "_blank", "{window_features}");'
			# Display the JavaScript code to open the new window
			display(Javascript(javascript_code))

	# Set up the main camera
	camera.setup()

######################
# CLEAR #
######################

def clear():
	"""Clear the renderer scene of all objects
	"""
	camera.clear()
	custom.clear()
	lines.clear()
	meshes.clear()
	particles.clear()
	probes.clear()
	text.clear()
	texture.clear()
	volumes.clear()