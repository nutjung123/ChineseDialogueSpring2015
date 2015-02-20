using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LostManuscriptII
{
    public class FeatureGraph
    {
        private List<Feature> features;
        private Feature root;
        public FeatureGraph()
        {
            features = new List<Feature>();
            root = null;
        }
        public bool addFeature(Feature toAdd)
        {
            features.Add(toAdd);
            return true;
        }
        public bool setFeature(string data, Feature change)
        {
            int i = getFeatureIndex(data);
            if (i != -1)
            {
                features[i].Data = change.Data;
                features[i].Neighbors = change.Neighbors;
                features[i].Tags = change.Tags;
                features[i].DiscussedAmount = change.DiscussedAmount;
                features[i].DiscussedThreshold = change.DiscussedThreshold;
                features[i].flag = change.flag;
                features[i].Speaks = change.Speaks;
                return true;
            }
            return false;

        }
        public Feature getFeature(string data)
        {
            for (int x = 0; x < features.Count; x++)
            {
                if (features[x].Data == data) { return features[x]; }
            }
            return null;
        }
        public Feature getFeature(int index)
        {
            return features[index];
        }
        public int getFeatureIndex(string data)
        {
            for (int x = 0; x < features.Count; x++)
            {
                if (features[x].Data == data)
                {
                    return x;
                }
            }
            return -1;
        }
        public bool hasNodeData(string data)
        {
            for (int x = 0; x < features.Count; x++)
            {
                if(features[x].Data == data){return true;}
            }
            return false;
        }
        public bool connect(string A, string B, double weight = 0.0)
        {
            if (A == null || B == null) { throw new Exception("You cannot create a connection between two features if one is null"); }
            if (getFeature(A) == null || getFeature(B) == null) { throw new Exception("You cannot create a connection between two features if one of them is not in this FeatureGraph"); }
            if (A == B) { throw new Exception("You cannot connect a Feature to itself"); }
            getFeature(A).addNeighbor(getFeature(B), weight);
            return true;
        }
        public void print()
        {
            for (int x = 0; x < features.Count; x++)
            {
                features[x].print();
                System.Console.WriteLine("\n");
            }
        }
        public void resetAllFlags()
        {
            for (int x = 0; x < features.Count; x++)
            {
                if (features[x].flag) { features[x].resetReachableFlags(); }
            }
        }
        public List<string> getFeatureNames()
        {
            List<string> names = new List<string>();
            for (int x = 0; x < features.Count; x++)
            {
                names.Add(features[x].Data);
            }
            return names;
        }
        public bool removeFeature(string data)
        {
            for (int x = 0; x < features.Count; x++)
            {
                features[x].removeNeighbor(data);
            }
            for (int x = 0; x < features.Count; x++)
            {
                if (features[x].Data == data)
                {
                    features.RemoveAt(x);
                    return true;
                }
            }
            return false;
        }
        public int Count
        {
            get { return this.features.Count; }
        }
        public Feature Root
        {
            get
            {
                return this.root;
            }
            set
            {
                if (value == null)
                {
                    throw new Exception("You cannot set the root to null");
                }
                else //if (this.root == null)
                {
                    this.root = value;
                }
            }
        }
        public List<Feature> Features
        {
            get { return features; }
        }
    }
}
