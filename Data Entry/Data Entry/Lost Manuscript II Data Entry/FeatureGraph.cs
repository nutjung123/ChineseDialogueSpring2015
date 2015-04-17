using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    public class FeatureGraph
    {
        private List<Feature> features;
        private Feature root;
        private int maxDepth;
        private double maxDistance;
        public FeatureGraph()
        {
            features = new List<Feature>();
            root = null;
            maxDepth = -1;
            maxDistance = -1;
        }

        private void helperMaxDepthDSF(Feature current, int depth, bool[] checkEntry)
        {
            if (current.Neighbors.Count == 0)
            {
                if (depth > this.maxDepth)
                {
                    this.maxDepth = depth;
                }
            }
            
            int index = this.getFeatureIndex(current.Data);
            if (checkEntry[index])
            {
                return;
            }
            checkEntry[index] = true;

            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                helperMaxDepthDSF(current.Neighbors[x].Item1, depth + 1,checkEntry);
            }
        }

        private void helperMaxDepthBFS()
        {
            Feature current = this.Root;
            bool[] checkEntry = new bool[this.Count];
            List<Feature> queue = new List<Feature>();
            queue.Add(current);
            int index=0;
            while (queue.Count > index)
            {
                current = queue[index]; index++;
                if (current.Level > maxDepth)
                {
                    maxDepth = current.Level;
                }
                int ind = this.getFeatureIndex(current.Data);
                if (!checkEntry[ind])
                {
                    checkEntry[ind] = true;
                    for (int x = 0; x < current.Neighbors.Count; x++)
                    {
                        current.Neighbors[x].Item1.Level = current.Level + 1;
                        queue.Add(current.Neighbors[x].Item1);
                    }
                }
            }
        }

        //find max shortest path from the giving node and set shortestDistance 
        private double maxDistBFS(Feature node)
        {
            Feature current = node;
            bool[] checkEntry = new bool[this.Count];
            List<Feature> queue = new List<Feature>();
            queue.Add(current);
            int index = 0;
            double maxDistance = 0;
            //clear dist
            for (int x = 0; x < this.Count;x++ )
            {
                this.Features[x].Dist = 0;
            }

            //initialize node's ShortestDistance to zero
            node.ShortestDistance.Clear();
            for (int x = 0; x < this.Count;x++)
            {
                node.ShortestDistance.Add(0.0);
            }

            while (queue.Count > index)
            {
                current = queue[index]; index++;
                if (current.Dist > maxDistance)
                {
                    maxDistance = current.Dist;
                }
                int ind = this.getFeatureIndex(current.Data);
                if (!checkEntry[ind])
                {
                    checkEntry[ind] = true;
                    node.ShortestDistance[ind] = current.Dist;
                    for (int x = 0; x < current.Neighbors.Count; x++)
                    {
                        current.Neighbors[x].Item1.Dist = current.Dist + 1;
                        queue.Add(current.Neighbors[x].Item1);
                    }
                    for (int x = 0; x < current.Parents.Count;x++)
                    {
                        current.Parents[x].Item1.Dist = current.Dist + 1;
                        queue.Add(current.Parents[x].Item1);
                    }
                }
            }
            return maxDistance;
        }

        //update all features' shortestDistance 
        //shortestDistance has to be empty list
        private void allPairShortestPath()
        {
            for (int x = 0; x < this.Count;x++ )
            {
                this.Features[x].ShortestDistance.Clear();
            }

            //initialize all distance to infinity
            for (int x = 0; x < this.Count;x++)
            {
                for (int y = 0; y < this.Count; y++)
                {
                    this.Features[x].ShortestDistance.Add(2147483646); //maxint -1
                }
            }

            //distance to itself is zero
            for (int x = 0; x < this.Count; x++)
            {
                this.Features[x].ShortestDistance[x] = 0;
            }
            //for each edge (u,v) [if there is an edge connect between two nodes)
            for (int x = 0; x < this.Count; x++)
            {
                for (int y = 0; y < this.Features[x].Neighbors.Count; y++)
                {
                    int ind = this.getFeatureIndex(this.Features[x].Neighbors[y].Item1.Data);
                    double dist = this.Features[x].Neighbors[y].Item2;
                    //if dist = 0, set it to 1. This is because the default weight of edge used to be 0.
                    if (dist == 0)
                    {
                        dist = 1;
                    }
                    this.Features[x].ShortestDistance[ind] = dist;
                }
                for (int y = 0; y < this.Features[x].Parents.Count;y++)
                {
                    int ind = this.getFeatureIndex(this.Features[x].Parents[y].Item1.Data);
                    double dist = this.Features[x].Parents[y].Item2;
                    this.Features[x].ShortestDistance[ind] = dist;
                }
            }
            for (int k = 0; k < this.Count; k++)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    for (int j = 0; j < this.Count; j++)
                    {
                        if (this.Features[i].ShortestDistance[j] > this.Features[i].ShortestDistance[k] + this.Features[k].ShortestDistance[j])
                        {
                            this.Features[i].ShortestDistance[j] = this.Features[i].ShortestDistance[k] + this.Features[k].ShortestDistance[j];
                        }
                    }
                }
            }
        }

        private void printShortestDistance()
        {
            for (int x = 0; x < this.Count; x++)
            {
                for (int y = 0; y < this.Count; y++)
                {
                    Console.Write(this.Features[x].ShortestDistance[y] +" ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public void getMaxDistance()
        {
            //find all the shortest distance from every node to every other node 
            /*for (int x = 0; x < this.Count; x++)
            {
                double temp = maxDistBFS(this.Features[x]);
                if (this.maxDistance < temp)
                {
                    this.maxDistance = temp;
                }
            }*/
            //printShortestDistance();
            allPairShortestPath();
            //find max distance
            for (int x = 0; x < this.Count;x++)
            {
                for (int y = 0; y < this.Count; y++)
                {
                    if (this.Features[x].ShortestDistance[y] > this.MaxDistance)
                    {
                        this.maxDistance = this.Features[x].ShortestDistance[y];
                    }
                }
            }
            //printShortestDistance();
        }

        public int getMaxDepth()
        {
            if(maxDepth==-1)
            {
                if (root != null)
                {
                    //bool[] checkEntry = new bool[this.Count]; 
                    //helperMaxDepthDSF(root, 0, checkEntry);
                    helperMaxDepthBFS();
                }
            }
            return maxDepth;
        }
        public void setMaxDepth(int h)
        {
            maxDepth = h;
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

        public bool setFeatureDiscussedAmount(string data, float amount)
        {
            int i = getFeatureIndex(data);
            if (i != -1)
            {
                features[i].DiscussedAmount = amount;
                return true;
            }
            return false;
        }

        public bool setFeatureData(int index, string newName)
        {
            if (index >= 0 && index < features.Count)
            {
                features[index].Data = newName;
                return true;
            }
            return false;
        }
        public bool setFeatureNeighbors(int index, List<Tuple<Feature, double,string>> newNeighbors)
        {
            if (index >= 0 && index < features.Count)
            {
                features[index].Neighbors = newNeighbors;
                return true;
            }
            return false;
        }
        public bool setFeatureTags(int index, List<Tuple<string, string, string>> newTags)
        {
            if (index >= 0 && index < features.Count)
            {
                features[index].Tags = newTags;
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
            throw new Exception("If you see this msg when you save the file. Please report and don't close your program.");
        }
        public bool hasNodeData(string data)
        {
            for (int x = 0; x < features.Count; x++)
            {
                if(features[x].Data == data){return true;}
            }
            return false;
        }
        public bool connect(string A, string B, double weight = 1.0)
        {
            if (A == null || B == null) { throw new Exception("You cannot create a connection between two features if one is null"); }
            if (getFeature(A) == null || getFeature(B) == null) { throw new Exception("You cannot create a connection between two features if one of them is not in this FeatureGraph"); }
            if (A == B) { throw new Exception("You cannot connect a Feature to itself"); }
            getFeature(A).addNeighbor(getFeature(B), weight);
            getFeature(B).addParent(getFeature(A));
            //getFeature(B).addNeighbor(getFeature(A),weight);
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

        public bool removeDouble(string data)
        {

            bool xyz = false;
            for (int x = 0; x < features.Count; x++)
            {

                if (features[x].Data == data && xyz == false)
                {
                    xyz = true;
                    features.RemoveAt(x);
                    return true;
                }
            }
            return false;
        }

        public double MaxDistance
        {
            get { return this.maxDistance; }
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
            get { return this.features; }
        }
    }
}
