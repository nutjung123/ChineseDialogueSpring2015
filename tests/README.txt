analogy RESTful server

requirements:
	python 3
	flask module

to start:
	run analogyserver.py

to request an analogy:
	<server_address>:<port>/get_analogy?id=<feature_id>&port=<callback_port>

return data:
	JSON object of format:

	{"source": source feature id <string>
         "target": target feature id <string>
         "evidence": [[source feature id 1 <string>,target feature id 1 <string>],
                      [source feature id 2 <string>,target feature id 2 <string>],
                      ...,
                      [source feature id N <string>,target feature id N <string>]]
         "explanation": explanation of analogy <string>

	OR {} if no analogy found

	This gets posted to <request_address>/callback/analogy