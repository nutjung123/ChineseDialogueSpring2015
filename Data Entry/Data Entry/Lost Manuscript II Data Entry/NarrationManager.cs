using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIMLbot;

namespace Dialogue_Data_Entry
{
    //Narration Manager manages the flow of narration.
    //It keeps all memory of the conversation, such as what topics have been visited. 
    //It also has functions that provide what to say under different circumstances.
    class NarrationManager
    {
        private FeatureGraph feature_graph;     //The data structure holding every feature in the knowledge base.
        private Bot aiml_bot;                   //The AIML bot being used to help answer queries.
        private User user;                 //A user to make requests of the AIML bot.
        private Feature topic;                  //The current topic of conversation.
        private int turn;                       //A count of what turn of the conversation we are on.
        private List<Feature> topic_history;    //The history of topics in this conversation. Last item is always the topic.

        private List<TemporalConstraint> temporal_constraint_list;  //The list for temporal constraint checking. Does not change after init.

        //An buffer of things to say. Can be pulled from for outputs, and is reset
        //whenever the topic changes.
        private string[] _buffer;
        private string[] buffer { get { return _buffer; } set { _buffer = value; b = 0; } }
        private int b;  // buffer index. Gets reset when buffer does.

        public NarrationCalculator calculator;  //The calculator, for score/novelty values and constraints

        private const string IDK = "I'm afraid I don't know anything about that topic." + "##" + "对不起，我不知道。" + "##";

        public NarrationManager(FeatureGraph fg, List<TemporalConstraint> tcl)
        {
            feature_graph = fg;

            //Initialize the AIML chat bot
            this.aiml_bot = new Bot();
            aiml_bot.loadSettings();
            aiml_bot.isAcceptingUserInput = false;
            aiml_bot.loadAIMLFromFiles();
            aiml_bot.isAcceptingUserInput = true;
            this.user = new User("user", this.aiml_bot);

            //Default initializations
            topic = null;
            turn = 1;
            topic_history = new List<Feature>();
            temporal_constraint_list = tcl;
            calculator = new NarrationCalculator(feature_graph, tcl);

            //The first topic should be the root node of the feature graph
            if (this.topic == null)
                SetNextTopic(this.feature_graph.Root);
        }//end method DialogueManager

        //ACCESSIBLE FUNCTIONS
        /// <summary>
        /// Decides on the next topic automatically.
        /// Returns something to say about the next topic, with the addition of speak adornments (metaphor, lead-in statements, etc.)
        /// </summary>
        public string DefaultNextTopicResponse()
        {
            Feature nextTopic = this.topic;
            string[] newBuffer;
            string return_string = "";
            // Can't guarantee it'll actually move on to anything...

            //Have the calculator decide on the next topic.
            nextTopic = calculator.GetNextTopic(nextTopic, "", this.turn, this.TopicHistory);
            //Gets a list of things to say about the topic (speak values or neighbor relations)
            newBuffer = FindStuffToSay(nextTopic);

            //Set the next topic and reset the buffer to the list
            //of output strings from function FindStuffToSay
            SetNextTopic(nextTopic, newBuffer);

            //Get an output string from the buffer.
            return_string = PullOutputFromBuffer();

            //Adorn the answer, which sends it through speak transforms.
            //The answer will be used later on in the function.
            return_string = SpeakWithAdornments(this.topic, return_string);

            return return_string;
        }//end function DefaultNextTopic

        /// <summary>
        /// Returns the next thing to say about the current topic. Does not change topics,
        /// clear buffer, or update history list, but increments discussed_amount for current topic.
        /// </summary>
        public string TalkMoreAboutTopic()
        {
            string return_string = "";

            //ZEV: Change this in feature graph
            //Increment current topic discuss amount
            this.topic.DiscussedAmount += 1;
            this.feature_graph.setFeatureDiscussedAmount(this.topic.Id, this.topic.DiscussedAmount);

            //Get the next item from the output buffer.
            return_string = PullOutputFromBuffer();

            return return_string;
        }//end method TalkMoreAboutTopic

