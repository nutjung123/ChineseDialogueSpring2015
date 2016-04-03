import xml.etree.ElementTree as ET
from pprint import pprint

class Feature:
    def __init__(self,name):
        self.name = name
        self.relations = set() #set of specific relations to other objects
        self.connections = set() #set of associated objects
        self.rtypes = set() #set of relation types between this and other objects
        self.knowledge_level = len(self.relations)

        self.predecessors = set() #inverse connections for fast lookup

        #for topo sort
        self.marked = False
        self.visited = False
        self.value = 0

    def add_predecessor(self,pred):
        self.predecessors.add(pred)

    def add_relation(self,rtype,dest):
        self.connections.add(dest)
        self.relations.add((rtype,dest))
        self.rtypes.add(rtype)
        self.knowledge_level = len(self.relations)

    def __repr__(self):
        return "<%s>(%d,%.d)"%(self.name,self.knowledge_level,self.value)

class AIMind:
    def __init__(self,filename):
        self.features = {}
        self.usage_map = {}


        self.topo_sorted_features = []

        tree = ET.parse(filename)
        root = tree.getroot()
        features = root.find("Features")
        relations = root.find("Relations")

        feature_id_table = {}

        #map all feature ids to name
        for feature in features.iter('Feature'):
            feature_id_table[feature.attrib["id"]] = feature.attrib["data"]

        #build relation structure
        for feature in features.iter('Feature'):
            fobj = Feature(feature.attrib["data"])
            neighbors = feature.find('neighbors')
            for neighbor in neighbors.iter('neighbor'):
                #print(neighbor.attrib)
                fobj.add_relation(neighbor.attrib['relationship'],
                                  feature_id_table[neighbor.attrib['dest']])
            self.features[fobj.name] = (fobj)

        for feature in self.features.values():
            for rtype, dest in feature.relations:
                self.usage_map.setdefault(rtype,set()).add((feature.name,dest))
                self.features[dest].add_predecessor(feature.name)


        def val(v,visited):
            visited.add(v)
            children = self.features[v].connections
            return 1 + sum([val(w,visited) for w in self.features[v].predecessors if not w in visited])

        #calculate values
        for feature in self.features:
            self.features[feature].value = val(feature,set())



    def generate_candidates(self, feature):
        #generate src candidates
        return {a for rtype in self.features[feature].rtypes for a,b in self.usage_map[rtype]  if not a == feature}

    def generate_candidates2(self,rtype):
        #generate dest candidates based on type
        return {b for a,b in self.usage_map[rtype]}

    def find_best_analogy(self,feature):
        #find candidates based on immediate relations, ignore self
        candidates = self.generate_candidates(feature)
        results = []
        node = self.features[feature]
        #score each candidate hypothesis
        for c in candidates:
            cnode = self.features[c]
            main_comparison = (feature,c)
            hypotheses = {} #source --> target mapping
            r_hypotheses = {} #target --> source mapping
            score = 0
            #for each relation in the candidate relations
            for rtype,dest in cnode.relations:
                destval = self.features[dest].value
                if (rtype,dest) in node.relations:#if exact match
                    score += cnode.value
                elif rtype in node.rtypes:#otherwise try to find another mapping
                    for c2 in self.generate_candidates2(rtype):
                        if (rtype,c2) in node.relations:
                            c2val = self.features[c2].value
                            #if potential match
                            #check source --> target consistency
                            if dest in hypotheses:#if already source --> target mapping
                                #check if better
                                tmpval = hypotheses[dest][1]
                                if c2val > tmpval:
                                    score -= tmpval
                                    hypotheses[dest] = (c2,c2val,rtype)
                                    r_hypotheses[c2] = (dest, destval,rtype)
                                    score += c2val

                            #check target --> source consistency
                            elif c2 in r_hypotheses:
                                #check if better
                                tmpval = r_hypotheses[c2][1]
                                if destval > tmpval:
                                    score -= tmpval
                                    del hypotheses[r_hypotheses[c2][0]]
                                    hypotheses[dest] = (c2,c2val,rtype)
                                    r_hypotheses[c2] = (dest, destval,rtype)
                                    score += c2val

                            else:#else make new mapping
                                hypotheses[dest] = (c2,c2val,rtype)
                                r_hypotheses[c2] = (dest, destval,rtype)
                                score += c2val
                else:
                    score -= cnode.value
            results.append((score,main_comparison,r_hypotheses))

        if not results:
            return None

        ##pprint(sorted(results))
        best = sorted(results)[-1]
        return best

    def briefly_explain_analogy(self, analogy):
        #only explain main relation
        score, (h1,h2), evidence = analogy
        narrative = ""
        narrative += "\t%s is like %s. "%(h1,h2)
        narrative += "This is because"
        nchunks = []
        anode = self.features[h1]
        bnode = self.features[h2]
        for a,(b,c,d) in evidence.items():
            nchunks.append((h1,d,a,h2,d,b))
        for i,nc in enumerate(nchunks):
            a,b,c,d,e,f = nc
            if i == len(nchunks)-1:
                narrative += " and %s %s %s in the same way that %s %s %s.\n"%(a,b,c,d,e,f)
            else:
                narrative += " %s %s %s in the same way that %s %s %s,"%(a,b,c,d,e,f)
        print(narrative)

    def elaborate_on_analogy(self, analogy):
        #go ham with the explanation

        score, (h1,h2), evidence = analogy

        ##print(analogy)

        akeys = {h1:h2}
        for a,(b,c) in evidence.items():
            print(a,"-->",b," score: ",c/score)
            akeys[a] = b

        self.topological_sort()

        #sort talking points to make more sense
        talking_points = sorted(akeys.keys(), key=lambda x: self.topo_sorted_features.index(x))

        narrative = ""

        ##print(talking_points)

        for point in talking_points:
            if len(self.features[point].connections):
                narrative += "\t%s is like %s. "%(point,akeys[point])
                narrative += "This is because"
                nchunks = []
                anode = self.features[point]
                bnode = self.features[akeys[point]]
                for a,b in anode.relations:
                    for x,y in bnode.relations:
                        if a == x:
                            nchunks.append((point,a,b,akeys[point],x,y))

                for i,nc in enumerate(nchunks):
                    a,b,c,d,e,f = nc
                    if i == len(nchunks)-1:
                        narrative += " and %s %s %s in the same way that %s %s %s.\n"%(a,b,c,d,e,f)
                    else:
                        narrative += " %s %s %s in the same way that %s %s %s,"%(a,b,c,d,e,f)

        print(narrative)

    def topological_sort(self):
        def explore(feature,node_list):
            if feature.marked:
                return
            elif not feature.visited:
                feature.marked = True
                for dest in feature.connections:
                    tmp = self.features[dest]
                    explore(tmp,node_list)
                feature.visited = True
                feature.marked = False
                node_list.insert(0,feature)

        sorted_nodes = []
        tmp = self.features.values()
        for feature in tmp:
            feature.visited = False
            feature.marked = False

        #cc produces better topic groups than scc
        for cluster in self.connected_components():
            for feature in cluster:
                explore(self.features[feature],sorted_nodes)

        self.topo_sorted_features = [x.name for x in sorted_nodes]
        return sorted_nodes

    def strongly_connected_components(self):

        """yields sets of SCCs"""

        identified = set()
        stack = []
        index = {}
        lowlink = {}

        def dfs(v):
            index[v] = len(stack)
            stack.append(v)
            lowlink[v] = index[v]

            for r,w in self.features[v].relations:
                if w not in index:
                    yield from dfs(w)
                    lowlink[v] = min(lowlink[v], lowlink[w])
                elif w not in identified:
                    lowlink[v] = min(lowlink[v], lowlink[w])

            if lowlink[v] == index[v]:
                scc = set(stack[index[v]:])
                del stack[index[v]:]
                identified.update(scc)
                yield scc

        for v in self.features:
            if v not in index:
                yield from dfs(v)


    def connected_components(self):
        seen = set()
        def component(feature):
            nodes = {feature}
            while nodes:
                node = nodes.pop()
                seen.add(node)
                nodes |= self.features[node].connections - seen
                yield node

        for feature in self.features:
            if feature not in seen:
                yield set(component(feature))



