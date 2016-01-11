using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dialogue_Data_Entry;
using AIMLbot;
using System.Collections;
using System.Diagnostics;

namespace Dialogue_Data_Entry
{
    enum Direction : int
    {
        NORTH = 1, SOUTH = -1,
        EAST = 2, WEST = -2,
        NORTHEAST = 3, SOUTHWEST = -3,
        NORTHWEST = 4, SOUTHEAST = -4,
        CONTAIN = 5, INSIDE = -5,
        HOSTED = 6, WAS_HOSTED_AT = -6,
        WON = 0
    }
    enum Question : int
    {
        WHAT = 0, WHERE = 1, WHEN = 2
    }

    /// <summary>
    /// A data structure to hold information about a query
    /// </summary>
    class Query
    {
        // The name of the Feature that the user asked about
        public Feature MainTopic { get; set; }
        // Whether or not the input was an explicit question
        public bool IsQuestion { get { return QuestionType != null; } }
        // The type of Question
        public Question? QuestionType { get; private set; }
        // The direction/relationship asked about.
        public Direction? Direction { get; private set; }
        public string DirectionWord { get; private set; }
        public bool HasDirection { get { return Direction != null; } }

        public Query(Feature mainTopic, Question? questionType, Direction? directions, string direction_word = "")
        {
            MainTopic = mainTopic;
            QuestionType = questionType;
            Direction = directions;
            DirectionWord = direction_word;
        }
        public override string ToString()
        {
            string s = "Topic: " + MainTopic.Data;
            s += "\nQuestion type: " + QuestionType ?? "none";
            s += "\nDirection specified: " + Direction ?? "none";
            s += "\nDirection word: " + DirectionWord ?? "none";
            return s;
        }
    }

    /// <summary>
    /// A utility class to parse natural input into a Query and a Query into natural output.
    /// </summary>
    class QueryHandler
    {
        private const string FORMAT = "FORMAT:";
        private const string IDK = "I'm afraid I don't know anything about that topic." + "##" + "对不起，我不知道。" + "##";
        private string[] punctuation = { ",", ";", ".", "?", "!", "\'", "\"", "(", ")", "-" };
        private string[] questionWords = { "?", "what", "where", "when" };

        private string[] directionWords = {"inside", "contain", "north", "east", "west", "south",
                                      "northeast", "northwest", "southeast", "southwest",
                                      "hosted", "was_hosted_at", "won"};

        private string[] Directional_Words = { "is southwest of", "is southeast of"
                , "is northeast of", "is north of", "is west of", "is east of", "is south of", "is northwest of" };

        //Related to spatial constraint. Relationships that can be used to describe the location of something.
        private string[] locational_words = { "is north of", "is northwest of", "is east of", "is south of"
                                                , "is in", "is southwest of", "is west of", "is northeast of"
                                                , "is southeast of", "took place at", "was held by"
                                                , "was partially held by" };

        
        // "is in" -> contains?
        private Bot bot;
        private User user;
        private FeatureGraph graph;
        private Feature topic;
        private List<string> features;
        private string[] _buffer;
        private string[] buffer { get { return _buffer; } set { _buffer = value; b = 0; } }
        private int b;  // buffer index gets reset when buffer does
        private int turn;
        private int noveltyAmount = 5;
        private List<TemporalConstraint> temporalConstraintList;
        private List<string> topicHistory = new List<string>();
        private string prevSpatial;

        public LinkedList<Feature> prevCurr = new LinkedList<Feature>();

        //A list of all the features that have been chosen as main topics
        public LinkedList<Feature> feature_history = new LinkedList<Feature>();
        //The topic before the current one
        public Feature previous_topic;

		public int countFocusNode = 0;
		public double noveltyValue = 0.0;

        //A list of string lists, each of which represents a set of relationship
        //words which may be interchangeable when used to find analogies.
        public List<List<string>> equivalent_relationships = new List<List<string>>();

        //FILTERING:
        //A list of nodes to filter out of mention.
        //Nodes in this list won't be spoken explicitly unless they
        //are directly queried for.
        //These nodes are still included in traversals, but upon traveling to
        //one of these nodes the next step in the traversal is automatically taken.
        public List<string> filter_nodes = new List<string>();
        //A list of relationships which should not be used for analogies.
        public List<String> no_analogy_relationships = new List<string>();

        //JOINT MENTIONS:
        //A list of feature lists, each of which represent
        //nodes that should be mentioned together
        public List<List<Feature>> joint_mention_sets = new List<List<Feature>>();

        //Which language we are operating in.
        //Default is English.
        public int language_mode_display = Constant.EnglishMode;
        public int language_mode_tts = Constant.EnglishMode;