        /// <summary>
        /// Uses the given query to get the next topic. Returns something to say about
        /// the topic in the query.
        /// </summary>
        public string TalkFromQuery(Query query)
        {
            string return_string = "";

            //From a null query, return an "I don't know" response
            if (query == null)
            {
                return "I'm afraid I don't know the answer to that.";
            }//end if

            bool topic_switch = false;
            Console.WriteLine("Query main topic before: " + query.MainTopic.Id);
            Feature topic_before = query.MainTopic;
            Feature next_topic;
            string[] new_buffer;

            //The buffer should be filled with the output strings from ParseQuery
            new_buffer = ParseQuery(query);

            //The next topic is the topic identified by the query.
            next_topic = query.MainTopic;

            //Detect a topic switch
            if (!next_topic.Equals(topic_before))
            {
                topic_switch = true;
                Console.WriteLine("Query main topic changed to: " + query.MainTopic.Id);
            }//end if

            //Set the next topic and refill the buffer.
            SetNextTopic(next_topic, new_buffer);

            //The return_string, right now, is the result from ParseQuery.

            //If there is a topic change, make sure to introduce the new topic with its speak value.
            if (topic_switch)
            {
                //Get the current topic's speak value and adorn it
                String[] temp_buffer = FindStuffToSay(this.topic);
                String topic_speak = temp_buffer[0];
                //If there is no topic switch, use relationships during adornment. If there is, don't use relationships.
                topic_speak = SpeakWithAdornments(this.topic, topic_speak, !topic_switch);

                return_string = return_string + " " + topic_speak;
            }//end if

            //ZEV: Change this in feature graph
            //Increment current topic discuss amount
            this.topic.DiscussedAmount += 1;
            this.feature_graph.setFeatureDiscussedAmount(this.topic.Id, this.topic.DiscussedAmount);

            //Get the next item from the newly reset output buffer.
            return_string = PullOutputFromBuffer();

            return return_string;
        }//end method TalkMoreAboutTopic

        public List<Feature> ForwardProjection(Feature currentTopic, int forwardTurn)
        {
            //remember internal variables for forward projection
            List<Feature> internalTopicHistory = new List<Feature>(this.TopicHistory);
            int internalTurn = this.Turn;
            Feature internalTopic = this.Topic;
            Feature tempCurrentTopic = currentTopic;
            List<TemporalConstraint> temp_temporal_constraint_list = new List<TemporalConstraint>();
            for (int x = 0; x < temporal_constraint_list.Count(); x++)
            {
                temp_temporal_constraint_list.Add(new TemporalConstraint(temporal_constraint_list[x].FirstArgument,
                    temporal_constraint_list[x].SecondArgument, temporal_constraint_list[x].ThirdArgument,
                    temporal_constraint_list[x].FourthArgument, temporal_constraint_list[x].FifthArgument));
            }

            //Forward Projection
            List<Feature> topicList = new List<Feature>();
            //topicList.Add(currentTopic);
            //Progress 'forwardTurn' number of turns
            for (int x = 0; x < forwardTurn; x++)
            {
                //update Internal variables
                tempCurrentTopic = calculator.GetNextTopic(tempCurrentTopic, "", this.Turn, this.TopicHistory);
                this.SetNextTopic(tempCurrentTopic);
                this.Turn += 1;
                topicList.Add(tempCurrentTopic);
            }//end for

            //recover all old variables
            this.TopicHistory = internalTopicHistory;
            this.Turn = internalTurn;
            this.temporal_constraint_list = temp_temporal_constraint_list;
            this.Topic = internalTopic;
            for (int x = 0; x < forwardTurn; x++)
            {
                topicList[x].DiscussedAmount -= 1;
            }//end for

            return topicList;
        }

        //PUBLIC UTILITY FUNCTIONS
        //Uses the calculator to get the components of the score calculated between
        //the two given features.
        public double[] GetScoreComponents(Feature current_feature, Feature previous_feature)
        {
            return calculator.CalculateScoreComponents(current_feature, previous_feature
                                                        , this.Turn, this.TopicHistory);
        }//end function GetScoreComponents

