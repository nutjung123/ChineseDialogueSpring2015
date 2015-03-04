using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LostManuscriptII;

namespace Lost_Manuscript_II_Data_Entry
{
    class QueryController
    {
        private FeatureGraph featGraph;
        private string tellMeMore;
        private int turn;
        private Feature currentTopic;
        public QueryController(FeatureGraph graph)
        {
            featGraph = graph;
            tellMeMore = "";
            turn = 1;
            currentTopic = null;
        }

        public string makeQuery(string query)
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
