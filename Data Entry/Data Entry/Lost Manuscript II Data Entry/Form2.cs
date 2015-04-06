using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LostManuscriptII;
using System.IO;
using System.Collections;

namespace Lost_Manuscript_II_Data_Entry
{
    public partial class Form2 : Form
    {
        private FeatureGraph featGraph;
        private QueryController myController;
        private float featureWeight;
        private float tagKeyWeight;

        public Form2(FeatureGraph myGraph)
        {
            InitializeComponent();
            myController = new QueryController(myGraph);
            featGraph = myGraph;
            featureWeight = .6f;
            tagKeyWeight = .2f;
            chatBox.AppendText("Hello, and Welcome to the Query. \r\n");
            inputBox.KeyDown += new KeyEventHandler(this.inputBox_KeyDown);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void inputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                query_Click(sender, e);
            }
        }

        private void query_Click(object sender, EventArgs e)
        {
            string query = inputBox.Text;
            if (myController == null)
            {
                myController = new QueryController(featGraph);
            }
            chatBox.AppendText(myController.makeQuery(query) +"\r\n");
            inputBox.Clear();
        }
    }
}