        /// <summary>
        /// Returns a string consisting of the name and score of the first 'amount'
        /// number of nodes with the highest score calculated against the given feature.
        /// </summary>
        public string ListMostProximalFeatures(Feature current_feature, int amount = 5)
        {
            string answer = "";

            List<Tuple<Feature, double>> listScore = calculator.GetMostProximalFeatures(current_feature, this.Turn, this.TopicHistory, amount);

            //The string returned will consist of the ID and calculated score of the first amount nodes
            for (int x = 0; x < amount; x++)
            {
                answer += feature_graph.getFeatureIndex(listScore[x].Item1.Id) + " " + listScore[x].Item2 + " ";
            }//end for

            return answer;
        }//end ListMostNovelFeatures

        /// <summary>
        /// Returns a string consisting of the name and novelty value of the first 'amount'
        /// number of most novel nodes calculated against the given feature.
        /// </summary>
        public string ListMostNovelFeatures(Feature current_feature, int amount = 5)
        {
            string answer = "";

            List<Tuple<Feature, double>> listScore = calculator.GetMostNovelFeatures(current_feature, this.Turn, this.TopicHistory, amount);

            //The string returned will consist of the ID and calculated score of the first amount nodes
            for (int x = 0; x < amount; x++)
            {
                answer += feature_graph.getFeatureIndex(listScore[x].Item1.Id) + " " + listScore[x].Item2 + " ";
            }//end for

            return answer;
        }//end ListMostNovelFeatures

        /// <summary>
        /// Tells the input to the AIML chat bot. Returns the response from the chat bot.
        /// </summary>
        public string TellChatBot(string input)
        {
            string output = "";
            //Create a request, which can be passed to the chatbot.
            Request request = new Request(input, this.user, this.aiml_bot);
            Console.WriteLine("Chatbot Input: " + input);
            //Get the response from the chatbot
            Result result = aiml_bot.Chat(request);
            output = result.Output;
            Console.WriteLine("Chatbot Output: " + output);
            //<set name="it"><set name="that"><set name="this">
            //<think><set name="topic"><star/></set></think>
            return output;
        }//end method SayToChatBot

        //PRIVATE UTILITY FUNCTIONS
        //Returns the speak value passed in with adornments according to the feature passed in, such as topic lead-ins and analogies.
        private string SpeakWithAdornments(Feature feat, string speak, bool use_relationships = true)
        {
            //Treat the feature passed in as the current topic
            Feature current_topic = feat;

            Feature previous_topic = null;

            if (topic_history.Count < 2)
                previous_topic = null;
            else
                previous_topic = topic_history[topic_history.Count - 1];


            //Create the speak transform object, initialized with history list and the previous topic
            SpeakTransform transform = new SpeakTransform(topic_history, previous_topic);

            //Pass in the given feature and speak value to be transformed.
            String to_speak = transform.TransformSpeak(feat, speak);

            return to_speak;
        }//end method AdornMessage

        /// <summary>
        /// Finds one or more things to say about the given feature
        /// and returns them as an array of strings.
        /// </summary>
        /// <param name="feature">The feature to finds something to say about.</param>
        private string[] FindStuffToSay(Feature feature)
        {
            //Check for null input
            if (feature == null)
                return null;

            List<string> stuff = new List<string>();
            //Get all of this feature's speak values
            string[] speaks = feature.Speaks.ToArray();
            //Add each of the feature's speak values to the list of things to say about it
            if (speaks.Length > 0)
            {
                stuff.AddRange(speaks);
            }// end if

            stuff.AddRange(SpeakNeighborRelations(feature.Name, FindAllDirectionalNeighbors(feature)));

            //If nothing else can be found, simply speak the feature's name
            if (stuff.Count() == 0)
            {
                stuff.Add(feature.Name);
            }//end if
            return stuff.ToArray();
        }// end function FindStuffToSay