        //A string to be used for text-to-speech
        public string buffered_tts = "";

        /// <summary>
        /// Create a converter for the specified XML file
        /// </summary>
        /// <param name="xmlFilename"></param>
        public QueryHandler(FeatureGraph graph, List<TemporalConstraint> myTemporalConstraintList)
        {
            // Load the AIML Bot
            this.bot = new Bot();
            this.temporalConstraintList = myTemporalConstraintList;
            bot.loadSettings();
            bot.isAcceptingUserInput = false;
            bot.loadAIMLFromFiles();
            bot.isAcceptingUserInput = true;
            this.user = new User("user", this.bot);

            // Load the Feature Graph
            this.graph = graph;

            // Feature Names, with which to index the graph
            this.features = graph.getFeatureNames();

            this.turn = 1;
            this.topic = null;

            //Build lists of equivalent relationships
            //is, are, was, is a kind of, is a
            equivalent_relationships.Add(new List<string>() { "is", "are", "was", "is a kind of", "is a" });
            //was a member of, is a member of
            equivalent_relationships.Add(new List<string>() { "was a member of", "is a member of" });
            //won a gold medal in, won
            equivalent_relationships.Add(new List<string>() { "won a gold medal in", "won" });
            //is one of, was one of the, was one of
            equivalent_relationships.Add(new List<string>() { "is one of", "was one of the", "was one of" });
            //include, includes, included
            equivalent_relationships.Add(new List<string>() { "include", "includes", "included" });
            //took place on
            equivalent_relationships.Add(new List<string>() { "took place on" });
            //took place at
            equivalent_relationships.Add(new List<string>() { "took place at" });
            //has, had
            equivalent_relationships.Add(new List<string>() { "has", "had" });
            //includes event
            equivalent_relationships.Add(new List<string>() { "includes event" });
            //includes member, included member
            equivalent_relationships.Add(new List<string>() { "includes member", "included member" });
            //include athlete
            equivalent_relationships.Add(new List<string>() { "include athlete" });
            //is southwest of, is southeast of, is northeast of, is north of,
            //is west of, is east of, is south of, is northwest of
            equivalent_relationships.Add(new List<string>() { "is southwest of", "is southeast of"
                , "is northeast of", "is north of", "is west of", "is east of", "is south of", "is northwest of" });

            //Build list of filter nodes.
            //Each filter node is identified by its Data values in the XML
            filter_nodes.Add("Male");
            filter_nodes.Add("Female");
            filter_nodes.Add("Cities");
            filter_nodes.Add("Sports");
            filter_nodes.Add("Gold Medallists");
            filter_nodes.Add("Venues");
            filter_nodes.Add("Time");
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


            //Build list of relationships which should not be used in analogies.
            no_analogy_relationships.Add("occurred before");
            no_analogy_relationships.Add("occurred after");
            no_analogy_relationships.Add("include");
            no_analogy_relationships.Add("includes");
            no_analogy_relationships.Add("included");
            no_analogy_relationships.Add("has");
            no_analogy_relationships.Add("had");
        }
			
	    private string MessageToServer(Feature feat, string speak, string noveltyInfo, string proximalInfo = "", bool forLog = false, bool out_of_topic_response = false)
        {
            String return_message = "";

            String to_speak = speak; //SpeakWithAdornments(feat, speak);

            //Add adjacent node info to the end of the message.
            //
            //to_speak += AdjacentNodeInfo(feat, last);

            if (out_of_topic_response)
            {
                //"I'm afraid I don't know anything about ";
                to_speak = "I'm sorry, I'm afraid I don't understand what you are asking. But here's something I do know about. "
                   + "##" + "对不起，我不知道您在说什么。但我知道这些。" + "##" + to_speak;

            }//end if

            string tts = ParseOutput(to_speak, language_mode_tts);
            buffered_tts = tts;
            to_speak = ParseOutput(to_speak, language_mode_display);

            if (forLog)
                return_message = to_speak + "\r\n";
            else
            {
                return_message = " ID:" + this.graph.getFeatureIndex(feat.Data) + ":Speak:" + to_speak + ":Novelty:" + noveltyInfo + ":Proximal:" + proximalInfo;
                //return_message += "##" + tts;
            }//end else
                

            //Console.WriteLine("to_speak: " + to_speak);

            return return_message;
        }//end function MessageToServer

