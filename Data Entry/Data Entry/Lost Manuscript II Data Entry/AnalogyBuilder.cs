using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry {
	class AnalogyBuilder {
		private FeatureGraph feature_graph;

		public AnalogyBuilder(FeatureGraph feature_graph) {
			this.feature_graph = feature_graph;
		}

		private HashSet<string> generate_candidates(string node) {
			//find analogy candidates based on shared relations
			HashSet<string> ret = new HashSet<string>();
			foreach (var r in feature_graph.relationMap[node]) {
				foreach(var kvp in feature_graph.inverse_relationMap[r.Item1]) {
					if(!(kvp.Item1.Equals(node))) {
						ret.Add(kvp.Item1);
					}
				}
			}
			return ret;
		}

		private HashSet<string> generate_dest_candidates(string rtype) {
			//generate destination candidates based on relation type
			HashSet<string> ret = new HashSet<string>();
			foreach(var r in feature_graph.inverse_relationMap[rtype]) {
				ret.Add(r.Item2);
			}
			return ret;
		}

		public HashSet<Tuple<string,string>> find_best_analogy(Feature feature) {
			//given a feature, find the best analogy for it
			string node = feature.Name;

			if (!feature_graph.relationMap.ContainsKey(node)) return null;

			//find candidates based on immediate relations, ignore self
			HashSet<string> candidates = generate_candidates(node);

			foreach (var c in candidates) {
				Console.WriteLine("candidate for " + node + ": " + c);
			}

			//node relations
			HashSet<Tuple<string, string>> nrels = feature_graph.relationMap[node];
			//node relation types
			HashSet<string> ntypes = new HashSet<string>();
			foreach (var r in nrels) {
				ntypes.Add(r.Item1);
			}

			//score each candidate hypothesis
			List<Tuple<float, HashSet<Tuple<string, string>>>> results = new List<Tuple<float, HashSet<Tuple<string, string>>>>();

			foreach (var c in candidates) {
				HashSet<Tuple<string, string>> hypotheses = new HashSet<Tuple<string, string>> { new Tuple<string, string>(c, node) };
				float score = 0;
				//for each relation in the candidate relations
				foreach (var rd in feature_graph.relationMap[c]) {
					string rtype = rd.Item1;
					string dest = rd.Item2;
					if (nrels.Contains(new Tuple<string, string>(rtype, dest))) {
						//if exact match
						score += 1;
					}
					else if (ntypes.Contains(rtype)) {
						//otherwise try to find another mapping
						foreach (string c2 in generate_dest_candidates(rtype)) {
							if (!c2.Equals(dest)) {
								if (nrels.Contains(new Tuple<string, string>(rtype, c2))) {
									hypotheses.Add(new Tuple<string, string>(dest, c2));
									score += .5f;
								}
							}
						}
					}
					else {
						score -= 1;
					}
				}
				results.Add(new Tuple<float, HashSet<Tuple<string, string>>>(score, hypotheses));
			}
			//sort by score and return the best mappings
			return results.OrderByDescending(x => x.Item1).ToList()[0].Item2;
		}

		public string elaborate_on_analogy(HashSet<Tuple<string, string>> analogy) {
			Dictionary<string, string> akeys = new Dictionary<string, string>();
			foreach(var x in analogy) {
				akeys[x.Item1] = x.Item2;
			}
			string narrative = "";
			foreach(var kvp in akeys) {
				//list of tuples of analogous relations (x rel y) --> (a rel b)
				List<Tuple<string, string, string, string, string, string>> nchunks = new List<Tuple<string, string, string, string, string, string>>();
				string k = kvp.Key;
				string v = kvp.Value;
				if (feature_graph.relationMap.ContainsKey(k)) {
					narrative += k + " is like " + v + ". ";
					narrative += "This is because";
					HashSet<string> rtypes = new HashSet<string>();
					foreach (var r in feature_graph.relationMap[k]) {
						rtypes.Add(r.Item1);
					}
					foreach (var ab in feature_graph.relationMap[k]) {
						foreach (var xy in feature_graph.relationMap[v]) {
							if (rtypes.Contains(xy.Item1) && akeys.ContainsKey(ab.Item2)) {
								if (akeys[ab.Item2] == xy.Item2) {
									nchunks.Add(new Tuple<string, string, string, string, string, string>(k, ab.Item1, ab.Item2, v, xy.Item1, xy.Item2));
								}
							}
						}
					}
					for(int i=0; i<nchunks.Count; i++) {
						var nc = nchunks[i];
						if(i == nchunks.Count - 1) {
							narrative += String.Format(" and {0} {1} {2} in the same way that {3} {4} {5}.\n", nc.Item1, nc.Item2, nc.Item3, nc.Item4, nc.Item5, nc.Item6);
						}
						else {
							narrative += String.Format(" {0} {1} {2} in the same way that {3} {4} {5},", nc.Item1, nc.Item2, nc.Item3, nc.Item4, nc.Item5, nc.Item6);
						}
					}
				}	
			}
			return narrative;
		}
	}
}