        /// <summary>
        /// Takes a Query object and builds a list of output strings
        /// to talk about the query's MainTopic, taking its specified question
        /// words and direction words, if any, into consideration.
        /// </summary>
        /// <param name="query"></param>
        private string[] ParseQuery(Query query)
        {
            if (query == null)
                return new string[] { "I don't know." };

           //Related to spatial constraint. Relationships that can be used to describe the location of something.
           string[] locational_words = { "is north of", "is northwest of", "is east of", "is south of"
                                                , "is in", "is southwest of", "is west of", "is northeast of"
                                                , "is southeast of", "took place at", "was held by"
                                                , "was partially held by" };

            List<string> output = new List<string>();

            if (query.IsQuestion)
            {
                switch (query.QuestionType)
                {
                    case Question.WHAT:
                        if (query.HasDirection)
                        {
                            // e.g. What is Direction of Topic?
                            // Find names of features that is DIRECTION of MainTopic`
                            // Get list of <neighbor> tags
                            string dir = query.Direction.ToString().ToLower();
                            if (query.Direction == Direction.WON)
                            {
                                Feature[] temp_neighbors = query.MainTopic.GetNeighborsByRelationship(dir);
                                // If the topic has no "won" links, then it is the event
                                if (temp_neighbors.Length == 0)
                                {
                                    // So find the winner among its available neighbors
                                    temp_neighbors = query.MainTopic.GetNeighborsByRelationship("");
                                    foreach (Feature temp_neighbor in temp_neighbors)
                                    {
                                        // Look at ITS neighbors and see if there is a "won" whose name matches this one
                                        foreach (var triple in temp_neighbor.Neighbors)
                                        {
                                            if (triple.Item1.Id == query.MainTopic.Id && triple.Item3 == "won")
                                                output.Add(string.Format("{0} won {1}.", temp_neighbor.Name, query.MainTopic.Id));
                                        }// end foreach
                                    }// end foreach
                                }// end if
                                // Otherwise it is the winner
                                else
                                {
                                    List<string> neighbor_names = new List<string>();
                                    foreach (Feature temp_neighbor in temp_neighbors)
                                        neighbor_names.Add(temp_neighbor.Name);

                                    output.Add(string.Format("{0} won {1}.", query.MainTopic.Id, neighbor_names.JoinAnd()));
                                }// end else
                            }//end if

                            //Directional What is question (e.g., What is south of...?)
                            else if (query.Direction == Direction.NORTH
                                || query.Direction == Direction.SOUTH
                                || query.Direction == Direction.EAST
                                || query.Direction == Direction.WEST
                                || query.Direction == Direction.NORTHEAST
                                || query.Direction == Direction.SOUTHEAST
                                || query.Direction == Direction.NORTHWEST
                                || query.Direction == Direction.SOUTHWEST)
                            {
                                //Relationships to answer these question have the form "is <direction> of".
                                //From the topic of the query, look for such a relationship.
                                Feature query_topic = query.MainTopic;

                                foreach (Tuple<Feature, double, string> temp_neighbor in query_topic.Neighbors)
                                {
                                    //The main topic's neighbor
                                    Feature temp_feature = temp_neighbor.Item1;
                                    if (temp_feature.getRelationshipNeighbor(query_topic.Id).ToLower().Contains(query.DirectionWord.ToLower()))
                                    {
                                        output.Add(string.Format("{0} " + temp_feature.getRelationshipNeighbor(query_topic.Id) + " {1}.", temp_feature.Id, query_topic.Id));
                                        break;
                                    }//end if
                                }//end foreach
                            }//end else if

                            //Handles question like "what did x host?"
                            else if (query.Direction == Direction.HOSTED)
                            {
                                //string[] neighbors = FindNeighborsByRelationship(query.MainTopic, dir);
                                //These relationships signal that something was hosted
                                string[] hosted_words = { "held", "partially held" };
                                Feature query_topic = query.MainTopic;
                                string for_output = "";
                                //if (neighbors.Length > 0)
                                //    output.Add(string.Format("{0} hosted {1}.", query.MainTopic.Id, neighbors.ToList().JoinAnd()));

                                //Check from topic to neighbors
                                for_output = ConstructQueryOutputByRelationship(query, hosted_words.ToList<string>());

                                if (!for_output.Equals(""))
                                {
                                    output.Add(for_output);
                                }//end if
                            }//else if

                            //Questions like "What is inside x?"
                            else if (query.Direction == Direction.INSIDE)
                            {
                                //These relationships signal that something was inside something else (e.g., venues inside the olympic green)
                                string[] inside_words = { "is in" };
                                Feature query_topic = query.MainTopic;
                                string for_output = "";
                                //if (neighbors.Length > 0)
                                //    output.Add(string.Format("{0} hosted {1}.", query.MainTopic.Id, neighbors.ToList().JoinAnd()));

                                foreach (Tuple<Feature, double, string> temp_neighbor in query_topic.Neighbors)
                                {
                                    foreach (string inside_word in inside_words)
                                    {
                                        /*if (temp_feature.getRelationshipNeighbor(query_topic.Id).ToLower().Contains(query.DirectionWord.ToLower()))
                                        {
                                            output.Add(string.Format("{0} " + temp_feature.getRelationshipNeighbor(query_topic.Id) + " {1}.", temp_feature.Id, query_topic.Id));
                                            break;
                                        }//end if*/
                                        //Checking from Neighbor to Topic
                                        if (temp_neighbor.Item1.getRelationshipNeighbor(query_topic.Id).ToLower().Contains(query.DirectionWord.ToLower()))
                                        {
                                            //DEBUG
                                            Console.Out.WriteLine("Inside word " + inside_word + " found.");
                                            //END DEBUG
                                            if (for_output.Equals(""))
                                                for_output = string.Format("{0} " + temp_neighbor.Item3 + " {1}", temp_neighbor.Item1.Id, query_topic.Id);
                                            else
                                                for_output += string.Format(", {0} " + temp_neighbor.Item3 + " {1}", temp_neighbor.Item1.Id, query_topic.Id);
                                            //for_output.Add(string.Format("{0} " + temp_neighbor.Item3 + " {1}.", query_topic.Id, temp_neighbor.Item1.Id));
                                        }//end if
                                    }//end foreach
                                }//end foreach
                                if (!for_output.Equals(""))
                                {
                                    for_output += ".";
                                    output.Add(for_output);
                                }//end if
                            }//end else if

                            else
                            {
                                Feature[] neighbors = query.MainTopic.GetNeighborsByRelationship(dir);
                                List<string> neighbor_names = new List<string>();
                                foreach (Feature temp_neighbor in neighbors)
                                {
                                    neighbor_names.Add(temp_neighbor.Name);
                                }//end foreach

                                if (neighbors.Length > 0)
                                    output.Add(string.Format("{0} of {1} {2} {3}", dir.ToUpperFirst(), query.MainTopic.Id,
                                        (neighbors.Length > 1) ? "are" : "is", neighbor_names.JoinAnd()));
                            }//end else
                        }// end if
                        //Otherwise, the WHAT question has no direction.
                        else
                        {
                            // e.g. What is Topic?
                            // Get the <speak> attribute, if able
                            string[] speak = FindStuffToSay(query.MainTopic);
                            if (speak.Length > 0)
                            {
                                //Addorn the speak value
                                speak[0] = SpeakWithAdornments(query.MainTopic, speak[0]);

                                output.AddRange(speak);
                            }//end if
                        }//end else
                        break;
                    case Question.WHERE:
                        if (false)
                        {

                        }
                        else
                        {
                            // e.g. Where is Topic?
                            // Get all the neighbors from this feature and the "opposite" directions
                            //output.AddRange((SpeakNeighborRelations(query.MainTopic.Id, FindAllNeighbors(query.MainTopic))));

                            //Where is the main topic
                            Feature query_topic = query.MainTopic;

                            string for_output = "";

                            //Check from topic to neighbors
                            for_output = ConstructQueryOutputByRelationship(query, locational_words.ToList<string>());

                            if (!for_output.Equals(""))
                            {
                                output.Add(for_output);
                            }//end if
                        }
                        break;
                    case Question.WHEN:
                        // e.g. When was Topic made/built/etc.?
                        break;
                }
            }//end if
            else
            {
                // e.g.:
                // Tell me about Topic.
                // Topic.
                output.Add(SpeakWithAdornments(query.MainTopic, FindStuffToSay(query.MainTopic)[0]));
            }//end else

            return output.Count() > 0 ? output.ToArray() : new string[] { IDK };
        }// end function ParseQuery

