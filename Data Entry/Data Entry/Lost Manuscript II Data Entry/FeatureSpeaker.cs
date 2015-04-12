using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dialogue_Data_Entry;

namespace Dialogue_Data_Entry
{
    class FeatureSpeaker
    {
        private float[] dramaticFunction;

        public FeatureSpeaker()
        {
            //define dramaticFunction manually here
            dramaticFunction = new float[10] {1,2,3,2,1,2,3,4,5,3};
        }

        //call this function with answer =-1;
        private void getHeight(FeatureGraph featGraph, Feature current, Feature target, int h, ref int height,bool[] checkEntry)
        {
            if (current == target)
            {
                height = h;
            }
            if (height != -1)
            {
                return;
            }
            
            int index = featGraph.getFeatureIndex(current.Data);
            if (checkEntry[index])
            {
                return;
            }
            checkEntry[index] = true;

            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                getHeight(featGraph, current.Neighbors[x].Item1, target, h + 1, ref height,checkEntry);
            }
        }

        private double getScore(FeatureGraph featGraph, Feature current,Feature oldTopic, int h,int oldh)
        {
            double score =0;
            
            //check dramatic goal value



            //check mentionCount
            float DiscussedAmount = current.DiscussedAmount;

            //check hierachical consistency
            int FathertoChild = 0; //old is a father
            int ChildtoFather = 0; //old is a child
            if(!(oldTopic==current))
            {
                if(oldTopic.canReachFeature(current.Data))
                {
                    FathertoChild = 1;
                }
                if(current.canReachFeature(oldTopic.Data))
                {
                    ChildtoFather = 1;
                }
            }
            //check difference distance 
            double diffDist = Math.Abs(h - oldh);

            //Score calculation
            double maxDepth = (double) featGraph.getMaxDepth();
            score = DiscussedAmount * -1.0 + FathertoChild * 1.0 + ChildtoFather * 1.0 + (maxDepth-diffDist)/maxDepth;

            return score;
        }

        private void travelGraph(FeatureGraph featGraph,Feature current, Feature oldTopic,int h,
            int oldh, int limit, ref List<Tuple<Feature,double> > listScore,bool[] checkEntry)
        {
             //current's height is higher than the limit
            if (h > limit)
            {
                return;
            }
            int index = featGraph.getFeatureIndex(current.Data);
            if (checkEntry[index])
            {
                return;
            }
            checkEntry[index] = true;
            //Calculate score and add to list
            listScore.Add(new Tuple<Feature, double>(current, getScore(featGraph, current, oldTopic,h,oldh)));
            //search children of current node
            for (int x = 0; x < current.Neighbors.Count; x++) 
            {
                travelGraph(featGraph, current.Neighbors[x].Item1, oldTopic, h+1, oldh,limit, ref listScore,checkEntry);
            }
        }

        public Feature getNextTopic(FeatureGraph featGraph, Feature oldTopic, string query, int turn)
        {
            if (turn == 1)
            {
                //initial case
                return featGraph.Root;

            }else if(turn > 1 && query =="")
            {
                //next topic case
                int height = -1;
                int limit = 999;
                bool[] checkEntry = new bool[featGraph.Count]; //checkEntry is to check that it won't check the same node again
                getHeight(featGraph, featGraph.Root, oldTopic, 0, ref height,checkEntry);
                checkEntry = new bool[featGraph.Count];
                //search the next topic
                List<Tuple<Feature, double>> listScore = new List<Tuple<Feature,double>>();
                travelGraph(featGraph, featGraph.Root, oldTopic, 0, height, limit, ref listScore,checkEntry);
                
                //find max score
                if (listScore.Count == 0)
                {
                    return null;
                }
                double maxScore = listScore[0].Item2;
                int maxIndex = 0;
                for (int x = 1; x < listScore.Count; x++)
                {
                    if (listScore[x].Item2 > maxScore)
                    {
                        maxScore = listScore[x].Item2;
                        maxIndex = x;
                    }
                }
                return listScore[maxIndex].Item1;

            }else if(turn >1 && query != "")
            {
                //answer question case
            }
            return null;
        }

        public string getChildSpeak(Feature toSpeak)
        {
            string result = toSpeak.Data;
            Random myRand = new Random();
            List<string> startOptions = new List<string>();
            startOptions.Add(" is a very interesting topic. ");
            startOptions.Add(" is important, a lot can be said about it. ");
            startOptions.Add(" is something important to me. ");
            result += startOptions[myRand.Next(0, 3)];
            if (toSpeak.Neighbors.Count == 0)
            {
                return result;
            }

            //select the elements
            List<Tuple<Feature, double,string>> toDirect = toSpeak.Neighbors;
            if (toSpeak.Neighbors.Count > 5)
            {
                toDirect = new List<Tuple<Feature,double,string>>();
                int choice = 0;
                while(toDirect.Count < 5)
                {
                    choice = myRand.Next(0, 5);
                    if(!toDirect.Contains(toSpeak.Neighbors[choice]))
                    {
                        toDirect.Add(toSpeak.Neighbors[choice]);
                    }
                }
            }
            //make the string
            result += "There are several things, mainly ";
            for (int x = 0; x < toDirect.Count; x++)
            {
                result += toDirect[x].Item1.Data;
                if (x + 1 < toDirect.Count)
                {
                    result += ", ";
                    if (x + 2 == toDirect.Count)
                    {
                        result += " and ";
                    }
                }
                else
                {
                    result += ".";
                }
            }
            return result;
        }

        public string getTagSpeak(Feature toSpeak)
        { /*
                Attribute (has)
                Base (in)
                Base Past (in)
                Date (on)
                Property (is)
                Directive (by)
           */
            if (toSpeak.Tags.Count == 0)
            {
                return "__EOT__";
            }
            string result = "Here are some interesting facts about " + toSpeak.Data + ". ";
            for (int x = 0; x < toSpeak.Tags.Count; x++)
            {
                if (toSpeak.Tags[x].Item3 == "Attribute (has)")
                {
                    result += "The " + toSpeak.Tags[x].Item1 + " has " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Base (in)")
                {
                    result += "It is " + toSpeak.Tags[x].Item1 + " in " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Base Past (in)")
                {
                    result += "It was " + toSpeak.Tags[x].Item1 + " in " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Date (on)")
                {
                    result += "It is " + toSpeak.Tags[x].Item1 + " on " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Property (is)")
                {
                    result += "It's " + toSpeak.Tags[x].Item1 + " is " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Directive (by)")
                {
                    result += "It is " + toSpeak.Tags[x].Item1 + " by " + toSpeak.Tags[x].Item2 + ". ";
                }
            }
            return result;
        }
    }
}
