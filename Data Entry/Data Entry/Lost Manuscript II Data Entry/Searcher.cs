using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dialogue_Data_Entry;

namespace Dialogue_Data_Entry
{
    public partial class Searcher : Form
    {
        private FeatureGraph featGraph;
        private Form1 myParent;
        public Searcher(FeatureGraph newGraph, Form1 parent)
        {
            myParent = parent;
            featGraph = newGraph;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fillSearchResults();
        }
        private void fillSearchResults()
        {
            listBox1.Items.Clear();
            List<string> Everything = new List<string>();
            string query = textBox1.Text;
            if (comboBox1.Text == "Everything")
            {
                Everything.AddRange(getFeatureResults(query));
                Everything.AddRange(getTagKeyResults(query));
                Everything.AddRange(getTagValueResults(query));
            }
            else if (comboBox1.Text == "Features")
            {
                Everything.AddRange(getFeatureResults(query));
            }
            else if (comboBox1.Text == "Tags")
            {
                Everything.AddRange(getTagKeyResults(query));
                Everything.AddRange(getTagValueResults(query));
            }
            else if (comboBox1.Text == "Tag Keys")
            {
                Everything.AddRange(getTagKeyResults(query));
            }
            else if (comboBox1.Text == "Tag Values")
            {
                Everything.AddRange(getTagValueResults(query));
            }
            else
            {
                listBox1.Items.Clear();
                listBox1.Items.AddRange(new object[] { "Unknown search parameters entered, no results returned." });
                listBox1.Refresh();
                return;
            }
            if (Everything.Count == 0)
            {
                listBox1.Items.AddRange(new object[] { "Finished Searching the Feature Graph with no results" });
            }
            else
            {
                listBox1.Items.AddRange((object[])Everything.ToArray());
                listBox1.Refresh();
            }
        }

        private List<string> getFeatureResults(string query)
        {
            List<string> result = new List<string>();
            //result.Add("========== Features ==========");
            for (int x = 0; x < featGraph.Features.Count; x++)
            {
                if(featGraph.Features[x].Name.ToLower().Contains(query.ToLower()))
                {
                    result.Add("Feature: (" + featGraph.Features[x].Id + ")");
                }
            }
            return result;
        }
        private List<string> getTagKeyResults(string query)
        {
            List<string> result = new List<string>();
            //result.Add("========== Tag Keys ==========");
            for (int x = 0; x < featGraph.Features.Count; x++)
            {
                for (int y = 0; y < featGraph.Features[x].Tags.Count; y++)
                {
                    if (featGraph.Features[x].Tags[y].Item1.ToLower().Contains(query.ToLower()))
                    {
                        result.Add("Feature: (" + featGraph.Features[x].Id + ") \tTag Key: <" + featGraph.Features[x].Tags[y].Item1 + ", " + featGraph.Features[x].Tags[y].Item2 + ">");
                    }
                }
            }
            return result;
        }
        private List<string> getTagValueResults(string query)
        {
            List<string> result = new List<string>();
            //result.Add("========== Tag Values ==========");
            for (int x = 0; x < featGraph.Features.Count; x++)
            {
                for (int y = 0; y < featGraph.Features[x].Tags.Count; y++)
                {
                    if (featGraph.Features[x].Tags[y].Item2.ToLower().Contains(query.ToLower()))
                    {
                        result.Add("Feature: (" + featGraph.Features[x].Id + ") \tTag Value: <" + featGraph.Features[x].Tags[y].Item1 + ", " + featGraph.Features[x].Tags[y].Item2 + ">");
                    }
                }
            }
            return result;
        }
        //button to open selection in search funciton
        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.Items[0].ToString() == "Finished Searching the Feature Graph with no results" ||
                listBox1.Items[0].ToString() == "Unknown search parameters entered, no results returned.")
            {
                MessageBox.Show("There are no results to present, please try a different search", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("You have not selected anything to view, please select something to view it", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                string featId = "";
                string tagData = "";
                try { featId = listBox1.SelectedItem.ToString().Split('(')[1].Split(')')[0]; }
                catch (Exception) { }
                try { tagData = listBox1.SelectedItem.ToString().Split('<')[1].Split(',')[0]; }
                catch (Exception) { }
                myParent.openFeature(featId, tagData);
                this.Close();
            }
        }
        private void textBox1_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Enter)
            {
                fillSearchResults();
            }
        }
    }
}