"""
for every directed edge uv from vertex u
to vertex v, u comes before v in the ordering.
"""


##a1 = AIMind('atom-solar.xml')
##pprint(a1.find_best_analogy("Sun"))
##a1.elaborate_on_analogy(a1.find_best_analogy("Sun"))
##pprint(a1.features)


a1 = AIMind('music.xml')
pprint(a1.briefly_explain_analogy(a1.find_best_analogy("Rock music")))




##a1.elaborate_on_analogy(a1.find_best_analogy("Finland"))


#a1 = AIMind('googledata.xml')
#pprint(a1.find_best_analogy("Google"))

#a1.elaborate_on_analogy(a1.find_best_analogy("Python (programming language)"))
#a1.briefly_explain_analogy(a1.find_best_analogy("Python (programming language)"))

#a1.briefly_explain_analogy(a1.find_best_analogy("Google"))


##a = a1.features['Java (programming language)']
##b = a1.features['Python (programming language)']

##a1 = AIMind('atom-solar.xml')
##a1.elaborate_on_analogy(a1.find_best_analogy("Sun"))


##a1 = AIMind('music_small.xml')
##a2 = AIMind('plang_small.xml')
##
##
##pprint(a1.cross_domain_analogy('Singing',a2))

##all_best_analogies = [a1.find_best_analogy(f) for f in a1.features]
##all_best_analogies = [x for x in all_best_analogies if x]
##
##pprint(sorted(all_best_analogies))