        //Returns the speak value passed in with adornments according to the feature passed in, such as topic lead-ins and analogies.
        public string SpeakWithAdornments(Feature feat, string speak, bool use_relationships = true)
        {
            //Update the feature history list
            feature_history.AddLast(feat);
            countFocusNode += 1;
            //Store the last history_size number of nodes
            int history_size = 100;
            if (feature_history.Count > history_size)
            {
                feature_history.RemoveFirst();
            }//end if

            //Treat the feature passed in as the current topic
            Feature current_topic = feat;

            //Create the speak transform object, initialized with history list and the previous topic
            SpeakTransform transform = new SpeakTransform(feature_history, previous_topic);

            //Pass in the given feature and speak value to be transformed.
            String to_speak = transform.TransformSpeak(feat, speak);

            //The current topic has been used, and is now the previous topic.
            previous_topic = current_topic;

            return to_speak;
        }//end method AdornMessage
       
        //update various history when the system choose the next topic
        public void updateHistory(Feature nextTopic)
        {
            //update spatial constraint information
            bool spatialExist = false;
            if (topicHistory.Count() > 0)
            {
                Feature prevTopic = graph.getFeature(topicHistory[topicHistory.Count() - 1]);
                if (prevTopic.getNeighbor(nextTopic.Data) != null)
                {
                    foreach(string str in Directional_Words)
                    {
                        if (str == prevTopic.getRelationshipNeighbor(nextTopic.Data))
                        {
                            prevSpatial = str;
                            spatialExist = true;
                            break;
                        }
                    }
                }
            }
            if (!spatialExist)
            {
                prevSpatial = "";
            }

            //update temporal constraint information
            FeatureSpeaker temp = new FeatureSpeaker(this.graph, temporalConstraintList);
            List<int> temporalIndex = temp.temporalConstraint(nextTopic,turn,topicHistory);
            for (int x = 0; x < temporalIndex.Count(); x++)
            {
                temporalConstraintList[temporalIndex[x]].Satisfied = true;
            }
            //update topic's history
            topicHistory.Add(nextTopic.Data);
        }//end method updateHistory

