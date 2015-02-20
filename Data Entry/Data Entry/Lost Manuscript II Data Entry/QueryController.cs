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
        public QueryController(FeatureGraph graph)
        {
            featGraph = graph;
            tellMeMore = "";
        }

        public string makeQuery(string query)
        {
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
            FeatureSpeaker mySpeaker = new FeatureSpeaker();
            tellMeMore = mySpeaker.getTagSpeak(featuresDict.Keys.ElementAt(maxIndex));
            return mySpeaker.getChildSpeak(featuresDict.Keys.ElementAt(maxIndex));
            }
            catch
            {
                FeatureSpeaker mySpeaker = new FeatureSpeaker();
                tellMeMore = mySpeaker.getTagSpeak(featGraph.Root);
                return mySpeaker.getChildSpeak(featGraph.Root);
            }
        }



        public List<Feature> getFeatureResults(string query)
        {
            List<Feature> result = new List<Feature>();
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
        public List<Feature> getTagKeyResults(string query)
        {
            List<Feature> result = new List<Feature>();
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
    }
}
