using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dialogue_Data_Entry;

namespace Dialogue_Data_Entry
{
    class QueryController
    {
        private FeatureGraph featGraph;
        private string tellMeMore;
        private int turn;
        private Feature currentTopic;
        private string[] punctuaion = new string[] { ",", ";", ".", "?", "!", "\'", "\"","(",")","-" };
        private string startTopic = ""; //Can change to the name of the node that you want this t

        public QueryController(FeatureGraph graph)
        {
            featGraph = graph;
            tellMeMore = "";
            turn = 1;
            currentTopic = null;
        }

        private string PunctuationHandle(string str)
        {
            string temp = str;
            for (int x = 0; x < punctuaion.Length; x++)
            {
                temp = temp.Replace(punctuaion[x]," "+punctuaion[x]);
            }
            return temp;
        }

        //return true if target is in sentence, false otherwise
        private bool fullContain(string[] sentence, string target)
        {
            string[] targets = target.Split();
            for (int x = 0; x < sentence.Count()-targets.Count()+1; x++)
            {
                if (sentence[x] == targets[0])
                {
                    bool check = true; 
                    for (int y = 0; y < targets.Count(); y++)
                    {
                        if (sentence[x + y] != targets[y])
                        {
                            check = false;
                            break;
                        }
                    }
                    if (check)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool continueNextTopic(string query)
        {
            if (query == "")
            {
                return true;
            }
            return false;
        }

        public Feature queryHandle(string query)
        {
            Feature target = null;
            int targetLen = 0;
            //preprocess query
            query = query.ToLower();
            query = PunctuationHandle(query);
            string[] sentence = query.Split();
            for (int x = 0; x < featGraph.Features.Count; x++)
            {
                if (fullContain(sentence, PunctuationHandle(featGraph.Features[x].Data.ToLower()) ))
                {
                    if (featGraph.Features[x].Data.Length > targetLen)
                    {
                        target = featGraph.Features[x];
                        targetLen = featGraph.Features[x].Data.Length;
                    }
                }
            }
            return target;
        }

        private string getSpeak(Feature feat)
        {
            if (feat.Speaks.Count() != 0)
            {
                return "ID:"+ featGraph.getFeatureIndex(feat.Data) + ":Speak:" + feat.Speaks[this.turn%feat.Speaks.Count()];
            }
            return "ID:"+featGraph.getFeatureIndex(feat.Data)+":Speak:"+feat.Data;
        }

        public string makeQuery(string query)
        {
            string answer = "";
            //first turn set currentTopic to the root or pre-define topic
            if (this.currentTopic == null)
            {
                this.currentTopic = featGraph.getFeature(this.startTopic);
                if (this.startTopic == "" || this.currentTopic == null)
                {
                    this.currentTopic = featGraph.Root;
                }
            }
            //no query or continue to next topic case
            if (continueNextTopic(query))
            {
                FeatureSpeaker mySpeaker = new FeatureSpeaker();
                Feature nextTopic = mySpeaker.getNextTopic(featGraph, this.currentTopic, "", this.turn);
                nextTopic.DiscussedAmount += 1;
                featGraph.setFeatureDiscussedAmount(nextTopic.Data, nextTopic.DiscussedAmount);
                this.currentTopic = nextTopic;
                //return mySpeaker.getChildSpeak(featGraph.Root);
                answer = getSpeak(nextTopic);
            } //Tell me more about the current topic
            else if (isTellMeMoreQuery(query))
            {
                this.currentTopic.DiscussedAmount += 1;
                featGraph.setFeatureDiscussedAmount(this.currentTopic.Data,this.currentTopic.DiscussedAmount);
                answer = getSpeak(this.currentTopic);
            } //question, query or don't understand query
            else 
            {
                Feature target = queryHandle(query);
                if (target == null)
                {
                    //don't understand query case
                    answer = "No answer for the query.";
                }else
                {
                    //question or query
                    target.DiscussedAmount += 1;
                    featGraph.setFeatureDiscussedAmount(target.Data, target.DiscussedAmount);
                    this.currentTopic = target;
                    answer =  getSpeak(target);
                }
            }
            this.turn += 1;
            return answer;
        }

        public string makeQuery2(string query)
        {
            if (this.currentTopic == null)
            {
                this.currentTopic = featGraph.Root;
            }
            if (isTellMeMoreQuery(query))
            {
                string tmp = tellMeMore;
                tellMeMore = "I don't know anything else about that__EOT__";
                return tmp;
            }
            try{
            float featureWeight = .6f;
            float tagKeyWeight = .2f;
            List<string> words = query.Split().ToList<string>();
            Dictionary<Feature, float> featuresDict = new Dictionary<Feature, float>();
            for (int x = 0; x < words.Count; x++)
            {
                //Search the features
                List<Feature> featureResults = getFeatureResults(words[x]);
                for (int i = 0; i < featureResults.Count; i++)
                {
                    if (featuresDict.Keys.Contains(featureResults[i]))
                    {
                        featuresDict[featureResults[i]] += featureWeight;
                    }
                    else
                    {
                        featuresDict.Add(featureResults[i], featureWeight);
                    }
                }
                //search the keys
                featureResults = getTagKeyResults(words[x]);
                for (int i = 0; i < featureResults.Count; i++)
                {
                    if (featuresDict.Keys.Contains(featureResults[i]))
                    {
                        featuresDict[featureResults[i]] += tagKeyWeight;
                    }
                    else
                    {
                        featuresDict.Add(featureResults[i], tagKeyWeight);
                    }
                }
            }

            float maxScore = 0.0f;
            int maxIndex = -1;
            for (int i = 0; i < featuresDict.Values.Count; i++)
            {
                if (maxScore < featuresDict.Values.ElementAt(i))
                {
                    maxScore = featuresDict.Values.ElementAt(i);
                    maxIndex = i;
                }
            }

            //FeatureSpeaker 
            FeatureSpeaker mySpeaker = new FeatureSpeaker();
            tellMeMore = mySpeaker.getTagSpeak(featuresDict.Keys.ElementAt(maxIndex));
            return mySpeaker.getChildSpeak(featuresDict.Keys.ElementAt(maxIndex));
            }
            catch
            {
                //don't understand anything in sentence (don't use tellMeMore yet)
                FeatureSpeaker mySpeaker = new FeatureSpeaker();
                //tellMeMore = mySpeaker.getTagSpeak(featGraph.Root);
                Feature nextTopic = mySpeaker.getNextTopic(featGraph, this.currentTopic, "", this.turn);
                nextTopic.DiscussedAmount += 1;
                featGraph.setFeature(nextTopic.Data,nextTopic);
                this.currentTopic = nextTopic;
                this.turn += 1;
                //return mySpeaker.getChildSpeak(featGraph.Root);
                return nextTopic.Data;
            }
        }



        public List<Feature> getFeatureResults(string query)
        {
            List<Feature> result = new List<Feature>();
            if (query == "")
            {
                return result;
            }
            //result.Add("========== Features ==========");
            for (int x = 0; x < featGraph.Features.Count; x++)
            {
                if(featGraph.Features[x].Data.ToLower().Contains(query.ToLower()))
                {
                    result.Add(featGraph.Features[x]);
                }
            }
            return result;
        }
        //input is the whole sentence
        public Feature getFeatureFromSentence(string query)
        {
            for (int x = 0; x < featGraph.Features.Count;x++ )
            {
                if (query.ToLower().Contains(featGraph.Features[x].Data.ToLower()))
                {
                    return featGraph.Features[x];
                }
            }
            return null;
        }

        public List<Feature> getTagKeyResults(string query)
        {
            List<Feature> result = new List<Feature>();
            if (query == "")
            {
                return result;
            }
            //result.Add("========== Tag Keys ==========");
            for (int x = 0; x < featGraph.Features.Count; x++)
            {
                for (int y = 0; y < featGraph.Features[x].Tags.Count; y++)
                {
                    if (featGraph.Features[x].Tags[y].Item1.ToLower().Contains(query.ToLower()))
                    {
                        result.Add(featGraph.Features[x]);
                    }
                }
            }
            return result;
        }

        bool isTellMeMoreQuery(string query)
        {
            query = query.ToLower();
            return query.Contains("more") && query.Contains("tell");
        }

        //Nut's stuff below 

        private string Breath_travel(FeatureGraph myGraph)
        {
            string speak = "";
            List<Feature> myQueue = new List<Feature>();
            myQueue.Add(myGraph.Root);
            while (myQueue.Count != 0)
            {
                Feature currentFeature = myQueue[0];
                speak += currentFeature.Data + "\r\n";
                myQueue.RemoveAt(0);
                for (int i = 0; i < currentFeature.Neighbors.Count; i++)
                {
                    myQueue.Add(currentFeature.Neighbors[i].Item1);
                }
            }
            return speak;
        }

        private void Depth_travel_helper(Feature currentFeature, ref string speak)
        {
            speak += currentFeature.Data + "\r\n";
            if (currentFeature.Neighbors.Count == 0)
            {
                return;
            }
            for (int i = 0; i < currentFeature.Neighbors.Count; i++)
            {
                Depth_travel_helper(currentFeature.Neighbors[i].Item1, ref speak);
            }
        }

        private string Depth_travel(FeatureGraph myGraph)
        {
            string speak = "";
            Depth_travel_helper(myGraph.Root, ref speak);
            return speak;
        }
    }
}
