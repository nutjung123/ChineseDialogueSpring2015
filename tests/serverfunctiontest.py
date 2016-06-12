import urllib
import urllib.request
import urllib.parse
import json
from pprint import pprint

req = urllib.request.Request("http://127.0.0.1:5000/get_analogy",
                             data=json.dumps({"filename":"../data%20files/techdata.xml",
                                   "id":"10"

                             }).encode('utf8'),
                             headers={'content-type': 'application/json'})


pprint(json.loads(urllib.request.urlopen(req).read().decode("utf8")))