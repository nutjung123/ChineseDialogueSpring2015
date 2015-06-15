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
        private int heightLimit = 999;
        private string[] spatialKey = new string[8] { "east", "north", "northeast", "northwest", "south", "southeast", "southwest", "west" };
        private string[] hierarchyKey = new string[2] { "contain", "won" };
        private string previousSpatial = "";
        private const string SPATIAL = "spatial";
        private const string HIERACHY = "hierachy";
        private const string FUN_FACT = "Fun Fact";
        private double[] currentNovelty;
        private double currentTopicNovelty = -1;


        public FeatureSpeaker(FeatureGraph featG)
        {
            //define dramaticFunction manually here
            this.featGraph = featG;
            expectedDramaticV = new double[20] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        }

        //call this function with height =-1;
        private void getHeight(Feature current, Feature target, int h, bool[] checkEntry, ref int height)
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
                getHeight(current.Neighbors[x].Item1, target, h + 1, checkEntry, ref height);
            }
        }


        private bool hierachyConstraint(Feature current, Feature oldTopic)
        {
            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                if (current.Neighbors[x].Item1.Data == oldTopic.Data)
                {
                    for (int y = 0; y < hierarchyKey.Length; y++)
                    {
                        if (hierarchyKey[y] == current.Neighbors[x].Item3)
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
                    for (int y = 0; y < hierarchyKey.Length; y++)
                    {
                        if (hierarchyKey[y] == current.Parents[x].Item3)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool spatialConstraint(Feature current, Feature oldTopic)
        {
            if (previousSpatial == "")
            {

                for (int x = 0; x < current.Neighbors.Count; x++)
                {
                    if (current.Neighbors[x].Item1.Data == oldTopic.Data)
                    {
                        for (int y = 0; y < spatialKey.Length; y++)
                        {
                            if (spatialKey[y] == current.Neighbors[x].Item3)
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
                        for (int y = 0; y < spatialKey.Length; y++)
                        {
                            if (spatialKey[y] == current.Parents[x].Item3)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else //TO DO: update previousSpatial
            {
                for (int x = 0; x < current.Neighbors.Count; x++)
                {
                    if (current.Neighbors[x].Item1.Data == oldTopic.Data)
                    {
                        if (previousSpatial == current.Neighbors[x].Item3)
                        {
                           return true;
                        }
                    }
                }
                for (int x = 0; x < current.Parents.Count; x++)
                {
                    if (current.Parents[x].Item1.Data == oldTopic.Data)
                    {
                        if (previousSpatial == current.Parents[x].Item3)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private double getNeighborDiscussAmount(Feature target)
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
            if (printCalculation)
            {
               Console.WriteLine("Number of Neighbors Not Talked: " + sumNotTalk + ", Number of Neighbors Talked: " + sumTalk);
            }
            return sumNotTalk / (sumTalk + sumNotTalk);
        }

        //calculate the novelty of the giving current feature
        private double calculateNovelty(Feature current, Feature oldTopic)
        {
            double noveltyValue = 0;

            // distance
            double dist = oldTopic.ShortestDistance[featGraph.getFeatureIndex(current.Data)] / featGraph.MaxDistance;

            // previous talk
            double previousTalkPercentage = this.getNeighborDiscussAmount(current);

            // tags
            double funFactTag = 0.0;
            if (current.findTagType(FUN_FACT) != null)
            {
                funFactTag = 1.0;
            }

            noveltyValue = dist * 0.5 + previousTalkPercentage * 0.5 + funFactTag * 0.5;
            if (printCalculation)
            {
                Console.WriteLine("Novelty Calculation");
                Console.WriteLine("Distance from current topic to previous topic: " + dist);
                Console.WriteLine("Percentage of related topics NOT covered: " + previousTalkPercentage);
                Console.WriteLine("Fun fact: " + funFactTag);
                Console.WriteLine("Novelty Value (0.5* distance + 0.5* % of related topics Not covered + 0.5*fun fact): " + noveltyValue);
            }
            return noveltyValue;
        }

        private double calculateScore(Feature current, Feature oldTopic)
        {
            double score = 0;
            int currentIndex = featGraph.getFeatureIndex(current.Data);

            //set of Weight (W == Weight)
            //Get the weights from the graph.
            double[] weight_array = featGraph.getWeightArray();
            double discussAmountW = weight_array[Constant.discussAmountWeightIndex];
            double noveltyW = weight_array[Constant.noveltyWeightIndex];
            double spatialConstraintW = weight_array[Constant.spatialWeightIndex];
            double hierachyConstraintW = weight_array[Constant.hierarchyWeightIndex];

            // novelty

            double noveltyValue = calculateNovelty(current,oldTopic);

            //getting novelty information
            if (currentNovelty != null)
            {
                currentNovelty[currentIndex] = noveltyValue;
            }

            //spatial Constraint
            double spatialConstraintValue = 0.0;
            if (spatialConstraint(current, oldTopic))
            {
                spatialConstraintValue = 1.0;
            }
            //hierachy Constraint
            double hierachyConstraintValue = 0.0;
            if (hierachyConstraint(current, oldTopic))
            {
                hierachyConstraintValue = 1.0;
            }

            //check mentionCount
            float DiscussedAmount = current.DiscussedAmount;

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expectedDramaticV[currentTurn % expectedDramaticV.Count()] - noveltyValue) * noveltyW);
            score += spatialConstraintValue * spatialConstraintW;
            score += hierachyConstraintValue * hierachyConstraintW;

            if (printCalculation)
            {
                System.Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                System.Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                System.Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);
                
                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + currentTurn + "] - dramaticValue)*" + noveltyW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " = " + score;
                System.Console.WriteLine(scoreFormula);
            }
            return score;
        }
        //Calculate the score, and return a data structure containing
        //each score component as well as the score itself.
        public double[] calculateScoreComponents(Feature current, Feature oldTopic)
        {
            double score = 0;
            int currentIndex = featGraph.getFeatureIndex(current.Data);

            //ZEV TODO: Replace these with adjustable weight variables
            //Get the weights from the graph.
            double[] weight_array = featGraph.getWeightArray();
            double discussAmountW = weight_array[0];
            double noveltyW = weight_array[1];
            double spatialConstraintW = weight_array[2];
            double hierachyConstraintW = weight_array[3];

            // novelty

            double noveltyValue = calculateNovelty(current, oldTopic);

            //getting novelty information
            if (currentNovelty != null)
            {
                currentNovelty[currentIndex] = noveltyValue;
            }

            //spatial Constraint
            double spatialConstraintValue = 0.0;
            if (spatialConstraint(current, oldTopic))
            {
                spatialConstraintValue = 1.0;
            }
            //hierachy Constraint
            double hierachyConstraintValue = 0.0;
            if (hierachyConstraint(current, oldTopic))
            {
                hierachyConstraintValue = 1.0;
            }

            //check mentionCount
            float DiscussedAmount = current.DiscussedAmount;

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expectedDramaticV[currentTurn % expectedDramaticV.Count()] - noveltyValue) * noveltyW);
            score += spatialConstraintValue * spatialConstraintW;
            score += hierachyConstraintValue * hierachyConstraintW;

            if (printCalculation)
            {
                System.Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                System.Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                System.Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);

                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + currentTurn + "] - dramaticValue)*" + noveltyW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " = " + score;
                System.Console.WriteLine(scoreFormula);
            }

            //Store score components, and score, in return array.
            //Indices are as follows:
            //0 = score
            //1 = novelty
            //2 = discussed amount
            //3 = expected dramatic value
            //4 = spatial constraint value
            //5 = hierarchy constraint value
            double[] return_array = new double[Constant.scoreArraySize];

            //NOTE: Weights are NOT included.
            return_array[Constant.scoreArrayScoreIndex] = score;
            return_array[Constant.scoreArrayNoveltyIndex] = noveltyValue;
            return_array[Constant.scoreArrayDiscussedAmountIndex] = DiscussedAmount;
            return_array[Constant.scoreArrayExpectedDramaticIndex] = expectedDramaticV[currentTurn % expectedDramaticV.Count()];
            return_array[Constant.scoreArraySpatialIndex] = spatialConstraintValue;
            return_array[Constant.scoreArrayHierarchyIndex] = hierachyConstraintValue;

            return return_array;
        }//End method calculateScoreComponents

        //BFS to travel the whole graph
        //listScore keeps track of all nodes' score. 
        private void travelGraph(Feature current, Feature oldTopic, int h, bool isCalculatedScore,
            bool[] checkEntry, ref List<Tuple<Feature, double>> listScore)
        {
            //current's height is higher than the limit
            if (h >= heightLimit)
            {
                return;
            }
            int index = featGraph.getFeatureIndex(current.Data);
            if (checkEntry[index])
            {
                return;
            }
            checkEntry[index] = true;

            if (printCalculation)
            {
                System.Console.WriteLine("\nNode: " + current.Data);
            }

            //Calculate score of choice and add to list
            if (isCalculatedScore)
            {
                listScore.Add(new Tuple<Feature, double>(current, calculateScore(current, oldTopic)));
            }
            else
            {
                listScore.Add(new Tuple<Feature, double>(current, calculateNovelty(current, oldTopic)));
            }

            //search children of current node
            for (int x = 0; x < current.Neighbors.Count; x++)
            {
                travelGraph(current.Neighbors[x].Item1, oldTopic, h + 1, isCalculatedScore, checkEntry, ref listScore);
            }
            for (int x = 0; x < current.Parents.Count; x++)
            {
                travelGraph(current.Parents[x].Item1, oldTopic, h + 1, isCalculatedScore, checkEntry, ref listScore);
            }
        }

        public string getNovelty(Feature currentTopic, int turn, int amount = 5)
        {
            string answer = "";
            bool oldPrintFlag = printCalculation;
            printCalculation = false;

            bool[] checkEntry = new bool[featGraph.Count];
            List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
            this.travelGraph(featGraph.Root, currentTopic, 0, false, checkEntry, ref listScore);
            listScore.Sort((x,y) => y.Item2.CompareTo(x.Item2));
            
            for (int x = 0; x < amount; x++)
            {
                answer += featGraph.getFeatureIndex(listScore[x].Item1.Data)+" "+ listScore[x].Item2 +" ";
            }

            printCalculation = oldPrintFlag;

            return answer;
        }

        //Opposite of get novelty, get the ids of the features that, according to the calculation,
        //are closest to the current topic.
        public string getProximal(Feature currentTopic, int amount = 5)
        {
            string answer = "";

           // List<double> closestTopic = currentTopic.ShortestDistance;

            bool[] checkEntry = new bool[featGraph.Count];
            List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
            this.travelGraph(featGraph.Root, currentTopic, 0, true, checkEntry, ref listScore);
            listScore.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            //var sorted = closestTopic.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key).ToList();

            //List<int> closetTopicIndex = sorted.Select(x => x.Value).ToList();
            //skip the first index, because that index is itself
            for (int x = 1; x <= amount; x++)
            {
                //answer += closetTopicIndex[x] + " " + closestTopic[closetTopicIndex[x]]+" ";
                answer += featGraph.getFeatureIndex(listScore[x].Item1.Data) + " " + listScore[x].Item2 + " ";
            }

            return answer;
        }//end method getProximal

        //Return the next topic
        public Feature getNextTopic(Feature oldTopic, string query, int turn)
        {
            //set up the variables
            currentTurn = turn;
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
                //int height = -1;
                bool[] checkEntry = new bool[featGraph.Count]; //checkEntry is to check that it won't check the same node again
                //getHeight(featGraph.Root, oldTopic, 0, checkEntry, ref height);
                checkEntry = new bool[featGraph.Count];
                //search the next topic

                List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
                //list score order is based on the traveling (DFS) order.
                travelGraph(featGraph.Root, oldTopic, 0, true, checkEntry, ref listScore);

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

    }
}