        //Form2 calls this function
        //input is the input to be parsed.
        //messageToServer indicates whether or not we are preparing a response to the front-end.
        //forLog indicates whether or not we are preparing a response for a log output.
        //outOfTopic indicates whether or not we are continuing out-of-topic handling.
        //projectAsTopic true means we use forward projection to choose the next node to traverse to based on
        //  how well the nodes in the n-length path from the current node relate to the current node.
        public string ParseInput(string input, bool messageToServer = false, bool forLog = false, bool outOfTopic = false, bool projectAsTopic = false)
        {
            string answer = IDK;
            string noveltyInfo = "";
            double currentTopicNovelty = -1;
            // Pre-processing

            //Console.WriteLine("parse input " + input);

            //The input may be delimited by colons. Try to split it.
            String[] split_input = input.Trim().Split(':');
            //Console.WriteLine("split input " + split_input[0]);

            // Lowercase for comparisons
            input = input.Trim().ToLower();
            //Console.WriteLine("trimmed lowered input " + input);

            if (!string.IsNullOrEmpty(input))
            {
                // Check to see if the AIML Bot has anything to say.
                Request request = new Request(input, this.user, this.bot);
                //Call the AIML Chat Bot and give it the input ParseInput was given.
                Result result = bot.Chat(request);
                string output = result.Output;
                
                //If the chatbot has a feedback response, it will begin its
                //response with "FORMAT"
                if (output.Length > 0)
                {
                    //If the word "FORMAT" is not found, the response from the chatbot
                    //is not a feedback response. Return it.
                    if (!output.StartsWith(FORMAT))
                        return output;
                    
                    //MessageBox.Show("Converted output reads: " + output);
                    input = output.Replace(FORMAT, "").ToLower();
                }//end if
            }//end if

            // Remove punctuation
            input = RemovePunctuation(input);
            
            // If there is no topic node, start at the graph's root node
            if (this.topic == null)
                this.topic = this.graph.Root;

            // Create a new feature speaker to speak about this input
            FeatureSpeaker speaker = new FeatureSpeaker(this.graph, temporalConstraintList, prevSpatial, topicHistory);

            //Check for an explicit command in the input.
            if (split_input.Length != 0 || messageToServer)
            {
                string command_result = HandleInputCommand(split_input, speaker);
                //Only stop and return the result of a command here if there
                //is a command result to return.
                if (!command_result.Equals(""))
                    return command_result;
            }//end else if

            //Whether or not we had to switch topics
            bool topic_switch = false;

            // CASE: Nothing / Move on to next topic
            if (string.IsNullOrEmpty(input))
            {

                Feature nextTopic = this.topic;
                string[] newBuffer;
                
                // Can't guarantee it'll actually move on to anything...

                //Have the feature speaker decide on the next topic.
                nextTopic = speaker.getNextTopic(nextTopic, "", this.turn);
                //Console.WriteLine("Next Topic from " + this.topic.Data + " is " + nextTopic.Data);
                //Gets the first noveltyAmount number of nodes with the highest novelty.
                //Which nodes are most novel is decided by the getNovelty function in FeatureSpeaker.
                noveltyInfo = speaker.getNovelty(nextTopic, this.turn, noveltyAmount);
                //FeatureSpeaker maintains a list of novelty values. This gets the novelty of the
                //current topic calculated from the previous topic.
                currentTopicNovelty = speaker.getCurrentTopicNovelty();
                //Update the novelty value stored in QueryHandler (ZEV: do we need this?)
				noveltyValue = speaker.getCurrentTopicNovelty();
                //Gets a list of things to say about the topic (speak values or neighbor relations)
                newBuffer = FindStuffToSay(nextTopic);
                //MessageBox.Show("Explored " + nextTopic.Data + " with " + newBuffer.Length + " speaks.");
                //Increment how many times we've talked about the topic feature.
                nextTopic.DiscussedAmount += 1;
                //Sets the feature graph's internal count of how many times we've talked about this feature.
                //ZEV: Redundant with above? Should discuss amount setting in feature be included in setFeaturedDiscussedAmount?
                this.graph.setFeatureDiscussedAmount(nextTopic.Data, nextTopic.DiscussedAmount);
                //Set the current topic to the next topic the FeatureSpeaker has decided on.
                this.topic = nextTopic;
                //The buffer index, b, is set to 0 each time buffer's value is reset.
                this.buffer = newBuffer;
                // Set the answer to the first place in the buffer. The buffer index has now increased.
                answer = this.buffer[b++];
                //Adorn the answer, which sends it through speak transforms.
                //The answer will be used later on in the function.
                answer = SpeakWithAdornments(this.topic, answer);

            }//end if
            // CASE: Tell me more / Continue speaking
            //NOTE: Saying something like "tell me more about (different topic)" will not work
            //because we're just looking for the words tell and more...
            else if (input.Contains("more") && input.Contains("tell"))
            {
                //ZEV: Again, redundant discuss amount incrementing. Put function in FeatureGraph
                //to increment the discuss amount of a feature.
                this.topic.DiscussedAmount += 1;
                this.graph.setFeatureDiscussedAmount(this.topic.Data, this.topic.DiscussedAmount);

                // "Tell me more" implies the user wants to hear more about the current topic.
                // Since the buffer was filled with things to say about the current topic in previous
                // calls to ParseInput, we talk about the next item in the buffer.
                if (b < this.buffer.Length)
                    answer = this.buffer[b++];
                else
                {
                    answer = "I've said all I can about that topic!" + "##" + "我已经把我知道的都说完了。" + "##";
                }//end else
                
                //Update the novelty info
                noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
            }
            // CASE: New topic/question
            //If the input was neither the empty string nor "Tell me more," assume it
            //is an entirely new question that requires interaction with the chat bot.
            else
            {
                Query query = BuildQuery(input);
                if (query == null)
                {
                    return ParseInput("", messageToServer, false, true);
                    //answer = "I'm sorry, I'm afraid I don't understand what you are asking. But here's something I do know about. ";
                    //answer = answer + ParseInput("", false, false);
                    //out_of_topic = true;
                }
                else
                {
                    Console.WriteLine("Query main topic before: " + query.MainTopic.Data);
                    Feature topic_before = query.MainTopic;
                    this.buffer = ParseQuery(query);
                    if (!query.MainTopic.Equals(topic_before))
                    {
                        topic_switch = true;
                        Console.WriteLine("Query main topic changed to: " + query.MainTopic.Data);
                    }//end if

                    Feature feature = query.MainTopic;
                    feature.DiscussedAmount += 1;
                    this.graph.setFeatureDiscussedAmount(feature.Data, feature.DiscussedAmount);
                    this.topic = feature;

                    answer = this.buffer[b++];
                    //The answer, right now, is the result from ParseQuery.

                    //If there is a topic change, make sure to introduce the new topic with its speak value.
                    if (topic_switch)
                    {
                        //Get the current topic's speak value and adorn it
                        String[] temp_buffer = FindStuffToSay(this.topic);
                        String topic_speak = temp_buffer[0];
                        //If there is no topic switch, use relationships during adornment. If there is, don't use relationships.
                        topic_speak = SpeakWithAdornments(this.topic, topic_speak, !topic_switch);

                        answer = answer + " " + topic_speak;
                    }//end if

                    noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
                }
            }//end else

            //Update 
            updateHistory(this.topic);
            this.turn++;

            if (answer.Length == 0)
            {
                return IDK;
            }
            else
            {
                //answer = SpeakWithAdornments(this.topic, answer);
                if (messageToServer)
                {
                    //Return message to Unity front-end with both novel and proximal nodes
                    return MessageToServer(this.topic, answer, noveltyInfo, speaker.getProximal(this.topic, noveltyAmount), forLog, outOfTopic);
                }

                if (outOfTopic)
                    answer += ParseInput("", false, false);

                if (forLog)
                    return answer;
                else
                {
                    return answer;
                }//end else
            }//end else
        }//end if

