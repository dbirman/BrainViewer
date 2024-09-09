
from . import client
import requests
import hashlib
import os

from vbl_aquarium.models.dock import BucketRequest, SaveRequest, LoadRequest, DockModel, LoadModel

# Define the API endpoint URL
api_url = "http://localhost:5000"

active_bucket = None
password_hash = None
test_token = "c503675a-506c-48c0-9b5e-5265e8260a06"

callback_filename = None

def _save_callback(data):
    global callback_filename

    current_path = os.getcwd()
    full_path = os.path.join(current_path, callback_filename)

    print(f"(dock) Save callback received, saving data to {full_path}")
    # data will be a serialized LoadModel, but we can just ignore that and save it


    with open(full_path, 'w') as file:
        file.write(data)

def create_bucket(bucket_name, password, api_url = api_url, token = test_token):
    """Create a new bucket for storing data

    Parameters
    ----------
    bucket_name : str
        Folder data will be stored in
    password : str
        Passwords are hashed client-side
    api_url : str, optional
        host:port, by default api_url

    Returns
    -------
    str
        bucket name
    """
    global active_bucket
    global password_hash

    headers = {
        "Content-Type": "application/json"
    }

    # Set the API URL in Urchin
    api_data = DockModel(
        dock_url=api_url
    )

    client.sio.emit('urchin-dock-data', api_data.to_json_string())

    # Request new bucket
    create_url = f'{api_url}/create/{bucket_name}'

    active_bucket = bucket_name
    password_hash = hash256(password)

    data = BucketRequest(
        token = test_token,
        password = password_hash
    )

    print(f'Attempting to create {create_url}')
    response = requests.post(create_url, data=data.model_dump_json(), headers=headers)

    # Check the response
    if response.status_code == 201:
        print(response.text)
    else:
        print("Error:", response.status_code, response.text)

    return bucket_name


def save(filename = None, bucket_name = None, password = None):
    """Save all current data, either to a file or to a cloud bucket.

    Either the filename or bucket/password are required

    Parameters
    ----------
    bucket_name : str
    password : str
    """
    global active_bucket
    global password_hash

    if filename is not None:
        global callback_filename
        callback_filename = filename
    else:
        check_and_store(bucket_name, password)

    data = SaveRequest(
        filename= "" if filename is None else filename,
        bucket= "" if active_bucket is None else active_bucket,
        password= "" if password_hash is None else password_hash
    )

    client.sio.emit('urchin-save', data.to_json_string())

def load(filename = None, bucket_name = None, password= None):
    """Load all data from a bucket

    Either a filename or bucket/password are required

    Parameters
    ----------
    filename : str, optional
    bucket_name : str, optional
    password : str, optional
    """
    global active_bucket
    global password_hash

    if filename is not None:
        with open(filename, 'r') as file:
            data_raw = file.read()
            
        client.sio.emit('urchin-load-data', data_raw)
    else:
    
        check_and_store(bucket_name, password)

        data = LoadRequest(
            filename= "" if filename is None else filename,
            bucket= "" if active_bucket is None else active_bucket,
            password= "" if password_hash is None else password_hash
        )

        client.sio.emit('urchin-load', data.to_json_string())

def check_and_store(bucket_name, password):
    global active_bucket
    global password_hash
    bucket_name = bucket_name if bucket_name is not None else active_bucket
    if bucket_name is None:
        raise Exception("Bucket name is required if there isn't one already stored.")
    elif active_bucket is None:
        active_bucket = bucket_name

    password = hash256(password) if password is not None else password_hash
    if password is None:
        raise Exception("Password is required if there isn't one already stored.")
    elif password_hash is None:
        password_hash = password

def hash256(password):
    return hashlib.sha256(password.encode()).hexdigest()
