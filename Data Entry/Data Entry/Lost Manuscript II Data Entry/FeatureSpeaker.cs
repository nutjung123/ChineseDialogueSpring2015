using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dialogue_Data_Entry;

namespace Dialogue_Data_Entry
{
    class FeatureSpeaker
    {
        private double[] expectedDramaticV;
        private FeatureGraph featGraph;
        private bool printCalculation = false;
        private int currentTurn;
        private string[] spatialKey = new string[8] { "east", "north", "northeast", "northwest", "south", "southeast", "southwest", "west" };
        private string[] hierarchyKey = new string[2] { "contain", "won" };
        private const string SPATIAL = "spatial";
        private const string HIERACHY = "hierachy";
        private const string FUN_FACT = "Fun Fact";
        private double[] nextNovelty;
        private double[] currentNovelty;
        private double currentTopicNovelty = -1;

        public FeatureSpeaker()
        {
            //define dramaticFunction manually here
            expectedDramaticV = new double[20] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        }

        //call this function with answer =-1;
        private void getHeight(FeatureGraph featGraph, Feature current, Feature target, int h, ref int height, bool[] checkEntry)
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
                getHeight(featGraph, current.Neighbors[x].Item1, target, h + 1, ref height, checkEntry);
            }
        }

        private bool relationshipConstraint(FeatureGraph featGraph, Feature current, Feature oldTopic, string relationship)
        {
            string[] relationshipKey = null;
            if (relationship == SPATIAL)
            {
                relationshipKey = spatialKey;
            }
            else if (relationship == HIERACHY)
            {
                relationshipKey = hierarchyKey;
            }
            else
            {
                return false;
            }
            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                if (current.Neighbors[x].Item1.Data == oldTopic.Data)
                {
                    for (int y = 0; y < relationshipKey.Length; y++)
                    {
                        if (relationshipKey[y] == current.Neighbors[x].Item3)
                        {
                            return true;
                        }
                    }
                }
            }
            for (int x = 0; x < current.Parents.Count; x++)
            {
                if (current.Parents[x].Item1.Data == oldTopic.Data)
                {
                    for (int y = 0; y < relationshipKey.Length; y++)
                    {
                        if (relationshipKey[y] == current.Parents[x].Item3)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private double getNeighborAmount(FeatureGraph featGraph, Feature target)
        {
            double sumTalk = 0.0;
            double sumNotTalk = 0.0;
            for (int x = 0; x < target.Parents.Count; x++)
            {
                List<Tuple<Feature, double, string>> neighbors = target.Parents[x].Item1.Neighbors;
                for (int y = 0; y < neighbors.Count; y++)
                {
                    //check all other nodes except itself
                    if (neighbors[y].Item1.Data != target.Data)
                    {
                        if (neighbors[y].Item1.DiscussedAmount >= 1)
                        {
                            sumTalk++;
                        }
                        else
                        {
                            sumNotTalk++;
                        }
                    }
                }
            }
            //about itself
            if (target.DiscussedAmount >= 1)
            {
                sumTalk++;
            }
            else
            {
                sumNotTalk++;
            }
            /*
             if (printCalculation)
             {
                 Console.WriteLine("Number of Neighbors Not Talked: " + sumNotTalk + ", Number of Neighbors Talked: " + sumTalk);
             }
             * */
            return sumNotTalk / (sumTalk + sumNotTalk);
        }

        private double getScore(FeatureGraph featGraph, Feature current, Feature oldTopic, int h, int oldh)
        {
            double score = 0;
            int currentIndex = featGraph.getFeatureIndex(current.Data);

            double discussAmountW = -3.0;
            double dramaticValueW = -1.0;
            double spatialConstraintW = 1.0;
            double hierachyConstraintW = 1.0;

            //current.Tags;

            //check dramatic goal value

            // novelty

            //// distance
            double dist = oldTopic.ShortestDistance[featGraph.getFeatureIndex(current.Data)] / featGraph.MaxDistance;

            //// previous talk
            double previousTalkPercentage = getNeighborAmount(featGraph, current);

            // tags
            double funFactTag = 0.0;
            if (current.findTagType(FUN_FACT)!=null)
            {
                funFactTag = 1.0;
            }

            double dramaticValue = dist * 0.5 + previousTalkPercentage * 0.5 + funFactTag*0.5;

            //getting novelty information
            if (nextNovelty != null)
            {
                nextNovelty[currentIndex] = dramaticValue;
            }
            if (currentNovelty != null)
            {
                currentNovelty[currentIndex] = dramaticValue;
            }


            //spatial Constraint
            double spatialConstraintValue = 0.0;
            if (relationshipConstraint(featGraph, current, oldTopic, SPATIAL))
            {
                spatialConstraintValue = 1.0;
            }
            //hierachy Constraint
            double hierachyConstraintValue = 0.0;
            if (relationshipConstraint(featGraph, current, oldTopic, HIERACHY))
            {
                hierachyConstraintValue = 1.0;
            }

            //check mentionCount
            float DiscussedAmount = current.DiscussedAmount;

            //Score calculation
            double maxDepth = (double)featGraph.getMaxDepth();

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expectedDramaticV[currentTurn % expectedDramaticV.Count()] - dramaticValue) * dramaticValueW);
            score += spatialConstraintValue * spatialConstraintW;
            score += hierachyConstraintValue * hierachyConstraintW;

            if (printCalculation)
            {
                System.Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                //System.Console.WriteLine("Distance from "+oldTopic.Data+" to "+current.Data+": "+dist+", Max Distance: "+featGraph.MaxDistance);
                System.Console.WriteLine("Distance from current topic: " + dist);
                System.Console.WriteLine("Percentage of related topics NOT covered: " + previousTalkPercentage);
                System.Console.WriteLine("Fun fact: " + funFactTag);
                System.Console.WriteLine("Dramatic Value (0.5* distance + 0.5* % of related topics Not covered + 0.5*fun fact): " + dramaticValue);
                System.Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                System.Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);
                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + currentTurn + "] - dramaticValue)*" + dramaticValueW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " = " + score;
                System.Console.WriteLine(scoreFormula);
            }
            return score;
        }

        private void travelGraph(FeatureGraph featGraph, Feature current, Feature oldTopic, int h,
            int oldh, int limit, ref List<Tuple<Feature, double>> listScore, bool[] checkEntry)
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
            if (printCalculation)
            {
                System.Console.WriteLine("\nNode: " + current.Data);
            }
            listScore.Add(new Tuple<Feature, double>(current, getScore(featGraph, current, oldTopic, h, oldh)));
            //search children of current node
            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                travelGraph(featGraph, current.Neighbors[x].Item1, oldTopic, h + 1, oldh, limit, ref listScore, checkEntry);
            }
            for (int x = 0; x < current.Parents.Count; x++)
            {
                travelGraph(featGraph, current.Parents[x].Item1, oldTopic, h + 1, oldh, limit, ref listScore, checkEntry);
            }
        }

        public string getNovelty(FeatureGraph featGraph, Feature currentTopic, int turn, int amount = 5)
        {
            string answer = "Novelty:";
            bool oldPrintFlag = printCalculation;
            printCalculation = false;
            if (nextNovelty == null)
            {
                nextNovelty = new double[featGraph.Features.Count()];
                if (turn == 1)
                {
                    turn++;
                }
                this.getNextTopic(featGraph, currentTopic, "", turn);
            }
            if (nextNovelty != null)
            {
                var sorted = nextNovelty.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderByDescending(x => x.Key).ToList();
                List<int> idx = sorted.Select(x => x.Value).ToList();
                for (int x = 0; x < amount; x++)
                {
                    if (x >= nextNovelty.Count())
                    {
                        break;
                    }
                    answer += idx[x]+" "+nextNovelty[idx[x]];
                    if (x < amount-1 && x < nextNovelty.Count())
                    {
                        answer += " ";
                    }
                }
            }
            nextNovelty = null;
            printCalculation = oldPrintFlag;
            return answer;
        }

        //Return the next topic
        public Feature getNextTopic(FeatureGraph featGraph, Feature oldTopic, string query, int turn)
        {
            //set up the variables
            currentTurn = turn;
            this.featGraph = featGraph;
            if (turn == 1)
            {
                //initial case
                return oldTopic;

            }
            else if (turn > 1 && query == "")
            {
                //next topic case
                if (currentNovelty == null)
                {
                    currentNovelty = new double[featGraph.Features.Count()];
                }
                int height = -1;
                int limit = 999;
                bool[] checkEntry = new bool[featGraph.Count]; //checkEntry is to check that it won't check the same node again
                getHeight(featGraph, featGraph.Root, oldTopic, 0, ref height, checkEntry);
                checkEntry = new bool[featGraph.Count];
                //search the next topic

                List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
                //list score order is based on the traveling (DFS) order.
                travelGraph(featGraph, featGraph.Root, oldTopic, 0, height, limit, ref listScore, checkEntry);

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
                currentTopicNovelty = currentNovelty[featGraph.getFeatureIndex(listScore[maxIndex].Item1.Data)];
                if (printCalculation)
                {
                    System.Console.WriteLine("\n\nMax score: " + maxScore);
                    System.Console.WriteLine("Novelty: "+ currentTopicNovelty);
                    System.Console.WriteLine("Node: " + listScore[maxIndex].Item1.Data);
                    System.Console.WriteLine("==========================================");
                }
                return listScore[maxIndex].Item1;

            }
            else if (turn > 1 && query != "")
            {
                //answer question case
            }
            return null;
        }

        public double getCurrentTopicNovelty()
        {
            if (currentTopicNovelty == -1)
            {
                System.Console.WriteLine("ERROR currentTopicNovelty has not been calculated.");
                return -1;
            }
            return currentTopicNovelty;
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
            List<Tuple<Feature, double, string>> toDirect = toSpeak.Neighbors;
            if (toSpeak.Neighbors.Count > 5)
            {
                toDirect = new List<Tuple<Feature, double, string>>();
                int choice = 0;
                while (toDirect.Count < 5)
                {
                    choice = myRand.Next(0, 5);
                    if (!toDirect.Contains(toSpeak.Neighbors[choice]))
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
