using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    //NarrationCalculator handles all calculations that relate to selecting a topic.
    //It contains functions for calculating values for groups of features,
    //such as the score or the novelty between two features.
    //It also contains functions for checking constraints for which node would be best to choose.
    //NarrationCalculator maintains no memory of the conversation and operates on whatever
    //topic history is passed to it.
    class NarrationCalculator
    {
        //Relationships that count for the spatial constraint.
        private string[] spatial_key = new string[13] { "is east of##在东方于##", "is north of##在北方于##", "is northeast of##在东北方于##", "is northwest of##在西北方于##"
                                                    , "is south of##在南方于##", "is southeast of##在东南方于##", "is south of##在南方于##", "is west of##在西方于##"
                                                    , "took place at##曾举办于##", "was held by##被举办于##", "was partially held by##被举办于##"
                                                    , "held##举办了##", "partially held##举办了部分##"};
        //Relationships that count for the hierarchy constraint
        private string[] hierarchy_key = new string[40] {"has##有##", "partially held##举办了部分##", "is southeast of##在东南方于##", "include##包括##",
                                                    "is north of##在北方于##", "was one of the##是##", "held##举办了##", "was participated by##被参与##"
                                                    , "was included by##被包括##", "is south of##在南方于##", "wais a member of", "is northwest of##在西北方于##"
                                                    , "is##是##", "are##是##", "included##曾包括##", "is southwest of##在西南方于##", "won##赢了##", "includes##包括##"
                                                    , "was a member of##曾隶属于##", "was had by##被有##", "is the venue where the gold medal was won by##是金牌被获得的场地##"
                                                    , "is a##是##", "belongs to##属于##", "is a kind of##是一种##", "took place on##曾举办于##", "competed in##参赛于##"
                                                    , "is a member of##隶属于##", "was held by##被举办于##", "is one of##是一个##", "is east of##在东方于##"
                                                    , "took place at##曾举办于##", "was one of##曾是##", "is west of##在西方于##", "is northeast of##在东北方于##"
                                                    , "won a gold medal in##曾赢得金牌##", "was##是##", "was won by##被赢##", "is in##在##", "leads to the construction of##领引建设了##"
                                                    , "was partially held by##部分被举办于##"};

        private List<TemporalConstraint> temporal_constraint_list;  //The list for temporal constraint checking. Does not change after init.

        //FILTERING:
        //A list of nodes to filter out of mention.
        //Nodes in this list won't be spoken explicitly unless they
        //are directly queried for.
        //These nodes are still included in traversals, but upon traveling to
        //one of these nodes the next step in the traversal is automatically taken.
        public List<string> filter_nodes = new List<string>();

        //Stores what the expected dramatic value at each turn should be.
        private double[] expected_dramatic_value;

        FeatureGraph feature_graph;             //The data structure holding every feature in the knowledge base.

        private int height_limit = 999;         //Height limit for BFS over the feature graph.
        bool print_calculation = false;         //Debugging variable.

        public NarrationCalculator(FeatureGraph fg, List<TemporalConstraint> tcl)
        {
            feature_graph = fg;
            this.temporal_constraint_list = new List<TemporalConstraint>();
            for (int x = 0; x < tcl.Count(); x++)
            {
                this.temporal_constraint_list.Add(new TemporalConstraint(tcl[x].FirstArgument,
                    tcl[x].SecondArgument, tcl[x].ThirdArgument,
                    tcl[x].FourthArgument, tcl[x].FifthArgument));
            }//end for
            //Create the hierarchy key from the feature graph
            hierarchy_key = CreateHierarchyKey(feature_graph);
            //Default initializations
            expected_dramatic_value = new double[20] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
            SetFilterNodes();
        }//end constructor NarrationCalculator
        private void SetFilterNodes()
        {
            //Build list of filter nodes.
            //Each filter node is identified by its Data values in the XML
            filter_nodes = new List<string>();

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

        //ACCESSIBLE FUNCTIONS

        //Decide on the next topic from the previous topic, the current input string, and the current turn.
        //Returns the next topic feature.
        public Feature GetNextTopic(Feature previous_topic, string query, int turn, List<Feature> topic_history)
        {
            if (turn == 1)
            {
                //initial case
                return previous_topic;
            }
            else if (turn > 1 && query == "")
            {
                //next topic case
                /*if (currentNovelty == null)
                {
                    currentNovelty = new double[feature_graph.Features.Count()];
                }*/
                //int height = -1;
                bool[] checkEntry = new bool[feature_graph.Count]; //checkEntry is to check that it won't check the same node again
                //getHeight(featGraph.Root, oldTopic, 0, checkEntry, ref height);
                checkEntry = new bool[feature_graph.Count];
                //search the next topic

                List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
                //Get a list of each feature's score calculated against previous_topic.
                //List order is based on the traveling (DFS) order.
                TravelGraph(feature_graph.Root, previous_topic, 0, true, checkEntry, turn, topic_history, ref listScore);

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
                        if (filter_nodes.Contains(listScore[x].Item1.Name))
                        {
                            //If it is a filter node, take another step.
                            Console.WriteLine("Filtering out " + listScore[x].Item1.Id);
                            continue;
                        }//end if

                        maxScore = listScore[x].Item2;
                        maxIndex = x;
                    }
                }

                if (print_calculation)
                {
                    System.Console.WriteLine("\n\nMax score: " + maxScore);
                    //System.Console.WriteLine("Novelty: " + currentTopicNovelty);
                    System.Console.WriteLine("Node: " + listScore[maxIndex].Item1.Id);
                    System.Console.WriteLine("==========================================");
                }
                return listScore[maxIndex].Item1;

            }//end else if
            else if (turn > 1 && query != "")
            {
                //answer question case
            }//end else if
            return null;
        }//end function GetNextTopic

        /// <summary>
        /// Calculates the score between the two given features. Returns a data structure containing
        /// each component of the score as well as the score itself.
        /// </summary>
        public double[] CalculateScoreComponents(Feature current_feature, Feature last_feature, int turn_count, List<Feature> topic_history)
        {
            double score = 0;
            int currentIndex = feature_graph.getFeatureIndex(current_feature.Id);

            //set of Weight (W == Weight)
            //Get the weights from the graph.
            double[] weight_array = feature_graph.getWeightArray();
            double discussAmountW = weight_array[Constant.DiscussAmountWeightIndex];
            double noveltyW = weight_array[Constant.NoveltyWeightIndex];
            double spatialConstraintW = weight_array[Constant.SpatialWeightIndex];
            double hierachyConstraintW = weight_array[Constant.HierarchyWeightIndex];
            double temporalConstraintW = weight_array[Constant.TemporalWeightIndex];

            // novelty

            double noveltyValue = CalculateNovelty(current_feature, last_feature);

            //getting novelty information
            //Updates an array of novelty values for each feature
            /*if (currentNovelty != null)
            {
                currentNovelty[currentIndex] = noveltyValue;
            }//end if*/

            //spatial Constraint
            double spatialConstraintValue = 0.0;
            if (SpatialConstraint(current_feature, last_feature, topic_history))
            {
                spatialConstraintValue = 1.0;
            }
            //hierachy Constraint
            double hierachyConstraintValue = 0.0;
            if (HierachyConstraint(current_feature, last_feature))
            {
                hierachyConstraintValue = 1.0;
            }

            //Temporal Constraint
            double temporalConstraintValue = TemporalConstraint(current_feature, turn_count, topic_history).Count();

            //check mentionCount
            float DiscussedAmount = current_feature.DiscussedAmount;

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expected_dramatic_value[turn_count % expected_dramatic_value.Count()] - noveltyValue) * noveltyW);
            score += spatialConstraintValue * spatialConstraintW;
            score += (hierachyConstraintValue * hierachyConstraintW);
            score += (temporalConstraintValue * temporalConstraintW);

            if (print_calculation)
            {
                Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);
                Console.WriteLine("Temporal Constraint Satisfied: " + temporalConstraintValue);
                Console.WriteLine("Temporal Calculation: " + temporalConstraintValue * temporalConstraintW);
                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + turn_count + "] - dramaticValue)*" + noveltyW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " + temporalConstraint*" + temporalConstraintW;
                scoreFormula += " = " + score;
                System.Console.WriteLine(scoreFormula);
            }//end if

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
            return_array[Constant.ScoreArrayExpectedDramaticIndex] = expected_dramatic_value[turn_count % expected_dramatic_value.Count()];
            return_array[Constant.ScoreArraySpatialIndex] = spatialConstraintValue;
            return_array[Constant.ScoreArrayHierarchyIndex] = hierachyConstraintValue;

            return return_array;
        }//End method calculateScoreComponents

        /// <summary>
        /// Calculates the score between the two given features and returns it.
        /// </summary>
        private double CalculateScore(Feature current_feature, Feature last_feature, int turn_count, List<Feature> topic_history)
        {
            double score = 0;
            int currentIndex = feature_graph.getFeatureIndex(current_feature.Id);

            //set of Weight (W == Weight)
            //Get the weights from the graph.
            double[] weight_array = feature_graph.getWeightArray();
            double discussAmountW = weight_array[Constant.DiscussAmountWeightIndex];
            double noveltyW = weight_array[Constant.NoveltyWeightIndex];
            double spatialConstraintW = weight_array[Constant.SpatialWeightIndex] * 10;
            double hierachyConstraintW = weight_array[Constant.HierarchyWeightIndex];
            double temporalConstraintW = weight_array[Constant.TemporalWeightIndex];

            // novelty

            double noveltyValue = CalculateNovelty(current_feature, last_feature);

            //getting novelty information
            /*if (currentNovelty != null)
            {
                currentNovelty[currentIndex] = noveltyValue;
            }*/

            //spatial Constraint
            double spatialConstraintValue = 0.0;
            if (SpatialConstraint(current_feature, last_feature, topic_history))
            {
                spatialConstraintValue = 1.0;
            }
            //hierachy Constraint
            double hierachyConstraintValue = 0.0;
            if (HierachyConstraint(current_feature, last_feature))
            {
                hierachyConstraintValue = 1.0;
            }

            //Temporal Constraint
            double temporalConstraintValue = TemporalConstraint(current_feature, turn_count, topic_history).Count();

            //check mentionCount
            float DiscussedAmount = current_feature.DiscussedAmount;

            score += (DiscussedAmount * discussAmountW);
            score += (Math.Abs(expected_dramatic_value[turn_count % expected_dramatic_value.Count()] - noveltyValue) * noveltyW);
            score += spatialConstraintValue * spatialConstraintW;
            score += (hierachyConstraintValue * hierachyConstraintW);
            score += (temporalConstraintValue * temporalConstraintW);

            //If this is a filter node, or the same node as the focus node, artificially set its score low
            if (filter_nodes.Contains(current_feature.Name.Split(new string[] { "##" }, StringSplitOptions.None)[0])
                || current_feature.Id.Equals(last_feature.Id))
            {
                //Console.WriteLine("Filtering out node " + current.Id);
                score = -1000000;
            }//end if

            //if (hierachyConstraintValue > 0)
            //  Console.WriteLine("hierarchy constraint for " + current.Id + " from " + oldTopic.Id + ": " + hierachyConstraintValue);

            if (print_calculation)
            //if (true)
            {
                Console.WriteLine("Have been addressed before: " + DiscussedAmount);
                Console.WriteLine("Spatial Constraint Satisfied: " + spatialConstraintValue);
                Console.WriteLine("Hierachy Constraint Satisfied: " + hierachyConstraintValue);
                Console.WriteLine("Temporal Constraint Satisfied: " + temporalConstraintValue);
                Console.WriteLine("Temporal Calculation: " + temporalConstraintValue * temporalConstraintW);
                string scoreFormula = "";
                scoreFormula += "score = Have Been Addressed * " + discussAmountW + " + abs(expectedDramaticV[" + turn_count + "] - dramaticValue)*" + noveltyW;
                scoreFormula += " + spatialConstraint*" + spatialConstraintW;
                scoreFormula += " + hierachyConstraint*" + hierachyConstraintW;
                scoreFormula += " + temporalConstraint*" + temporalConstraintW;
                scoreFormula += " = " + score;
                System.Console.WriteLine(scoreFormula);
            }
            return score;
        }//end function CalculateScore

        /// <summary>
        /// Returns a list of features that are most novel calculated against the given feature.
        /// Only returns the first 'amount' number of features.
        /// </summary>
        public List<Tuple<Feature, double>> GetMostNovelFeatures(Feature current_feature, int turn, List<Feature> topic_history, int amount = 5)
        {
            bool[] checkEntry = new bool[feature_graph.Count];
            List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
            this.TravelGraph(feature_graph.Root, current_feature, 0, false, checkEntry, turn, topic_history, ref listScore);
            //After calling travelGraph, listScore now contains a list of the score of each node
            //calculated against the currentTopic node passed in.
            //The following sort will sort them in descending order of calculated score.
            //This will place the "most novel" nodes
            listScore.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            return listScore;
        }// end getNovelty

        //Opposite of get novelty, get the ids of the features that, according to the calculation,
        //are most likely to be chosen as the next topic.
        /// <summary>
        /// Returns a list of features with the highest score calculated against the given feature.
        /// Only returns the first 'amount' number of features.
        /// </summary>
        public List<Tuple<Feature, double>> GetMostProximalFeatures(Feature currentTopic, int turn, List<Feature> topic_history, int amount = 5)
        {
            bool[] checkEntry = new bool[feature_graph.Count];
            List<Tuple<Feature, double>> listScore = new List<Tuple<Feature, double>>();
            this.TravelGraph(feature_graph.Root, currentTopic, 0, true, checkEntry, turn, topic_history, ref listScore);
            listScore.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            return listScore;
        }//end method getProximal

        //Returns the feature that would serve best as a switch point given
        // the input storyline.
        //Motivated by measure of proximity in paper by Leal, Rodrigues, and Queiros, 2012.
        public Feature IdentifySwitchPoint(List<Feature> storyline)
        {
            //Each feature will have a count of how many other features
            //in the storyline it is directly connected to.
            List<Tuple<Feature, int>> connectedness = new List<Tuple<Feature, int>>();
            foreach (Feature story_feature in storyline)
            {
                int number_of_connections = 0;
                foreach (Feature feature_to_compare in storyline)
                {
                    //Check if there is a relationship in either direction between the two.
                    if ((!story_feature.getRelationshipNeighbor(feature_to_compare.Id).Equals("")
                        && !(story_feature.getRelationshipNeighbor(feature_to_compare.Id) == null))
                        || (!feature_to_compare.getRelationshipNeighbor(story_feature.Id).Equals("")
                        && !(feature_to_compare.getRelationshipNeighbor(story_feature.Id) == null)))
                    {
                        //If so, then increment the connections count.
                        number_of_connections += 1;
                    }//end if
                }//end foreach
                //We now have how many other features in the storyline this feature is connected to.
                //Store it as a tuple in the connectedness list.
                connectedness.Add(new Tuple<Feature, int>(story_feature, number_of_connections));
            }//end foreach

            //Find the tuple with the highest connectedness.
            Feature return_feature = null;
            int highest_connectedness = 0;
            foreach (Tuple<Feature, int> connectedness_pair in connectedness)
            {
                if (connectedness_pair.Item2 > highest_connectedness)
                {
                    return_feature = connectedness_pair.Item1;
                    highest_connectedness = connectedness_pair.Item2;
                }//end if
            }//end foreach

            return return_feature;
        }//end method IdentifySwitchPoint

        //Calculate the relatedness between two features
        public double CalculateRelatedness(Feature feature_1, Feature feature_2)
        {
            double relatedness = 0;

            //SHORTEST PATH METRIC
            //Relatedness is equal to the inverse shortest path length between two features.
            relatedness = 1 / (feature_1.ShortestDistance[feature_2.Id]);

            return relatedness;
        }//end method CalculateRelatedness

        //Calculate the proximity between two features.
        //Proximity calculation based on paper by Leal, Rodrigues, and Queiros, 2012.
        //In the method from the paper described above, DBPedia nodes were being used as
        //proxies for similarity between terms. However, if DBPedia nodes are being taken
        //as the subject of proximity in the first place, this measure is even more accurate.
        public int CalculateProximity(Feature feature_1, Feature feature_2)
        {
            HashSet<Path> paths_from_1 = new HashSet<Path>();
            HashSet<Path> paths_from_2 = new HashSet<Path>();

            paths_from_1.Add(new Path(feature_1, 0));
            paths_from_2.Add(new Path(feature_2, 0));

            return Proximity(paths_from_1, paths_from_2, 1);
        }//end method CalculateProximity

        //Private recursive part of proximity function. Performs BFS from source and destination
        //to find path between the two.
        private int Proximity(HashSet<Path> paths_from_1, HashSet<Path> paths_from_2, int step)
        {
            int proximity = 0;

            //If either paths are empty, return the default proximity value.
            if (paths_from_1.Count == 0 || paths_from_2.Count == 0)
            {
                return proximity;
            }//end if

            HashSet<Path> unused_1 = new HashSet<Path>(paths_from_1);
            HashSet<Path> unused_2 = new HashSet<Path>(paths_from_2);

            foreach (Path path_1 in paths_from_1)
            {
                foreach (Path path_2 in paths_from_2)
                {
                    //If any path nodes are equal to each other, the paths connect
                    if (path_1.feature.Id.Equals(path_2.feature.Id))
                    {
                        int step3 = step * step * step;
                        proximity += (path_1.distance + path_2.distance) / step3;
                        unused_1.Remove(path_1);
                        unused_2.Remove(path_2);
                    }//end if
                }//end foreach
            }//end foreach

            //The maximum number of steps we're allowed to go.
            int MAX_STEP = 5;
            if (step < MAX_STEP)
            {
                HashSet<Path> related_1 = RelatedPaths(unused_1);
                HashSet<Path> related_2 = RelatedPaths(unused_2);

                proximity += Proximity(related_1, related_2, step + 1);
            }//end if

            return proximity;
        }//end method proximity

        private HashSet<Path> RelatedPaths(HashSet<Path> paths)
        {
            HashSet<Path> related = new HashSet<Path>();

            foreach (Path path in paths)
            {
                foreach (Tuple<Feature, double, string> temp_neighbor in path.feature.Neighbors)
                {
                    //Property property = arc.property <-- The relationship between these nodes.
                    //Node target = arc.target <-- the neighboring feature itself.
                    int relationship_weight = 1; //<-- In the future, relationships will be appropriately weighted.
                    //For now, it is assumed all weights are 1.

                    int distance = path.distance + relationship_weight;

                    related.Add(new Path(temp_neighbor.Item1, distance));
                }//end foreach
            }//end foreach

            return related;
        }//end method RelatedPaths

        //PRIVATE UTILITY FUNCTIONS
        //Make the hierarchy key from the relationships in the feature graph.
        private String[] CreateHierarchyKey(FeatureGraph source_graph)
        {
            String[] return_key = null;

            List<String> return_list = new List<String>();
            //Go through each node in the feature graph
            foreach (Feature temp_feat in feature_graph.Features)
            {
                //Go through each of the node's relationships
                foreach (Tuple<Feature, double, string> temp_neighbor_tuple in temp_feat.Neighbors)
                {
                    //Third member of each tuple is the relationship type.
                    String relationship = temp_neighbor_tuple.Item3;
                    //Add it to the return key if it isn't already there.
                    if (!return_list.Contains<String>(relationship))
                    {
                        return_list.Add(relationship);
                    }//end if
                }//end foreach
            }//end foreach

            return_key = return_list.ToArray();
            return return_key;
        }//end method CreateHierarchyKey

        /// <summary>
        /// Calculate the novelty of the given current feature against the given
        /// previous feature.
        /// </summary>
        private double CalculateNovelty(Feature current_feature, Feature previous_feature)
        {
            double noveltyValue = 0;

            // distance
            double dist = previous_feature.ShortestDistance[feature_graph.getFeatureIndex(current_feature.Id)] / feature_graph.MaxDistance;

            // previous talk
            double previousTalkPercentage = current_feature.getNeighborDiscussAmount();

            // tags
            double funFactTag = 0.0;
            if (current_feature.findTagType(Constant.FUN_FACT) != null)
            {
                funFactTag = 1.0;
            }//end if

            noveltyValue = dist * 0.5 + previousTalkPercentage * 0.5 + funFactTag * 0.5;
            if (print_calculation)
            {
                Console.WriteLine("Novelty Calculation");
                Console.WriteLine("Distance from current topic to previous topic: " + dist);
                Console.WriteLine("Percentage of related topics NOT covered: " + previousTalkPercentage);
                Console.WriteLine("Fun fact: " + funFactTag);
                Console.WriteLine("Novelty Value (0.5* distance + 0.5* % of related topics Not covered + 0.5*fun fact): " + noveltyValue);
            }//end if
            return noveltyValue;
        }//end function CalculateNovelty

        /// <summary>
        /// Determines whether or not the spatial constraint is met between the two given features
        /// with the given history of topic features.
        /// </summary>
        private bool SpatialConstraint(Feature current_feature, Feature previous_feature, List<Feature> topic_history)
        {
            string[] Directional_Words = { "is southwest of", "is southeast of"
                , "is northeast of", "is north of", "is west of", "is east of", "is south of", "is northwest of" };

            //From the history list, determine what the previous directional relationship was.
            string previous_directional_relationship = "";

            if (topic_history.Count() > 1)
            {
                //The current topic is always at the end of the history list.
                Feature current_topic = topic_history[topic_history.Count() - 1];
                Feature previous_topic = topic_history[topic_history.Count() - 2];
                if (previous_topic.getNeighbor(current_topic.Id) != null)
                {
                    foreach (string str in Directional_Words)
                    {
                        //Check whether the relationship between the previous topic and the
                        //current topic is a directional word.
                        if (str == previous_topic.getRelationshipNeighbor(current_topic.Id))
                        {
                            //If so, count it as our previous spatial relationship.
                            previous_directional_relationship = str;
                            break;
                        }//end if
                    }//end foreach
                }//end if
            }//end if

            //If there is no previous directional relationship, the spatial constraint
            //is considered met if the relationship between the previous feature
            //and the current feature is in the spatial_key.
            if (previous_directional_relationship == "")
            {
                for (int x = 0; x < current_feature.Neighbors.Count; x++)
                {
                    if (current_feature.Neighbors[x].Item1.Id == previous_feature.Id)
                    {
                        for (int y = 0; y < spatial_key.Length; y++)
                        {
                            if (spatial_key[y] == current_feature.Neighbors[x].Item3)
                            {
                                return true;
                            }//end if
                        }//end for
                    }//end if
                }//end for
                for (int x = 0; x < current_feature.Parents.Count; x++)
                {
                    if (current_feature.Parents[x].Item1.Id == previous_feature.Id)
                    {
                        for (int y = 0; y < spatial_key.Length; y++)
                        {
                            if (spatial_key[y] == current_feature.Parents[x].Item3)
                            {
                                return true;
                            }//end if
                        }//end for
                    }//end if
                }//end for
            }//end if
            //If there is a previous directional relationship, the spatial constraint is considered
            //met if the relationship between the current feature and the previous feature
            //is the previous directional relationship.
            else
            {
                for (int x = 0; x < current_feature.Neighbors.Count; x++)
                {
                    if (current_feature.Neighbors[x].Item1.Id == previous_feature.Id)
                    {
                        if (previous_directional_relationship == current_feature.Neighbors[x].Item3)
                        {
                            return true;
                        }//end if
                    }//end if
                }//end for
            }//end else
            return false;
        }//end function spatialConstraint

        /// <summary>
        /// Determines whether or not the hierarchy constraint is met between the two given features.
        /// </summary>
        private bool HierachyConstraint(Feature current_feature, Feature previous_feature)
        {
            //If the relationship between the previous feature and the current feature is
            //in the hierarchy key, then the hierarchy constraint is met.
            for (int x = 0; x < current_feature.Neighbors.Count; x++)
            {
                if (current_feature.Neighbors[x].Item1.Id == previous_feature.Id)
                {
                    for (int y = 0; y < hierarchy_key.Length; y++)
                    {
                        if (hierarchy_key[y] == current_feature.Neighbors[x].Item3)
                        {
                            return true;
                        }//end if
                    }//end for
                }//end if
            }//end for
            for (int x = 0; x < current_feature.Parents.Count; x++)
            {
                if (current_feature.Parents[x].Item1.Id == previous_feature.Id)
                {
                    for (int y = 0; y < hierarchy_key.Length; y++)
                    {
                        if (hierarchy_key[y] == current_feature.Parents[x].Item3)
                        {
                            return true;
                        }//end if
                    }//end for
                }//end if
            }//end for
            return false;
        }//end function hierarchyConstraint

        /// <summary>
        /// Input: the current topic, the current turn and the whole history
        /// Return: a list of temporal constraint that this topic can satisfy that are not satisfied yet.
        /// </summary>
        public List<int> TemporalConstraint(Feature current_topic, int turn, List<Feature> topic_history)
        {
            List<int> indexList = new List<int>();
            for (int x = 0; x < temporal_constraint_list.Count(); x++)
            {
                if (temporal_constraint_list[x].FirstArgument == current_topic.Name && !temporal_constraint_list[x].Satisfied)
                {
                    //Third argument is turn case 
                    if (temporal_constraint_list[x].getThirdArgumentType() == "turn")
                    {
                        if (temporal_constraint_list[x].SecondArgument == ">")
                        {
                            if (turn > Convert.ToInt32(temporal_constraint_list[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }//end if
                        }//end if
                        else if (temporal_constraint_list[x].SecondArgument == ">=")
                        {
                            if (turn >= Convert.ToInt32(temporal_constraint_list[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }//end if
                        }//end else if
                        else if (temporal_constraint_list[x].SecondArgument == "==")
                        {
                            if (turn == Convert.ToInt32(temporal_constraint_list[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }//end if
                        }//end else if
                        else if (temporal_constraint_list[x].SecondArgument == "<=")
                        {
                            if (turn <= Convert.ToInt32(temporal_constraint_list[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }//end if
                        }//end else if
                        else if (temporal_constraint_list[x].SecondArgument == "<")
                        {
                            if (turn < Convert.ToInt32(temporal_constraint_list[x].ThirdArgument))
                            {
                                indexList.Add(x);
                            }//end if
                        }//end else if
                    }//end for 
                    //Third argument is a topic case
                    else if (temporal_constraint_list[x].getThirdArgumentType() == "topic")
                    {
                        //There is only one prosible case that the constraint will be satisfied by current topic.
                        // First > Third , and Third has already been discussed (It is in history).
                        // this turn is already greater than all of the turn of topics in history.
                        // Only need to check whether third argument exists in history or not.
                        for (int y = 0; y < topic_history.Count(); y++)
                        {
                            if (temporal_constraint_list[x].ThirdArgument == topic_history[y].Id)
                            {
                                if (temporal_constraint_list[x].FourthArgument == "")
                                {
                                    indexList.Add(x);
                                    break; //Only need to find once instance of this topic in the history
                                }//end if
                                //To Do: Adding the case of fourth and fifth arguments
                            }//end if
                        }//end for
                    }//end else if
                }//end if
            }//end for
            return indexList;
        }//end function temporalConstraint

        //Using DFS, calculate the score between the previous_feature and every other feature,
        //starting from current_feature.
        //listScore stores every node's score relative to previous_feature. 
        private void TravelGraph(Feature current_feature, Feature previous_feature, int h, bool isCalculatedScore,
            bool[] checkEntry, int turn_count, List<Feature> topic_history, ref List<Tuple<Feature, double>> listScore)
        {
            //current's height is higher than the limit
            if (h >= height_limit)
            {
                return;
            }//end if
            int index = feature_graph.getFeatureIndex(current_feature.Id);
            if (checkEntry[index])
            {
                return;
            }//end if
            checkEntry[index] = true;

            if (print_calculation)
            {
                System.Console.WriteLine("\nNode: " + current_feature.Id);
            }//end if

            //Calculate score of choice and add to list
            if (isCalculatedScore)
            {
                listScore.Add(new Tuple<Feature, double>(current_feature, CalculateScore(current_feature, previous_feature, turn_count, topic_history)));
            }//end if
            else
            {
                listScore.Add(new Tuple<Feature, double>(current_feature, CalculateNovelty(current_feature, previous_feature)));
            }//end else

            //search children of current node
            for (int x = 0; x < current_feature.Neighbors.Count; x++)
            {
                TravelGraph(current_feature.Neighbors[x].Item1, previous_feature, h + 1, isCalculatedScore, checkEntry, turn_count, topic_history, ref listScore);
            }//end for
            for (int x = 0; x < current_feature.Parents.Count; x++)
            {
                TravelGraph(current_feature.Parents[x].Item1, previous_feature, h + 1, isCalculatedScore, checkEntry, turn_count, topic_history, ref listScore);
            }//end for
        }//end method TravelGraph

    }//end class NarrationCalculator
}