        //PARSE INPUT UTILITY FUNCTIONS

        //Takes an input, split by the character ":", and a feature speaker
        //Returns the result of the command of any valid command is found.
        //If no valid command is found, returns the empty string.
        private string HandleInputCommand(string[] split_input, FeatureSpeaker speaker)
        {
            string return_string = "";

                //Step-through command from Query window.
                // Calls ParseInput with the empty string several times, stepping
                // through with default responses.
                if (split_input[0].Equals("STEP"))
                {
                    //Step through the program with blank inputs a certain number of times, 
                    //specified by the second argument in the command
                    //Console.WriteLine("step_count " + split_input[1]);
                    int step_count = int.Parse(split_input[1]);

                    //Create a response by calling the ParseInput function step_count times.
                    for (int s = 0; s < step_count; s++)
                    {
                        return_string += ParseInput("", true, true, false, false);
                        return_string += "\n";
                    }//end for
                }//end if
                // GET_NODE_VALUES command from Unity front-end
                // Uses FeatureSpeaker to calculate the score between two nodes, specified
                // in the input array. Returns individual components of that score.
                else if (split_input[0].Equals("GET_NODE_VALUES"))
                {
                    Console.WriteLine("In get node values");
                    //Get the node we wish to get a set of values for, by data.
                    //"data" is represented by each node's data field in the XML.
                    //In the split input string, index 1 is the data of the node we want
                    //to get values for.
                    //Index 2 is the data of the node we are getting values relative to.
                    string current_node_data = split_input[1];
                    string old_node_data = split_input[2];
                    //Get the features for these two nodes
					Feature current_feature = this.graph.getFeature(current_node_data);
					Feature old_feature = this.graph.getFeature(old_node_data);
                    //If EITHER feature is null, return an error message.
                    if (current_feature == null || old_feature == null)
                        return "no feature found";
                    double[] return_node_values = speaker.calculateScoreComponents(current_feature, old_feature);
                    //Turn them into a colon-separated string, headed by
                    //the key-phrase "RETURN_NODE_VALUES"
                    return_string = return_node_values[Constant.ScoreArrayScoreIndex] + ":"
                        + return_node_values[Constant.ScoreArrayNoveltyIndex] + ":" 
                        + return_node_values[Constant.ScoreArrayDiscussedAmountIndex] + ":"
                        + return_node_values[Constant.ScoreArrayExpectedDramaticIndex] + ":" 
                        + return_node_values[Constant.ScoreArraySpatialIndex] + ":"
                        + return_node_values[Constant.ScoreArrayHierarchyIndex] + ":";
                }//end if
                // GET_WEIGHT command from Unity front-end
                // Returns the value of each weight from the feature graph
                else if (split_input[0].Equals("GET_WEIGHT"))
                {
                    //Return a colon-separated string of every weight value
                    return_string = "Weights: ";
                    double[] weight_array = this.graph.getWeightArray();
                    for (int i = 0; i < weight_array.Length; i++)
                    {
                        if (i != 0)
                            return_string += ":";
                        return_string += weight_array[i];
                    }//end for
                }//end else if
                // SET_WEIGHT command from Unity front-end
                // Sets the value of each weight in the feature graph, specified
                // in the input array, then returns each weight.
                else if (split_input[0].Equals("SET_WEIGHT"))
                {
                    //For each pair,
                    //Index 1 is the index of the weight we wish to adjust.
                    //Index 2 is the new weight value.
                    for (int m = 1; m < split_input.Length; m += 2)
                    {
                        this.graph.setWeight(int.Parse(split_input[m]), double.Parse(split_input[m + 1]));
                    }//end for

                    //Return the new weight values right away.
                    return_string = "Weights: ";
                    double[] weight_array = this.graph.getWeightArray();
                    for (int i = 0; i < weight_array.Length; i++)
                    {
                        if (i != 0)
                            return_string += ":";
                        return_string += weight_array[i];
                    }//end for
                }//end else if
                //GET_RELATED command from Unity front-end.
                //Returns a message containing a list of most novel and most proximal nodes.
                else if (split_input[0].Equals("GET_RELATED"))
                {
                    //GET_RELATED only gets related nodes for the current topic.
                    string noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
                    return_string = "Novelty:" + noveltyInfo + ":Proximal:" + speaker.getProximal(this.topic, noveltyAmount);
                }//end else if
                //SET_LANGUAGE command from Unity front-end.
                //Sets which language text and TTS will be in, according to values
                // in the input array.
                else if (split_input[0].Equals("SET_LANGUAGE"))
                {
                    //Index 1 is the new language display mode.
                    language_mode_display = int.Parse(split_input[1]);
                    //Index 2 is the new language TTS mode.
                    language_mode_tts = int.Parse(split_input[2]);
                    return_string = "Language to display set to " + language_mode_display + ": Language of TTS set to " + language_mode_tts;
                }//end else if
                //BEGIN_TTS command from Unity front-end.
                // Returns the string to be spoken by TTS that has been buffered, if any.
                // Also appends TTS_COMPLETE to signal TTS to start on the string, then
                // resets the TTS buffer.
                else if (split_input[0].Equals("BEGIN_TTS"))
                {
                    if (buffered_tts.Equals(""))
                    {
                        return_string = "-1";
                    }//end if
                    else
                    {
                        return_string = "TTS_COMPLETE##" + buffered_tts;
                        buffered_tts = "";
                    }//end else
                }//end else if
                //GET_TTS command from Unity front-end.
                // Returns the buffered TTS string, if any, without
                // triggering TTS.
                else if (split_input[0].Equals("GET_TTS"))
                {
                    if (buffered_tts.Equals(""))
                    {
                        return_string = "-1";
                    }//end if
                    else
                    {
                        return_string = buffered_tts;
                    }//end else
                }//end else if

            return return_string;
        }//end method HandleInputCommand

