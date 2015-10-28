using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Dialogue_Data_Entry;
using System.Windows.Forms;
using System.Security;

namespace Dialogue_Data_Entry
{

    class XMLFilerForFeatureGraph
    {
        public static XmlDocument docOld = new XmlDocument();

        public static string escapeInvalidXML(string s)
        {
            if (s == null)
            {
                return s;
            }
            return SecurityElement.Escape(s);
        }

        public static string unEscapeInvalidXML(string s)
        {
            if (s == null)
            {
                return s;
            }
            string str = s;
            str = str.Replace("&apos;", "'");
            str = str.Replace("&quot;", "\""); 
            str = str.Replace("&gt;", ">");
            str = str.Replace("&lt;", "<");
            str = str.Replace("&amp;", "&");
            return str;
        }

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
                    writer.WriteLine("<Feature id=\"" + x + "\" data=\"" + escapeInvalidXML(tmp.Data) + "\">");
                    //Neighbor
                    for (int y = 0; y < tmp.Neighbors.Count; y++)
                    {
                        int id = toWrite.getFeatureIndex(tmp.Neighbors[y].Item1.Data);
                        writer.WriteLine("<neighbor dest=\"" + id + "\" weight=\"" + tmp.Neighbors[y].Item2 + "\" relationship=\"" + escapeInvalidXML(tmp.Neighbors[y].Item3) + "\"/>");
                    }
                    //Parent 
                    for (int y = 0; y < tmp.Parents.Count; y++)
                    {
                        int id = toWrite.getFeatureIndex(tmp.Parents[y].Item1.Data);
                        writer.WriteLine("<parent dest=\"" + id + "\" weight=\"" + tmp.Parents[y].Item2 + "\" relationship=\"" + escapeInvalidXML(tmp.Parents[y].Item3) + "\"/>");
                    }
                    //Tag
                    List<Tuple<string, string, string>> tags = tmp.Tags;
                    for (int y = 0; y < tags.Count; y++)
                    {
                        string toWriteTag = "<tag key=\"" + escapeInvalidXML(tags[y].Item1);
                        toWriteTag += "\" value=\"" + escapeInvalidXML(tags[y].Item2);
                        toWriteTag += "\" type=\"" + escapeInvalidXML(tags[y].Item3) + "\"/>";
                        writer.WriteLine(toWriteTag);
                    }
                    //Speak
                    List<string> speaks = tmp.Speaks;
                    for (int y = 0; y < speaks.Count; y++)
                    {
                        writer.WriteLine("<speak value=\"" + escapeInvalidXML(speaks[y]) + "\"/>");
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
                    string data = unEscapeInvalidXML(node.Attributes["data"].Value);
                    result.addFeature(new Feature(data));
                }
                foreach (XmlNode node in features)
                {
                    Feature tmp = result.getFeature(node.Attributes["data"].Value);
                    
                    //Check whether this feature also encodes a state
                    if (node.Attributes["state"].Value.Equals("true"))
                    {
                        tmp.is_state = true;
                    }//end if
                    else
                    {
                        tmp.is_state = false;
                    }//end else

                    //Neighbor
                    XmlNodeList neighbors = node.SelectNodes("neighbor");
                    foreach (XmlNode neighborNode in neighbors)
                    {
                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);
                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                        string relationship = "";
                        if (neighborNode.Attributes["relationship"] != null)
                        {
                            relationship = unEscapeInvalidXML(Convert.ToString(neighborNode.Attributes["relationship"].Value));
                        }
                        tmp.addNeighbor(result.Features[id], weight,relationship);

                        //pre-process in case no parent exist
                        foreach (XmlNode tempNode in features)
                        {
                            if (tempNode.Attributes["data"].Value == result.Features[id].Data)
                            {
                                XmlNodeList tempParents = tempNode.SelectNodes("parent");
                                if (tempParents.Count == 0)
                                {
                                    result.Features[id].addParent(tmp);
                                }
                            }
                        }
                        //result.Features[id].addNeighbor(tmp,weight);
                    }
                    //Parent
                    XmlNodeList parents = node.SelectNodes("parent");
                    foreach (XmlNode parentNode in parents)
                    {
                        int id = Convert.ToInt32(parentNode.Attributes["dest"].Value);
                        double weight = Convert.ToDouble(parentNode.Attributes["weight"].Value);
                        string relationship = "";
                        if (parentNode.Attributes["relationship"] != null)
                        {
                            relationship = unEscapeInvalidXML(Convert.ToString(parentNode.Attributes["relationship"].Value));
                        }
                        tmp.addParent(result.Features[id], weight, relationship);
                    }
                    //Tag
                    XmlNodeList tags = node.SelectNodes("tag");
                    foreach (XmlNode tag in tags)
                    {
                        string key = unEscapeInvalidXML(tag.Attributes["key"].Value);
                        string val = unEscapeInvalidXML(tag.Attributes["value"].Value);
                        string type = unEscapeInvalidXML(tag.Attributes["type"].Value);
                        tmp.addTag(key, val, type);
                    }
                    //Speak
                    XmlNodeList speaks = node.SelectNodes("speak");
                    foreach (XmlNode speak in speaks)
                    {
                        tmp.addSpeak(unEscapeInvalidXML(speak.Attributes["value"].Value));
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
                int countUp2 = 0;
                int countD = 0;
                XmlNodeList features2 = docOld.SelectNodes("AIMind");



                if (features2[0] != null)
                {//if the first document opened has features{
                    features2 = features2[0].SelectNodes("Feature");///this is put here because it would cause a crash outside if there were no features
                    foreach (XmlNode node in features2)
                    {
                        string data = node.Attributes["data"].Value;
                        result.addFeature(new Feature(data));
                        countD++;
                    }
                }
                foreach (XmlNode node in features){
                        bool checkifDuplicatesExist = false;
                        foreach (XmlNode nodePrime in features2){                      
                            string dataa = node.Attributes["data"].Value;                      
                            string data2 = nodePrime.Attributes["data"].Value;                       
                            if (dataa == data2)                    //if there are two datas with the same name, merge them
                            {                         
                                checkifDuplicatesExist = true;                       
                            }                   
                        }
                    if (checkifDuplicatesExist == false){//if there doesn't exist a version of the feature, add one
                        countUp++;
                        string data = node.Attributes["data"].Value;
                        result.addFeature(new Feature(data));
                        Feature tmp = result.getFeature(node.Attributes["data"].Value);
                         XmlNodeList neighbors = node.SelectNodes("neighbor");
                        foreach (XmlNode neighborNode in neighbors){
                                int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp);
                                double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                                tmp.addNeighbor(result.Features[id], weight);
                                result.Features[id].addParent(tmp);
                                //result.Features[id].addNeighbor(tmp, weight);
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
                          
                        foreach (XmlNode speak in speaks){
                                tmp.addSpeak(speak.Attributes["value"].Value);
                            }
                        
                    }
                    else
                    {
                        countUp++;
                        Feature tmp = result.getFeature(node.Attributes["data"].Value);

                        XmlNodeList neighbors = node.SelectNodes("neighbor");
                        foreach (XmlNode neighborNode in neighbors)
                        {
                            int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp);
                            double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                            tmp.addNeighbor(result.Features[id], weight);
                            result.Features[id].addParent(tmp);
                            //result.Features[id].addNeighbor(tmp,weight);
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

                    }
                    
                }


                docOld = doc;
                //after loading the data from the two documents, run through the nodes found
                foreach (XmlNode node in features2)///add the features from the second file
                {
                    Feature tmp = result.getFeature(node.Attributes["data"].Value);
                    XmlNodeList neighbors = node.SelectNodes("neighbor");

                   string secDet = Convert.ToString(Convert.ToInt32(node.Attributes["id"].Value) + countUp);
                   

                    foreach (XmlNode neighborNode in neighbors)
                    {
                        int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value) +countUp;// +0 + 1;
                        double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                        
                          tmp.addNeighbor(result.Features[id], weight);
                          result.Features[id].addParent(tmp);//add neighbors to node
                        //result.Features[id].addNeighbor(tmp,weight);
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

                foreach (XmlNode node in features2)
                {
          
                }

             /*   foreach (XmlNode node in features)
                {
                    foreach (XmlNode node2 in features2)
                    {
                        if (node.Attributes["data"] != null && node2.Attributes["data"] != null)
                        {
                            string data = node.Attributes["data"].Value;
                            string data2 = node2.Attributes["data"].Value;

                            if (data == data2)
                            {
                                XmlNode nodea = node.CloneNode(true);
                                string dataa = nodea.Attributes["data"].Value;
                                Feature tmp = result.getFeature(nodea.Attributes["data"].Value);  
                                result.addFeature(new Feature(dataa));

                                node.RemoveAll();
                                node2.RemoveAll();
                                result.removeDouble(data);
                                result.removeDouble(data2);
                                XmlNodeList neighbors = nodea.SelectNodes("neighbor");
                                foreach (XmlNode neighborNode in neighbors){
                                    int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp);
                                    double weight = Convert.ToDouble(neighborNode.Attributes["weight"].Value);
                                    tmp.addNeighbor(result.Features[id], weight);
                                    result.Features[id].Parents.Add(tmp);
                                }
                                XmlNodeList tags = nodea.SelectNodes("tag");
                                foreach (XmlNode tag in tags){
                                    string key = tag.Attributes["key"].Value;
                                    string val = tag.Attributes["value"].Value;
                                    string type = "0";
                                    type = tag.Attributes["type"].Value;
                                    tmp.removeTag(key);
                                    tmp.addTag(key, val, type);
                                } 

                            }
                        }
                    }
                }
                /*
                bool doneWithMerge = false;
                foreach (XmlNode node in features){
                    foreach (XmlNode node2 in features2){
                        if (node.Attributes["data"] != null && node2.Attributes["data"] != null){
                        string data = node.Attributes["data"].Value;
                        string data2 = node2.Attributes["data"].Value;

                        if (data == data2 && doneWithMerge == false)
                        {//if the node is found in both 

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
                          /*  XmlNodeList neighbors = node.SelectNodes("neighbor");
                            foreach (XmlNode neighborNode in neighbors)
                            {
                                int id = Convert.ToInt32(neighborNode.Attributes["dest"].Value);// +countUp;// + countUp)      
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



                            //     result.removeDouble(data);
                            //   result.removeDouble(data2);
                            string da2 = " ";
                            Feature tmp2 = new Feature(da2);

                            for (int i = 0; i < countTheTag; i++)
                            {
                                // tmp2.addTag(keys1a[i], vals1a[i], types1a[i]);

                            }
                            node.Attributes["data"].Value = null;
                            node.Attributes["id"].Value = null;
                            //  result.removeDouble(data);
                            node.RemoveAll();
                            node2.RemoveAll();
                        }
                        }
                    }
                }

                 */




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
