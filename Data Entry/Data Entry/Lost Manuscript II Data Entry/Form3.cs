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

        public Form3(string from, string to,string relationship="")
        {
            InitializeComponent();
            this.label1.Text = "Type in the key word of relationship between " +from+ " and "+to+".";
            this.comboBoxRelationship.Text = relationship;
        }
    }
}
