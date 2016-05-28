#! /usr/bin/env python3
from flask import Flask
from flask import request
import urllib
import urllib.request
import json
import cgi

from analogyscoretest3_7 import AIMind

app = Flask(__name__)

@app.route('/get_analogy', methods=['GET'])
def get_analogy():
    #try:
        port = request.args["port"]
        feature_id = request.args["id"]
        filename = request.args.get("filename")
        return_address = "http://%s:%s"%(request.remote_addr, port)

        print("ret: ",return_address)

        if not filename:
            #get graph data from knowledge explorer
            graphdata = urllib.request.urlopen("%s/generate/xml"%return_address)
            graphdata_buffer = graphdata.read()#.decode("utf-8")
            a1 = AIMind(rawdata=graphdata_buffer)
##        else:
##            #else read specified file
##            a1 = AIMind(filename=urllib.unquote(filename).decode('utf8') )
        analogyData = a1.find_best_analogy(a1.get_feature(feature_id),a1)

        if analogyData:
            score, (src, trg), mapping, hypotheses = analogyData
            evidence = [(a1.get_id(a[1]),a1.get_id(b[1])) for a,b in hypotheses.items()]
            explanation = cgi.escape(a1.explain_analogy(analogyData))
            data = {
                "source":a1.get_id(src), #source topic
                "target":a1.get_id(trg), #target topic
                "evidence":evidence, #analogous mappings
                "connections":[], #direct connections
                "explanation":explanation #text explanation
            }
        else:
            data = {}

        #post data back to knowledge explorer
        req = urllib.request.Request(
                        "%s/callback/analogy"%return_address,
                        data=json.dumps(data).encode('utf8'),
                        headers={'content-type': 'application/json'})
        urllib.request.urlopen(req)

        return "Success"

    #except Exception as e:
        return "Exception: ",e

if __name__ == '__main__':
    app.run(debug=True)