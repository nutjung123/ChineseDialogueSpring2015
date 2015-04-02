using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using LostManuscriptII;
using System.Windows.Forms;

namespace Lost_Manuscript_II_Data_Entry
{

    class XMLFilerForFeatureGraph
    {
        public static XmlDocument docOld = new XmlDocument();
        public static bool writeFeatureGraph(FeatureGraph toWrite, string fileName)
        {
            try
            {
                StreamWriter writer = new StreamWriter(fileName);
                writer.WriteLine("<AIMind>");
                if (toWrite.Root != null)
                {
                    writer.WriteLine("<Root id=\"" + toWrite.getFeatureIndex(toWrite.Root.Data) + "\"/>");
                }
                for (int x = 0; x < toWrite.Features.Count; x++)
                {
                    Feature tmp = toWrite.Features[x];
                    writer.WriteLine("<Feature id=\"" + x + "\" data=\"" + tmp.Data + "\">");
                    for (int y = 0; y < tmp.Neighbors.Count; y++)
                    {
                        int id = toWrite.getFeatureIndex(tmp.Neighbors[y].Item1.Data);
                        writer.WriteLine("<neighbor dest=\"" + id + "\" weight=\"" + tmp.Neighbors[y].Item2 + "\"/>");
                    }
                    List<Tuple<string, string, string>> tags = tmp.Tags;
                    for (int y = 0; y < tags.Count; y++)
                    {
                        writer.WriteLine("<tag key=\"" + tags[y].Item1 + "\" value=\"" + tags[y].Item2 + "\" type=\"" + tags[y].Item3 + "\"/>");
                    }
                    List<string> speaks = tmp.Speaks;
                    for (int y = 0; y < speaks.Count; y++)
                    {
                        writer.WriteLine("<speak value=\"" + speaks[y] + "\"/>");
                    }
                    writer.WriteLine("</Feature>");
                }
                writer.WriteLine("</AIMind>");
                writer.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        public static FeatureGraph readFeatureGraph(string toReadPath)
        {
            try
            {
                FeatureGraph result = new FeatureGraph();
                XmlDocument doc = new XmlDocument();
                doc.Load(toReadPath);
                docOld = doc;
                XmlNodeList features = doc.SelectNodes("AIMind");
                features = features[0].SelectNodes("Feature");
                foreach (XmlNode node in features)
                {
                    string data = node.Attributes["data"].Value;
                    result.addFeature(new Feature(data));
                }
                foreach (XmlNode node in features)
                {
                    Feature tmp = result.getFeature(node.Attributes["data"].Value);
                    XmlNodeList neighbors = node.SelectNodes("neighbor");
                    foreach (XmlNode neighborNode in neighbors)
                    {
                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);
                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                        tmp.addNeighbor(result.Features[id], weight);

                        result.Features[id].Parents.Add(tmp);
                    }
                    XmlNodeList tags = node.SelectNodes("tag");
                    foreach (XmlNode tag in tags)
                    {
                        string key = tag.Attributes["key"].Value;
                        string val = tag.Attributes["value"].Value;
                        string type = tag.Attributes["type"].Value;
                        tmp.addTag(key, val, type);
                    }
                    XmlNodeList speaks = node.SelectNodes("speak");
                    foreach (XmlNode speak in speaks)
                    {
                        tmp.addSpeak(speak.Attributes["value"].Value);
                    }
                }
                int rootId = -1;
                try
                {
                    features = doc.SelectNodes("AIMind");
                    rootId = Convert.ToInt32(features[0].SelectNodes("Root")[0].Attributes["id"].Value);
                }
                catch (Exception) { }
                if (rootId != -1) { result.Root = result.getFeature(rootId); }
                return result;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return null;
            }
        }
        /* merge two files*/
        public static FeatureGraph readFeatureGraph2(string toReadPath)
        {
            try
            {
                FeatureGraph result = new FeatureGraph();
                XmlDocument doc = new XmlDocument();//doc is the second document, the one selected to merge with after a file has been opened
                doc.Load(toReadPath);
                XmlNodeList features = doc.SelectNodes("AIMind");
                features = features[0].SelectNodes("Feature");
                int countUp = 0;
                int countD = 0;
                XmlNodeList features2 = docOld.SelectNodes("AIMind");
                if (features2[0] != null)//if the first document opened has features
                {
                    features2 = features2[0].SelectNodes("Feature");///this is put here because it would cause a crash outside if there were no features
                    foreach (XmlNode node in features2)
                    {
                        string data = node.Attributes["data"].Value;
                        result.addFeature(new Feature(data));
                        countUp++;
                    }
                }
                foreach (XmlNode node in features)
                {
                    string data = node.Attributes["data"].Value;
                    result.addFeature(new Feature(data));
              
                    countD++;
                }

                docOld = doc;
                //after loading the data from the two documents, run through the nodes found
                foreach (XmlNode node in features)
                {
                    Feature tmp = result.getFeature(node.Attributes["data"].Value);
                    XmlNodeList neighbors = node.SelectNodes("neighbor");
                    foreach (XmlNode neighborNode in neighbors)
                    {
                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value) + countUp;// + countUp);
                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                        tmp.addNeighbor(result.Features[id], weight);
                        result.Features[id].Parents.Add(tmp);//add neighbors to node
                    }
                    XmlNodeList tags = node.SelectNodes("tag");
                    foreach (XmlNode tag in tags)
                    {
                        string key = tag.Attributes["key"].Value;
                        string val = tag.Attributes["value"].Value;
                        string type = "0";
                        if (tag.Attributes["type"].Value == null)
                        {
                            type = "0";
                        }
                        else
                        {
                            type = tag.Attributes["type"].Value;
                        }
                        tmp.addTag(key, val, type);
                    }
                    XmlNodeList speaks = node.SelectNodes("speak");
                    foreach (XmlNode speak in speaks)
                    {
                        tmp.addSpeak(speak.Attributes["value"].Value);
                    }
                }

                foreach (XmlNode node in features2){
                    Feature tmp = result.getFeature(node.Attributes["data"].Value);
                    XmlNodeList neighbors = node.SelectNodes("neighbor");
                    foreach (XmlNode neighborNode in neighbors){
                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp);
                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                        tmp.addNeighbor(result.Features[id], weight);
                        result.Features[id].Parents.Add(tmp);
                    }
                    XmlNodeList tags = node.SelectNodes("tag");
                    foreach (XmlNode tag in tags){
                        string key = tag.Attributes["key"].Value;
                        string val = tag.Attributes["value"].Value;
                        string type = "0";
                        if (tag.Attributes["type"].Value == null){
                            type = "0";
                        }else{
                            type = tag.Attributes["type"].Value;
                        }
                        tmp.addTag(key, val, type);
                    }
                    XmlNodeList speaks = node.SelectNodes("speak");
                    foreach (XmlNode speak in speaks){
                        tmp.addSpeak(speak.Attributes["value"].Value);
                    }
                }

                bool doneWithMerge = false;
              //  string[] first1a = new string[5000];
               // string[] first1b = new string[5000];
                //int countToPreventOverlap = 0;
                foreach (XmlNode node in features){
                    foreach (XmlNode node2 in features2){
                        string data = node.Attributes["data"].Value;
                        string data2 = node2.Attributes["data"].Value;
                       
                        if (data == data2  && doneWithMerge == false){//if the node is found in both 
                            int idb = 0;
                            int idb2 = 0;
                            idb = Convert.ToInt32(node.Attributes["id"].Value);
                            idb2 = Convert.ToInt32(node2.Attributes["id"].Value);
                            string[] keys1a = new string[100];
                            string[] vals1a = new string[100];
                            string[] types1a = new string[100];
                            int countTheTag = 0;
                            string da = " ";
                            Feature tmp = new Feature(da);/// = result.getFeature(node.Attributes["data"].Value);
                               // if (idb > idb2){

                                     

                                    XmlNodeList neighbors = node.SelectNodes("neighbor");
                                    foreach (XmlNode neighborNode in neighbors)
                                    {
                                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp);
                                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                                        tmp.addNeighbor(result.Features[id], weight);
                                        result.Features[id].Parents.Add(tmp);
                                    }
                                    neighbors = node2.SelectNodes("neighbor");
                                    foreach (XmlNode neighborNode in neighbors)
                                    {
                                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp);
                                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                                        tmp.addNeighbor(result.Features[id], weight);
                                        result.Features[id].Parents.Add(tmp);
                                    }
                               
                                    XmlNodeList tags = node.SelectNodes("tag");
                                    foreach (XmlNode tag in tags)
                                    {
                                        string key = tag.Attributes["key"].Value;
                                        string val = tag.Attributes["value"].Value;
                                        string type = "0";
                                        if (tag.Attributes["type"].Value == null)
                                        {
                                            type = "0";
                                        }
                                        else
                                        {
                                            type = tag.Attributes["type"].Value;
                                        }
                                        ///tmp.editExistingTag(key, val, type, false);
                                       // tmp.addTag(key, val, type);
                                        keys1a[countTheTag] = key;
                                        vals1a[countTheTag] = val;
                                        types1a[countTheTag] = type;
                                        countTheTag++;
                                    }
                                    XmlNodeList tags2 = node2.SelectNodes("tag");
                                    foreach (XmlNode tag2 in tags2)
                                    {
                                        string key = tag2.Attributes["key"].Value;
                                        string val = tag2.Attributes["value"].Value;
                                        string type = "0";
                                        if (tag2.Attributes["type"].Value == null)
                                        {
                                            type = "0";
                                        }
                                        else
                                        {
                                            type = tag2.Attributes["type"].Value;
                                        }
                                       /// tmp.editExistingTag(key, val, type, false);
                                        ///tmp.addTag(key, val, type);
                                        keys1a[countTheTag] = key;
                                        vals1a[countTheTag] = val;
                                        types1a[countTheTag] = type;
                                        countTheTag++;
                                    }

                                    XmlNodeList speaks = node.SelectNodes("speak");
                                    foreach (XmlNode speak in speaks)
                                    {
                                        tmp.addSpeak(speak.Attributes["value"].Value);
                                    }
                                    speaks = node2.SelectNodes("speak");
                                    foreach (XmlNode speak in speaks)
                                    {
                                        tmp.addSpeak(speak.Attributes["value"].Value);
                                    }
                              //  } 
                        //   result.removeDouble(data2);
                                    
                       // }
                                //result.removeDouble(data);
                                for (int i = 0; i < countTheTag; i++)
                                {
                                    tmp.addTag(keys1a[i], vals1a[i], types1a[i]);

                                }
                               // result.removeDouble(data);
                        }
                    }
                }

                 




                int rootId = -1;
                try
                {
                    features = doc.SelectNodes("AIMind");
                    rootId = Convert.ToInt32(features[0].SelectNodes("Root")[0].Attributes["id"].Value);
                }
                catch (Exception) { }
                if (rootId != -1) { result.Root = result.getFeature(rootId); }
                return result;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return null;
            }
        }
    }
}