        //Constructs an output to a given query by examining the list of words to check against the relationships
        //that the query's main topic has with its neighbors.
        //Last optional parameter decides whether we are checking the relationships from the topic to its neighbors or 
        //the relationships from the neighbors to the topic.
        private string ConstructQueryOutputByRelationship(Query query, List<string> words_to_check, bool from_topic_to_neighbors = true)
        {
            string output_string = "";

            //Where is the main topic
            Feature query_topic = query.MainTopic;
            //What topic should we change to
            Feature topic_change = query.MainTopic;

            if (from_topic_to_neighbors)
                //Look for one of the locational words in the main topic's relationships
                foreach (Tuple<Feature, double, string> temp_neighbor in query_topic.Neighbors)
                {
                    foreach (string word_to_check in words_to_check)
                    {
                        if (temp_neighbor.Item3.ToLower().Contains(word_to_check.ToLower()))
                        {
                            //DEBUG
                            Console.Out.WriteLine("Word to check " + word_to_check + " found.");
                            //END DEBUG
                            if (output_string.Equals(""))
                            {
                                //For now, just take the first matching feature and (potentially) change the topic to that.
                                topic_change = temp_neighbor.Item1;
                                output_string = string.Format("{0} " + temp_neighbor.Item3 + " {1}", query_topic.Id, temp_neighbor.Item1.Id);
                            }//end if
                            else
                                output_string += string.Format(", " + temp_neighbor.Item3 + " {0}", temp_neighbor.Item1.Id);
                            //for_output.Add(string.Format("{0} " + temp_neighbor.Item3 + " {1}.", query_topic.Id, temp_neighbor.Item1.Id));
                        }//end if
                    }//end foreach
                }//end foreach

            if (!output_string.Equals(""))
            {
                //Find the last comma and put an "and" after it
                if (output_string.LastIndexOf(",") > 0)
                {
                    output_string = output_string.Insert(output_string.LastIndexOf(","), " and");
                }//end if
                output_string += ". ";

                //If the query topic and the current topic are the same, avoid
                //repeating the current topic by changing the query topic.
                if (query_topic.Equals(this.topic))
                {
                    //Change the main topic
                    query.MainTopic = topic_change;
                }//end if

                //Whether or not the topic has changed, say something about the main topic at the end.
                //output_string += FindStuffToSay(query.MainTopic)[0];
            }//end if
            return output_string;
        }//end method ConstructQueryOutputByRelationship

