using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LostManuscriptII;

namespace Lost_Manuscript_II_Data_Entry
{
    class FeatureSpeaker
    {
        public FeatureSpeaker()
        { }

        //call this function with answer =-1;
        private void getHeight(FeatureGraph featGraph, Feature current, Feature target,int h, ref int answer)
        {
            if (current == target)
            {
                answer = h;
            }
            if (answer != -1)
            {
                return;
            }
            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                getHeight(featGraph,current.Neighbors[x].Item1,target,h+1,ref answer);
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
            int diffDist = Math.Abs(h - oldh);

            //Score calculation
            score = DiscussedAmount * -1 + FathertoChild * 0.5 + ChildtoFather * 0.5 + diffDist/featGraph.getMaxDepth();

            return score;
        }

        private void travelGraph(FeatureGraph featGraph,Feature current, Feature oldTopic,int h,
            int oldh, int limit, ref List<Tuple<Feature,double> > listScore)
        {
             //base case
            if (h > limit)
            {
                return;
            }
            //Calculate score and add to list
            listScore.Add(new Tuple<Feature, double>(current, getScore(featGraph, current, oldTopic,h,oldh)));
            for (int x = 0; x < current.Neighbors.Count; x++) 
            {
                travelGraph(featGraph, current.Neighbors[x].Item1, oldTopic, h, oldh,limit, ref listScore);
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
                getHeight(featGraph, featGraph.Root, oldTopic, 0, ref height);
                //search the next topic
                List<Tuple<Feature, double>> listScore = new List<Tuple<Feature,double>>();
                travelGraph(featGraph, featGraph.Root, oldTopic, 0, height, limit, ref listScore);
                
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
            List<Tuple<Feature, double>> toDirect = toSpeak.Neighbors;
            if (toSpeak.Neighbors.Count > 5)
            {
                toDirect = new List<Tuple<Feature,double>>();
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
