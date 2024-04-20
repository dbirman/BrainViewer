
from . import client
import requests
import hashlib

from vbl_aquarium.models.dock import BucketRequest, SaveRequest, LoadRequest, LoadModel

# Define the API endpoint URL
api_url = "http://localhost:5000"

active_bucket = None
password_hash = None
test_token = "c503675a-506c-48c0-9b5e-5265e8260a06"

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
      with open(filename, 'rb') as file:
        json_data = file.read()
      
    #   data = LoadModel(**)

    else:
      check_and_store(bucket_name, password)

      data = SaveRequest(
          filename= "" if filename is None else filename,
          bucket= active_bucket,
          password= password_hash
      )

      client.sio.emit('urchin-save', data.to_string())

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
        raise Exception("Not implemented yet")
    
    check_and_store(bucket_name, password)

    data = LoadRequest(
        filename= "" if filename is None else filename, 
        bucket= active_bucket,
        password= password_hash
    )

    client.sio.emit('urchin-load', data.to_string())

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
