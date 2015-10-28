using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dialogue_Data_Entry
{
    public class FeatureGraph
    {
        private List<Feature> features;
        private Feature root;
        private int maxDepth;
        private double maxDistance;

        //An array of weights, for use in calculations.
        //The indices are as follows:
        //  0 - discuss amount weight
        //  1 - novelty weight
        //  2 - spatial constraint weight
        //  3 - hierarchy constraint weight
        private double[] weight_array;

        //A list of constraints for the system.
        //Each constraint consists of the name of the feature the node is for
        //and a list of clauses AND or OR with each other.
        public List<Constraint> constraint_list;

        public FeatureGraph()
        {
            features = new List<Feature>();
            root = null;
            maxDepth = -1;
            maxDistance = -1;
            //Default values for weights
            //double discussAmountW = -3.0;
            //double noveltyW = -1.0;
            //double spatialConstraintW = 1.0;
            //double hierachyConstraintW = 1.0;
            //double temporalConstraintW = 1.0;
            weight_array = new double[Constant.WeightArraySize];
            weight_array[Constant.DiscussAmountWeightIndex] = -3.0;
            weight_array[Constant.NoveltyWeightIndex] = -1.0;
            weight_array[Constant.SpatialWeightIndex] = 1.0;
            weight_array[Constant.HierarchyWeightIndex] = 100.0;
            weight_array[Constant.TemporalWeightIndex] = 0.2;
            //joint weight relates to mentioning nodes together
            weight_array[Constant.JointWeightIndex] = 100.0f;
            constraint_list = new List<Constraint>();
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
                        if (current.Neighbors[x].Item1.Dist == 0)
                        {
                            current.Neighbors[x].Item1.Dist = current.Dist + 1;
                            queue.Add(current.Neighbors[x].Item1);
                        }
                    }
                    for (int x = 0; x < current.Parents.Count;x++)
                    {
                        if (current.Parents[x].Item1.Dist == 0)
                        {
                            current.Parents[x].Item1.Dist = current.Dist + 1;
                            queue.Add(current.Parents[x].Item1);
                        }
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
                //Children edge
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
                //Parent edge
                for (int y = 0; y < this.Features[x].Parents.Count;y++)
                {
                    int ind = this.getFeatureIndex(this.Features[x].Parents[y].Item1.Data);
                    double dist = this.Features[x].Parents[y].Item2;
                    this.Features[x].ShortestDistance[ind] = dist;
                }
            }

            //All-pair shortest path
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
            //var sw = new Stopwatch();
            //sw.Start();
            
            allPairShortestPath();
            
            //sw.Stop();
            //Console.WriteLine("All-pair took "+sw.Elapsed);
            
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

        //Get a single weight from the weight array
        public double getSingleWeight(int weight_index)
        {
            if (weight_index < 0 || weight_index >= weight_array.Length)
                return -1;
            return weight_array[weight_index];
        }//end method getSingleWeight
        //Get the entire weight array
        public double[] getWeightArray()
        {
            return weight_array;
        }//end method getWeightArray
        //Set a single weight in the weight array
        public void setWeight(int weight_index, double weight_to_set)
        {
            if (weight_index < 0 || weight_index >= weight_array.Length)
                return;
            weight_array[weight_index] = weight_to_set;
        }//end method setWeight

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

        //Add a new constraint to the constraint list.
        public void addConstraint(Constraint new_constraint)
        {
            constraint_list.Add(new_constraint);
        }//end method addConstraint
        //Add a new constraint to the constraint list by parsing it as a string.
        public void addConstraint(string feature, string constraint)
        {
            
        }//end method addConstraint

        //Clear all constraints
        public void clearConstraints()
        {
            constraint_list.Clear();
        }//end method clearConstraints

        //Get a list of feature names for which the constraints are satisfied.
        //Base this off of an input list of features that have been traversed.
        public List<string> getSatisfiedFeatures(List<string> history_list)
        {
            List<string> return_list = new List<string>();

            //Go through each constraint
            string current_feature = "";
            List<Clause> temp_clause_list = new List<Clause>();
            foreach (Constraint constraint in constraint_list)
            {
                //Pull out each item
                current_feature = constraint.name;
                temp_clause_list = constraint.clauses;

                bool constraint_true = true;

                //If there is only one clause, just check it.
                if (temp_clause_list.Count == 1)
                {
                    constraint_true = evaluateClause(temp_clause_list[0], history_list);
                }//end if
                else
                {
                    //Go through and "fold up" the clauses to determine if the entire constraint is true or false.
                    bool previous_result = true;
                    for (int i = 0; i < temp_clause_list.Count; i++)
                    {
                        //If this is the first clause, check it alone.
                        if (i == 0)
                        {
                            previous_result = evaluateClause(temp_clause_list[0], history_list);
                            continue;
                        }//end if

                        //For any other clause, check it against the previous result to determine 
                        //the total result.
                        //If their relationship is an AND, both clauses must be true.
                        if (temp_clause_list[i].getOuterRelationshipId() == 0)
                        {
                            if (previous_result && evaluateClause(temp_clause_list[i], history_list))
                                previous_result = true;
                            else
                                previous_result = false;
                        }//end if
                        //If their relationship is an OR, at least one clause must be true.
                        else if (temp_clause_list[i].getOuterRelationshipId() == 1)
                        {
                            if (previous_result || evaluateClause(temp_clause_list[i], history_list))
                                previous_result = true;
                            else
                                previous_result = false;
                        }//end if
                    }//end for

                    //previous_result now tells us whether the constraint is true or false
                    constraint_true = previous_result;
                }//end else

                //If the constraint is true, add the name of this feature
                //to the return list (if it is not already there).
                //It is elligible for selection.
                if (constraint_true)
                {
                    //Also check if the node is already in the history list; no repeats allowed.
                    if (!return_list.Contains(constraint.name)
                        && !history_list.Contains(constraint.name))
                        return_list.Add(constraint.name);
                }//end if

            }//end foreach

            return return_list;
        }//end method getConstraints

        //Decide whether a clause is true or false based on the clause and a feature history list.
        private bool evaluateClause(Clause input_clause, List<string> history_list)
        {
            //Determine whether the clause is true or false.
            bool clause_true = false;
            //ID = 0 is A > B, which means A must be later than B
            //ID = 1 is A < B, which means A must be sooner than B
            //See which of the names in the clause appears in the history list first.
            int name_1_index = history_list.IndexOf(input_clause.getName1());
            int name_2_index = history_list.IndexOf(input_clause.getName2());

            int inner_rel_id = input_clause.getInnerRelationshipId();

            if (inner_rel_id == 0)
            {
                //relationship A > B

                //The first name must be later than the second name
                //If the second name doesn't appear at all, this clause if false.
                if (name_2_index == -1)
                {
                    clause_true = false;
                }//end if
                //If the first name doesn't appear at all and the second one does, this clause is true.
                else if (name_1_index == -1)
                {
                    clause_true = true;
                }//end if
                //If the second name has a lesser index than the first, this clause is true.
                else if (name_2_index < name_1_index)
                {
                    clause_true = true;
                }//end else if
                //Otherwise, the clause if false
                else
                    clause_true = false;
            }//end if
            else if (inner_rel_id == 1)
            {
                //relationship A < B

                //The second name must be later than the first.
                //If the first name doesn't appear at all, this clause if false.
                if (name_1_index == -1)
                {
                    clause_true = false;
                }//end if
                //If the second name doesn't appear at all and the first one does, this clause is true.
                else if (name_2_index == -1)
                {
                    clause_true = true;
                }//end if
                //If the first name has a lesser index than the second, this clause is true.
                else if (name_1_index < name_2_index)
                {
                    clause_true = true;
                }//end else if
                //Otherwise, the clause is false
                else
                    clause_true = false;
            }//end else if
            else if (inner_rel_id == 2)
            {
                //relationship A true

                //The first name must have appeared at all in the history list.
                //If the first name doesn't appear at all, this clause is false.
                if (name_1_index == -1)
                {
                    clause_true = false;
                }//end if
                //Otherwise, this clause is true.
                else
                {
                    clause_true = true;
                }//end else
            }//end else if

            //If the clause is negated, negate it here.
            if (input_clause.getNot())
            {
                if (clause_true)
                    clause_true = false;
                else
                    clause_true = true;
            }//end if

            return clause_true;
        }//end method evaluateClause
    }
}
