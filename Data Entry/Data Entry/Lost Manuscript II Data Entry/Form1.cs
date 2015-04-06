using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using LostManuscriptII;

namespace Lost_Manuscript_II_Data_Entry
{
    public partial class Form1 : Form
    {
        private FeatureGraph featGraph;
        private Feature toChange;
        private QueryController myQController;
        private string editorFeatureSelected;
        private string editorKeySelected;
        private string currentFileName;
        private string currentQueryFolderName;
        private int queryCounter;
        private int selectedIndex;
        private const string BAD_CHARS = "<>,()\"";
        private int tIndex = -1;
        private ToolTip toolTip1;
        private bool shouldIgnoreCheckEvent;
        private Form2 myQuery;

        public Form1()
        {
            queryCounter = 0;
            selectedIndex = -1;
            myQController = new QueryController(featGraph);
            featGraph = new FeatureGraph();
            toChange = null;
            currentFileName = "";
            InitializeComponent();
            //set up tooltip
            toolTip1 = new ToolTip();
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;

            //Add enter method
            textBox1.KeyDown += new KeyEventHandler(this.featureCreateTextBox1_KeyDown);
            //Add tooltip method to checkedListBox2
            checkedListBox2.MouseHover += new EventHandler(checkedListBox2_MouseHover);
            checkedListBox2.MouseMove += new MouseEventHandler(checkedListBox2_MouseMove);
            //check event handle
            checkedListBox2.ItemCheck += new ItemCheckEventHandler(CheckedListBox2_ItemCheck);
            //Add tooltip method to createTagComboBox
            createTagComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            createTagComboBox.DrawItem += createTagComboBox_DrawItem;
            createTagComboBox.DropDownClosed += createTagComboBox_DropDownClosed;
            //Add tooltip method to editTagComboBox
            editTagComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            editTagComboBox.DrawItem += editTagComboBox_DrawItem;
            editTagComboBox.DropDownClosed += createTagComboBox_DropDownClosed;

            shouldIgnoreCheckEvent = true;
            if (backgroundWorker1.IsBusy != true)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        //All of the operations for the File dropdown menu
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            featGraph = new FeatureGraph();
            toChange = null;
            selectedIndex = -1;
            editorFeatureSelected = "";
            refreshAll();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Choose the XML file you want to load";
            openFileDialog1.FileName = "";
            openFileDialog1.Multiselect = false;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Filter = "XML File|*.xml|All Files|*";
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != "" && openFileDialog1.CheckFileExists)
            {
                currentFileName = openFileDialog1.FileName;
                featGraph = XMLFilerForFeatureGraph.readFeatureGraph(openFileDialog1.FileName);
                selectedIndex = -1;
                refreshAllButUpdateFeature();
                listBox2.Items.Clear();
                listBox3.Items.Clear();
                checkedListBox2.Items.Clear();
                clearAllTextBoxes();
                myQController = new QueryController(featGraph);
            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.saveAs();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentFileName != "")
            {
                XMLFilerForFeatureGraph.writeFeatureGraph(featGraph, currentFileName);
            }
            else
            {
                this.saveAs();

            }
        }
        private void saveAs()
        {
            saveFileDialog1.Title = "Save Feature Graph";
            saveFileDialog1.FileName = "";
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.AddExtension = false;
            saveFileDialog1.Filter = "XML File|*.xml|All Files|*";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                XMLFilerForFeatureGraph.writeFeatureGraph(featGraph, saveFileDialog1.FileName);
                currentFileName = saveFileDialog1.FileName;
            }
        }
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentFileName != "")
            {
                featGraph = XMLFilerForFeatureGraph.readFeatureGraph(currentFileName);
                refreshAllButUpdateFeature();
                listBox2.Items.Clear();
                checkedListBox2.Items.Clear();
                clearAllTextBoxes();
                myQController = new QueryController(featGraph);
            }
        }
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //All of the operations for the Search dropdown menu
        private void findFeatureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Searcher mySearcher = new Searcher(featGraph, this);
            mySearcher.Show();
        }

        //All of the operations for the View dropdown menu
        private void visualizerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            runVisualizer();
        }
        private void runVisualizer()
        {
            //Start everything up
            try
            {
                //
                System.IO.Directory.CreateDirectory("tmp");
                XMLFilerForFeatureGraph.writeFeatureGraph(featGraph, "tmp/toVis.xml");

                Process proc = new Process();
                proc.StartInfo.FileName = "python";
                proc.StartInfo.Arguments = "Visualizer\\main.py " + "tmp/toVis.xml";
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
                System.IO.File.Delete("tmp/toVis.xml");
                System.IO.Directory.Delete("tmp");
            }
            catch
            {
                MessageBox.Show("An unknown error occured with the visualizer");
            }
        }
        private void treeTrunkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeFrom myTreeForm = new TreeFrom(featGraph, false);
            if (myTreeForm.CanDisplay)
            {
                myTreeForm.Show();
            }
        }
        private void fullTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeFrom myTreeForm = new TreeFrom(featGraph, true);
            myTreeForm.Show();
        }

        //Helper functions to refresh listbox that contains a list of feature
        private void refreshFeatureListBox(ListBox toRefresh, string toIgnore = null)
        {
            toRefresh.Items.Clear();
            List<string> tmp = featGraph.getFeatureNames();
            for (int x = 0; x < tmp.Count; x++)
            {
                if (toIgnore != null && tmp[x] != toIgnore)
                {
                    toRefresh.Items.AddRange(new object[] { tmp[x] });
                }
                else if (toIgnore == null)
                {
                    toRefresh.Items.AddRange(new object[] { tmp[x] });
                }
            }
            toRefresh.Refresh();
        }
        private void refreshFeatureTagListBox(ListBox toRefresh)
        {
            toRefresh.Items.Clear();
            if (selectedIndex == -1) { toRefresh.Refresh(); return; }
            List<string> tmp = featGraph.Features[selectedIndex].getTagKeys();
            for (int x = tmp.Count-1; x >= 0; x--)
            {
                toRefresh.Items.AddRange(new object[] { tmp[x] });
            }
            toRefresh.Refresh();
        }
        private void refreshFeatureSpeaksListBox(ListBox toRefresh)
        {
            toRefresh.Items.Clear();
            if (selectedIndex==-1) { toRefresh.Refresh(); return; }
            List<string> tmp = featGraph.Features[selectedIndex].Speaks;
            for (int x = tmp.Count - 1; x >= 0; x--)
            {
                toRefresh.Items.AddRange(new object[] { tmp[x] });
            }
            toRefresh.Refresh();
        }
        public void refreshListBoxes()
        {
            refreshFeatureListBox(checkedListBox1);
            refreshFeatureListBox(checkedListBox2);
            refreshFeatureListBox(checkedListBox3);
            refreshFeatureListBox(listBox1);
            refreshFeatureTagListBox(listBox2);
            refreshFeatureSpeaksListBox(listBox3);
        }
        public void refreshAll()
        {
            clearAllTextBoxes();
            refreshListBoxes();
        }
        public void refreshAllButUpdateFeature()
        {
            refreshFeatureListBox(checkedListBox1);
            refreshFeatureListBox(checkedListBox3);
            refreshFeatureListBox(listBox1);
        }
        public void clearAllTextBoxes()
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            //textBox6.Clear();
            textBox7.Clear();
            maskedTextBox1.Clear();
        }
        //open feature from search function
        public void openFeature(string featureData, string tagData = "")
        {
            refreshListBoxes();
            tabControl1.SelectedIndex = 1;
            tabControl1.Refresh();
            toChange = featGraph.getFeature(featureData);
            if (toChange != null)
            {
                initEditor(toChange);
                listBox1.SelectedIndex = (indexOfIn(featureData, listBox1));
                selectedIndex = listBox1.SelectedIndex;
            }
            if (tagData != "")
            {
                textBox3.Text = tagData;
                editTagComboBox.Text = toChange.getTag(tagData).Item2;
                editorKeySelected = tagData;
                listBox2.SelectedIndex = (indexOfIn(tagData, listBox2));
            }
        }
        private int indexOfIn(string val, ListBox toSearch)
        {
            for (int x = 0; x < toSearch.Items.Count; x++)
            {
                if (toSearch.Items[x].ToString() == val) { return x; }
            }
            return -1;
        }
        private bool hasBadChar(string toCheck)
        {
            for (int x = 0; x < toCheck.Length; x++)
            {
                if (BAD_CHARS.Contains(toCheck[x])) { return true; }
            }
            return false;
        }

        //Feature Creation Methods
        private void featureCreateButton_Click(object sender, EventArgs e)
        {
            if (textBox1.Lines.Length == 0)
            {
                MessageBox.Show("You cannot create a feature with no name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string data = textBox1.Lines[0];
            if (featGraph.hasNodeData(data))
            {
                MessageBox.Show("You cannot create two features with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            textBox1.Clear();
            Feature toAdd = new Feature(data);
            for (int x = 0; x < checkedListBox1.CheckedItems.Count; x++)
            {
                toAdd.addNeighbor(featGraph.getFeature(checkedListBox1.CheckedItems[x].ToString()));
                featGraph.getFeature(checkedListBox1.CheckedItems[x].ToString()).Parents.Add(toAdd);
            }
            featGraph.addFeature(toAdd);
            refreshAllButUpdateFeature();
        }

        //Feature Creation Methods using Enter Key
        private void featureCreateTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //(Note): not sure will there be any problem when sending KeyEventArgs to EventArgs 
                featureCreateButton_Click(sender, e);
            }
        }

        //Feature Editor Methods
        private void featureUpdateButton_Click(object sender, EventArgs e)
        {
            if (selectedIndex == -1)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (textBox2.Lines.Length == 0 || textBox2.Text == "")
            {
                MessageBox.Show("You cannot set a feature with no name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (featGraph.hasNodeData(textBox2.Text) && editorFeatureSelected != textBox2.Text)
            {
                MessageBox.Show("You cannot create two features with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (editorFeatureSelected != "")
            {
                featGraph.Features[selectedIndex].Data = textBox2.Text;
                if (checkBox1.Checked)
                {
                    featGraph.Root = featGraph.getFeature(selectedIndex);
                }
                refreshAllButUpdateFeature();
                clearAllTextBoxes();
                checkedListBox2.Items.Clear();
                listBox2.Items.Clear();
                listBox3.Items.Clear();
                checkBox1.Checked = false;
                checkBox1.Refresh();
                editorFeatureSelected = "";
            }
        }
        //feature list selection
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            selectedIndex = listBox1.SelectedIndex;
            //toChange = featGraph.getFeature(listBox1.SelectedItem.ToString()).deepCopy();
            
            initEditor(featGraph.Features[selectedIndex]);
        }

        private void initEditor(Feature toEdit)
        {
            try
            {
                checkBox1.Checked = false;
                clearAllTextBoxes();
                createTagComboBox.Items.Clear();
                for (int i = 0; i < featGraph.Features.Count; i++)
                {
                    createTagComboBox.Items.Add(featGraph.Features[i].Data);
                }
                createTagComboBox.Text = "";
                editTagComboBox.Items.Clear();
                for (int i = 0; i < featGraph.Features.Count; i++)
                {
                    editTagComboBox.Items.Add(featGraph.Features[i].Data);
                }
                editTagComboBox.Text = "";
                maskedTextBox1.Text = toEdit.DiscussedThreshold.ToString();
                textBox2.Text = toEdit.Data;
                editorFeatureSelected = toEdit.Data;
                refreshFeatureListBox(checkedListBox2, toEdit.Data);
                refreshFeatureSpeaksListBox(listBox3);
                refreshFeatureTagListBox(listBox2);
                shouldIgnoreCheckEvent = true;
                for (int x = 0; x < toEdit.Neighbors.Count; x++)
                {
                    for (int y = 0; y < checkedListBox2.Items.Count; y++)
                    {
                        if (checkedListBox2.Items[y].ToString() == toEdit.Neighbors[x].Item1.Data)
                        {
                            checkedListBox2.SetItemChecked(y, true);
                        }
                    }
                }
                shouldIgnoreCheckEvent = false;
                checkedListBox2.Refresh();

                if (featGraph.Root != null && toEdit.Data == featGraph.Root.Data)
                {
                    checkBox1.Checked = true;
                }
                checkBox1.Refresh();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        //All of the operations needed for the tag editor
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedIndex == -1)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (listBox2.SelectedIndex != -1)
            {
                //setup the edit tag field
                string featureKey = listBox2.Items[listBox2.SelectedIndex].ToString();
                string keyValue = featGraph.Features[selectedIndex].getTag(featureKey).Item2;
                string type = featGraph.Features[selectedIndex].getTag(featureKey).Item3;
                textBox3.Text = featureKey;
                editTagComboBox.Text = keyValue;
                comboBox2.Text = type;
                editorKeySelected = featureKey;
            }
        }

        //create tag button
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (selectedIndex == -1)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (textBox6.Text == "" || createTagComboBox.Text == "")
            {
                MessageBox.Show("You cannot create a tag with an empty key or value", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (featGraph.Features[selectedIndex].hasTagWithKey(textBox6.Text))
            {
                MessageBox.Show("There is already a tag with that key in this feature\nPlease choose another key", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (hasBadChar(createTagComboBox.Text) || hasBadChar(textBox6.Text))
            {
                MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (comboBox1.Text == "")
            {
                MessageBox.Show("There is no selected attribute, please choose one from the drop down menue", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                featGraph.Features[selectedIndex].addTag(textBox6.Text, createTagComboBox.Text, comboBox1.Text);
            }
            textBox6.Text = "";
            comboBox1.Text = "";
            createTagComboBox.Text = "";
            refreshFeatureTagListBox(listBox2);
        }
        //edit tag button
        private void button2_Click_1(object sender, EventArgs e)
        {
            if (selectedIndex == -1 || editorKeySelected == "")
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (textBox3.Text == "" || editTagComboBox.Text == "")
            {
                MessageBox.Show("You cannot create a tag with an empty key or value", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (featGraph.Features[selectedIndex].hasTagWithKey(textBox3.Text) && editorKeySelected != textBox3.Text)
            {
                MessageBox.Show("There is already a tag with that key in this feature\nPlease choose another key", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (hasBadChar(textBox3.Text) || hasBadChar(editTagComboBox.Text))
            {
                MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else if (comboBox2.Text == "")
            {
                MessageBox.Show("There is no selected attribute, please choose one from the drop down menue", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                featGraph.Features[selectedIndex].removeTag(editorKeySelected);
                featGraph.Features[selectedIndex].addTag(textBox3.Text, editTagComboBox.Text, comboBox2.Text);
                textBox3.Text = "";
                comboBox2.Text = "";
                editTagComboBox.Text = "";
                refreshFeatureTagListBox(listBox2);
            }
         
        }
        //remove tag button
        private void button3_Click(object sender, EventArgs e)
        {
            if(editorKeySelected!=""&&selectedIndex!=-1){
                featGraph.Features[selectedIndex].removeTag(editorKeySelected);
                editorKeySelected = "";
                refreshFeatureTagListBox(listBox2);
            }
        }

        //tooltip helper function 
        private void checkedListBox2_MouseMove(object sender, MouseEventArgs e)
        {
            int index = checkedListBox2.IndexFromPoint(e.Location);
            if (tIndex != index)
            {
                GetToolTip();
            }
        }
        //tooltip helper function
        private void checkedListBox2_MouseHover(object sender, EventArgs e)
        {
            GetToolTip();
        }
        //show tooltip 
        private void GetToolTip()
        {
            Point pos = checkedListBox2.PointToClient(MousePosition);
            tIndex = checkedListBox2.IndexFromPoint(pos);
            if (tIndex > -1)
            {
                toolTip1.SetToolTip(checkedListBox2, checkedListBox2.Items[tIndex].ToString());
            }
        }

        //Two function to create tooltip for createTagComboBox
        private void createTagComboBox_DropDownClosed(object sender, EventArgs e)
        {
            toolTip1.Hide(createTagComboBox);
        }
        private void createTagComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) { return;  }
            string text = createTagComboBox.GetItemText(createTagComboBox.Items[e.Index]);
            e.DrawBackground();
            using (SolidBrush br = new SolidBrush(e.ForeColor))
            { e.Graphics.DrawString(text, e.Font, br, e.Bounds); }
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            { toolTip1.Show(text, createTagComboBox, e.Bounds.Right, e.Bounds.Bottom); }
            e.DrawFocusRectangle();
        }

        //Two functions to create tooltip for editTagComboBox
        private void editTagComBox_DropDownClosed(object sender, EventArgs e)
        {
            toolTip1.Hide(editTagComboBox);
        }
        private void editTagComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) { return; }
            string text = createTagComboBox.GetItemText(editTagComboBox.Items[e.Index]);
            e.DrawBackground();
            using (SolidBrush br = new SolidBrush(e.ForeColor))
            { e.Graphics.DrawString(text, e.Font, br, e.Bounds); }
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            { toolTip1.Show(text, editTagComboBox, e.Bounds.Right, e.Bounds.Bottom); }
            e.DrawFocusRectangle();
        }

        //Feature Removal Methods
        private void checkedListBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        
        private void removeFeatureButton_Click(object sender, EventArgs e)
        {
            if (checkedListBox3.CheckedItems.Count == 0)
            {
                MessageBox.Show("You have not selected any Features to remove", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            DialogResult diag = MessageBox.Show("Deleting a Feature could cause other Features that have connections to it fail as they no longer know where to link to.\n\nWould you like to remove all links TO this Feature as well?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            if (diag == DialogResult.No)
            {
                MessageBox.Show("No changes were made", "Update", MessageBoxButtons.OK ,MessageBoxIcon.Exclamation);
                return;
            } 
            
            for (int x = 0; x < checkedListBox3.CheckedItems.Count; x++)
            {
                featGraph.removeFeature(checkedListBox3.CheckedItems[x].ToString());
            }
            refreshAllButUpdateFeature();
        }

        private void viewToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }
        //unused function
        private void button4_Click(object sender, EventArgs e)
        {
            FeatureSpeaker mySpeaker = new FeatureSpeaker();
            MessageBox.Show(mySpeaker.getChildSpeak(toChange));
            MessageBox.Show(mySpeaker.getTagSpeak(toChange));
        }
        //Read query from file and write response to file
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                string queryFileName = currentQueryFolderName + "\\query" + queryCounter;
                string responseFileName = currentQueryFolderName + "\\response" + queryCounter;
                //MessageBox.Show(queryFileName + ":" + File.Exists(queryFileName));
                if (File.Exists(queryFileName))
                {
                    //MessageBox.Show(queryFileName);
                    var fs = File.Open(queryFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    var sr = new StreamReader(fs);
                    string query = sr.ReadToEnd();
                    string response = myQController.makeQuery(query);
                    fs.Close();
                    fs = File.Open(responseFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    var sw = new StreamWriter(fs);
                    sw.Write(response);
                    sw.Close();
                    queryCounter += 1;
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        private void designateQueryFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            currentQueryFolderName = folderBrowserDialog1.SelectedPath;
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void FeatureEditor_Click(object sender, EventArgs e)
        {

        }
        //Add speak button
        private void button4_Click_1(object sender, EventArgs e)
        {
            if (selectedIndex == -1)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (hasBadChar(this.textBox7.Text))
            {
                MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.textBox7.Text != "")
            {
                //toChange.addSpeak(this.textBox7.Text);
                featGraph.Features[selectedIndex].Speaks.Add(this.textBox7.Text);
            }
            refreshFeatureSpeaksListBox(listBox3);
            textBox7.Clear();
        }
        //Edit speak button
        private void button6_Click(object sender, EventArgs e)
        {
            if (selectedIndex == -1)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (hasBadChar(this.textBox7.Text))
            {
                MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.textBox7.Text != "")
            {
                //toChange.editSpeak(index, this.textBox7.Text);
                int editIndex = featGraph.Features[selectedIndex].Speaks.Count - listBox3.SelectedIndex - 1;
                featGraph.Features[selectedIndex].editSpeak(editIndex,this.textBox7.Text);
            }
            refreshFeatureSpeaksListBox(listBox3);
            textBox7.Clear();
        }
        //Remove speak button
        private void button5_Click(object sender, EventArgs e)
        {
            if (selectedIndex == -1)
            {
                MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (listBox3.SelectedIndex != -1)
            {
                //toChange.removeSpeak(toChange.Speaks.Count - listBox3.SelectedIndex - 1);
                int removedIndex = featGraph.Features[selectedIndex].Speaks.Count - listBox3.SelectedIndex - 1;
                featGraph.Features[selectedIndex].removeSpeak(removedIndex);
                refreshFeatureSpeaksListBox(listBox3);
            }
            textBox7.Clear();
        }

        //speak list box
        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex != -1)
            {
                textBox7.Text = (string)listBox3.Items[listBox3.SelectedIndex];
            }
        }

        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Choose the XML file you want to merge";
            openFileDialog1.FileName = "";
            openFileDialog1.Multiselect = false;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Filter = "XML File|*.xml|All Files|*";
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != "" && openFileDialog1.CheckFileExists)
            {
                currentFileName = openFileDialog1.FileName;

                featGraph = XMLFilerForFeatureGraph.readFeatureGraph2(openFileDialog1.FileName);
                refreshAllButUpdateFeature();
                listBox2.Items.Clear();
                checkedListBox2.Items.Clear();
                clearAllTextBoxes();
                myQController = new QueryController(featGraph);
            }
        }

        private void chatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myQuery = new Form2(featGraph);
            myQuery.Show();
        }

        // add neighbor checkedListBox method (+ parent)
        private void CheckedListBox2_ItemCheck(Object sender, ItemCheckEventArgs e)
        {
            if (!shouldIgnoreCheckEvent && selectedIndex!=-1)
            {
                if (e.NewValue == CheckState.Checked && e.CurrentValue == CheckState.Unchecked)
                {
                    //if check insert the new neighbor 
                    int neighborIndex = featGraph.getFeatureIndex(checkedListBox2.Items[e.Index].ToString());
                    featGraph.Features[selectedIndex].addNeighbor(featGraph.Features[neighborIndex]);
                    featGraph.Features[neighborIndex].Parents.Add(featGraph.Features[selectedIndex]);
                }
                else if (e.NewValue == CheckState.Unchecked && e.CurrentValue == CheckState.Checked)
                {
                    //if uncheck remove the neighbor
                    int neighborIndex = featGraph.getFeatureIndex(checkedListBox2.Items[e.Index].ToString());
                    featGraph.Features[selectedIndex].removeNeighbor(featGraph.Features[neighborIndex].Data);
                    featGraph.Features[neighborIndex].Parents.Remove(featGraph.Features[selectedIndex]);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}