        //END OF PARSE INPUT UTILITY FUNCTIONS

        /// <summary>
        /// Convert a regular string to a Query object,
        /// identifying the MainTopic and any question and direction words
        /// </summary>
        /// <param name="input">A string of input, asking about a topic</param>
        /// <returns>A Query object that can be passed to ParseQuery for output.</returns>
        public Query BuildQuery(string input)
        {
            //DEBUG
            Console.Out.WriteLine("Building query from: " + input);
            //END DEBUG

            string mainTopic;
            Question? questionType = null;
            Direction? directionType = null;
            string directionWord = "";

            // Find the main topic!
            Feature f = FindFeature(input);
            if (f == null)
            {
                //MessageBox.Show("FindFeature returned null for input: " + input);
                return null;
            }
            this.topic = f;
            mainTopic = this.topic.Data;
            if (string.IsNullOrEmpty(mainTopic))
            {
                //MessageBox.Show("mainTopic IsNullOrEmpty");
                return null;
            }

            //DEBUG
            Console.Out.WriteLine("Topic of query: " + mainTopic);
            //END DEBUG

            // Is the input a question?
            if (input.Contains("where"))
            {
                //DEBUG
                Console.Out.WriteLine("Where question");
                //END DEBUG
                questionType = Question.WHERE;
                //if (input.Contains("was_hosted_at"))
                //{
                //    directionType = Direction.WAS_HOSTED_AT;
                //}
            }
            else if (input.Contains("when"))
            {
                questionType = Question.WHEN;
            }
            else if (input.Contains("what") || input.Contains("?"))
            {
                //DEBUG
                Console.Out.WriteLine("What question");
                //END DEBUG
                questionType = Question.WHAT;

                // Check for direction words
				//if (input.Contains("direction"))
				//{
					foreach (string direction in directionWords)
					{
						// Ideally only one direction is specified
						if (input.Contains(direction))
                    	{
	                        directionType = (Direction)Enum.Parse(typeof(Direction), direction, true);
                            directionWord = direction;
	                        // Don't break. If "northwest" is asked, "north" will match first
	                        // but then get replaced by "northwest" (and so on).
	                    }//end if
					}//end foreach

                    //DEBUG
                if (directionType != null)
                    Console.Out.WriteLine("Input contained direction: " + directionType.ToString());
                    //END DEBUG

				//}//end if
            }//end else if
            else
            {
                int t = input.IndexOf("tell"), m = input.IndexOf("me"), a = input.IndexOf("about");
                if (0 <= t && t < m && m < a)
                {
                    // "Tell me about" in that order, with any words or so in between
                    // TODO:  Anything?  Should just talk about the topic, then.
                }
            }
            return new Query(this.graph.getFeature(mainTopic), questionType, directionType, directionWord);
        }

