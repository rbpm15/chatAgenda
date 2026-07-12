import json
import urllib.request
import urllib.error
import jwt
import time

def get_token():
    with open('/home/robert/.local/share/ChatAgenda/chatagenda.db', 'rb') as f:
        # Just manually parse the json from the db or use the known service account
        pass
