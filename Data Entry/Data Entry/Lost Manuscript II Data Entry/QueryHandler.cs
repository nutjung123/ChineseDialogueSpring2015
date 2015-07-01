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
        public Feature MainTopic { get; private set; }
        // Whether or not the input was an explicit question
        public bool IsQuestion { get { return QuestionType != null; } }
        // The type of Question
        public Question? QuestionType { get; private set; }
        // The direction/relationship asked about.
        public Direction? Direction { get; private set; }
        public bool HasDirection { get { return Direction != null; } }

        public Query(Feature mainTopic, Question? questionType, Direction? directions)
        {
            MainTopic = mainTopic;
            QuestionType = questionType;
            Direction = directions;
        }
        public override string ToString()
        {
            string s = "Topic: " + MainTopic.Data;
            s += "\nQuestion type: " + QuestionType ?? "none";
            s += "\nDirection specified: " + Direction ?? "none";
            return s;
        }
    }

    /// <summary>
    /// A utility class to parse natural input into a Query and a Query into natural output.
    /// </summary>
    class QueryHandler
    {
        private const string FORMAT = "FORMAT:";
        private const string IDK = "I'm afraid I don't know anything about that topic.";
        private string[] punctuation = { ",", ";", ".", "?", "!", "\'", "\"", "(", ")", "-" };
        private string[] questionWords = { "?", "what", "where", "when" };
        private string[] directionWords = {"inside", "contain", "north", "east", "west", "south",
                                      "northeast", "northwest", "southeast", "southwest",
                                      "hosted", "was_hosted_at", "won"};
        private string[] Directional_Words = {"north", "east", "west", "south",
                                      "northeast", "northwest", "southeast", "southwest"};
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
		public LinkedList<Feature> MetList = new LinkedList<Feature>();
		public int countFocusNode = 0;
		public double noveltyValue = 0.0;

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
        }

		private string LeadingTopic(Feature last, Feature first)
		{
			string return_message = " ";
			// Check if there is a relationship between two nodes
			if (last.getNeighbor(first.Data) != null || first.getNeighbor(last.Data) == null)
			{
				// Check if last has first as its neighbor
				if (last.getRelationshipNeighbor (first.Data) != null) {
					return_message = last.Data + last.getRelationshipNeighbor(first.Data) + first.Data;
				}
				// If last is a child node of first (first is a parent of last)
				else if (last.getRelationshipParent (first.Data) != null)
				{
					return_message = last.Data + last.getRelationshipParent(first.Data) + first.Data;
				}
			}
				
			// Neither neighbor or parent/child
			// NEED TO consider novelty value (low)
			else if (last.getNeighbor(first.Data) == null || first.getNeighbor(last.Data) == null
						&& noveltyValue >= 0.6)
			{
				return_message = "Now let's talk about " + first.Data + ". ";
			}

			return return_message;
		}

		private string RelationshipAnalogy(Feature old, Feature newOld, Feature prevOfCurr,Feature current)
		{
			string return_message = "";
			Console.WriteLine("old: " + old.Data);
			Console.WriteLine("new: " + newOld.Data);
			Console.WriteLine("previous of current: " + prevOfCurr.Data);
			Console.WriteLine("current: " + current.Data);


			// Senten Patterns list - for 3 nodes
			// NEED TO implement 4 nodes relationship
			List<string> sentencePatterns = new List<string>();

			Random rnd = new Random();

			if (old.getRelationshipNeighbor(newOld.Data) == prevOfCurr.getRelationshipNeighbor(current.Data) &&
				old.getRelationshipNeighbor(newOld.Data) != "" && prevOfCurr.getRelationshipNeighbor(current.Data) != "")
			{
				string relationship = old.getRelationshipNeighbor(newOld.Data);
				// 4 nodes
				sentencePatterns.Add(" Just as [" + old.Data + ", " + relationship + ", " + newOld.Data
					+ "], so too [" + prevOfCurr.Data + ", " + relationship + ", " + current.Data + "]. ");
				sentencePatterns.Add("[" + prevOfCurr.Data + ", " + relationship + ", " + current.Data
					+ "] much like [" + old.Data + "] and [" + newOld.Data + "]. ");
				sentencePatterns.Add("Like [" + old.Data + ", " + relationship + ", " + newOld.Data + "]"
					+ "[" + prevOfCurr.Data + "] also " + "[" + relationship + current.Data + "]. ");
				sentencePatterns.Add("In the way that [" + old.Data + ", " + relationship + ", " + newOld.Data
					+ "], " + "[" + prevOfCurr.Data + ", " + relationship + ", " + current.Data + "]. ");
				sentencePatterns.Add("Remember how " + "[" + old.Data + ", " + relationship + ", " + newOld.Data
					+ "]?" + "Well, in the same way, " + "[" + prevOfCurr.Data + "] also " + "[" + relationship + current.Data + "]. ");
				sentencePatterns.Add("[" + prevOfCurr.Data + "] also " + "[" + relationship + current.Data + "]" +
					"similar to how [" + old.Data + ", " + relationship + ", " + newOld.Data + "]. ");

				int r = rnd.Next(sentencePatterns.Count);

				return_message += sentencePatterns[r];
			}
			return return_message;
		}
			
	    private string MessageToServer(Feature feat, string speak, string noveltyInfo, string proximalInfo = "", bool forLog = false)
        {
            String return_message = "";

            prevCurr.AddFirst(feat);
	    	MetList.AddLast(feat);
	    	countFocusNode += 1;

            if (prevCurr.Count > 2)
            {
		        prevCurr.RemoveLast();
	        }
			if (MetList.Count > 20)
			{
				MetList.RemoveFirst();
			}

			// Previous-Current nodes
            Feature first = prevCurr.First();   // Current node
            Feature last = prevCurr.Last();     // Previous node

			// Metaphor - 3 nodes
			Feature old = MetList.First();
            Feature newOld = null;
			//int countNode = 1;
			if (MetList.Count () >= 2)
			{
				newOld = MetList.ElementAt(1);
			}
			Feature current = MetList.Last();
			// 4th node
			// NEED TO check all possibilities (17 pairs - linear time)

            Feature prevOfCurr = null;
            if (MetList.Count() >= 2)
                prevOfCurr = MetList.ElementAt(MetList.Count - 2);

            if (MetList.Count() >= 4)
            {
				// Analogy
				if (newOld != null )
				{

				}

				//while (old.getRelationshipNeighbor(newOld.Data) != prevOfCurr.getRelationshipNeighbor(current.Data))
                for (int countNode = 0; countNode < MetList.Count - 1; countNode++ )
                {
                    old = MetList.ElementAt(countNode);
                    newOld = MetList.ElementAt(countNode + 1);
                    //countNode += 1;
                    if (old.Data.Equals(prevOfCurr.Data) && newOld.Data.Equals(current.Data))
                    {
                        continue;
                        //countNode = 1;
                        //break;
                    }
                    if (old.getRelationshipNeighbor(newOld.Data) == prevOfCurr.getRelationshipNeighbor(current.Data))
                    {
                        //countNode = 1;
                        
                        // Count relationship in the list (<=20 nodes)
			int count_relationship = 0;
			int cc = 0;
			while (cc <= MetList.Count())
			{
				if (old.getRelationshipNeighbor (newOld.Data) == prevOfCurr.getRelationshipNeighbor (current.Data))
				{
					count_relationship += 1;
				}
				cc += 1;

			}
			// Only display rare
			if (count_relationship <= 5)
			{
				return_message += RelationshipAnalogy (old, newOld, prevOfCurr, current);
			}
			break;
                    }//end if
                }
            }

			// Leading-topic sentence
			if (prevCurr.Count > 1 && countFocusNode == 5)
			{
				return_message = LeadingTopic (last, first);
                countFocusNode = 0; // Set back to 0
			}


            String to_speak = return_message + speak;

            if (forLog)
                return_message += " ID:" + this.graph.getFeatureIndex(feat.Data) + ":Speak:" + to_speak + "\r\n";
            else
                return_message += " ID:" + this.graph.getFeatureIndex(feat.Data) + ":Speak:" + to_speak + ":Novelty:" + noveltyInfo + ":Proximal:" + proximalInfo;

            return return_message;
        }

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
        }

        //Form2 calls this function
        //input is the input to be parsed.
        //messageToServer indicates whether or not we are preparing a response to the front-end.
        //forLog indicates whether or not we are preparing a response for a log output.
        public string ParseInput(string input, bool messageToServer = false, bool forLog = false)
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
                // Check to see if the AIML Bot has anything to say
                Request request = new Request(input, this.user, this.bot);
               
                Result result = bot.Chat(request);
                string output = result.Output;
                
                if (output.Length > 0)
                {
                    if (!output.StartsWith(FORMAT))
                        return output;
                    
                    //MessageBox.Show("Converted output reads: " + output);
                    input = output.Replace(FORMAT, "").ToLower();
                }
            }

            // Remove punctuation
            input = RemovePunctuation(input);
            
            // Check
            if (this.topic == null)
                this.topic = this.graph.Root;
            FeatureSpeaker speaker = new FeatureSpeaker(this.graph, temporalConstraintList, prevSpatial, topicHistory);

            if (split_input.Length != 0 || messageToServer)
            {
                //Step-through command from Query window.
                if (split_input[0].Equals("STEP"))
                {
                    //Step through the program with blank inputs a certain number of times, 
                    //specified by the second argument in the command
                    //Console.WriteLine("step_count " + split_input[1]);
                    int step_count = int.Parse(split_input[1]);
                    //Create an answer by calling the ParseInput function step_count times.
                    answer = "";
                    for (int s = 0; s < step_count; s++)
                    {
                        //Get forServer and forLog responses.
                        answer += ParseInput("", true, true);
                        answer += "\n";
                    }
                    //Console.WriteLine("answer " + answer);
                    //Just return this answer by itself
                    return answer;
                }//end if

                // GET_NODE_VALUES command from Unity front-end
                if (split_input[0].Equals("GET_NODE_VALUES"))
                {
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
                    string return_string = return_node_values[Constant.ScoreArrayScoreIndex] + ":"
                        + return_node_values[Constant.ScoreArrayNoveltyIndex] + ":" 
                        + return_node_values[Constant.ScoreArrayDiscussedAmountIndex] + ":"
                        + return_node_values[Constant.ScoreArrayExpectedDramaticIndex] + ":" 
                        + return_node_values[Constant.ScoreArraySpatialIndex] + ":"
                        + return_node_values[Constant.ScoreArrayHierarchyIndex] + ":";
                    
                    return return_string;
                }//end if
                // GET_WEIGHT command from Unity front-end
                else if (split_input[0].Equals("GET_WEIGHT"))
                {
                    //Return a colon-separated string of every weight value
                    string return_string = "Weights: ";
                    double[] weight_array = this.graph.getWeightArray();
                    for (int i = 0; i < weight_array.Length; i++)
                    {
                        if (i != 0)
                            return_string += ":";
                        return_string += weight_array[i];
                    }//end for
                    return return_string;
                }//end else if
                // SET_WEIGHT command from Unity front-end
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
                    string return_string = "Weights: ";
                    double[] weight_array = this.graph.getWeightArray();
                    for (int i = 0; i < weight_array.Length; i++)
                    {
                        if (i != 0)
                            return_string += ":";
                        return_string += weight_array[i];
                    }//end for
                    return return_string;
                }//end else if
                //GET_RELATED command from Unity front-end.
                //Returns a message containing a list of most novel and most proximal nodes
                else if (split_input[0].Equals("GET_RELATED"))
                {
                    //GET_RELATED only gets related nodes for the current topic.
                    noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
                    return "Novelty:" + noveltyInfo + ":Proximal:" + speaker.getProximal(this.topic, noveltyAmount);
                }//end else if
            }//end else if

            // CASE: Nothing / Move on to next topic
            if (string.IsNullOrEmpty(input))
            {
                Feature nextTopic = this.topic;
                string[] newBuffer;
                
                // == testing forward projection
                if (true)
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    
                    int forwardTurn = 20;
                    List<Feature> testingForwardP = speaker.forwardProjection(nextTopic, forwardTurn);
                    
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    // Format and display the TimeSpan value. 
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    Console.WriteLine("RunTime of forward projection" + elapsedTime);
                    //print out all the topics
                    for (int i = 0; i < forwardTurn; i++)
                    {
                        Console.WriteLine(testingForwardP[i].Data);
                    }
                }
                
                // Can't guarantee it'll actually move on to anything...
                nextTopic = speaker.getNextTopic(nextTopic, "", this.turn);
                noveltyInfo = speaker.getNovelty(nextTopic, this.turn, noveltyAmount);
                currentTopicNovelty = speaker.getCurrentTopicNovelty();
				noveltyValue = speaker.getCurrentTopicNovelty();
                newBuffer = FindStuffToSay(nextTopic);
                //MessageBox.Show("Explored " + nextTopic.Data + " with " + newBuffer.Length + " speaks.");

                nextTopic.DiscussedAmount += 1;
                this.graph.setFeatureDiscussedAmount(nextTopic.Data, nextTopic.DiscussedAmount);
                this.topic = nextTopic;
                // talk about
                this.buffer = newBuffer;
                answer = this.buffer[b++];
            }
            // CASE: Tell me more / Continue speaking
            else if (input.Contains("more") && input.Contains("tell"))
            {
                this.topic.DiscussedAmount += 1;
                this.graph.setFeatureDiscussedAmount(this.topic.Data, this.topic.DiscussedAmount);
                // talk about
                if (b < this.buffer.Length)
                    answer = this.buffer[b++];
                else
                    answer = "I've said all I can about that topic!";
                noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
            }
            // CASE: New topic/question
            else
            {
                Query query = BuildQuery(input);
                if (query == null)
                {
                    answer = "I'm sorry, but I don't understand what you are asking.";
                }
                else
                {
                    Feature feature = query.MainTopic;
                    feature.DiscussedAmount += 1;
                    this.graph.setFeatureDiscussedAmount(feature.Data, feature.DiscussedAmount);
                    this.topic = feature;
                    this.buffer = ParseQuery(query);
                    answer = this.buffer[b++];
                    noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
                }
            }

            //Update 
            updateHistory(this.topic);
            this.turn++;

            if (answer.Length == 0)
            {
                return IDK;
            }
            else
            {
                if (messageToServer)
                {
                    //Return message to Unity front-end with both novel and proximal nodes
                    return MessageToServer(this.topic, answer, noveltyInfo, speaker.getProximal(this.topic, noveltyAmount), forLog);
                }

                if (forLog)
                    return answer;
                else
                    return answer + " <Novelty Info: " + noveltyInfo + " > <Proximal Info: " + speaker.getProximal(this.topic, noveltyAmount) + ">";
            }
        }

        /// <summary>
        /// Convert a regular string to a Query object,
        /// identifying the MainTopic and any question and direction words
        /// </summary>
        /// <param name="input">A string of input, asking about a topic</param>
        /// <returns>A Query object that can be passed to ParseQuery for output.</returns>
        public Query BuildQuery(string input)
        {
            string mainTopic;
            Question? questionType = null;
            Direction? directionType = null;

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

            // Is the input a question?
            if (input.Contains("where"))
            {
                questionType = Question.WHERE;
                if (input.Contains("was_hosted_at"))
                {
                    directionType = Direction.WAS_HOSTED_AT;
                }
            }
            else if (input.Contains("when"))
            {
                questionType = Question.WHEN;
            }
            else if (input.Contains("what") || input.Contains("?"))
            {
                questionType = Question.WHAT;
                // Check for direction words
				if (input.Contains("direction"))
				{
					foreach (string direction in directionWords)
					{
						// Ideally only one direction is specified
						if (input.Contains(direction))
                    	{
	                        directionType = (Direction)Enum.Parse(typeof(Direction), direction, true);
	                        // Don't break. If "northwest" is asked, "north" will match first
	                        // but then get replaced by "northwest" (and so on).
	                    }
					}
				}
            }
            else
            {
                int t = input.IndexOf("tell"), m = input.IndexOf("me"), a = input.IndexOf("about");
                if (0 <= t && t < m && m < a)
                {
                    // "Tell me about" in that order, with any words or so in between
                    // TODO:  Anything?  Should just talk about the topic, then.
                }
            }
            return new Query(this.graph.getFeature(mainTopic), questionType, directionType);
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
                if (input.Contains(RemovePunctuation(item.ToLower())))
                {
                    if (item.Length > targetLen)
                    {
                        target = this.graph.getFeature(item);
                        targetLen = target.Data.Length;
                    }
                }
            }
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
                            // Find names of features that is DIRECTION of MainTopic
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
                            }
                            else if (query.Direction == Direction.HOSTED)
                            {
                                string[] neighbors = FindNeighborsByRelationship(query.MainTopic, dir);
                                if (neighbors.Length > 0)
                                    output.Add(string.Format("{0} hosted {1}.", query.MainTopic.Data, neighbors.ToList().JoinAnd()));
                            }
                            else
                            {
                                string[] neighbors = FindNeighborsByRelationship(query.MainTopic, dir);
                                if (neighbors.Length > 0)
                                    output.Add(string.Format("{0} of {1} {2} {3}", dir.ToUpperFirst(), query.MainTopic.Data,
                                        (neighbors.Length > 1) ? "are" : "is", neighbors.ToList().JoinAnd()));
                            }
                        }
                        else
                        {
                            // e.g. What is Topic?
                            // Get the <speak> attribute, if able
                            string[] speak = FindStuffToSay(query.MainTopic);
                            if (speak.Length > 0)
                                output.AddRange(speak);
                        }
                        break;
                    case Question.WHERE:
                        // e.g. "Where was Topic hosted at?"
                        if (query.HasDirection && query.Direction == Direction.WAS_HOSTED_AT)
                        {
                            string[] hostedAt = FindNeighborsByRelationship(query.MainTopic, query.Direction.ToString());
                            // Should only have one host, but treat it as an array
                            foreach (string host in hostedAt)
                                output.Add(query.MainTopic + " was hosted at " + host + ".");
                        }
                        else
                        {
                            // e.g. Where is Topic?
                            // Get all the neighbors from this feature and the "opposite" directions
                            output.AddRange((SpeakNeighborRelations(query.MainTopic.Data, FindAllNeighbors(query.MainTopic))));
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
                output.AddRange(FindStuffToSay(query.MainTopic));
            }

            return output.Count() > 0 ? output.ToArray() : new string[] { IDK };
        }

        private string[] FindSpeak(Feature feature)
        {
            return feature.Speaks.ToArray();
        }

        private string[] FindStuffToSay(Feature feature)
        {
            List<string> stuff = new List<string>();
            string[] speaks = FindSpeak(feature);
            if (speaks.Length > 0)
                stuff.AddRange(speaks);
            stuff.AddRange(SpeakNeighborRelations(feature.Data, FindAllNeighbors(feature)));
            if (stuff.Count() == 0)
            {
                stuff.Add(feature.Data);
            }
            return stuff.ToArray();
        }

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

        private string[] SpeakNeighborRelations(string featureName, Tuple<string, Direction>[] neighbors)
        {
            string[] neighborRelations = new string[neighbors.Length];
            if (neighborRelations.Length == 0)
                return new string[] { };
            for (int i = 0; i < neighborRelations.Length; i++)
                neighborRelations[i] = string.Format("{0} is {1} of {2}.",
                    (i == 0) ? featureName : "It",
                    neighbors[i].Item2.Invert().ToString().ToLower(),
                    neighbors[i].Item1);
            return neighborRelations;
        }
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
