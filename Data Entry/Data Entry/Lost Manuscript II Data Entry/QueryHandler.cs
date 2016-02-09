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
            string s = "Topic: " + MainTopic.Id;
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

        private List<string> features;

        private int noveltyAmount = 5;
        private List<TemporalConstraint> temporalConstraintList;
        //private List<int> topicHistory = new List<int>();
        private string prevSpatial;

        private NarrationManager narration_manager;

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
            //this.bot = new Bot();
            this.temporalConstraintList = myTemporalConstraintList;
            /*bot.loadSettings();
            bot.isAcceptingUserInput = false;
            bot.loadAIMLFromFiles();
            bot.isAcceptingUserInput = true;
            this.user = new User("user", this.bot);*/

            // Load the Feature Graph
            this.graph = graph;

            // Feature Names, with which to index the graph
            this.features = graph.getFeatureNames();

            //Initialize the dialogue manager
            narration_manager = new NarrationManager(this.graph, myTemporalConstraintList);

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
        }//end constructor QueryHandler
			
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
                return_message = " ID:" + feat.Id + ":Speak:" + to_speak + ":Novelty:" + noveltyInfo + ":Proximal:" + proximalInfo;
                //return_message += "##" + tts;
            }//end else
                

            //Console.WriteLine("to_speak: " + to_speak);

            return return_message;
        }//end function MessageToServer

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
            // Pre-processing

            //Console.WriteLine("parse input " + input);

            //The input may be delimited by colons. Try to split it.
            String[] split_input = input.Trim().Split(':');
            //Console.WriteLine("split input " + split_input[0]);

            // Lowercase for comparisons
            input = input.Trim().ToLower();
            //Console.WriteLine("trimmed lowered input " + input);

            //Check for an explicit command in the input.
            if (split_input.Length != 0 || messageToServer)
            {
                string command_result = CommandResponse(split_input);
                //Only stop and return the result of a command here if there
                //is a command result to return.
                if (!command_result.Equals(""))
                    return command_result;
            }//end else if

            // Check to see if the AIML Bot has anything to say.
            if (!string.IsNullOrEmpty(input))
            {
                //Call the AIML Chat Bot in NarrationManager and give it the input ParseInput was given.
                string output = narration_manager.TellChatBot(input);
                
                //If the chatbot has a feedback response, it will begin its
                //response with "FORMAT"
                if (output.Length > 0)
                {
                    //If the word "FORMAT" is not found, the response from the chatbot
                    //is not a feedback response. Return it.
                    if (!output.StartsWith(FORMAT))
                        return output;
                    
                    //MessageBox.Show("Converted output reads: " + output);
                    //Otherwise, remove the word FORMAT and continue with
                    //the chatbot's output as the new input.
                    input = output.Replace(FORMAT, "").ToLower();
                }//end if
            }//end if

            // Remove punctuation
            input = RemovePunctuation(input);

            // CASE: Nothing / Move on to next topic
            if (string.IsNullOrEmpty(input))
            {
                answer = narration_manager.DefaultNextTopicResponse();
            }//end if
            // CASE: Tell me more / Continue speaking
            else if (input.Contains("more") && input.Contains("tell"))
            {
                answer = narration_manager.TalkMoreAboutTopic();
            }//end else if
            // CASE: New topic/question
            //If the input was neither the empty string nor "Tell me more," assume it
            //is an entirely new topic/question that requires a Query
            else
            {
                //Construct a query using the input.
                Query query = BuildQuery(input);
                //Find what to say with it.
                answer = narration_manager.TalkFromQuery(query);
            }//end else

            //Gets the first noveltyAmount number of nodes with the highest novelty.
            noveltyInfo = narration_manager.ListMostNovelFeatures(narration_manager.Topic, noveltyAmount);
            //Gets the first noveltyAmount number of nodes with the highest score.
            string proximal_info = narration_manager.ListMostProximalFeatures(narration_manager.Topic, noveltyAmount);
            //Increment conversation turn
            narration_manager.Turn += 1;

            //At this point, we have either automatically moved on (blank input),
            //talked more about the current topic ("tell me more"),
            //or built and parsed a query out of the input.
            //answer holds the result of one of these three.

            //If there is no answer, then return the I Don't Know response
            if (answer.Length == 0)
            {
                return IDK;
            }//end if
            else
            {
                //If this was a message from the front-end to the back-end, send the
                //answer through additional formatting before returning it to the front-end.
                if (messageToServer)
                {
                    //Return message to Unity front-end with both novel and proximal nodes
                    return MessageToServer(narration_manager.Topic, answer, noveltyInfo, proximal_info, forLog, outOfTopic);
                }//end if

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

        /// <summary>
        /// ParseInput utility function. Looks for an explicit command word in the given input and tries to carry
        /// out the command. Returns the result of the command if any valid command is found.
        /// </summary>
        /// <param name="split_input">A string of input split into an array by the character ":"</param>
        private string CommandResponse(string[] split_input)
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
                // Uses NarrationCalculator to calculate the score between two nodes, specified
                // by the input. Returns individual components of that score.
                else if (split_input[0].Equals("GET_NODE_VALUES"))
                {
                    Console.WriteLine("In get node values");
                    //Get the node we wish to get a set of values for, by id.
                    //"id" is represented by each node's data field in the XML.
                    //In the split input string, index 1 is the id of the node we want
                    //to get values for.
                    //Index 2 is the id of the node we are getting values relative to.
                    string current_node_id = split_input[1];
                    string old_node_id = split_input[2];
                    //Get the features for these two nodes
                    Feature current_feature = this.graph.getFeature(current_node_id);
                    Feature old_feature = this.graph.getFeature(old_node_id);
                    //If EITHER feature is null, return an error message.
                    if (current_feature == null || old_feature == null)
                        return "no feature found";
                    double[] return_node_values = narration_manager.GetScoreComponents(current_feature, old_feature);
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
                    string noveltyInfo = narration_manager.ListMostNovelFeatures(narration_manager.Topic, noveltyAmount);
                    string proximalInfo = narration_manager.ListMostProximalFeatures(narration_manager.Topic, noveltyAmount);
                    return_string = "Novelty:" + noveltyInfo + ":Proximal:" + proximalInfo;
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
                //FORWARD_PROJECTION command.
                // Returns the names of the sequence of topics
                // found by Forward Projection.
                else if (split_input[0].Equals("FORWARD_PROJECTION"))
                {
                    //The second index of the command is the number of turns to
                    //perform forward projection with.
                    List<Feature> result_list = narration_manager.ForwardProjection(narration_manager.Topic, int.Parse(split_input[1]));
                    return_string = "Forward Projection result:";
                    foreach (Feature temp_feature in result_list)
                    {
                        return_string = return_string + " --> " + temp_feature.Name;
                    }//end foreach
                }//end else if
                //BACKGROUND_TOPIC command.
                // Sets the background topic in the narration manager to the given feature, by either name or ID.
                else if (split_input[0].Equals("BACKGROUND_TOPIC"))
                {
                    Feature new_background_topic = null;

                    String string_topic = split_input[1];
                    //Try to convert the topic to an int to check if it's an id.
                    int int_topic = -1;
                    bool parse_success = int.TryParse(string_topic, out int_topic);
                    if (parse_success)
                    {
                        //Check that the new integer topic is a valid id.
                        new_background_topic = graph.getFeature(int_topic);
                    }//end if
                    else
                    {
                        new_background_topic = FindFeature(string_topic);
                    }//end else

                    if (new_background_topic == null)
                    {
                        Console.WriteLine("No topic found");
                    }//end if
                    else
                    {
                        narration_manager.SetBackgroundTopic(new_background_topic);
                    }//end else
                }//end else if

            return return_string;
        }//end function CommandResponse

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
            //narration_manager.Topic = f;
            mainTopic = f.Name;
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
                }//end if
            }//end else
            return new Query(f, questionType, directionType, directionWord);
        }//end function BuildQuery

        private string PadPunctuation(string s)
        {
            foreach (string p in punctuation)
            {
                s = s.Replace(p, " " + p);
            }//end foreach
            return s;
        }//end function PadPunctuation
        private string RemovePunctuation(string s)
        {
            foreach (string p in punctuation)
            {
                s = s.Replace(p, "");
            }
            string[] s0 = s.Split(' ');
            return string.Join(" ", s0);
        }//end function RemovePunctuation

        //Identifies the feature in the given input
        /// <summary>
        /// Takes a string and identifies which
        /// feature, if any, appears in it. Returns the feature.
        /// </summary>
        /// <param name="input">A string for the function to look for a feature in.</param>
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
                        targetLen = target.Name.Length;
                    }
                }
                /*
                // original
                if (input.Contains(RemovePunctuation(item.ToLower())))
                {
                    if (item.Length > targetLen)
                    {
                        target = this.graph.getFeature(item);
                        targetLen = target.Id.Length;
                    }
                }
                */
            }
            //If the target is still null, check for 'that' or 'this'
            if (input.Contains("this") || input.Contains("that") || input.Contains("it") || input.Contains("something"))
                target = narration_manager.Topic;

            return target;
        }//end function FindFeature

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
        }//end function FindSpeak

    }//end class QueryHandler

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