        //Creates a list of strings that say the featureName passed in and its relationship to each neighbor.
        private string[] SpeakNeighborRelations(string featureName, Tuple<string, Direction>[] neighbors)
        {
            string[] neighborRelations = new string[neighbors.Length];
            if (neighborRelations.Length == 0)
                return new string[] { };
            for (int i = 0; i < neighborRelations.Length; i++)
                //Creates strings of the form "(featureName or It) is (relationship) of (neighbor node)
                neighborRelations[i] = string.Format("{0} is {1} of {2}.",
                    (i == 0) ? featureName : "It",
                    neighbors[i].Item2.Invert().ToString().ToLower(),
                    neighbors[i].Item1);
            return neighborRelations;
        }//end function SpeakNeighborRelations

        /// <summary>
        /// Finds all neighbors to the given feature which have a directional relationship
        /// to the feature. Returns them in a tuple comprised of the neighbor's name and its direction.
        /// </summary>
        private Tuple<string, Direction>[] FindAllDirectionalNeighbors(Feature feature)
        {
            string[] directionWords = {"inside", "contain", "north", "east", "west", "south",
                                      "northeast", "northwest", "southeast", "southwest",
                                      "hosted", "was_hosted_at", "won"};

            var _neighbors = feature.Neighbors;
            var neighbors = new List<Tuple<string, Direction>>();
            foreach (var triple in _neighbors)
            {
                string neighborName = triple.Item1.Name;
                string relationship = triple.Item3;
                if (directionWords.Contains(relationship))
                    neighbors.Add(new Tuple<string, Direction>(neighborName,
                        ((Direction)Enum.Parse(typeof(Direction), relationship.ToUpper().Replace(' ', '_')))));
            }
            return neighbors.ToArray();
        }//end function FindAllNeighbors

        /// <summary>
        /// Pulls an output string from the buffer according to the buffer index.
        /// Increments the buffer index, so the next pull will be from the next item in the buffer.
        /// Getting to the end of the buffer will result in an "out of responses" reply.
        /// </summary>
        public string PullOutputFromBuffer()
        {
            if (b > this.buffer.Length - 1)
                return "I've said all I can about that topic!" + "##" + "我已经把我知道的都说完了。" + "##";
            else
                return this.buffer[b++];
        }//end function PullOutputFromBuffer

