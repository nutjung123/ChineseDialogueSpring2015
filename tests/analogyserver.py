#! /usr/bin/env python3
from flask import Flask
from flask import request
import urllib.request
import json

from analogyscoretest3_5 import AIMind

app = Flask(__name__)

@app.route('/get_analogy', methods=['GET'])
def get_analogy():
    try:
        feature_id = request.args.get("id")

        #get graph data from knowledge explorer
        graphdata = urllib.request.urlopen("%s/generate/xml"%request.remote_addr)
        a1 = AIMind(rawdata=graphdata)
        analogyData = a1.find_best_analogy(a1.get_feature(feature_id))

        if analogyData:
            _, (src, trg), hypotheses, matches = analogyData
            connections = [a1.get_id(a) for a in matches]
            evidence = [(a1.get_id(a),a1.get_id(b[0])) for a,b in hypotheses.items()]
            explanation = a1.briefly_explain_analogy(analogyData)
            data = {
                "source":a1.get_id(src), #source topic
                "target":a1.get_id(trg), #target topic
                "evidence":evidence, #analogous mappings
                "connections":connections, #direct connections
                "explanation":explanation #text explanation
            }

            #post data back to knowledge explorer
            req = urllib.request.Request(
                        "%s/callback/analogy"%request.remote_addr,
                        data=json.dumps(data).encode('utf8'),
                        headers={'content-type': 'application/json'})
            urllib.request.urlopen(req)
            return "Success"
        else:
            req = urllib.request.Request(
                        "%s/callback/analogy"%request.remote_addr,
                        data='{}'.encode('utf8'),
                        headers={'content-type': 'application/json'})
            urllib.request.urlopen(req)
            return "No analogy found"

    except Exception as e:
        return "Exception: ",e

if __name__ == '__main__':
    app.run(debug=True)