        private string PadPunctuation(string s)
        {
            foreach (string p in punctuation)
            {
                s = s.Replace(p, " " + p);
            }
            return s;
        }
        private string RemovePunctuation(string s)
        {
            foreach (string p in punctuation)
            {
                s = s.Replace(p, "");
            }
            string[] s0 = s.Split(' ');
            return string.Join(" ", s0);
        }

        private Feature FindFeature(string input)
        {
            Feature target = null;
            int targetLen = 0;
            input = input.ToLower();
            foreach (string item in this.features)
            {
                string parse_item = item;
                parse_item = parse_item.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                if (input.Contains(RemovePunctuation(parse_item.ToLower())))
                {
                    if (parse_item.Length > targetLen)
                    {
                        target = this.graph.getFeature(item);
                        targetLen = target.Data.Length;
                    }
                }
                /*
                // original
                if (input.Contains(RemovePunctuation(item.ToLower())))
                {
                    if (item.Length > targetLen)
                    {
                        target = this.graph.getFeature(item);
                        targetLen = target.Data.Length;
                    }
                }
                */
            }
            //If the target is still null, check for 'that' or 'this'
            if (input.Contains("this") || input.Contains("that") || input.Contains("it") || input.Contains("something"))
                target = this.topic;

            return target;
        }

        /// <summary>
        /// Takes a Query object and builds a list of output strings
        /// to talk about the query's MainTopic with its specified question
        /// words and direction words, if any, into consideration.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>List of output strings.</returns>
        public string[] ParseQuery(Query query)
        {
            if (query == null)
                return new string[] { "I don't know." };

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
                                string[] neighbors = FindNeighborsByRelationship(query.MainTopic, dir);
                                // If the topic has no "won" links, then it is the event
                                if (neighbors.Length == 0)
                                {
                                    // So find the winner among its available neighbors
                                    neighbors = FindNeighborsByRelationship(query.MainTopic, "");
                                    foreach (string neighbor in neighbors)
                                    {
                                        // Look at ITS neighbors and see if there is a "won" whose name matches this one
                                        Feature nf = this.graph.getFeature(neighbor);
                                        foreach (var triple in nf.Neighbors)
                                        {
                                            if (triple.Item1.Data == query.MainTopic.Data && triple.Item3 == "won")
                                                output.Add(string.Format("{0} won {1}.", neighbor, query.MainTopic.Data));
                                        }
                                    }
                                }
                                // Otherwise it is the winner
                                else
                                {
                                    output.Add(string.Format("{0} won {1}.", query.MainTopic.Data, neighbors.ToList().JoinAnd()));
                                }
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
                                    if (temp_feature.getRelationshipNeighbor(query_topic.Data).ToLower().Contains(query.DirectionWord.ToLower()))
                                    {
                                        output.Add(string.Format("{0} " + temp_feature.getRelationshipNeighbor(query_topic.Data) + " {1}.", temp_feature.Data, query_topic.Data));
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
                                //    output.Add(string.Format("{0} hosted {1}.", query.MainTopic.Data, neighbors.ToList().JoinAnd()));
                                
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
                                //    output.Add(string.Format("{0} hosted {1}.", query.MainTopic.Data, neighbors.ToList().JoinAnd()));

                                foreach (Tuple<Feature, double, string> temp_neighbor in query_topic.Neighbors)
                                {
                                    foreach (string inside_word in inside_words)
                                    {
                                        /*if (temp_feature.getRelationshipNeighbor(query_topic.Data).ToLower().Contains(query.DirectionWord.ToLower()))
                                        {
                                            output.Add(string.Format("{0} " + temp_feature.getRelationshipNeighbor(query_topic.Data) + " {1}.", temp_feature.Data, query_topic.Data));
                                            break;
                                        }//end if*/
                                        //Checking from Neighbor to Topic
                                        if (temp_neighbor.Item1.getRelationshipNeighbor(query_topic.Data).ToLower().Contains(query.DirectionWord.ToLower()))
                                        {
                                            //DEBUG
                                            Console.Out.WriteLine("Inside word " + inside_word + " found.");
                                            //END DEBUG
                                            if (for_output.Equals(""))
                                                for_output = string.Format("{0} " + temp_neighbor.Item3 + " {1}", temp_neighbor.Item1.Data, query_topic.Data);
                                            else
                                                for_output += string.Format(", {0} " + temp_neighbor.Item3 + " {1}", temp_neighbor.Item1.Data, query_topic.Data);
                                            //for_output.Add(string.Format("{0} " + temp_neighbor.Item3 + " {1}.", query_topic.Data, temp_neighbor.Item1.Data));
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
                                string[] neighbors = FindNeighborsByRelationship(query.MainTopic, dir);
                                if (neighbors.Length > 0)
                                    output.Add(string.Format("{0} of {1} {2} {3}", dir.ToUpperFirst(), query.MainTopic.Data,
                                        (neighbors.Length > 1) ? "are" : "is", neighbors.ToList().JoinAnd()));
                            }
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
                        }
                        break;
                    case Question.WHERE:
                        if (false)
                        {

                        }
                        else
                        {
                            // e.g. Where is Topic?
                            // Get all the neighbors from this feature and the "opposite" directions
                            //output.AddRange((SpeakNeighborRelations(query.MainTopic.Data, FindAllNeighbors(query.MainTopic))));
                            
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
            }
            else
            {
                // e.g.:
                // Tell me about Topic.
                // Topic.
                output.Add(SpeakWithAdornments(query.MainTopic, FindStuffToSay(query.MainTopic)[0]));
            }

            return output.Count() > 0 ? output.ToArray() : new string[] { IDK };
        }
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
                                output_string = string.Format("{0} " + temp_neighbor.Item3 + " {1}", query_topic.Data, temp_neighbor.Item1.Data);
                            }//end if
                            else
                                output_string += string.Format(", " + temp_neighbor.Item3 + " {0}", temp_neighbor.Item1.Data);
                            //for_output.Add(string.Format("{0} " + temp_neighbor.Item3 + " {1}.", query_topic.Data, temp_neighbor.Item1.Data));
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

        //Parses a bilingual output based on the language_mode passed in
        public string ParseOutput(string to_parse, int language_mode)
        {
            string answer = "";
            string[] answers = to_parse.Split(new string[] { "##" }, StringSplitOptions.None);

            for (int i = 0; i < answers.Length; i++)
            {
                if (language_mode == Constant.EnglishMode && i % 2 == 0)
                {
                    answer += answers[i];
                }
                if (language_mode == Constant.ChineseMode && i % 2 == 1)
                {
                    answer += answers[i];
                }
            }
            return answer;
        }

        private string[] FindSpeak(Feature feature)
        {
            return feature.Speaks.ToArray();
        }

        //From the given feature, find a collection of strings to say about it.
        //ZEV: Should this be in FeatureSpeaker???
        private string[] FindStuffToSay(Feature feature)
        {
            List<string> stuff = new List<string>();
            string[] speaks = FindSpeak(feature);
            //Add each of the feature's speak values to the list of things to say about it
            if (speaks.Length > 0)
            {
                stuff.AddRange(speaks);
            }// end if
                
            //ZEV: Do we need to call this? If there are no speaks, we speak this. (But when
            //aren't there speaks?)
            stuff.AddRange(SpeakNeighborRelations(feature.Data, FindAllNeighbors(feature)));

            //If nothing else can be found, simply speak the feature's id
            if (stuff.Count() == 0)
            {
                stuff.Add(feature.Data);
            }//end if
            return stuff.ToArray();
        }// end function FindStuffToSay

        private string[] FindNeighborsByRelationship(Feature feature, string relationship)
        {
            List<string> neighborNames = new List<string>();
            var neighbors = feature.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                var triple = neighbors[i];
                Feature neighbor = triple.Item1;
                string relation = triple.Item3;
                if (relation.ToLower().Replace(' ', '_') == relationship.ToLower())
                    neighborNames.Add(neighbor.Data);
            }
            return neighborNames.ToArray();
        }

