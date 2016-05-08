import xml.etree.ElementTree as ET
from pprint import pprint

from itertools import product

from collections import Counter
from itertools import combinations, permutations, zip_longest
from hashlib import md5

def jaccard_index(a,b):
    if len(a) == len(b) == 0:
        return 1
    return len(a&b) / len(a|b)

def sqmag(v):
    return sum(x*x for x in v)

def sq_edist(v1,v2):
    return sum((x-y)*(x-y) for x,y in zip(v1,v2))

def vadd(v1,v2):
    return tuple(x+y for x,y in zip(v1,v2))



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
                fobj.add_relation(neighbor.attrib['relationship'],
                                  feature_id_table[neighbor.attrib['dest']])
            self.features[fobj.name] = (fobj)

        for feature in self.features.values():
            for rtype, dest in feature.relations:
                self.usage_map.setdefault(rtype,set()).add((feature.name,dest))
                self.features[dest].add_predecessor(feature.name)

        def val(v,visited):
            visited.add(v)
            return 1 + sum([val(w,visited) for w in self.features[v].predecessors if not w in visited])

        #calculate values
        for feature in self.features:
            self.features[feature].value = val(feature,set())

        #calculate rtype jaccard index
        self.rtype_index = self.index_rtypes()

    def explain_analogy(self, analogy, verbose=False):
        #only explain main relation
        if not analogy:
            return

        score, (src,trg), rassert, evidence = analogy
        narrative = ""
        narrative += "\t%s is like %s. "%(src,trg)

        narrative += "This is because"
        nchunks = []

        mentioned = set()

        for (a,b),(c,d) in evidence.items():
            if not verbose and a in mentioned:
                continue
            nchunks.append((src,a,b,trg,c,d))
            mentioned.add(a)
        for i,nc in enumerate(nchunks):
            a,b,c,d,e,f = nc
            if i == len(nchunks)-1:
                narrative += " and %s <%s> %s in the same way that %s <%s> %s.\n"%(a,b,c,d,e,f)
            else:
                narrative += " %s <%s> %s in the same way that %s <%s> %s,"%(a,b,c,d,e,f)
        return narrative

    def index_rtypes(self):
        hm = {} #aggregate rtypes across all usages
        for fnode1 in self.features.values():
            for (rtype,dest) in fnode1.relations:
                loses = fnode1.rtypes - self.features[dest].rtypes
                gains = self.features[dest].rtypes - fnode1.rtypes
                same = self.features[dest].rtypes & fnode1.rtypes
                lc,gc,sm = hm.setdefault(rtype,(Counter(),Counter(),Counter()))
                for r in loses:
                    lc[r] += 1
                for r in gains:
                    gc[r] += 1
                for r in same:
                    sm[r] += 1

        out = {} #compute metrics from rtypes
        for rtype, (lc, gc, sm) in hm.items():
            x = set(lc)
            y = set(gc)
            z = set(sm)

            score = (jaccard_index(x,y),
                     jaccard_index(x,z),
                     jaccard_index(y,z))

            out[rtype] = score
        return out

    def find_best_analogy(self,feature,target_domain):

        if not feature in self.features:
            return None

        ixmap = self.rtype_index
        #merge target domain and current
        #use highest score version for now
        #probably best to merge before jaccard
        for k,v in target_domain.rtype_index.items():
            if k in ixmap:
                ixmap[k] = max(ixmap[k],v,key=lambda x: sqmag(x))
            else:
                ixmap[k] = v

        #filter out rtypes with not enough similarity information to be useful
        useful = {k:v for k,v in ixmap.items() if sqmag(v) != 0}

        node = self.features[feature]
        f1 = [r for r in node.rtypes if r in useful]

        candidate_results = []
        for n,c in target_domain.features.items():
            if n == feature:
                continue
            f2 = [r for r in c.rtypes if r in useful]
            # number of relation mappings is equal to smallest
            # number of relation types, because one-to-one
            a = f1 if len(f1) > len(f2) else f2
            b = f2 if a is f1 else f1

            tmph = list(tuple(product([x],a)) for x in b)

            def good(mhs):
                tmp = set()
                for a,b in mhs:
                    if b in tmp:
                        return False
                    else:
                        tmp.add(b)
                return True

            h2 = [x for x in product(*tmph) if good(x)]

            ret = []

            for mhs in h2:
                rscore = 0
                for r1,r2 in mhs:
                    rscore += sq_edist(useful[r1],useful[r2])
                ret.append((rscore,mhs))

            bijections = sorted(ret)
            ##print(n,len(bijections))
            if not len(bijections):
                continue
            basescore = bijections[0][0]

            ##results = []

            #keep track of the best result only
            bestrating = 0
            bestresult = None

            for bscore, rassert in bijections:
                #for each possible rtype configuration,
                #check how it plays out in reality

                #heuristic to only try relatively reasonable combinations
                if bscore > basescore * 2:
                    break

                rassert = dict(rassert)
                hypotheses = set()
                for r1,d1 in c.relations: #for each pair in candidate
                    destval = target_domain.features[d1].value
                    for r2,d2 in node.relations:#find best rtype to compare with
                        rdiff = sq_edist(ixmap[r1],ixmap[r2])
                        hypotheses.add((destval/(rdiff+1),d2,d1,r1,r2))

                hmap = {}
                best = {}
                rating = node.value
                #for each mh, pick the best then pick the next best non-conflicting
                for score,src,target,r1,r2 in sorted(hypotheses,reverse=True):
                    if r1 == r2 or rassert.get(r1) == r2:
                        if src not in hmap.keys() and target not in hmap.values():
                            hmap[src] = target
                            best[(r2,src)] = (r1,target)
                            rating += score

                if rating > bestrating:
                    bestrating = rating
                    bestresult = (rating,(feature,n),rassert,best)
                ##results.append((rating,(feature,n),rassert,best))


##            if len(results):
##                candidate_results.append(sorted(results,key=lambda x:x[0])[-1])#best local analogy
##            else:
##                return None
            if bestresult:
                candidate_results.append(bestresult)

        return sorted(candidate_results,key=lambda x:x[0])[-1]#best global analogy




##a1 = AIMind('atom-solar.xml')
##
##tmp = a1.find_best_analogy("Sun",a1)
##pprint(tmp)
##print(a1.explain_analogy(tmp))



#a1 = AIMind('plang_small.xml')
#a2 = AIMind('music_small.xml')

a1 = AIMind('googledata.xml')
a2 = AIMind('music.xml')

##for f in a1.features:
##    a1.find_best_analogy(f,a2)
#pprint(a1.find_best_analogy("C (programming language)",a2))
##pprint(a1.find_best_analogy("California",a2))
##pprint(a2.find_best_analogy("Mister Maker Comes to Town",a1))

tmp = a1.find_best_analogy("C (programming language)",a2)
pprint(tmp)
print(a1.explain_analogy(tmp,True))


##tmp = a2.find_best_analogy("Rock music",a1)
##pprint(tmp)
##print(a1.explain_analogy(tmp))

