using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LostManuscriptII;

namespace Lost_Manuscript_II_Data_Entry
{
    class FeatureSpeaker
    {
        public FeatureSpeaker()
        { }

        public string getChildSpeak(Feature toSpeak)
        {
            string result = toSpeak.Data;
            Random myRand = new Random();
            List<string> startOptions = new List<string>();
            startOptions.Add(" is a very interesting topic. ");
            startOptions.Add(" is important, a lot can be said about it. ");
            startOptions.Add(" is something important to me. ");
            result += startOptions[myRand.Next(0, 3)];
            if (toSpeak.Neighbors.Count == 0)
            {
                return result;
            }

            //select the elements
            List<Tuple<Feature, double>> toDirect = toSpeak.Neighbors;
            if (toSpeak.Neighbors.Count > 5)
            {
                toDirect = new List<Tuple<Feature,double>>();
                int choice = 0;
                while(toDirect.Count < 5)
                {
                    choice = myRand.Next(0, 5);
                    if(!toDirect.Contains(toSpeak.Neighbors[choice]))
                    {
                        toDirect.Add(toSpeak.Neighbors[choice]);
                    }
                }
            }
            //make the string
            result += "There are several things, mainly ";
            for (int x = 0; x < toDirect.Count; x++)
            {
                result += toDirect[x].Item1.Data;
                if (x + 1 < toDirect.Count)
                {
                    result += ", ";
                    if (x + 2 == toDirect.Count)
                    {
                        result += " and ";
                    }
                }
                else
                {
                    result += ".";
                }
            }
            return result;
        }

        public string getTagSpeak(Feature toSpeak)
        { /*
                Attribute (has)
                Base (in)
                Base Past (in)
                Date (on)
                Property (is)
                Directive (by)
           */
            if (toSpeak.Tags.Count == 0)
            {
                return "__EOT__";
            }
            string result = "Here are some interesting facts about " + toSpeak.Data + ". ";
            for (int x = 0; x < toSpeak.Tags.Count; x++)
            {
                if (toSpeak.Tags[x].Item3 == "Attribute (has)")
                {
                    result += "The " + toSpeak.Tags[x].Item1 + " has " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Base (in)")
                {
                    result += "It is " + toSpeak.Tags[x].Item1 + " in " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Base Past (in)")
                {
                    result += "It was " + toSpeak.Tags[x].Item1 + " in " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Date (on)")
                {
                    result += "It is " + toSpeak.Tags[x].Item1 + " on " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Property (is)")
                {
                    result += "It's " + toSpeak.Tags[x].Item1 + " is " + toSpeak.Tags[x].Item2 + ". ";
                }
                if (toSpeak.Tags[x].Item3 == "Directive (by)")
                {
                    result += "It is " + toSpeak.Tags[x].Item1 + " by " + toSpeak.Tags[x].Item2 + ". ";
                }
            }
            return result;
        }
    }
}
