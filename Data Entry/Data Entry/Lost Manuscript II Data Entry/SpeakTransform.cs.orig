using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dialogue_Data_Entry
{
    class SpeakTransform
    {
        private List<Feature> history_list;
        private Feature previous_topic;

        //A list of sets of relationships that should be considered the same
        private List<List<String>> equivalent_relationships;
        //A list of relationships that should not be used to make analogies
        private List<String> no_analogy_relationships;

        private AnalogyBuilder analogy_builder;
        //A list of features that can be referred back to, different from the history list.
        private List<Feature> reference_list;

        //Instantiate the SpeakTransform with both a history list and the previous topic.
        //The history list includes, at its last element, the current feature.
        public SpeakTransform(List<Feature> history_in, Feature previous_topic_in, List<Feature> ref_list = null)
        {
            history_list = history_in;
            if (previous_topic_in != null)
                previous_topic = previous_topic_in;
            else
                previous_topic = null;

            if (ref_list != null)
                reference_list = ref_list;

            equivalent_relationships = new List<List<String>>();
            //Build lists of relationships that should be considered the same
            //is, are, was, is a kind of, is a
            equivalent_relationships.Add(new List<String>() { "is", "are", "was", "is a kind of", "is a" });
            //was a member of, is a member of
            equivalent_relationships.Add(new List<String>() { "was a member of", "is a member of" });
            //won a gcurrent_node medal in, won
            equivalent_relationships.Add(new List<String>() { "won a gcurrent_node medal in", "won" });
            //is one of, was one of the, was one of
            equivalent_relationships.Add(new List<String>() { "is one of", "was one of the", "was one of" });
            //include, includes, included
            equivalent_relationships.Add(new List<String>() { "include", "includes", "included" });
            //took place on
            equivalent_relationships.Add(new List<String>() { "took place on" });
            //took place at
            equivalent_relationships.Add(new List<String>() { "took place at" });
            //has, had
            equivalent_relationships.Add(new List<String>() { "has", "had" });
            //includes event
            equivalent_relationships.Add(new List<String>() { "includes event" });
            //includes member, included member
            equivalent_relationships.Add(new List<String>() { "includes member", "included member" });
            //include athlete
            equivalent_relationships.Add(new List<String>() { "include athlete" });
            //is southwest of, is southeast of, is northeast of, is north of,
            //is west of, is east of, is south of, is northwest of
            equivalent_relationships.Add(new List<String>() { "is southwest of", "is southeast of"
                , "is northeast of", "is north of", "is west of", "is east of", "is south of", "is northwest of" });

            no_analogy_relationships = new List<String>();
            //Build list of relationships which should not be used in analogies.
            //List should be different for each XML.
            no_analogy_relationships.Add("occurred before");
            no_analogy_relationships.Add("occurred after");
            no_analogy_relationships.Add("include");
            no_analogy_relationships.Add("includes");
            no_analogy_relationships.Add("included");
            no_analogy_relationships.Add("has");
            no_analogy_relationships.Add("had");
            no_analogy_relationships.Add("");

            if (ab != null)
                analogy_builder = ab;
        }//end constructor SpeakTransform

        //Takes a feature and its speak value. Using the history list and feature graph, 
        //attempts to add to the speak value (e.g. lead-in statements, analogies, etc.)
        //Returns the transformed speak value.
        public String TransformSpeak(Feature feat, string speak)
        {
            String transformed_speak = "";

            //First, try to make an analogy.
            bool analogy_made = true;
            string analogy = MakeAnalogy(feat);
            if (analogy.Equals(""))
                analogy_made = false;
            else
                transformed_speak = analogy + speak;

            //If no analogy had been made, add a lead in statement
            if (analogy_made == false)
            {
                transformed_speak = LeadInTopic(previous_topic, feat) + speak;
            }//end if

            if (reference_list != null)
                transformed_speak = transformed_speak + TieBack(feat, reference_list, previous_topic);

            return transformed_speak;
        }//end method TransformSpeak

        //Tries to make an analogy with the given feature.
        //Returns the empty string if no analogy can be made.
        private String MakeAnalogy(Feature feat)
        {
            string analogy = "";

            //NOTE: To change the method by which analogies are made, create a new function,
            //call it here, and return its result as the analogy.

            //Make an analogy based on the feature passed in, the previous topic, and the history list
            //and the relationships bewteen nodes.

            //Every fifth node (starting from the 5th), try to make an analogy
            //using the remote analogy code.
            if (history_list.Count % 6 == 5)
                analogy = RemoteAnalogy(feat);
            //else
                //analogy = RelationshipAnalogy(feat);


            /*var best_analogy = analogy_builder.find_best_analogy(feat);
			if(best_analogy != null) analogy = analogy_builder.elaborate_on_analogy(best_analogy);*/

            return analogy;
        }//end method MakeAnalogy
        //Make an analogy between the two given features
        public string MakeAnalogy(Feature current_feature, Feature past_feature)
        {
            string analogy = "Analogy between " + current_feature.Name + " and " + past_feature.Name;

            return analogy;
        }//end method MakeAnalogy

        private string RemoteAnalogy(Feature feat)
        {
            string analogy = "";

            using (var client = new HttpClient())
            {
                string url = "http://localhost:5000/get_analogy_narration";
                string url_parameters = "?id=" + feat.Id.ToString() + "&filename=" + @"C:\Users\Zev\Documents\GitHub\ChineseDialogueSpring2015\data files\2008_Summer_Olympic_Games_2_2_2016.xml";// +"&port=9000";

                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //string http_get_text = "127.0.0.1:5000/get_analogy?id=" + feat.Id.ToString();// + "&port=5001";

                HttpResponseMessage response = client.GetAsync(url_parameters).Result;

                //Read the json string from the http response content
                Task<string> read_string_task = response.Content.ReadAsStringAsync();
                read_string_task.Wait(100000);

                string content_string = read_string_task.Result;
                JObject json_response = JObject.Parse(content_string);

                string explanation = json_response["explanation"].ToString();
                
                //JsonSerializer temp_serializer = new JsonSerializer();
                //var deserialized_response = temp_serializer.Deserialize(json_response);

                Console.WriteLine("Response to remote analogy: " + explanation);

                analogy = explanation;

                /*if (response.IsSuccessStatusCode)
                {
                }//end if*/
            }//end using

            return analogy;
        }//end method RemoteAnalogy

        //Creates an analogy based on the history of nodes traversed and relationships between nodes.
        //Returns the empty string if no analogy can be made.
        private string RelationshipAnalogy(Feature feat)
        {
            string analogy = "";

            //First, check that there are at least 4 nodes in the history list.
            //If not, then an analogy cannot be made.
            if (history_list.Count < 4)
                return "";

            //If the relationship between the previous topic and the current topic is on the no_analogy list,
            //then an analogy should not be made with them.
            if (no_analogy_relationships.Contains(previous_topic.getRelationshipNeighbor(feat.Id).Split(new string[] { "##" }, StringSplitOptions.None)[0]))
            {
                return "";
            }//end if

            //Next, go through each node in the history list.
            for (int i = 0; i < history_list.Count - 2; i++)
            {
                //Get each node and the node after it.
                Feature current_node = history_list.ElementAt(i);
                Feature next_node = history_list.ElementAt(i + 1);

                //If the current and next nodes are the previous topic and the current feature,
                //then we have found an exact match. Do not make an analogy of it.
                if (current_node.Id.Equals(previous_topic.Id) && next_node.Id.Equals(feat.Id))
                {
                    continue;
                }//end if

                //If the relationship between the current node and the next node matches
                //the relationship between the previous topic and the current feature, make an analogy.
                //NOTE: Checking relationships in both directions
                String analogy_built = RelationshipAnalogyBuilder(current_node, next_node, feat);
                //If the analogy builder returns the empty string, no analogy has been made. Keep trying.
                if (analogy_built.Equals(""))
                    continue;
                else
                {
                    analogy = analogy_built;
                    //Let the loop continue so that we make the analogy based off of
                    //the most recent pair of nodes.
                }//end else
            }//end for

            return analogy;
        }//end function RelationshipAnalogy
        //Helper function for RelationshipAnalogy.
        //Performs actual construction of the analogy based on relationships.
        private string RelationshipAnalogyBuilder(Feature current_node, Feature next_node, Feature current_topic)
        {
            String return_message = "";

            // Senten Patterns list - for 3 nodes
            List<string> sentencePatterns = new List<string>();

            Random rnd = new Random();

            //Define A1, B1, A2, B2, R1,and R2.
            //  Node A1 has relationship R1 with node B1.
            //  Node A2 has relaitonship R2 with node B2.
            //  AND R1 and R2 are equivalent relationships.
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
            /*if (Directional_Words.Contains(current_node.getRelationshipNeighbor(next_node.Id))
                || Directional_Words.Contains(next_node.getRelationshipNeighbor(current_node.Id)))
            {
                directional = true;
            }//end if*/

            foreach (List<string> list in equivalent_relationships)
            {
                if (found == true) break;
                if ((list.Contains(current_node.getRelationshipNeighbor(next_node.Id)) && list.Contains(previous_topic.getRelationshipNeighbor(current_topic.Id)))
                    || current_node.getRelationshipNeighbor(next_node.Id).Equals(previous_topic.getRelationshipNeighbor(current_topic.Id)))
                {
                    a1 = current_node.Name;
                    b1 = next_node.Name;
                    a2 = previous_topic.Name;
                    b2 = current_topic.Name;
                    r1 = current_node.getRelationshipNeighbor(next_node.Id);
                    r2 = previous_topic.getRelationshipNeighbor(current_topic.Id);
                    found = true;
                }
                else if ((list.Contains(next_node.getRelationshipNeighbor(current_node.Id)) && list.Contains(current_topic.getRelationshipNeighbor(previous_topic.Id)))
                    || next_node.getRelationshipNeighbor(current_node.Id).Equals(current_topic.getRelationshipNeighbor(previous_topic.Id)))
                {
                    a1 = next_node.Name;
                    b1 = current_node.Name;
                    a2 = current_topic.Name;
                    b2 = previous_topic.Name;
                    r1 = next_node.getRelationshipNeighbor(current_node.Id);
                    r2 = current_topic.getRelationshipNeighbor(previous_topic.Id);
                    found = true;
                }
                else if ((list.Contains(next_node.getRelationshipNeighbor(current_node.Id)) && list.Contains(previous_topic.getRelationshipNeighbor(current_topic.Id)))
                    || next_node.getRelationshipNeighbor(current_node.Id).Equals(previous_topic.getRelationshipNeighbor(current_topic.Id)))
                {
                    a1 = next_node.Name;
                    b1 = current_node.Name;
                    a2 = previous_topic.Name;
                    b2 = current_topic.Name;
                    r1 = next_node.getRelationshipNeighbor(current_node.Id);
                    r2 = previous_topic.getRelationshipNeighbor(current_topic.Id);
                    found = true;
                }
                else if ((list.Contains(current_node.getRelationshipNeighbor(next_node.Id)) && list.Contains(current_topic.getRelationshipNeighbor(previous_topic.Id)))
                    || current_node.getRelationshipNeighbor(next_node.Id).Equals(current_topic.getRelationshipNeighbor(previous_topic.Id)))
                {
                    a1 = current_node.Name;
                    b1 = next_node.Name;
                    a2 = current_topic.Name;
                    b2 = previous_topic.Name;
                    r1 = current_node.getRelationshipNeighbor(next_node.Id);
                    r2 = current_topic.getRelationshipNeighbor(previous_topic.Id);
                    found = true;
                }
            } //end foreach

            //If no matching relationship can be found, no analogy may be made.
            if (found == false)
                return "";
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

            //if (current_node.getRelationshipNeighbor(next_node.Id).Equals(previous_topic.getRelationshipNeighbor(current.Id)) &&
            //	current_node.getRelationshipNeighbor(next_node.Id) != "" && previous_topic.getRelationshipNeighbor(current.Id) != "")
            //{
            //string relationship = current_node.getRelationshipNeighbor(next_node.Id);

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
            Console.WriteLine("analogy builder return_message: " + return_message);

            return return_message;
        }//end method RelationshipAnalogyBuilder

        //optional parameter for_additional_info, if set true, will avoid any actual leading statements
        //except for relationship mentions. If no relationship mention can be made, then blank string
        //is returned.
        //private int topic_index = 0;
        private string LeadInTopic(Feature last, Feature first, bool use_relationships = true)
        {
            string return_message = "";

            string first_name_en = first.Name;
            string first_name_cn = first.Name;
            if (first.Name.Contains("##"))
            {
                first_name_en = first.Name.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                first_name_cn = first.Name.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            //First, check if "last", the previous topic, is null. If so, use an introduction.
            if (last == null)
                return "{First, let's talk about " + first_name_en + ".} " + "##" + "{首先，让我们谈谈 " + first_name_cn + "。} " + "##";


            //Console.WriteLine("In LeadInTopic, first_name_en " + first_name_en + " first_name_cn " + first_name_cn);

            string last_name_en = last.Name;
            string last_name_cn = last.Name;
            if (last.Name.Contains("##"))
            {
                last_name_en = last.Name.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                last_name_cn = last.Name.Split(new string[] { "##" }, StringSplitOptions.None)[1];
            }

            //Console.WriteLine("In LeadInTopic, last_name_en " + last_name_en + " last_name_cn " + last_name_cn);

            //First is the current node (the one that has just been traversed to)
            //A set of possible lead-in statements.
            List<string> lead_in_statements = new List<string>();
            lead_in_statements.Add("{So, about " + first_name_en + ".} " + "##" + "{还有" + first_name_cn + "呢。} " + "##");
            lead_in_statements.Add("{But let's talk about " + first_name_en + ".} " + "##" + "{我们来聊聊" + first_name_cn + "吧。} " + "##");
            lead_in_statements.Add("{And have I mentioned " + first_name_en + "?} " + "##" + "{之前我说过" + first_name_cn + "吗？} " + "##");
            lead_in_statements.Add("{Now, about " + first_name_en + ".} " + "##" + "{接下来是" + first_name_cn + "。} " + "##");
            lead_in_statements.Add("{Now, let's talk about " + first_name_en + ".} " + "##" + "{接着我们说说" + first_name_cn + "吧。} " + "##");
            lead_in_statements.Add("{I should touch on " + first_name_en + ".} " + "##" + "{我要谈谈关于" + first_name_cn + "。} " + "##");
            lead_in_statements.Add("{Have you heard of " + first_name_en + "?} " + "##" + "{你听说过" + first_name_cn + "吗？} " + "##");

            //A set of lead-in statements for non-novel nodes
            List<string> non_novel_lead_in_statements = new List<string>();
            non_novel_lead_in_statements.Add("{Have you heard of " + first_name_en + "?} " + "##" + "{还有" + first_name_cn + "呢。} " + "##");
            non_novel_lead_in_statements.Add("{Let's talk about " + first_name_en + ".} " + "##" + "{我们谈谈" + first_name_cn + "吧。} " + "##");
            non_novel_lead_in_statements.Add("{I'll mention " + first_name_en + " real quick.} " + "##" + "{我想简要提提" + first_name_cn + "。} " + "##");
            non_novel_lead_in_statements.Add("{So, about " + first_name_en + ".} " + "##" + "{那么,说说" + first_name_cn + "。} " + "##");
            non_novel_lead_in_statements.Add("{Now then, about " + first_name_en + ".} " + "##" + "{现在谈谈" + first_name_cn + "吧。} " + "##");
            non_novel_lead_in_statements.Add("{Let's talk about " + first_name_en + " for a moment.} " + "##" + "{我们聊一会儿" + first_name_cn + " 吧。} " + "##");
            non_novel_lead_in_statements.Add("{Have I mentioned " + first_name_en + "?} " + "##" + "{之前我说过" + first_name_cn + "吗？} " + "##");
            non_novel_lead_in_statements.Add("{Now, about " + first_name_en + ".} " + "##" + "{接着是" + first_name_cn + "。} " + "##");
            non_novel_lead_in_statements.Add("{Now, let's talk about " + first_name_en + ".} " + "##" + "{现在我们谈谈" + first_name_cn + "吧。} " + "##");
            non_novel_lead_in_statements.Add("{I should touch on " + first_name_en + ".} " + "##" + "{我要说说" + first_name_cn + "。} " + "##");

            //A set of lead-in statements for novel nodes
            //TODO: Author these again; things like let's talk about something different now.
            List<string> novel_lead_in_statements = new List<string>();
            novel_lead_in_statements.Add("{Let's talk about something different. " + "##" + "{我们聊点别的吧。" + "##");
            novel_lead_in_statements.Add("{Let's talk about something else. " + "##" + "{我们说点其他的吧。" + "##");
            novel_lead_in_statements.Add("{Let's switch gears. " + "##" + "{我们换个话题吧。" + "##");

            Random rand = new Random();

            Feature rel_last_to_first = last.getNeighbor(first.Id);
            Feature rel_first_to_last = first.getNeighbor(last.Id);
            // Check if there is a relationship between two nodes
            if ((last.getNeighbor(first.Id) != null || first.getNeighbor(last.Id) != null) && use_relationships)
            {
                string relationship_neighbor_en = last.getRelationshipNeighbor(first.Id);
                string relationship_neighbor_cn = last.getRelationshipNeighbor(first.Id);
                string relationship_parent_en = last.getRelationshipParent(first.Id);
                string relationship_parent_cn = last.getRelationshipParent(first.Id);

                //Console.WriteLine("In LeadInTopic, relationship_neighbor_en " + relationship_neighbor_en + " relationship_neighbor_cn " + relationship_neighbor_cn);

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

                // Check if last has first as its neighbor and their relationship is not blank
                if (!last.getRelationshipNeighbor(first.Id).Equals("")
                    && !(last.getRelationshipNeighbor(first.Id) == null))
                {
                    return_message = "{" + last_name_en + " " + relationship_neighbor_en + " "
                        + first_name_en + ".} " + "##" + "{" + last_name_cn + " " + relationship_neighbor_cn + " "
                        + first_name_cn + ".} " + "##";
                    Console.WriteLine("Lead-in topic result: " + return_message);
                    return return_message;
                }//end if
                // If not, check if first has last as its neighbor and their relationship is not blank
                else if (!first.getRelationshipNeighbor(last.Id).Equals("")
                        && !(first.getRelationshipNeighbor(last.Id) == null))
                {
                    //TODO: Chinese part isn't fixed yet, we need to get the relationship from first to last.
                    //Right now, it's still the relationship from last to first.
                    return_message = "{" + first_name_en + " " + first.getRelationshipNeighbor(last.Id) + " "
                        + last_name_en + ".} " + "##" + "{" + first_name_cn + " " + relationship_neighbor_cn + " "
                        + last_name_cn + ".} " + "##";
                    Console.WriteLine("Lead-in topic result: " + return_message);
                    return return_message;
                }//end else if
                // If last is a child node of first (first is a parent of last)
                else if (!last.getRelationshipParent(first.Id).Equals("")
                            && !(last.getRelationshipParent(first.Id) == null))
                {
                    return_message = "{" + last_name_en + " " + relationship_parent_en + " "
                        + first_name_en + ".} " + "##" + "{" + last_name_cn + " " + relationship_parent_cn + " "
                        + first_name_cn + ".} " + "##";
                    Console.WriteLine("Lead-in topic result: " + return_message);
                    return return_message;
                }//end else if
            }//end if
            // Neither neighbor or parent/child.
            //If this is for additional info, return blank string; the two nodes
            //are not neighbors or have a blank relationship.
            //if (for_additional_info)
            //    return "";

            // NEED TO consider novelty value (low)
            //else if (last.getNeighbor(first.Id) == null || first.getNeighbor(last.Id) == null)

            //If the novelty is high enough, always include a novel topic lead-in statement.
            //if (noveltyValue >= 0.6)
            if (false)
                return_message += novel_lead_in_statements[rand.Next(novel_lead_in_statements.Count)];
            //Otherwise, include a non-novel topic lead-in statement.
            else
            {
                //return_message += non_novel_lead_in_statements[rand.Next(non_novel_lead_in_statements.Count)];
                return_message += non_novel_lead_in_statements[rand.Next(non_novel_lead_in_statements.Count)];
                //topic_index += 1;
                //if (topic_index >= non_novel_lead_in_statements.Count)
                //    topic_index = 0;
            }//end if

            //!FindSpeak(first).Contains<string>(first.Id)
            Console.WriteLine("Lead-in topic no relationship result: " + return_message);
            return return_message;
        }//end function LeadInTopic

        //Foreshadow information about a feature based on a list of other features as reference.
        public string Foreshadow(Feature feature_to_foreshadow, List<Feature> reference_list)
        {
            //First member is the direction of the relationship.
            //0 is AWAY FROM feature to foreshadow, 1 is TOWARD feature to foreshadow.
            List<Tuple<int, string>> relationship_list = new List<Tuple<int, string>>();

            //Create tuples consisting of this feature's relationships to all other features.
            foreach (Feature feature_to_compare in reference_list)
            {
                //Check if there is a relationship in either direction between the two.
                if ((!feature_to_foreshadow.getRelationshipNeighbor(feature_to_compare.Id).Equals("")
                    && !(feature_to_foreshadow.getRelationshipNeighbor(feature_to_compare.Id) == null)))
                {
                    //If so, then add the relationship to the list.
                    relationship_list.Add(new Tuple<int, string>(0, feature_to_foreshadow.getRelationshipNeighbor(feature_to_compare.Id)));
                }//end if
                else if (!feature_to_compare.getRelationshipNeighbor(feature_to_foreshadow.Id).Equals("")
                    && !(feature_to_compare.getRelationshipNeighbor(feature_to_foreshadow.Id) == null))
                {
                    relationship_list.Add(new Tuple<int, string>(1, feature_to_compare.getRelationshipNeighbor(feature_to_foreshadow.Id)));
                }//end else if
            }//end foreach

            string return_string = "";

            return_string = "{We'll hear more about ";

            bool foreshadowed = false;

            foreach (Tuple<int, string> relationship_entry in relationship_list)
            {
                //Relationship away from feature to foreshadow
                if (relationship_entry.Item1 == 0)
                {
                    return_string += feature_to_foreshadow.Name + " " + relationship_entry.Item2 + ", ";
                    foreshadowed = true;
                }//end if
                else if (relationship_entry.Item1 == 1)
                {
                    //Relationship towards feature to foreshadow
                    return_string += " what " + relationship_entry.Item2 + " " + feature_to_foreshadow.Name + ", ";
                    foreshadowed = true;
                }//end else if
            }//end foreach

            return_string += "soon.}";

            if (!foreshadowed)
                return_string = "";

            return return_string;
        }//end method Foreshadow

        //This function takes a feature and a list of previously mentioned topics.
        //It then tries to state a relationship with one of them.
        //Search for a related topic is done from least recent to most recent, with the
        //exception of the previous node.
        public string TieBack(Feature feature_to_check, List<Feature> previously_mentioned_features, Feature previous_feature)
        {
            string return_message = "";

            string feature_name = feature_to_check.Name;

            List<Feature> reverse_history = previously_mentioned_features;
            reverse_history.Reverse();

            // Check if there is a relationship between two nodes
            foreach (Feature history_feature in reverse_history)
            {
                //Don't talk about node directly prior to this one
                if (history_feature.Id.Equals(previous_feature.Id))
                    continue;
                if (feature_to_check.getNeighbor(history_feature.Id) != null || history_feature.getNeighbor(feature_to_check.Id) != null)
                {
                    // Check both directions for a non-blank relationship to use
                    if (!feature_to_check.getRelationshipNeighbor(history_feature.Id).Equals("")
                        && !(feature_to_check.getRelationshipNeighbor(history_feature.Id) == null))
                    {
                        return_message = "{And do you remember " + history_feature.Name + "? Well, " + feature_name + " " + feature_to_check.getRelationshipNeighbor(history_feature.Id) + " "
                            + history_feature.Name + ".} ";
                        Console.WriteLine("Tie back result: " + return_message);
                        return return_message;
                    }//end if
                    else if (!history_feature.getRelationshipNeighbor(feature_to_check.Id).Equals("")
                        && !(history_feature.getRelationshipNeighbor(feature_to_check.Id) == null))
                    {
                        return_message = "{And do you remember " + history_feature.Name + "? Well, " + history_feature.Name + " " + history_feature.getRelationshipNeighbor(feature_to_check.Id) + " "
                            + feature_name + ".} ";
                        Console.WriteLine("Tie back result: " + return_message);
                        return return_message;
                    }//end if
                }//end if
            }//end foreach

            return return_message;
        }//end function TieBack

    } //end class SpeakTransform
}