        /// <summary>
        /// Sets the current topic feature to the given feature, incrementing the new topic's
        /// discussed amount. Updates topic history. Does not reset the output buffer.
        /// </summary>
        /// <param name="next_topic">The next topic feature, which will become the current topic.</param>
        private void SetNextTopic(Feature next_topic)
        {
            //Place the next topic in the history list
            UpdateTopicHistory(next_topic);

            next_topic.DiscussedAmount += 1;
            this.feature_graph.setFeatureDiscussedAmount(next_topic.Id, next_topic.DiscussedAmount);
            this.topic = next_topic;
            //Set the topic in the AIML chatbot
            string temp = TellChatBot("SETTOPIC " + this.topic.Name.Split(new string[] { "##" }, StringSplitOptions.None)[0]);
            //string temp = TellChatBot("SETTOPIC");
        }//end method ChangeTopic
        /// <summary>
        /// Sets the current topic feature to the given topic feature, incrementing the next topic's
        /// discussed amount. Updates topic history. Also resets the output string buffer to the given array. 
        /// Reseting the buffer resets the buffer index, b, to 0.
        /// </summary>
        /// <param name="next_topic">The next topic feature, which will become the current topic.</param>
        /// <param name="new_buffer">The string array that the output buffer will be set to.</param>
        public void SetNextTopic(Feature next_topic, string[] new_buffer)
        {
            //Set the topic
            SetNextTopic(next_topic);
            //Fill the passed in buffer
            this.buffer = new_buffer;
        }//end method ChangeTopic

        /// <summary>
        /// Adds the given feature to the end of the topic history list and updates any relevant
        /// other information. Currently, updates spatial and temporal constraint information.
        /// </summary>
        private void UpdateTopicHistory(Feature new_topic)
        {
            ///ZEV: Remove?
            /*string[] Directional_Words = { "is southwest of", "is southeast of"
                , "is northeast of", "is north of", "is west of", "is east of", "is south of", "is northwest of" };

            //update spatial constraint information
            bool spatialExist = false;
            if (topic_history.Count() > 0)
            {
                Feature prevTopic = topic_history[topic_history.Count() - 1];
                if (prevTopic.getNeighbor(new_topic.Id) != null)
                {
                    foreach (string str in Directional_Words)
                    {
                        //Check whether there was a directional word
                        if (str == prevTopic.getRelationshipNeighbor(new_topic.Id))
                        {
                            previous_directional = str;
                            spatialExist = true;
                            break;
                        }
                    }//end foreach
                }
            }//end if
            if (!spatialExist)
            {
                previous_directional = "";
            }//end if*/

            //update temporal constraint information
            /*NarrationManager temp = new NarrationManager(this.feature_graph, temporal_constraint_list);
            List<int> temporalIndex = temp.TemporalConstraint(new_topic, this.Turn, topic_history);
            for (int x = 0; x < temporalIndex.Count(); x++)
            {
                temporal_constraint_list[temporalIndex[x]].Satisfied = true;
            }//end for*/
            //Place the new topic at the end of the topic history
            topic_history.Add(new_topic);
        }//end method UpdateTopicHistory

        //ACCESSORS/MUTATORS
        /// <summary>
        /// The feature which is currently the main topic of narration/conversation.
        /// </summary>
        public Feature Topic
        {
            get
            {
                return this.topic;
            }//end get
            set
            {
                this.topic = value;
            }//end set
        }

        /// <summary>
        /// A count of which turn of conversation we are on.
        /// </summary>
        public int Turn
        {
            get
            {
                return this.turn;
            }//end get
            set
            {
                this.turn = value;
            }//end set
        }

        /// <summary>
        /// A history list of which features have been the topic of narration/conversation.
        /// In chronological order.
        /// </summary>
        public List<Feature> TopicHistory
        {
            get
            {
                return this.topic_history;
            }//end get
            set
            {
                this.topic_history = value;
            }//end set
        }

    }//end class NarrationManager
}//end namespace
