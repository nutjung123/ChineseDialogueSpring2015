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
		public LinkedList<Feature> MetList = new LinkedList<Feature>();
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

        //optional parameter for_additional_info, if set true, will avoid any actual leading statements
        //except for relationship mentions. If no relationship mention can be made, then blank string
        //is returned.
		private string LeadingTopic(Feature last, Feature first, bool for_additional_info = false)
		{
			string return_message = "";
            
            string first_data_en = first.Data;
            string first_data_cn = first.Data;
            if (first.Data.Contains("##"))
            {
                first_data_en = first.Data.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                first_data_cn = first.Data.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            Console.WriteLine("In LeadingTopic, first_data_en " + first_data_en + " first_data_cn " + first_data_cn);

            string last_data_en = last.Data;
            string last_data_cn = last.Data;
            if (last.Data.Contains("##"))
            {
                last_data_en = last.Data.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                last_data_cn = last.Data.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            Console.WriteLine("In LeadingTopic, last_data_en " + last_data_en + " last_data_cn " + last_data_cn);

            //First is the current node (the one that has just been traversed to)
            //A set of possible lead-in statements.
            List<string> lead_in_statements = new List<string>();
            lead_in_statements.Add("{There's also " + first_data_en + ".} " + "##" + "{还有" + first_data_cn + "呢。} " + "##");
            lead_in_statements.Add("{But let's talk about " + first_data_en + ".} " + "##" + "{我们来聊聊" + first_data_cn + "吧。} " + "##");
            lead_in_statements.Add("{And have I mentioned " + first_data_en + "?} " + "##" + "{之前我说过" + first_data_cn + "吗？} " + "##");
            lead_in_statements.Add("{Now, about " + first_data_en + ".} " + "##" + "{接下来是" + first_data_cn + "。} " + "##");
            lead_in_statements.Add("{Now, let's talk about " + first_data_en + ".} " + "##" + "{接着我们说说" + first_data_cn + "吧。} " + "##");
            lead_in_statements.Add("{I should touch on " + first_data_en + ".} " + "##" + "{我要谈谈关于" + first_data_cn + "。} " + "##");
            lead_in_statements.Add("{Have you heard of " + first_data_en + "?} " + "##" + "{你听说过" + first_data_cn + "吗？} " + "##");

            //A set of lead-in statements for non-novel nodes
            List<string> non_novel_lead_in_statements = new List<string>();
            non_novel_lead_in_statements.Add("{There's also " + first_data_en + ".} " + "##" + "{还有" + first_data_cn + "呢。} " + "##");
            non_novel_lead_in_statements.Add("{Let's talk about " + first_data_en + ".} " + "##" + "{我们谈谈" + first_data_cn + "吧。} " + "##");
            non_novel_lead_in_statements.Add("{I'll mention " + first_data_en + " real quick.} " + "##" + "{我想简要提提" + first_data_cn + "。} " + "##");
            non_novel_lead_in_statements.Add("{So, about " + first_data_en + ".} " + "##" + "{那么,说说" + first_data_cn + "。} " + "##");
            non_novel_lead_in_statements.Add("{Now then, about " + first_data_en + ".} " + "##" + "{现在谈谈" + first_data_cn + "吧。} " + "##");
            non_novel_lead_in_statements.Add("{Let's talk about " + first_data_en + " for a moment.} " + "##" + "{我们聊一会儿" + first_data_cn + " 吧。} " + "##");
            non_novel_lead_in_statements.Add("{Have I mentioned " + first_data_en + "?} " + "##" + "{之前我说过" + first_data_cn + "吗？} " + "##");
            non_novel_lead_in_statements.Add("{Now, about " + first_data_en + ".} " + "##" + "{接着是" + first_data_cn + "。} " + "##");
            non_novel_lead_in_statements.Add("{Now, let's talk about " + first_data_en + ".} " + "##" + "{现在我们谈谈" + first_data_cn + "吧。} " + "##");
            non_novel_lead_in_statements.Add("{I should touch on " + first_data_en + ".} " + "##" + "{我要说说" + first_data_cn + "。} " + "##");

            //A set of lead-in statements for novel nodes
            //TODO: Author these again; things like let's talk about something different now.
            List<string> novel_lead_in_statements = new List<string>();
            novel_lead_in_statements.Add("{Let's talk about something different. " + "##" + "{我们聊点别的吧。" + "##");
            novel_lead_in_statements.Add("{Let's talk about something else. " + "##" + "{我们说点其他的吧。" + "##");
            novel_lead_in_statements.Add("{Let's switch gears. " + "##" + "{我们换个话题吧。" + "##");

            Random rand = new Random();

			// Check if there is a relationship between two nodes
			if (last.getNeighbor(first.Data) != null || first.getNeighbor(last.Data) != null)
			{
                string relationship_neighbor_en = last.getRelationshipNeighbor(first.Data);
                string relationship_neighbor_cn = last.getRelationshipNeighbor(first.Data);
                string relationship_parent_en = last.getRelationshipParent(first.Data);
                string relationship_parent_cn = last.getRelationshipParent(first.Data);

                Console.WriteLine("In LeadingTopic, relationship_neighbor_en " + relationship_neighbor_en + " relationship_neighbor_cn " + relationship_neighbor_cn);

                if (relationship_neighbor_en.Contains("##"))
                {
                    relationship_neighbor_en = relationship_neighbor_en.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                    relationship_neighbor_cn = relationship_neighbor_cn.Split(new string[] { "##" }, StringSplitOptions.None)[1];
                }
                if (relationship_parent_en.Contains("##"))
                {
                    relationship_parent_en = relationship_parent_en.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                    relationship_parent_cn = relationship_parent_cn.Split(new string[] { "##" }, StringSplitOptions.None)[1];
                }//end if

                // Check if last has first as its neighbor
                if (!last.getRelationshipNeighbor (first.Data).Equals("")
                    && !(last.getRelationshipNeighbor (first.Data) == null))
                {
					return_message = "{" + last_data_en + " " + relationship_neighbor_en + " " 
						+ first_data_en + ".} " + "##" + "{" + last_data_cn + " " + relationship_neighbor_cn + " "
                        + first_data_cn + ".} " + "##";
                    return return_message;
				}//end if
				// If last is a child node of first (first is a parent of last)
				else if (!last.getRelationshipParent (first.Data).Equals("")
                            && !(last.getRelationshipParent(first.Data) == null))
				{
                    return_message = "{" + last_data_en + " " + relationship_parent_en + " "
                        + first_data_en + ".} " + "##" + "{" + last_data_cn + " " + relationship_parent_cn + " "
                        + first_data_cn + ".} " + "##";
                    return return_message;
				}//end else if
			}//end if
			// Neither neighbor or parent/child.
            //If this is for additional info, return blank string; the two nodes
            //are not neighbors or have a blank relationship.
            if (for_additional_info)
                return "";

			// NEED TO consider novelty value (low)
			//else if (last.getNeighbor(first.Data) == null || first.getNeighbor(last.Data) == null)

            //If the novelty is high enough, always include a novel topic lead-in statement.
            if (noveltyValue >= 0.6)
                return_message += novel_lead_in_statements[rand.Next(novel_lead_in_statements.Count)];
            //Otherwise, include a non-novel topic lead-in statement.
            else
            {
                return_message += non_novel_lead_in_statements[rand.Next(non_novel_lead_in_statements.Count)];
            }//end if

            //!FindSpeak(first).Contains<string>(first.Data)

			return return_message;
		}

		private string RelationshipAnalogy(Feature old, Feature newOld, Feature prevOfCurr, Feature current)
		{
			string return_message = "";
			/*Console.WriteLine("old: " + old.Data);
			Console.WriteLine("new: " + newOld.Data);
            Console.WriteLine("relationship: " + old.getRelationshipNeighbor(newOld.Data));
			Console.WriteLine("previous of current: " + prevOfCurr.Data);
			Console.WriteLine("current: " + current.Data);
            Console.WriteLine("relationship: " + prevOfCurr.getRelationshipNeighbor(current.Data));
            */

			// Senten Patterns list - for 3 nodes
			List<string> sentencePatterns = new List<string>();

			Random rnd = new Random();

            //Define A1, B1, A2, B2, R1,and R2.
            //  Node A1 has relationship R1 with node B1.
            //  Node A2 has relaitonship R2 with node B2.
			//  AND R1 and R2 are in the same list inside equivalent_relationship list.
            string a1 = "";
            string b1 = "";
            string a2 = "";
            string b2 = "";
            string r1 = "";
			string r2 = "";

			//Check equivalent and similarity
			bool found = false;
            bool directional = false;
            //Check if the relationship is a directional word.
            if (Directional_Words.Contains(old.getRelationshipNeighbor(newOld.Data))
                || Directional_Words.Contains(newOld.getRelationshipNeighbor(old.Data)))
            {
                directional = true;
            }//end if
            

			foreach (List<string> list in equivalent_relationships)
			{
				if (found == true) break;
				if ((list.Contains(old.getRelationshipNeighbor(newOld.Data)) && list.Contains(prevOfCurr.getRelationshipNeighbor(current.Data)))
					|| old.getRelationshipNeighbor(newOld.Data).Equals(prevOfCurr.getRelationshipNeighbor(current.Data)))
				{
					a1 = old.Data;
					b1 = newOld.Data;
					a2 = prevOfCurr.Data;
					b2 = current.Data;
					r1 = old.getRelationshipNeighbor(newOld.Data);
					r2 = prevOfCurr.getRelationshipNeighbor(current.Data);
					found = true;
				}
				else if ((list.Contains(newOld.getRelationshipNeighbor(old.Data)) && list.Contains(current.getRelationshipNeighbor(prevOfCurr.Data)))
					|| newOld.getRelationshipNeighbor(old.Data).Equals(current.getRelationshipNeighbor(prevOfCurr.Data)))
				{
					a1 = newOld.Data;
					b1 = old.Data;
					a2 = current.Data;
					b2 = prevOfCurr.Data;
					r1 = newOld.getRelationshipNeighbor(old.Data);
					r2 = current.getRelationshipNeighbor(prevOfCurr.Data);
					found = true;
				}
				else if ((list.Contains(newOld.getRelationshipNeighbor(old.Data)) && list.Contains(prevOfCurr.getRelationshipNeighbor(current.Data)))
					|| newOld.getRelationshipNeighbor(old.Data).Equals(prevOfCurr.getRelationshipNeighbor(current.Data)))
				{
					a1 = newOld.Data;
					b1 = old.Data;
					a2 = prevOfCurr.Data;
					b2 = current.Data;
					r1 = newOld.getRelationshipNeighbor(old.Data);
					r2 = prevOfCurr.getRelationshipNeighbor(current.Data);
					found = true;
				}
				else if ((list.Contains(old.getRelationshipNeighbor(newOld.Data)) && list.Contains(current.getRelationshipNeighbor(prevOfCurr.Data)))
					|| old.getRelationshipNeighbor(newOld.Data).Equals(current.getRelationshipNeighbor(prevOfCurr.Data)))
				{
					a1 = old.Data;
					b1 = newOld.Data;
					a2 = current.Data;
					b2 = prevOfCurr.Data;
					r1 = old.getRelationshipNeighbor(newOld.Data);
					r2 = current.getRelationshipNeighbor (prevOfCurr.Data);
					found = true;
				}
			}

            //If there is a blank relationship, no analogy may be made.
			if (r1.Equals("") || r2.Equals(""))
                return "";
            //if a1 equals a2 and b1 equals b2, no analogy may be made.
            if (a1.Equals(a2) && b1.Equals(b2))
                return "";
            //If the relationship is directional and b1 does NOT equal b2, then
            //no analogy may be made.
            if (directional && !(b1.Equals(b2)))
            {
                return "";
            }//end if

            //if (old.getRelationshipNeighbor(newOld.Data).Equals(prevOfCurr.getRelationshipNeighbor(current.Data)) &&
            //	old.getRelationshipNeighbor(newOld.Data) != "" && prevOfCurr.getRelationshipNeighbor(current.Data) != "")
            //{
            //string relationship = old.getRelationshipNeighbor(newOld.Data);

            // enable bilingual mode

            string a1_en = a1;
            string a1_cn = a1;
            if (a1.Contains("##"))
            {
                a1_en = a1.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                a1_cn = a1.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            string b1_en = b1;
            string b1_cn = b1;
            if (b1.Contains("##"))
            {
                b1_en = b1.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                b1_cn = b1.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            string r1_en = r1;
            string r1_cn = r1;
            if (r1.Contains("##"))
            {
                r1_en = r1.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                r1_cn = r1.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            string a2_en = a2;
            string a2_cn = a2;
            if (a2.Contains("##"))
            {
                a2_en = a2.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                a2_cn = a2.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            string b2_en = b2;
            string b2_cn = b2;
            if (b2.Contains("##"))
            {
                b2_en = b2.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                b2_cn = b2.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            string r2_en = r2;
            string r2_cn = r2;
            if (r2.Contains("##"))
            {
                r2_en = r2.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                r2_cn = r2.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            // 4 nodes
            sentencePatterns.Add("[Just as " + a1_en + " " + r1_en + " " + b1_en
                + ", so too " + a2_en + " " + r2_en + " " + b2_en + ".] " + "##"
                + "[正像" + a1_cn + r1_cn + b1_cn + "一样," + a2_cn + r2_cn + b2_cn + "。] " + "##");

            sentencePatterns.Add("[" + a2_en + " " + r2_en + " " + b2_en
                + ", much like " + a1_en + " " + r1_en + " " + b1_en + ".] " + "##"
                + "[" + a2_cn + r2_cn + b2_cn + "," + "就像" + a1_cn + r1_cn + b1_cn + "。] " + "##");

            sentencePatterns.Add("[Like " + a1_en + " " + r1_en + " " + b1_en + ", "
                + a2_en + " also " + r2_en + " " + b2_en + ".] " + "##"
                + "[像" + a1_cn + r1_cn + b1_cn + "一样," + a2_cn + "也" + r2_cn + b2_cn + "。] " + "##");

            sentencePatterns.Add("[The same way that " + a1_en + " " + r1_en + " " + b1_en
                + ", " + a2_en + " " + r2_en + " " + b2_en + ".] " + "##"
                + "[如同" + a1_cn + r1_cn + b1_cn + "一般," + a2_cn + r2_cn + b2_cn + "。] " + "##");

            sentencePatterns.Add("[Remember how " + a1_en + " " + r1_en + " " + b1_en
                + "? Well, in the same way, " + a2_en + " also " + r2_en + " " + b2_en + ".] " + "##"
                + "[就像" + a1_cn + r1_cn + b1_cn + "一样," + a2_cn + "也" + r2_cn + b2_cn + "。] " + "##");

            sentencePatterns.Add("[" + a2_en + " also " + r2_en + " " + b2_en
                + ", similar to how " + a1_en + " " + r1_en + " " + b1_en + ".] " + "##"
                + "[" + a2_cn + r2_cn + b2_cn + "," + "正像" + a1_cn + r1_cn + b1_cn + "。] " + "##");


			int random_int = rnd.Next(sentencePatterns.Count);

            return_message += sentencePatterns[random_int];
			//}

            //DEBUG
            Console.WriteLine("return_message: " + return_message);

			return return_message;
		}

        //Return information about the given node's neighbors
        private string AdjacentNodeInfo(Feature current, Feature last)
        {
            string return_string = ""; //" Also, ";

            //Get n adjacent nodes randomly for the given node.
            int n = 2;
            List<Tuple<Feature, double, string>> neighbor_list = new List<Tuple<Feature, double, string>>();

            neighbor_list = current.Neighbors;

            //for (int i = 0; i < n; i++)
            //{
                //Get a random neighbor's data that hasn't already
                //been added to the list of neighbors' data.
                //int random_index = new Random().Next(current.Neighbors.Count);
                //Tuple<Feature, double, string> neighbor = current.Neighbors[random_index];
                //if (!neighbor_list.Contains(neighbor))
                //    neighbor_list.Add(neighbor);
                //else
                //    i -= 1;
            //}//end for

            int neighbor_count = 0;
            String relationship_return = "";
            foreach (Tuple<Feature, double, string> neighbor_tuple in current.Neighbors)
            {
                //LeadingTopic("current" node, node we just came from)
                //Don't do the node we just came from
                if (neighbor_tuple.Item3.Equals(last.Data))
                    continue;

                relationship_return = LeadingTopic(neighbor_tuple.Item1, current);
                if (relationship_return.Equals(""))
                    continue;

                if (neighbor_count == n - 1)
                    return_string += ", and ";
                else if (neighbor_count != 0)
                    return_string += ", ";

                return_string += relationship_return;

                neighbor_count += 1;

                if (neighbor_count == n)
                    break;
            }//end foreach

            return return_string;
        }//end method AdjacentNodeInfo

		// Check to see if the name of the node is already mentioned in the speaks
		public bool CheckAlreadyMentioned(Feature feat)
		{
			List<string> speaks = feat.Speaks;
			string data = feat.Data;

            //Console.WriteLine(feat.Data + " mentioned in " + speaks[0] + " : " + speaks[0].Contains (data));

			return speaks[0].Contains (data);
		}
			
	    private string MessageToServer(Feature feat, string speak, string noveltyInfo, string proximalInfo = "", bool forLog = false, bool out_of_topic_response = false)
        {
            String return_message = "";

            prevCurr.AddFirst(feat);
	    	MetList.AddLast(feat);
	    	countFocusNode += 1;

            if (prevCurr.Count > 2)
            {
		        prevCurr.RemoveLast();
	        }
            //Store the last history_size number of nodes
            int history_size = 100;
            if (MetList.Count > history_size)
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

            bool analogy_made = false;
            if (MetList.Count() >= 4)
            {
				// Analogy
				if (newOld != null )
				{

				}

				//while (old.getRelationshipNeighbor(newOld.Data) != prevOfCurr.getRelationshipNeighbor(current.Data))
                //DEBUG
                if (prevOfCurr.getNeighbor(current.Data) != null)
                {
                    Console.WriteLine(prevOfCurr.Data + " is neighbors with " + current.Data + ", relationship " + prevOfCurr.getRelationshipNeighbor(current.Data));
                }//end if

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
                    //Check the no_analogy list first to see if an analogy should be made with this relationship.
                    //NOTE: List only contains the english half of each relationship. Check 0th index of split.
                    if (no_analogy_relationships.Contains(old.getRelationshipNeighbor(newOld.Data).Split(new string[] { "##" }, StringSplitOptions.None)[0]))
                    {
                        continue;
                    }//end if

                    //If the relationships match and neither relationship is the empty relationship,
                    //form an analogy.
                    //NOTE: Checking relationships in BOTH directions
                    bool try_analogy = false;
                    foreach (List<String> equivalent_set in equivalent_relationships)
                    {
                        //Check if the relationships are in the same equivalent set. If so, try to form an analogy.
                        if (equivalent_set.Contains(old.getRelationshipNeighbor(newOld.Data))
                            && equivalent_set.Contains(prevOfCurr.getRelationshipNeighbor(current.Data)))
                        {
                            try_analogy = true;
                            break;
                        }//end if
                    }//end foreach
                    if (((old.getRelationshipNeighbor(newOld.Data).Equals(prevOfCurr.getRelationshipNeighbor(current.Data))
                            || newOld.getRelationshipNeighbor(old.Data).Equals(current.getRelationshipNeighbor(prevOfCurr.Data))
                            || newOld.getRelationshipNeighbor(old.Data).Equals(prevOfCurr.getRelationshipNeighbor(current.Data))
                            || old.getRelationshipNeighbor(newOld.Data).Equals(current.getRelationshipNeighbor(prevOfCurr.Data)))
                        && old.getRelationshipNeighbor(newOld.Data) != "" && prevOfCurr.getRelationshipNeighbor(current.Data) != "")
                        || try_analogy)
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
                        // Not necessary at the moment to check for rareness of analogy
						if (count_relationship <= 1000)
						{
                            int return_message_length = return_message.Length;
							return_message += RelationshipAnalogy (old, newOld, prevOfCurr, current);
                            //If any addition has been made to the return message, then an
                            //analogy has been successfully made.
                            if (return_message.Length > return_message_length)
                                analogy_made = true;
                            //Otherwise, keep trying to find an analogy
                            else
                                continue;
						}
						break;
                    }//end if
                }
            }

            Console.WriteLine("analogy made " + analogy_made);
			// Leading-topic sentence.
            // Only place a leading topic sentence if there isn't already an analogy here.
            if (prevCurr.Count > 1 && !analogy_made) // && !CheckAlreadyMentioned(current))// && countFocusNode == 1)
            {
                Console.WriteLine("creating leading topic for " + last.Data + " to " + first.Data);
                return_message = LeadingTopic(last, first);
                countFocusNode = 0; // Set back to 0
            }
            //Otherwise, this is the first node being mentioned.
            else if (!analogy_made)
            {
                //As the first node, place an introduction phrase before it.
                // 8.18: replaced first.Data with first_data
                string first_data_en = first.Data;
                string first_data_cn = first.Data;

                if (first.Data.Contains("##"))
                {
                    first_data_en = first.Data.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                    first_data_cn = first.Data.Split(new string[] { "##" }, StringSplitOptions.None)[1];
                }
                return_message = "{First, let's talk about " + first_data_en + ".} " + "##" + "{首先，让我们谈谈 " + first_data_cn + "。} " + "##";

            }//end else

            String to_speak = return_message + speak;

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
            }
                

            //Console.WriteLine("to_speak: " + to_speak);

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
            //Console.WriteLine("Before new feature speaker in parse input");
            FeatureSpeaker speaker = new FeatureSpeaker(this.graph, temporalConstraintList, prevSpatial, topicHistory);
            //Console.WriteLine("after new speaker in parse input");
            if (split_input.Length != 0 || messageToServer)
            {
                //Step-through command from Query window.
                if (split_input[0].Equals("STEP"))
                {
                    //Step through the program with blank inputs a certain number of times, 
                    //specified by the second argument in the command
                    //Console.WriteLine("step_count " + split_input[1]);
                    int step_count = int.Parse(split_input[1]);

                    //TESTING JOINT MENTIONS
                    //If there are two more colon-separated integers in the command, they are two node IDs that should be mentioned together.
                    if (split_input.Length > 2)
                    {
                        //Since this is just a test, first, clear joint_mention_sets
                        joint_mention_sets.Clear();
                        //Get the two indices from the command
                        int index_1 = int.Parse(split_input[2]);
                        int index_2 = int.Parse(split_input[3]);
                        //Add the pair as a list of features to joint_mention_sets.
                        List<Feature> joint_set = new List<Feature>();
                        joint_set.Add(this.graph.getFeature(index_1));
                        joint_set.Add(this.graph.getFeature(index_2));
                        joint_mention_sets.Add(joint_set);
                    }//end if

                    //Create an answer by calling the ParseInput function step_count times.
                    answer = "";
                    for (int s = 0; s < step_count; s++)
                    {
                        //Get forServer and forLog responses.
                        //Treat every 5th node as topic
                        if (s % 5 == 1)
                        {
                            //Last parameter true means the current node is the topic node
                            answer += ParseInput("", true, true, false, false);
                        }//end if
                        else
                            answer += ParseInput("", true, true, false, false);
                        answer += "\n";
                    }
                    //Console.WriteLine("answer " + answer);
                    //Just return this answer by itself
                    return answer;
                }//end if

                // GET_NODE_VALUES command from Unity front-end
                if (split_input[0].Equals("GET_NODE_VALUES"))
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
                //SET_LANGUAGE command from Unity front-end.
                else if (split_input[0].Equals("SET_LANGUAGE"))
                {
                    //Index 1 is the new language mode.
                    language_mode_display = int.Parse(split_input[1]);
                    language_mode_tts = int.Parse(split_input[2]);
                    return "Language to display set to " + language_mode_display + ": Language of TTS set to " + language_mode_tts;
                }//end else if
                //BEGIN_TTS command from Unity front-end.
                else if (split_input[0].Equals("BEGIN_TTS"))
                {
                    if (buffered_tts.Equals(""))
                    {
                        return "-1";
                    }//end if
                    else
                    {
                        string to_return = "TTS_COMPLETE##" + buffered_tts;
                        buffered_tts = "";

                        return to_return;
                    }
                }//end else if
                //GET_TTS command from Unity front-end.
                else if (split_input[0].Equals("GET_TTS"))
                {
                    if (buffered_tts.Equals(""))
                    {
                        return "-1";
                    }//end if
                    else
                    {
                        return buffered_tts;
                    }//end else
                }//end else if
            }//end else if

            // CASE: Nothing / Move on to next topic
            if (string.IsNullOrEmpty(input))
            {
                Feature nextTopic = this.topic;
                string[] newBuffer;
                
                // == testing forward projection
                if (false)
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
                //If we are not projecting the current node as a topic, pick the next node normally
                if (!projectAsTopic)
                {
                    nextTopic = speaker.getNextTopic(nextTopic, "", this.turn);
                    //Console.WriteLine("Next Topic from " + this.topic.Data + " is " + nextTopic.Data);
                }//end if
                //If we are projecting the current node as a topic, pick the next node whose projected
                //path of nodes relate most to the current node (has the highest score).
                else
                {
                    Console.WriteLine("Current Topic: " + this.topic.Data);
                    //Go this many steps in the forward projection.
                    int forward_turn = 5;
                    //Get a list of all the neighbors to the current node
                    List<Tuple<Feature, double, string>> all_neighbors = this.topic.Neighbors;
                    //print out all neighbors
                    Console.WriteLine("Neighbors: ");
                    for (int i = 0; i < all_neighbors.Count; i++)
                    {
                        Console.WriteLine(all_neighbors[i].Item1.Data);
                    }//end for

                    //For each neighbor, find its projected path and sum the score of each node in the path relative to the current node.
                    double highest_score = -10000;
                    foreach (Tuple<Feature, double, string> neighbor_tuple in all_neighbors)
                    {
                        //First, check if the neighbor is a filtered node.
                        //If so, do not consider it.
                        if (filter_nodes.Contains(neighbor_tuple.Item1.Data))
                            continue;

                        List<Feature> projected_path = speaker.forwardProjection(neighbor_tuple.Item1, forward_turn);
                        //print out all the topics
                        /*Console.WriteLine("Projected Path: ");
                        for (int i = 0; i < forward_turn; i++)
                        {
                            Console.WriteLine(projected_path[i].Data);
                        }//end for*/

                        double total_score = 0;

                        //Total score calculation for topic
                        //Sum score of each path node relative to the current node
                        /*
                        foreach (Feature path_node in projected_path)
                        {
                            total_score += speaker.calculateScore(path_node, this.topic);
                        }//end foreach
                        //Console.WriteLine("Score for path: " + total_score);
                        */
                        //Total score calculation for joint mentions
                        //If a joint mention appears in the path, add an amount (currently just the joint mention weight)
                        //to the score of the neighbor (first path node) relative to the current node.
                        bool joint_mention_exists = true;
                        //For testing purposes, only check the first list in joint_mention_sets
                        foreach (Feature temp_node in joint_mention_sets[0])
                        {
                            if (!projected_path.Contains(temp_node))
                                joint_mention_exists = false;
                        }//end foreach
                        if (joint_mention_exists)
                        {
                            Console.WriteLine("Joint mention exists");
                            total_score = speaker.calculateScore(neighbor_tuple.Item1, this.topic) + this.graph.getSingleWeight(Constant.JointWeightIndex);
                        }//end if

                        if (total_score > highest_score)
                        {
                            highest_score = total_score;
                            nextTopic = neighbor_tuple.Item1;
                        }//end if
                    }//end foreach
                    //At the end of this foreach, nextTopic is set to the next node whose projected path had the highest sum score
                    //relative to the current node.
                    Console.WriteLine("Next Topic from " + this.topic.Data + " is " + nextTopic.Data + " with score " + highest_score);
                    Console.WriteLine("Path: ");
                    List<Feature> test_path = speaker.forwardProjection(nextTopic, forward_turn);
                    //print out all the topics
                    for (int i = 0; i < forward_turn; i++)
                    {
                        Console.WriteLine(test_path[i].Data);
                    }//end for
                }//end else

                /*
                //Check for filter nodes.
                if (filter_nodes.Contains(nextTopic.Data))
                {
                    //If it is a filter node, take another step.
                    Console.WriteLine("Filtering out " + nextTopic.Data);
                    ParseInput("", false, false);
                }//end if
                */
                
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
                if (projectAsTopic)
                    answer = "*****" + answer;
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
                {
                    answer = "I've said all I can about that topic!" + "##" + "我已经把我知道的都说完了。" + "##";
                }
                    
                noveltyInfo = speaker.getNovelty(this.topic, this.turn, noveltyAmount);
            }
            // CASE: New topic/question
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
                    return MessageToServer(this.topic, answer, noveltyInfo, speaker.getProximal(this.topic, noveltyAmount), forLog, outOfTopic);
                }

                if (outOfTopic)
                    answer += ParseInput("", false, false);

                if (forLog)
                    return answer;
                else
                    return answer;// +" <Novelty Info: " + noveltyInfo + " > <Proximal Info: " + speaker.getProximal(this.topic, noveltyAmount) + ">";
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
            if (input.Contains("this") || input.Contains("that"))
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
                            }

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
                output.AddRange(FindStuffToSay(query.MainTopic));
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
                                output_string = string.Format("{0} " + temp_neighbor.Item3 + " {1}", query_topic.Data, temp_neighbor.Item1.Data);
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
                output_string += ".";
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

        private string[] FindStuffToSay(Feature feature)
        {
            List<string> stuff = new List<string>();
            string[] speaks = FindSpeak(feature);
            if (speaks.Length > 0)
            {
                stuff.AddRange(speaks);
            }
                
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