        private Tuple<string, Direction>[] FindAllNeighbors(Feature feature)
        {
            var _neighbors = feature.Neighbors;
            var neighbors = new List<Tuple<string, Direction>>();
            foreach (var triple in _neighbors)
            {
                string neighborName = triple.Item1.Data;
                string relationship = triple.Item3;
                if (directionWords.Contains(relationship))
                    neighbors.Add(new Tuple<string, Direction>(neighborName,
                        ((Direction)Enum.Parse(typeof(Direction), relationship.ToUpper().Replace(' ', '_')))));
            }
            return neighbors.ToArray();
        }

        //ZEV: Should be in feature speaker? Should this exist at all??? Handled in SpeakTransform...
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

    }

    static class ExtensionMethods
    {
        public static Direction Invert(this Direction d)
        {
            return (Direction)(-(int)d);
        }

        public static string ToUpperFirst(this string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        public static string JoinAnd(this List<string> items)
        {
            switch (items.Count())
            {
                case 0:
                    return "";
                case 1:
                    return items.ElementAt(0);
                case 2:
                    return items.ElementAt(0) + " and " + items.ElementAt(1);
                default:
                    return string.Join(", ", items.GetRange(0, items.Count - 1))
                        + ", and " + items[items.Count - 1];
            }
        }
    }
}
