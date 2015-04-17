using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Dialogue_Data_Entry
{
    public partial class Form3 : Form
    {

        public Form3(string from, string to,string relationshipFT="",string relationshipTF="",string weight="")
        {
            InitializeComponent();
            this.label1.Text = "Type in the key word of relationship between " +from+ " and "+to+".";
            this.label2.Text = from+" --> "+to;
            this.label3.Text = to + " --> " + from;
            this.comboBoxRelationshipFT.Text = relationshipFT;
            this.comboBoxRelationshipTF.Text = relationshipTF;
            this.textBox1.Text = weight;
        }
    }
}
