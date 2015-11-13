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
        private int currentTurn=1;
        private int heightLimit = 999;
        private string[] spatialKey = new string[8] { "east", "north", "northeast", "northwest", "south", "southeast", "southwest", "west" };
        /*private string[] hierarchyKey = new string[33] { "is", "was a member of", "are", "won a gold medal in", "is a kind of", "is a member of"
            , "is southwest of", "won", "is one of", "include", "was", "took place on", "was one of the", "is southeast of", "took place at"
            , "was one of", "is a", "includes", "included", "is northeast of", "has", "is north of", "is in", "is west of"
            , "is east of", "is south of", "is northwest of", "had", "includes event", "includes member", "included member"
            , "include athlete", "" };*/

        /*private string[] hierarchyKey = new string[40] {"has", "partially held", "is southeast of", "include", "is north of", "was one of the",
                                                        "held", "was participated by", "was included by", "is south of", "wais a member of",
                                                        "is northwest of", "is", "are", "included", "is southwest of", "won", "includes", "was a member of",
                                                        "was had by", "is the venue where the gold medal was won by", "is a", "belongs to", "is a kind of",
                                                        "took place on", "competed in", "is a member of", "was held by", "is one of", "is east of",
                                                        "took place at", "was one of", "is west of", "is northeast of", "was a gold medal in", "was",
                                                        "was won by", "is in", "leads to the construction of", "was partially held by"};*/

        private string[] hierarchyKey = new string[1] {""};

        private string previousSpatial = "";
        private List<string> topicHistory;
        private const string SPATIAL = "spatial";
        private const string HIERACHY = "hierachy";
        private const string FUN_FACT = "Fun Fact";
        private double[] currentNovelty;
        private double currentTopicNovelty = -1;
        private List<TemporalConstraint> temporalConstraintList;
        private string[] Directional_Words = {"north", "east", "west", "south",
                                      "northeast", "northwest", "southeast", "southwest"};

        //FILTERING:
        //A list of nodes to filter out of mention.
        //Nodes in this list won't be spoken explicitly unless they
        //are directly queried for.
        //These nodes are still included in traversals, but upon traveling to
        //one of these nodes the next step in the traversal is automatically taken.
        public List<string> filter_nodes = new List<string>();

        public FeatureSpeaker(FeatureGraph featG,List<TemporalConstraint> myTemporalConstraintList)
        {
            setFilterNodes();
            //define dramaticFunction manually here
            this.temporalConstraintList = new List<TemporalConstraint>();
            for (int x = 0; x < myTemporalConstraintList.Count(); x++)
            {
                this.temporalConstraintList.Add(new TemporalConstraint(myTemporalConstraintList[x].FirstArgument,
                    myTemporalConstraintList[x].SecondArgument, myTemporalConstraintList[x].ThirdArgument,
                    myTemporalConstraintList[x].FourthArgument, myTemporalConstraintList[x].FifthArgument));
            }
            this.featGraph = featG;
            expectedDramaticV = new double[20] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        }

        public FeatureSpeaker(FeatureGraph featG, List<TemporalConstraint> myTemporalConstraintList,string prevSpatial,List<string> topicH)
        {
            setFilterNodes();
            this.temporalConstraintList = new List<TemporalConstraint>();
            for (int x = 0; x < myTemporalConstraintList.Count();x++ )
            {
                this.temporalConstraintList.Add(new TemporalConstraint(myTemporalConstraintList[x].FirstArgument,
                    myTemporalConstraintList[x].SecondArgument, myTemporalConstraintList[x].ThirdArgument,
                    myTemporalConstraintList[x].FourthArgument,myTemporalConstraintList[x].FifthArgument));
            }
            this.featGraph = featG;
            previousSpatial = prevSpatial;
            this.topicHistory = new List<string>(topicH);
            //define dramaticFunction manually here
            expectedDramaticV = new double[20] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        }

        private void setFilterNodes()
        {
            //Build list of filter nodes.
            //Each filter node is identified by its Data values in the XML
            filter_nodes.Add("Male");
            filter_nodes.Add("Female");
            filter_nodes.Add("Cities");
            filter_nodes.Add("Sports");
            filter_nodes.Add("Gold Medallists");
            filter_nodes.Add("Venues");
            filter_nodes.Add("Time");
            filter_nodes.Add("Motto");
            filter_nodes.Add("Anthem");
            filter_nodes.Add("Mascots");
            filter_nodes.Add("Aug. 8th, 2008");
            filter_nodes.Add("Aug. 24th, 2008");
            filter_nodes.Add("Aug. 9th, 2008");
            filter_nodes.Add("Aug. 10th, 2008");
            filter_nodes.Add("Aug. 11th, 2008");
            filter_nodes.Add("Aug. 12th, 2008");
            filter_nodes.Add("Aug. 13th, 2008");
            filter_nodes.Add("Aug. 14th, 2008");
            filter_nodes.Add("Aug. 15th, 2008");
            filter_nodes.Add("Aug. 16th, 2008");
            filter_nodes.Add("Aug. 17th, 2008");
            filter_nodes.Add("Aug. 18th, 2008");
            filter_nodes.Add("Aug. 19th, 2008");
            filter_nodes.Add("Aug. 20th, 2008");
            filter_nodes.Add("Aug. 21st, 2008");
            filter_nodes.Add("Aug. 22nd, 2008");
            filter_nodes.Add("Aug. 23rd, 2008");
        }//end method setFilterNodes

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
        //Input: the current topic, the current turn and the whole history
        //Return: a list of temporal constraint that this topic can satisfy that are not satisfied yet.
        public List<int> temporalConstraint(Feature current,int turn, List<string> topicH)
        {
            List<int> indexList = new List<int>();
            for (int x = 0; x < temporalConstraintList.Count(); x++)
            {
                if (temporalConstraintList[x].FirstArgument == current.Data && !temporalConstraintList[x].Satisfied)
                {
                    //Third argument is turn case 
                    if (temporalConstraintList[x].getThirdArgumentType() == "turn")
                    {
                        if (temporalConstraintList[x].SecondArgument == ">")
                        {
                            if (turn > Convert.ToInt32(temporalConstraintList[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }
                        }
                        else if (temporalConstraintList[x].SecondArgument == ">=")
                        {
                            if (turn >= Convert.ToInt32(temporalConstraintList[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }
                        }
                        else if (temporalConstraintList[x].SecondArgument == "==")
                        {
                            if (turn == Convert.ToInt32(temporalConstraintList[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }
                        }
                        else if (temporalConstraintList[x].SecondArgument == "<=")
                        {
                            if (turn <= Convert.ToInt32(temporalConstraintList[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }
                        }
                        else if (temporalConstraintList[x].SecondArgument == "<")
                        {
                            if (turn < Convert.ToInt32(temporalConstraintList[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }
                        }
                    } //Third argument is a topic case
                    else if (temporalConstraintList[x].getThirdArgumentType() == "topic")
                    {
                        //There is only one prosible case that the constraint will be satisfied by current topic.
                        // First > Third , and Third has already been discussed (It is in history).
                        // this turn is already greater than all of the turn of topics in history.
                        // Only need to check whether third argument exists in history or not.
                        for (int y = 0; y < topicH.Count(); y++)
                        {
                            if (temporalConstraintList[x].ThirdArgument == topicH[y])
                            {
                                if (temporalConstraintList[x].FourthArgument == "")
                                {
                                    indexList.Add(x);
                                    break; //Only need to find once instance of this topic in the history
                                }
                                //To Do: Adding the case of fourth and fifth arguments
                            }
                        }
                    }
                }
            }
            return indexList;
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
            else 
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
                /*for (int x = 0; x < current.Parents.Count; x++)
                {
                    if (current.Parents[x].Item1.Data == oldTopic.Data)
                    {
                        if (previousSpatial == current.Parents[x].Item3)
                        {
                            return true;
                        }
                    }
                }*/
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

        public double calculateScore(Feature current, Feature oldTopic)
        {
            double score = 0;
            int currentIndex = featGraph.getFeatureIndex(current.Data);

            //set of Weight (W == Weight)
            //Get the weights from the graph.
            double[] weight_array = featGraph.getWeightArray();
            double discussAmountW = weight_array[Constant.DiscussAmountWeightIndex];
            double noveltyW = weight_array[Constant.NoveltyWeightIndex];
            double spatialConstraintW = weight_array[Constant.SpatialWeightIndex];
            double hierachyConstraintW = weight_array[Constant.HierarchyWeightIndex];
            double temporalConstraintW = weight_array[Constant.TemporalWeightIndex];

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

            //Temporal Constraint
            double temporalConstraintValue = temporalConstraint(current, this.currentTurn, this.topicHistory).Count();

            //check mentionCount
            float DiscussedAmount = current.DiscussedAmount;

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expectedDramaticV[currentTurn % expectedDramaticV.Count()] - noveltyValue) * noveltyW);
            score += spatialConstraintValue * spatialConstraintW;
            score += (hierachyConstraintValue * hierachyConstraintW);
            score += (temporalConstraintValue * temporalConstraintW);

            //If this is a filter node, or the same node as the focus node, artificially set its score low
            if (filter_nodes.Contains(current.Data.Split(new string[] { "##" }, StringSplitOptions.None)[0])
                || current.Data.Equals(oldTopic.Data))
            {
                Console.WriteLine("Filtering out node " + current.Data);
                score = -1000000;
            }//end if

            //if (hierachyConstraintValue > 0)
            //  Console.WriteLine("hierarchy constraint for " + current.Data + " from " + oldTopic.Data + ": " + hierachyConstraintValue);

            if (printCalculation)
            //if (true)
            {
                Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);
                Console.WriteLine("Temporal Constraint Satisfied: "+temporalConstraintValue);
                Console.WriteLine("Temporal Calculation: "+temporalConstraintValue*temporalConstraintW);
                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + currentTurn + "] - dramaticValue)*" + noveltyW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " + temporalConstraint*" + temporalConstraintW;
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

            //set of Weight (W == Weight)
            //Get the weights from the graph.
            double[] weight_array = featGraph.getWeightArray();
            double discussAmountW = weight_array[Constant.DiscussAmountWeightIndex];
            double noveltyW = weight_array[Constant.NoveltyWeightIndex];
            double spatialConstraintW = weight_array[Constant.SpatialWeightIndex];
            double hierachyConstraintW = weight_array[Constant.HierarchyWeightIndex];
            double temporalConstraintW = weight_array[Constant.TemporalWeightIndex];

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

            //Temporal Constraint
            double temporalConstraintValue = temporalConstraint(current, this.currentTurn, this.topicHistory).Count();

            //check mentionCount
            float DiscussedAmount = current.DiscussedAmount;

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expectedDramaticV[currentTurn % expectedDramaticV.Count()] - noveltyValue) * noveltyW);
            score += spatialConstraintValue * spatialConstraintW;
            score += (hierachyConstraintValue * hierachyConstraintW);
            score += (temporalConstraintValue * temporalConstraintW);

            if (printCalculation)
            {
                Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);
                Console.WriteLine("Temporal Constraint Satisfied: " + temporalConstraintValue);
                Console.WriteLine("Temporal Calculation: " + temporalConstraintValue * temporalConstraintW);
                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + currentTurn + "] - dramaticValue)*" + noveltyW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " + temporalConstraint*" + temporalConstraintW;
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
            double[] return_array = new double[Constant.ScoreArraySize];

            //NOTE: Weights are NOT included.
            return_array[Constant.ScoreArrayScoreIndex] = score;
            return_array[Constant.ScoreArrayNoveltyIndex] = noveltyValue;
            return_array[Constant.ScoreArrayDiscussedAmountIndex] = DiscussedAmount;
            return_array[Constant.ScoreArrayExpectedDramaticIndex] = expectedDramaticV[currentTurn % expectedDramaticV.Count()];
            return_array[Constant.ScoreArraySpatialIndex] = spatialConstraintValue;
            return_array[Constant.ScoreArrayHierarchyIndex] = hierachyConstraintValue;

            return return_array;

            /*double score = 0;
            int currentIndex = featGraph.getFeatureIndex(current.Data);

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
            double[] return_array = new double[Constant.ScoreArraySize];

            //NOTE: Weights are NOT included.
            return_array[Constant.ScoreArrayScoreIndex] = score;
            return_array[Constant.ScoreArrayNoveltyIndex] = noveltyValue;
            return_array[Constant.ScoreArrayDiscussedAmountIndex] = DiscussedAmount;
            return_array[Constant.ScoreArrayExpectedDramaticIndex] = expectedDramaticV[currentTurn % expectedDramaticV.Count()];
            return_array[Constant.ScoreArraySpatialIndex] = spatialConstraintValue;
            return_array[Constant.ScoreArrayHierarchyIndex] = hierachyConstraintValue;

            return return_array;*/
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
        //are most likely to be chosen as the next topic.
        public string getProximal(Feature currentTopic, int amount = 5)
        {
            string answer = "";
            bool oldPrintCalculation = printCalculation;
            printCalculation = false;
           // List<double> closestTopic = currentTopic.ShortestDistance;

            bool[] checkEntry = new bool[featGraph.Count];
            List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
            this.travelGraph(featGraph.Root, currentTopic, 0, true, checkEntry, ref listScore);
            listScore.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            //var sorted = closestTopic.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key).ToList();

            //List<int> closetTopicIndex = sorted.Select(x => x.Value).ToList();
            
            for (int x = 0; x < amount; x++)
            {
                //answer += closetTopicIndex[x] + " " + closestTopic[closetTopicIndex[x]]+" ";
                answer += featGraph.getFeatureIndex(listScore[x].Item1.Data) + " " + listScore[x].Item2 + " ";
            }
            printCalculation = oldPrintCalculation;
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
                        //FILTERING:
                        //If the item in this list is one of the filter nodes,
                        //do not include it in max score determination.
                        //Check for filter nodes.
                        if (filter_nodes.Contains(listScore[x].Item1.Data))
                        {
                            //If it is a filter node, take another step.
                            Console.WriteLine("Filtering out " + listScore[x].Item1.Data);
                            continue;
                        }//end if

                        maxScore = listScore[x].Item2;
                        maxIndex = x;
                    }
                }

                currentTopicNovelty = currentNovelty[featGraph.getFeatureIndex(listScore[maxIndex].Item1.Data)];
                //if (printCalculation)
                if (false)
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

        private void internalUpdateHistory(Feature nextTopic)
        {
            //update spatial constraint information
            bool spatialExist = false;
            if (topicHistory.Count() > 0)
            {
                Feature prevTopic = this.featGraph.getFeature(topicHistory[topicHistory.Count() - 1]);
                if (prevTopic.getNeighbor(nextTopic.Data) != null)
                {
                    foreach(string str in Directional_Words)
                    {
                        if (str == prevTopic.getRelationshipNeighbor(nextTopic.Data))
                        {
                            previousSpatial = str;
                            spatialExist = true;
                            break;
                        }
                    }
                }
            }
            if (!spatialExist)
            {
                previousSpatial = "";
            }

            //update temporal constraint information
            FeatureSpeaker temp = new FeatureSpeaker(this.featGraph, temporalConstraintList);
            List<int> temporalIndex = temp.temporalConstraint(nextTopic,currentTurn,topicHistory);
            for (int x = 0; x < temporalIndex.Count(); x++)
            {
                temporalConstraintList[temporalIndex[x]].Satisfied = true;
            }
            //update topic's history
            topicHistory.Add(nextTopic.Data);
        }

        public List<Feature> forwardProjection(Feature currentTopic, int forwardTurn)
        {
            //remember internal variables for forward projection
            string internalPreviousSpatial = this.previousSpatial;
            List<string> internalTopicHistory = new List<string>(this.topicHistory);
            int internalTurn = this.currentTurn;
            Feature tempCurrentTopic = currentTopic;
            bool oldPrintCalculation = printCalculation;
            List<TemporalConstraint> internalTemporalConstraintList = new List<TemporalConstraint>();
            for (int x = 0; x < temporalConstraintList.Count();x++ )
            {
                internalTemporalConstraintList.Add(new TemporalConstraint(temporalConstraintList[x].FirstArgument,
                    temporalConstraintList[x].SecondArgument, temporalConstraintList[x].ThirdArgument,
                    temporalConstraintList[x].FourthArgument, temporalConstraintList[x].FifthArgument));
            }
            printCalculation = false;

            //Forward Projection
            List<Feature> topicList = new List<Feature>();
            //topicList.Add(currentTopic);
            for (int x = 0; x < forwardTurn;x++)
            {
                //update Internal variables
                tempCurrentTopic = this.getNextTopic(tempCurrentTopic, "", currentTurn);
                tempCurrentTopic.DiscussedAmount++;
                internalUpdateHistory(tempCurrentTopic);
                currentTurn++;
                topicList.Add(tempCurrentTopic);
            }

            //recover all old variables
            this.previousSpatial = internalPreviousSpatial;
            this.topicHistory = internalTopicHistory;
            this.currentTurn = internalTurn;
            this.temporalConstraintList = internalTemporalConstraintList;
            printCalculation = oldPrintCalculation;
            for (int x = 0; x < forwardTurn;x++)
            {
                topicList[x].DiscussedAmount--;
            }

            return topicList;
        }

    }
}
