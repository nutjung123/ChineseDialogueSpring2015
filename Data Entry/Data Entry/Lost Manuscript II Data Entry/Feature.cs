using System;   
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    [Serializable]
    public class Feature
    {
        private float discussedAmount;                   // This is the "ammount" that this feature has beed the topic of the conversation
        private float discussedThreshold;                // This is the threshold that when reached the topic will have beed exhausted
        private string data;                             // This is the data for this feature, it is essentially all of the information that needs to be represented
        private List<Tuple<Feature, double, string>> neighbors;  // This is a list of tuples that contain all of the features that can be reached from this topic and a weight that defines how distanced they are from the parent feature (this feature)
        private List<Tuple<Feature, double, string>> parents;    // This is a HashSet of features that can be reached to this feature node 
        private List<Tuple<string, string, string>>  tags;       // This is a list of tuples that are used to store the tags (generic, single use pices of information). The first element is the key, and the second element is the Data. This will simply operate as a map.
        private List<string> speaks;
        private List<double> shortestDistance;         //list of shortestDistance to all nodes (index is id)
        private int level, dist;
        public bool flag;                                // This is a public general use flag that can be used for things like traversals and stuff like that
       
        public Feature(string data)
        {
            this.speaks = new List<string>();
            this.data = data;
            this.neighbors = new List<Tuple<Feature, double, string>>();
            this.tags = new List<Tuple<string, string, string>>();
            this.flag = false;
            this.parents = new List<Tuple<Feature, double, string>>();
            this.level = 0;
            this.dist = 0;
            this.shortestDistance = new List<double>();
        }

        // This function is used to get a Feature that is a neighbor of this Feature, it takes a string and preforms a binary search over the list
        public Feature getNeighbor(string data)
        {
            int imax = neighbors.Count - 1;
            int imin = 0;
            while (imax >= imin)
            {
                int imid = (imax + imin) / 2;
                if (String.Compare(neighbors[imid].Item1.Data, data) < 0)
                {
                    imin = imid + 1;
                }
                else if (String.Compare(neighbors[imid].Item1.Data, data) > 0)
                {
                    imax = imid - 1;
                }
                else
                {
                    return neighbors[imin].Item1;
                }
            }
            return null;
        }
        // This function is used to get a Feature that is a neighbor of this Feature, it takes an int and accesses the specific element in the list
        public Feature getNeighbor(int index)
        {
            return neighbors[index].Item1;
        }
        // This function will get the respective edge weight along the connection between this feature and the feature with the passed data
        public double getNeighborWeight(string data)
        {
            if (neighbors.Count == 0) { return -1.0; }
            int checkIndex = neighbors.Count / 2;
            int tmp = checkIndex;
            Feature check = neighbors[checkIndex].Item1;
            if (check.Data == data) { return neighbors[checkIndex].Item2; }
            while (check.Data != data)
            {
                tmp = checkIndex;
                if (neighbors[checkIndex].Item1.Data == data) { return neighbors[checkIndex].Item2; }
                else if (String.Compare(neighbors[checkIndex].Item1.data, data) > 0) { checkIndex += (checkIndex / 2) - 1; }
                else { checkIndex -= (checkIndex / 2) + 1; }
                if (tmp == checkIndex) { return -1; }
                check = neighbors[checkIndex].Item1;
            }
            if (check.Data == data)
            {
                return neighbors[checkIndex].Item2;
            }
            return -1.0;
        }
        // This function will get the respective edge weight along the connection between this feature and the specific one at the passed index
        public double getNeighborWeight(int index)
        {
            return this.neighbors[index].Item2;
        }
        // This function will return the number of neighboring features that are connected to this feature
        public int getNeighborCount()
        {
            return this.neighbors.Count;
        }
        // This function will add a new feature to the neighbors given a new feature and a weight (weight is defaulted to one). 
        // This function will add a neighbor in alphabet order (insert)
        public bool addNeighbor(Feature neighbor, double weight = 1.0, string relationship="")
        {
            if (neighbors.Count == 0)
            {
                neighbors.Add(new Tuple<Feature, double, string>(neighbor, weight,relationship));
                return true;
            }
            for (int x = 0; x < neighbors.Count; x++)
            {
                if (String.Compare(neighbor.Data, neighbors[x].Item1.Data) == 0) { return false; }
                if (String.Compare(neighbor.Data, neighbors[x].Item1.Data) < 0)
                {
                    neighbors.Insert(x, new Tuple<Feature, double, string>(neighbor, weight,relationship));
                    return true;
                }
            }
            neighbors.Add(new Tuple<Feature, double, string>(neighbor, weight, relationship));
            return true;
        }

        public bool addParent(Feature parent, double weight = 1.0, string relationship = "")
        {
            if (parents.Count == 0)
            {
                parents.Add(new Tuple<Feature, double, string>(parent, weight, relationship));
                return true;
            }
            for (int x = 0; x < parents.Count; x++)
            {
                if (String.Compare(parent.Data, parents[x].Item1.Data) == 0) { return false; }
                if (String.Compare(parent.Data, parents[x].Item1.Data) < 0)
                {
                    parents.Insert(x, new Tuple<Feature, double, string>(parent, weight, relationship));
                    return true;
                }
            }
            this.parents.Add(new Tuple<Feature, double, string>(parent, weight, relationship));
            return true;
        }

        public string getRelationshipNeighbor(string neighbor)
        {
            for (int x = 0; x < neighbors.Count; x++)
            {
                if (neighbors[x].Item1.Data == neighbor)
                {
                    return neighbors[x].Item3;
                }
            }
            return "";
        }

        public string getRelationshipParent(string parent)
        {
            for (int x = 0; x < parents.Count; x++)
            {
                if (parents[x].Item1.Data == parent)
                {
                    return parents[x].Item3;
                }
            }
            return "";
        }

        public string getWeight(string neighbor)
        {
            for (int x = 0; x < neighbors.Count; x++)
            {
                if (neighbors[x].Item1.Data == neighbor)
                {
                    return Convert.ToString(neighbors[x].Item2);
                }
            }
            return "";
        }

        //set relationship between this feature and the neighbor
        public bool setNeighbor(Feature neighbor, double weight, string relationship)
        {
            //removeNeighbor(neighbor.Data);
            //addNeighbor(neighbor, 0.0, relationship);
            for (int x = 0; x < neighbors.Count; x++)
            {
                if (neighbors[x].Item1.Data == neighbor.Data)
                {
                    neighbors[x] = new Tuple<Feature, double, string>(neighbors[x].Item1, weight, relationship);
                    return true;
                }
            }
            return false;
        }

        //set relationship between this feature and the neighbor
        public bool setParent(Feature parent, double weight, string relationship)
        {
            for (int x = 0; x < parents.Count; x++)
            {
                if (parents[x].Item1.Data == parent.Data)
                {
                    parents[x] = new Tuple<Feature, double, string>(parents[x].Item1, weight, relationship);
                    return true;
                }
            }
            return false;
        }

        // This function will remove a neighbor that has the string data as its data
        public bool removeNeighbor(string data)
        {
            for (int x = 0; x < neighbors.Count; x++)
            {
                //System.Console.WriteLine(data);
                if (neighbors[x].Item1.Data == data)
                {
                    neighbors.RemoveAt(x);
                    return true;
                }
            }
            return false;
        }

        public bool removeParent(string data)
        {
            for (int x = 0; x < parents.Count; x++)
            {
                if (parents[x].Item1.Data == data)
                {
                    parents.RemoveAt(x);
                    return true;
                }
            }
            return false;
        }

        // This function will check through all of the features that can be reached from its neighbors and if it finds the one that we are looking for it return true, false otherwise
        private bool canReachHelper(string destData,bool checkLevel)
        {
            this.flag = true;
            if (this.data == destData) { return true; }
            for (int x = 0; x < neighbors.Count; x++)
            {
                // checkLevel -> (this.Level < neighbors[x].Item1.Level) {material condition}
                if (neighbors[x].Item1.flag == false && (!checkLevel ||(this.Level < neighbors[x].Item1.Level)) )
                {
                    if (neighbors[x].Item1.canReachHelper(destData,checkLevel)) 
                    {
                        return true; 
                    }
                }
            }
            return false;
        }
        // This function will call and return the canReachHelper method, but it will also rest all of the flags before it is done
        public bool canReachFeature(string destData,bool checkLevel=false)
        {
            bool tmp = canReachHelper(destData,checkLevel);
            resetReachableFlags();
            return tmp;
        }

        public string getSpeak(int index)
        {
            return this.speaks[index];
        }
        public void addSpeak(string newSpeak)
        {
            this.speaks.Add(newSpeak);
        }
        public void editSpeak(int toEdit, string edit)
        {
            if (toEdit < 0 || toEdit >= this.speaks.Count)
            {
                return;
            }
            this.speaks[toEdit] = edit;
        }
        public void removeSpeak(int index)
        {
            this.speaks.RemoveAt(index);
        }

        // This function will return a tuple that represents a tag that is stored in the topic, it does this by linear search, it will then update the lodations of the tags so that 
        //      the most recently accessed tags are at the top of the list and the ones that are older will begin to percolate down, basically this list is sorted by which tag was
        //      most recently accessed. This runs in O(2n) or O(n) where n is the number of elements in the list
        public Tuple<string, string, string> getTag(string key)
        {
            for (int x = tags.Count - 1; x >= 0; x--)                                // So for each tag that we have starting at the bottom and working to the top
            {
                if (tags[x].Item1 == key)                                            // If the current element is what we are looking for
                {
                    if (tags.Count == 1 || x == tags.Count - 1) { return tags[x]; }   // And if we only have one tag, or the tag that we have found is already at the bottom, return it
                    Tuple<string, string, string> tmp = tags[x];                              // Otherwise, store the tag that we have in a temp
                    for (int y = x; y < tags.Count - 1; y++)                          // And for each element that is below the element that we have found
                    {
                        tags[y] = tags[y + 1];                                        // Increase the index of each element below it by one, overwriting the element that we were looking
                                                                                      //      for, and leaving the bottom element unchanged
                    }
                    tags[tags.Count - 1] = tmp;                                       // Then set the last element to the value that we saved
                    return tmp;                                                   // lastly return it
                }
            }
            return null;                                                              // If we never did find it, then return null
        }
        //find the tag type from the input
        //return the tuple of that tag if exist, otherwise null
        public Tuple<string,string,string> findTagType(string type)
        {
            for (int x = 0; x < this.tags.Count();x++)
            {
                if (tags[x].Item3 == type)
                {
                    return tags[x];
                }
            }
            return null;
        }

        // This function will add a new tag to the list of tags, it does NOT do this in order and simply appends this to the end of the list.
        public void addTag(string key, string value, string type)
        {
            if (getTag(key) != null) { throw new Exception("Cannot have two tags with the same keys - Error occured in Feature: " + data + " for the key " + key + " and the value " + value); }
            tags.Add(new Tuple<string, string, string>(key, value, type));
        }
        // This function will either edit an already existing tag, or it will create a new tag based on the key and data that was passed
        public bool editExistingTag(string key, string newData, string type, bool debug = false)
        {
            bool success = removeTag(key);
            if (debug && !success)
            {
                System.Console.WriteLine("When editing the tag " + key + ", there was no tag found with that key. A new key has been added with the passed information");
            }
            addTag(key, newData, type);
            return true;
        }
        // This function will remove the respective tag that is using the key that was passed
        public bool removeTag(string key)
        {
            for (int x = 0; x < tags.Count; x++)
            {
                if (tags[x].Item1 == key)
                {
                    tags.RemoveAt(x);
                    return true;
                }
            }
            return false;
        }
        // This function will return a list of all of the keys that are tags for this feature
        public List<string> getTagKeys()
        {
            List<string> toReturn = new List<string>();
            for (int x = 0; x < tags.Count; x++)
            {
                toReturn.Add(Tags[x].Item1);
            }
            return toReturn;
        }
        // This function will return true if there is already a tag with the passed key and false otherwise
        public bool hasTagWithKey(string key)
        {
            for (int x = 0; x < tags.Count; x++)
            {
                if (tags[x].Item1 == key) { return true; }
            }
            return false;
        }
        // This function will remove all of the tags that are on this topic and delete them
        public void clearTags()
        {
            tags.Clear();
        }

        //Accessors and Mutators
        public float DiscussedAmount
        {
            get{return this.discussedAmount;}
            set
            {
                if (value >= 0)
                {
                    this.discussedAmount = value;
                    return;
                }
                this.discussedAmount = 0;
                System.Console.WriteLine("You cannot set the varaible discussedAmount to a negative value, it has been defaulted to zero (0)");
            }
        }
        public float DiscussedThreshold
        {
            get { return this.discussedThreshold; }
            set
            {
                if (value >= 0)
                {
                    this.discussedThreshold = value;
                    return;
                }
                this.discussedThreshold = 0;
                System.Console.WriteLine("You cannot set the varaible discussedThreshold to a negative value, it has been defaulted to zero (0)");
            }
        }
        public string Data
        {
            get { return this.data; }
            set
            {
                if (value == null)
                {
                    throw new Exception("Cannot set a Feature's data to null");
                }
                this.data = value;
            }
        }
        public int Dist
        {
            get
            {
                return this.dist;
            }
            set
            {
                this.dist = value;
            }
        }

        public List<Tuple<Feature, double, string>> Neighbors
        {
            get { return this.neighbors; }
            set { this.neighbors = value; }
        }
        public List<Tuple<string, string, string>> Tags
        {
            get
            {
                return this.tags;
            }
            set
            {
                this.tags = value;
            }
        }
        public List<string> Speaks
        {
            get
            {
                return this.speaks;
            }
            set
            {
                this.speaks = value;
            }
        }

        public int Level
        {
            get
            {
                return this.level;
            }
            set
            {
                this.level = value;
            }
        }

        public List<double> ShortestDistance
        {
            get
            {
                return this.shortestDistance;
            }
            set
            {
                this.shortestDistance = value;
            }
        }

        public List<Tuple<Feature, double, string>> Parents
        {
            get
            {
                return this.parents;
            }
            set
            {
                this.parents = value;
            }
        }

        public Feature NearestNeighbor
        {
            get
            {
                double best = Double.MaxValue;
                int bestIndex = -1;
                for (int x = 0; x < neighbors.Count; x++)
                {
                    if (neighbors[x].Item2 <= best)
                    {
                        bestIndex = x;
                        best = neighbors[x].Item2;
                    }
                }
                if (bestIndex < 0){return null;}
                return neighbors[bestIndex].Item1;
            }
        }

        // This function will look at how long this feature has been discussed and compare it to the threshold value. If the current amount of discussion is greater than or equal to the threshold value, it will return true otherwise it will return false 
        public bool doneDiscussing()
        {
            return (discussedThreshold >= discussedAmount);
        }
        // This function will look at the current value for how much it has been discussed, and it it has been touched on at all (ie. its value is greater than zero), it will return true
        public bool beenDiscussed()
        {
            return (discussedAmount > 0);
        }
        // This function will look through all of the features that can be reached from this feature and reset the flag value to false
        public void resetReachableFlags()
        {
            this.flag = false;
            for (int x = 0; x < neighbors.Count; x++)
            {
                if (neighbors[x].Item1.flag) { neighbors[x].Item1.resetReachableFlags(); }
            }
        }
        // This function will print out a literal representation of this node and all of the nodes that it can reach (i.e. this function will recurse over the elements in the graph
        public void print()
        {
            System.Console.WriteLine("================  FEATURE  ================");
            System.Console.WriteLine("================ Variables ================");
            System.Console.WriteLine("\tData:               " + this.data);
            System.Console.WriteLine("\tDiscussedAmmount:   " + this.DiscussedAmount);
            System.Console.WriteLine("\tDiscussedThreshold: " + this.DiscussedThreshold);
            System.Console.WriteLine("================ Tags      ================");
            for (int x = 0; x < Tags.Count; x++)
            {
                System.Console.WriteLine("\t" + Tags[x].Item1 + ":" + Tags[x].Item2);
            }
            System.Console.WriteLine("================ Neighbors ================");
            for (int x = 0; x < neighbors.Count; x++)
            {
                System.Console.WriteLine("\t" + Neighbors[x].Item2 + "\t" + Neighbors[x].Item1.Data); 
            }
            System.Console.WriteLine("===========================================\n");
        }

        public static bool operator ==(Feature x, Feature y)
        {
            if((object)x == null && (object)y == null){return true;}
            try{return x.Equals(y) && y.Equals(x); }
            catch{ return false; }
        }
        public static bool operator !=(Feature x, Feature y)
        {
            return !(x == y);
        }
        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            try
            {
                Feature x = (Feature)obj;
                Feature y = this;
                if (x.Data != y.Data) { return false; }
                if (x.DiscussedThreshold != y.DiscussedThreshold) { return false; }
                if (x.DiscussedAmount != y.DiscussedAmount) { return false; }
                if (x.Tags != y.Tags) { return false; }
                if (x.Neighbors != y.Neighbors) { return false; }
                return true;
            }
            catch { return false; }
        }

        public Feature deepCopy()
        {
            Feature copy = DeepClone.DeepCopy<Feature>(this);
            return copy;
        }
    }
}
