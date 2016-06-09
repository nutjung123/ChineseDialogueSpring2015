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
using Dialogue_Data_Entry;

namespace Dialogue_Data_Entry
{
	public partial class Form1 : Form
	{
		private FeatureGraph featGraph;
		private Feature toChange;
		private string editorFeatureSelected;
		private string editorKeySelected;
		private string currentFileName;
		private string currentConstraintFileName="";
		private string currentQueryFolderName;
		private int queryCounter;
		private int selectedIndex;
		private const string BAD_CHARS = "";
		//private const string BAD_CHARS = "<>()\"\'";
		private int tIndex = -1;
		private ToolTip toolTip1;
		private bool shouldIgnoreCheckEvent;
		private Form2 myQuery;
		private bool updateFlag;
		private TextBox lastFocused;
		private List<TemporalConstraint> temporalConstraintList;
		//Change this to change which file is loaded at program startup
		//private string defaultFilename = @"\2008_Summer_Olympic_Games.xml";
		//private string defaultFilename = @"\2008_Summer_Olympic_Games_4th_simple_tag.xml";
		//private string defaultFilename = @"\2008_Summer_Olympic_Games_4th_tag10.xml";
		//private string defaultFilename = @"\2008_Summer_Olympic_Games_4th_revised.xml";
		//private string defaultFilename = @"\empac_xml.xml";
		//private string defaultFilename = @"\2008_Summer_Olympic_Games_4th_tag_simple_chinese_2.xml";

		//private string defaultFilename = @"\2008_Summer_Olympic_Games_4th_tag_complex_chinese_new.xml";
		private string defaultFilename = @"\2008_Summer_Olympic_Games_2_2_2016.xml";
		//private string defaultFilename = @"\..\..\..\..\..\..\MandarinProject_01_31_16\Assets\xml\california.xml";
		//private string defaultFilename = @"\..\..\..\..\..\..\atom-solar.xml";

		private string constraintFilename = @"\constraint.txt";

		public Form1()
		{
			queryCounter = 0;
			selectedIndex = -1;
			featGraph = new FeatureGraph();
			toChange = null;
			currentFileName = "";
			updateFlag = false;
			
			InitializeComponent();

			//set up tooltip
			toolTip1 = new ToolTip();
			toolTip1.AutoPopDelay = 5000;
			toolTip1.InitialDelay = 1000;
			toolTip1.ReshowDelay = 500;
			toolTip1.ShowAlways = true;

			//Add enter method
			textBox1.KeyDown += new KeyEventHandler(this.featureCreateTextBox1_KeyDown);
			//Add tooltip method to childrenCheckedListBox
			childrenCheckedListBox.MouseHover += new EventHandler(childrenCheckedListBox_MouseHover);
			childrenCheckedListBox.MouseMove += new MouseEventHandler(childrenCheckedListBox_MouseMove);
			//check event handle
			childrenCheckedListBox.ItemCheck += new ItemCheckEventHandler(childrenCheckedListBox_ItemCheck);
			//right click event for adding relationship
			childrenCheckedListBox.MouseDown += new MouseEventHandler(childrenCheckedListBox_RightClick);
			//Add shortcuts
			textBox7.KeyUp += new KeyEventHandler(this.textBox7_KeyUp);

			//Add lostFocuse Method
			firstArgumentTextBox.LostFocus += textBoxFocusLost;
			thirdArgumentTextBox.LostFocus += textBoxFocusLost;


			//Add closing method
			this.FormClosing += Window_Closing;
			shouldIgnoreCheckEvent = true;
			if (backgroundWorker1.IsBusy != true)
			{
				backgroundWorker1.RunWorkerAsync();
			}

			//sorted list
			sortedChildrenComboBox.SelectedIndexChanged += new EventHandler(SortedChildrenComboBox_SelectedIndexChanged);
			sortedFeatureComboBox.SelectedIndexChanged += new EventHandler(SortedFeatureComboBox_SelectedIndexChanged);
			featureEditorListBox.Sorted = true;
			childrenCheckedListBox.Sorted = true;

			openDefaultXMLFile();
			openDefaultConstraintFile();

			//Open the query window
			openQueryWindow();
		}

		//Open the default file
		private void openDefaultXMLFile()
		{
			string defaultFile = Directory.GetCurrentDirectory() + defaultFilename;
			Console.WriteLine(defaultFile);
			if (File.Exists(defaultFile))
			{
				//Open it
				currentFileName = defaultFile;
				featGraph = XMLFilerForFeatureGraph.readFeatureGraph(currentFileName);
				selectedIndex = -1;
				refreshAllButUpdateFeature();
				tagListBox.Items.Clear();
				listBox3.Items.Clear();
				childrenCheckedListBox.Items.Clear();
				clearAllTextBoxes();
				this.Text = "Data Entry - Concept Graph : " + currentFileName;
			}
		}

		private void openDefaultConstraintFile()
		{
			string defaultConstraintFile = Directory.GetCurrentDirectory() + constraintFilename;
			if (File.Exists(defaultConstraintFile))
			{
				openExistingConstraintFile(defaultConstraintFile);
			}
		}

		//All of the operations for the File dropdown menu
		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			featGraph = new FeatureGraph();
			toChange = null;
			selectedIndex = -1;
			editorFeatureSelected = "";
			currentFileName = "";
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
				tagListBox.Items.Clear();
				listBox3.Items.Clear();
				childrenCheckedListBox.Items.Clear();
				clearAllTextBoxes();
				this.Text = "Data Entry - Concept Graph : " + currentFileName;
				//Now that there is a new featureGraph, open a new query window
				openQueryWindow();
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
				updateFlag = false;
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
				updateFlag = false;
				this.Text = "Data Entry - Concept Graph : " + currentFileName;
			}
		}
		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (currentFileName != "")
			{
				featGraph = XMLFilerForFeatureGraph.readFeatureGraph(currentFileName);
				refreshAllButUpdateFeature();
				tagListBox.Items.Clear();
				childrenCheckedListBox.Items.Clear();
				clearAllTextBoxes();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (updateFlag == true)
			{
				DialogResult result = MessageBox.Show("Do you want to save changes you made?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
				if (result == DialogResult.Yes)
				{
					saveToolStripMenuItem_Click(sender, e);
				}
				else if (result == DialogResult.Cancel)
				{
					e.Cancel = true;
				}
			}
		}
		private void quitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (updateFlag == true)
			{
				DialogResult result = MessageBox.Show("Do you want to save changes you made?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
				if (result == DialogResult.Yes)
				{
					saveToolStripMenuItem_Click(sender, e);
					this.Close();
				}
				else if (result == DialogResult.No)
				{
					this.Close();
				}
			}else
			{
				this.Close();
			}
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
		/* 
		private void refreshNeighborPanel(string toIgnore = null)
		{
			panelTest.Controls.Clear();

			listNeighborComboBox.Clear();
			listNeighborCheckBox.Clear();
			listNeighborButton.Clear();

			//add all the new components to the panel
			List<string> temp = featGraph.getFeatureNames();
			int counter = 0;
			for (int x = 0; x < temp.Count; x++)
			{
				if (temp[x] == toIgnore)
				{
					counter = 1;
					continue;
				}
				CheckBox checkBoxTemp = new CheckBox();
				checkBoxTemp.AutoSize = true;
				checkBoxTemp.Location = new System.Drawing.Point(3, 3 + (49 * (x-counter) ) );
				checkBoxTemp.Name = "checkBoxNeighbor" + x.ToString();
				checkBoxTemp.Size = new System.Drawing.Size(80, 17);
				checkBoxTemp.Text = temp[x];
				checkBoxTemp.UseVisualStyleBackColor = true;
				panelTest.Controls.Add(checkBoxTemp);
				listNeighborCheckBox.Add(checkBoxTemp);

				ComboBox comboBoxTemp = new ComboBox();
				comboBoxTemp.FormattingEnabled = true;
				comboBoxTemp.Location = new System.Drawing.Point(3, 25 + (49 * (x-counter) ) );
				comboBoxTemp.Name = "comboBoxNeighbor" + x.ToString();
				comboBoxTemp.Size = new System.Drawing.Size(141, 21);
				comboBoxTemp.Enabled = false;
				panelTest.Controls.Add(comboBoxTemp);
				listNeighborComboBox.Add(comboBoxTemp);

				Button buttonTemp = new Button();
				buttonTemp.Location = new System.Drawing.Point(150, 25 + (49 * (x-counter) ) );
				buttonTemp.Name = "buttonNeighbor" + x.ToString();
				buttonTemp.Size = new System.Drawing.Size(44, 21);
				buttonTemp.TabIndex = 13;
				buttonTemp.Text = "Add";
				buttonTemp.UseVisualStyleBackColor = true;
				buttonTemp.Enabled = false;
				panelTest.Controls.Add(buttonTemp);
				listNeighborButton.Add(buttonTemp);
			}
		}
		*/

		private void refreshFeatureTagListBox(ListBox toRefresh)
		{
			toRefresh.Items.Clear();
			if (selectedIndex == -1) { toRefresh.Refresh(); return; }
			List<string> tmp = featGraph.getFeature(selectedIndex).getTagKeys();
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
			List<string> tmp = featGraph.getFeature(selectedIndex).Speaks;
			for (int x = tmp.Count - 1; x >= 0; x--)
			{
				toRefresh.Items.AddRange(new object[] { tmp[x] });
			}
			toRefresh.Refresh();
		}
		public void refreshListBoxes()
		{
			refreshFeatureListBox(featureCreatorCheckedListBox);
			refreshFeatureListBox(childrenCheckedListBox);
			refreshFeatureListBox(featureRemoverCheckedListBox);
			refreshFeatureListBox(featureEditorListBox);
			refreshFeatureTagListBox(tagListBox);
			refreshFeatureSpeaksListBox(listBox3);
		}
		public void refreshAll()
		{
			clearAllTextBoxes();
			refreshListBoxes();
		}
		public void refreshAllButUpdateFeature()
		{
			refreshFeatureListBox(featureCreatorCheckedListBox);
			refreshFeatureListBox(featureRemoverCheckedListBox);
			refreshFeatureListBox(featureForConstraintListBox);
			refreshFeatureListBox(featureEditorListBox);
		}
		public void clearAllTextBoxes()
		{
			textBox1.Clear();
			editFeatureNameTextBox.Clear();
			textBox7.Clear();
			maskedTextBox1.Clear();
			tagValueTextBox.Clear();
			tagKeyTextBox.Clear();
		}
		//open feature from search function
		public void openFeature(string featureId, string tagData = "")
		{
			refreshListBoxes();
			tabControl1.SelectedIndex = 1;
			tabControl1.Refresh();
			toChange = featGraph.getFeature(featureId);
			if (toChange != null)
			{
				initEditor(toChange);
				featureEditorListBox.SelectedIndex = (indexOfIn(featureId, featureEditorListBox));
				selectedIndex = featGraph.getFeature(featureEditorListBox.SelectedItem.ToString()).Id;
			}
			if (tagData != "")
			{
				editorKeySelected = tagData;
				tagListBox.SelectedIndex = (indexOfIn(tagData, tagListBox));
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
			string name = textBox1.Lines[0];
			if (featGraph.hasNode(name))
			{
				MessageBox.Show("You cannot create two features with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (hasBadChar(name))
			{
				MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			textBox1.Clear();
			//Create a new feature and set its id to the next available id
			Feature toAdd = new Feature(name, featGraph.Features.Count);
			for (int x = 0; x < featureCreatorCheckedListBox.CheckedItems.Count; x++)
			{
				toAdd.addNeighbor(featGraph.getFeature(featureCreatorCheckedListBox.CheckedItems[x].ToString()));
				featGraph.getFeature(featureCreatorCheckedListBox.CheckedItems[x].ToString()).addParent(toAdd);
				//featGraph.getFeature(checkedListBox1.CheckedItems[x].ToString()).addNeighbor(toAdd);
			}
			featGraph.addFeature(toAdd);
			refreshAllButUpdateFeature();
			updateFlag = true;
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
			if (editFeatureNameTextBox.Lines.Length == 0 || editFeatureNameTextBox.Text == "")
			{
				MessageBox.Show("You cannot set a feature with no name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (featGraph.hasNode(editFeatureNameTextBox.Text) && editorFeatureSelected != editFeatureNameTextBox.Text)
			{
				MessageBox.Show("You cannot create two features with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (editorFeatureSelected != "")
			{
				featGraph.getFeature(selectedIndex).Name = editFeatureNameTextBox.Text;
				if (checkBox1.Checked)
				{
					featGraph.Root = featGraph.getFeature(selectedIndex);
				}
				refreshAllButUpdateFeature();
				clearAllTextBoxes();
				childrenCheckedListBox.Items.Clear();
				tagListBox.Items.Clear();
				listBox3.Items.Clear();
				checkBox1.Checked = false;
				checkBox1.Refresh();
				editorFeatureSelected = "";
				updateFlag = true;
			}
		}
		//feature list selection
		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (featureEditorListBox.SelectedItem == null)
			{
				MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			selectedIndex = featGraph.getFeature(featureEditorListBox.SelectedItem.ToString()).Id;
			//toChange = featGraph.getFeature(listBox1.SelectedItem.ToString()).deepCopy();
			
			initEditor(featGraph.getFeature(selectedIndex));
		}

		private void initEditor(Feature toEdit)
		{
			try
			{
				checkBox1.Checked = false;
				clearAllTextBoxes();
				tagTypeComboBox.Text = "";
				maskedTextBox1.Text = toEdit.DiscussedThreshold.ToString();
				editFeatureNameTextBox.Text = toEdit.Name;
				editorFeatureSelected = toEdit.Name;
				refreshFeatureListBox(childrenCheckedListBox, toEdit.Name);

				refreshFeatureSpeaksListBox(listBox3);
				refreshFeatureTagListBox(tagListBox);
				shouldIgnoreCheckEvent = true;
				
				for (int x = 0; x < toEdit.Neighbors.Count; x++)
				{
					for (int y = 0; y < childrenCheckedListBox.Items.Count; y++)
					{
						if (childrenCheckedListBox.Items[y].ToString() == toEdit.Neighbors[x].Item1.Name)
						{
							childrenCheckedListBox.SetItemChecked(y, true);
						}
					}
				}

				shouldIgnoreCheckEvent = false;
				childrenCheckedListBox.Refresh();

				if (featGraph.Root != null && toEdit.Id == featGraph.Root.Id)
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
			if (tagListBox.SelectedIndex != -1)
			{
				//setup the edit tag field
				string featureKey = tagListBox.Items[tagListBox.SelectedIndex].ToString();
				string keyValue = featGraph.getFeature(selectedIndex).getTag(featureKey).Item2;
				string type = featGraph.getFeature(selectedIndex).getTag(featureKey).Item3;
				tagKeyTextBox.Text = featureKey;
				tagValueTextBox.Text = keyValue;
				tagTypeComboBox.Text = type;
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
			else if (tagKeyTextBox.Text == "" || tagValueTextBox.Text == "")
			{
				MessageBox.Show("You cannot create a tag with an empty key or value", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (featGraph.getFeature(selectedIndex).hasTagWithKey(tagKeyTextBox.Text))
			{
				MessageBox.Show("There is already a tag with that key in this feature\nPlease choose another key", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (hasBadChar(tagValueTextBox.Text) || hasBadChar(tagKeyTextBox.Text))
			{
				MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (tagTypeComboBox.Text == "")
			{
				MessageBox.Show("There is no selected attribute, please choose one from the drop down menue", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else
			{
				featGraph.getFeature(selectedIndex).addTag(tagKeyTextBox.Text, tagValueTextBox.Text, tagTypeComboBox.Text);
				updateFlag = true;
				tagKeyTextBox.Text = "";
				tagTypeComboBox.Text = "";
				tagValueTextBox.Text = "";
				refreshFeatureTagListBox(tagListBox);
			}
			
		}
		//edit tag button
		private void button2_Click_1(object sender, EventArgs e)
		{
			if (selectedIndex == -1 || editorKeySelected == "")
			{
				MessageBox.Show("You haven't selected anything to edit yet", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (tagKeyTextBox.Text == "" || tagValueTextBox.Text == "")
			{
				MessageBox.Show("You cannot create a tag with an empty key or value", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (featGraph.getFeature(selectedIndex).hasTagWithKey(tagKeyTextBox.Text) && editorKeySelected != tagKeyTextBox.Text)
			{
				MessageBox.Show("There is already a tag with that key in this feature\nPlease choose another key", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (hasBadChar(tagKeyTextBox.Text) || hasBadChar(tagValueTextBox.Text))
			{
				MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (tagTypeComboBox.Text == "")
			{
				MessageBox.Show("There is no selected attribute, please choose one from the drop down menue", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else
			{
				featGraph.getFeature(selectedIndex).removeTag(editorKeySelected);
				featGraph.getFeature(selectedIndex).addTag(tagKeyTextBox.Text, tagValueTextBox.Text, tagTypeComboBox.Text);
				tagKeyTextBox.Text = "";
				tagTypeComboBox.Text = "";
				tagValueTextBox.Text = "";
				refreshFeatureTagListBox(tagListBox);
				updateFlag = true;
			}
		 
		}
		//remove tag button
		private void button3_Click(object sender, EventArgs e)
		{
			if(editorKeySelected!=""&&selectedIndex!=-1){
				featGraph.getFeature(selectedIndex).removeTag(editorKeySelected);
				editorKeySelected = "";
				refreshFeatureTagListBox(tagListBox);
				updateFlag = true;
			}
		}

		//last focuse helper
		private void textBoxFocusLost(object sender, EventArgs e)
		{
			lastFocused = (TextBox)sender;
		}

		//tooltip helper function 
		private void childrenCheckedListBox_MouseMove(object sender, MouseEventArgs e)
		{
			int index = childrenCheckedListBox.IndexFromPoint(e.Location);
			if (tIndex != index)
			{
				GetToolTip();
			}
		}
		//tooltip helper function
		private void childrenCheckedListBox_MouseHover(object sender, EventArgs e)
		{
			GetToolTip();
		}
		//show tooltip 
		private void GetToolTip()
		{
			Point pos = childrenCheckedListBox.PointToClient(MousePosition);
			tIndex = childrenCheckedListBox.IndexFromPoint(pos);
			if (tIndex > -1)
			{
				string showText = childrenCheckedListBox.Items[tIndex].ToString();
				//check for relationship
				if (childrenCheckedListBox.GetItemCheckState(tIndex) == CheckState.Checked)
				{
					showText = showText + "\nR: " + featGraph.getFeature(selectedIndex).getRelationshipNeighbor(showText);
				}
				toolTip1.SetToolTip(childrenCheckedListBox, showText);
			}
		}

		//Feature Removal Methods
		private void checkedListBox3_SelectedIndexChanged(object sender, EventArgs e)
		{

		}
		
		private void removeFeatureButton_Click(object sender, EventArgs e)
		{
			if (featureRemoverCheckedListBox.CheckedItems.Count == 0)
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
			
			for (int x = 0; x < featureRemoverCheckedListBox.CheckedItems.Count; x++)
			{
				featGraph.removeFeature(featureRemoverCheckedListBox.CheckedItems[x].ToString());
			}
			refreshAllButUpdateFeature();
			updateFlag = true;
		}

		private void viewToolStripMenuItem1_Click(object sender, EventArgs e)
		{

		}
		//unused function
		private void button4_Click(object sender, EventArgs e)
		{
			//FeatureSpeaker mySpeaker = new FeatureSpeaker();
			//MessageBox.Show(mySpeaker.getChildSpeak(toChange));
			//MessageBox.Show(mySpeaker.getTagSpeak(toChange));
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
					fs.Close();
					fs = File.Open(responseFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
					var sw = new StreamWriter(fs);
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
				featGraph.getFeature(selectedIndex).Speaks.Add(this.textBox7.Text);
				updateFlag = true;
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
				int editIndex = featGraph.getFeature(selectedIndex).Speaks.Count - listBox3.SelectedIndex - 1;
				featGraph.getFeature(selectedIndex).editSpeak(editIndex,this.textBox7.Text);
				updateFlag = true;
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
				int removedIndex = featGraph.getFeature(selectedIndex).Speaks.Count - listBox3.SelectedIndex - 1;
				featGraph.getFeature(selectedIndex).removeSpeak(removedIndex);
				refreshFeatureSpeaksListBox(listBox3);
				updateFlag = true;
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
				tagListBox.Items.Clear();
				childrenCheckedListBox.Items.Clear();
				clearAllTextBoxes();
			}
		}

		private void chatToolStripMenuItem_Click(object sender, EventArgs e)
		{
			openQueryWindow();
		}

		//Opens the Query window
		private void openQueryWindow()
		{
			//Close any query window already open
			if (myQuery != null)
			{
				myQuery.Close();
			}//end if
			featGraph.setMaxDepth(-1); //so that we force them to recalculate every time you call query in case of updating graph
			myQuery = new Form2(featGraph, temporalConstraintList);
			myQuery.Show();
		}//end method openQueryWindow

		// add neighbor checkedListBox method (+ parent)
		private void childrenCheckedListBox_ItemCheck(Object sender, ItemCheckEventArgs e)
		{
			if (!shouldIgnoreCheckEvent && selectedIndex!=-1)
			{
				if (e.NewValue == CheckState.Checked && e.CurrentValue == CheckState.Unchecked)
				{
					//if check insert the new neighbor 
					int neighborIndex = featGraph.getFeature(childrenCheckedListBox.Items[e.Index].ToString()).Id;
					featGraph.getFeature(selectedIndex).addNeighbor(featGraph.getFeature(neighborIndex));
					featGraph.getFeature(neighborIndex).addParent(featGraph.getFeature(selectedIndex));
					//featGraph.Features[neighborIndex].addNeighbor(featGraph.getFeature(selectedIndex));
					updateFlag = true;
				}
				else if (e.NewValue == CheckState.Unchecked && e.CurrentValue == CheckState.Checked)
				{
					//if uncheck remove the neighbor
					int neighborIndex = featGraph.getFeature(childrenCheckedListBox.Items[e.Index].ToString()).Id;
					featGraph.getFeature(selectedIndex).removeNeighbor(featGraph.getFeature(neighborIndex).Id);
					featGraph.getFeature(neighborIndex).removeParent(featGraph.getFeature(selectedIndex).Id);
					//featGraph.Features[neighborIndex].removeNeighbor(featGraph.getFeature(selectedIndex).Data);
					updateFlag = true;
				}
			}
		}
		//Right click event for adding relationship to the edge
		private void childrenCheckedListBox_RightClick(Object sender, MouseEventArgs e)
		{
			childrenCheckedListBox.SelectedIndex = childrenCheckedListBox.IndexFromPoint(e.X, e.Y);
			if (childrenCheckedListBox.SelectedIndex == -1)
			{
				return;
			}
			string selectedName = childrenCheckedListBox.Items[childrenCheckedListBox.SelectedIndex].ToString();
			if (e.Button == MouseButtons.Right 
				&& childrenCheckedListBox.GetItemCheckState(childrenCheckedListBox.SelectedIndex) == CheckState.Checked )
			{
				string OldRelationshipN = featGraph.getFeature(selectedIndex).getRelationshipNeighbor(selectedName);
				string OldRelationshipP = featGraph.getFeature(selectedName).getRelationshipParent(featGraph.getFeature(selectedIndex).Id);
				string OldWeight = featGraph.getFeature(selectedIndex).getWeight(selectedName);
				Form3 relationshipDialog = new Form3(featGraph.getFeature(selectedIndex).Name, selectedName, OldRelationshipN, OldRelationshipP, OldWeight);
				relationshipDialog.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
				string textNResult = "";
				string textPResult = "";
				double weightResult = 0.0;
				if (relationshipDialog.ShowDialog(this) == DialogResult.OK)
				{
					textNResult = relationshipDialog.comboBoxRelationshipFT.Text;        //neighbor result
					textPResult = relationshipDialog.comboBoxRelationshipTF.Text;       //parent result
					weightResult = Convert.ToDouble(relationshipDialog.textBox1.Text);

					if (hasBadChar(textNResult)||hasBadChar(textPResult))
					{
						MessageBox.Show("The values you have entered contain characters that are not allowed\nThe characters that you cannot use are " + BAD_CHARS, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}

					//add relationship
					featGraph.getFeature(selectedIndex).setNeighbor(featGraph.getFeature(selectedName), weightResult, textNResult);
					bool checkExistParent = featGraph.getFeature(selectedName).setParent(featGraph.getFeature(featGraph.getFeature(selectedIndex).Id), weightResult, textPResult);
					if (!checkExistParent)
					{
						featGraph.getFeature(selectedName).addParent(featGraph.getFeature(featGraph.getFeature(selectedIndex).Id), weightResult, textPResult);
					}
					if (OldRelationshipN != textNResult)
					{
						updateFlag = true;
					}
				}
				relationshipDialog.Dispose();
			}
		}

		private void SortedFeatureComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			string selectedSort = sortedFeatureComboBox.SelectedItem.ToString();
			if (selectedSort == "Sorted by Alphabet")
			{
				featureEditorListBox.Sorted = true;

			}
			else if (selectedSort == "Sorted by ID")
			{
				featureEditorListBox.Sorted = false;
			}
			refreshFeatureListBox(featureEditorListBox);
			if (selectedIndex != -1)
			{
				featureEditorListBox.SelectedItem = featGraph.getFeature(selectedIndex).Id;
				//initEditor(featGraph.getFeature(selectedIndex));
			}
		}

		private void SortedChildrenComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			string selectedSort = sortedChildrenComboBox.SelectedItem.ToString();
			if (selectedSort == "Sorted by Alphabet")
			{
				childrenCheckedListBox.Sorted = true;
			}
			else if (selectedSort == "Sorted by ID")
			{
				childrenCheckedListBox.Sorted = false;
			}
			if (selectedIndex != -1)
			{
				refreshFeatureListBox(childrenCheckedListBox, featGraph.getFeature(selectedIndex).Name);
				//update the check state of childrenCheckedListBox 
				shouldIgnoreCheckEvent = true;
				for (int x = 0; x < featGraph.getFeature(selectedIndex).Neighbors.Count; x++)
				{
					for (int y = 0; y < childrenCheckedListBox.Items.Count; y++)
					{
						if (childrenCheckedListBox.Items[y].ToString() == featGraph.getFeature(selectedIndex).Neighbors[x].Item1.Name)
						{
							childrenCheckedListBox.SetItemChecked(y, true);
						}
					}
				}

				shouldIgnoreCheckEvent = false;
				childrenCheckedListBox.Refresh();
			}
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
		}

		private void featureForConstraintListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			
			if (lastFocused != null)
			{
				lastFocused.Text = featureForConstraintListBox.SelectedItem.ToString();
			}
		}

		private void refreshShowConstraintListBox()
		{
			showConstraintListBox.Items.Clear();
			if (temporalConstraintList != null)
			{
				for (int x = 0; x < temporalConstraintList.Count(); x++)
				{
					string toAdd = temporalConstraintList[x].FirstArgument+ " ";
					toAdd += temporalConstraintList[x].SecondArgument+" ";
					toAdd += temporalConstraintList[x].ThirdArgument;
					showConstraintListBox.Items.Add(toAdd);
				}
			}
			//clear all the field
			firstArgumentTextBox.Text = "";
			secondArgumentComboBox.SelectedIndex = -1;
			thirdArgumentTextBox.Text = "";
			fourthArgumentComboBox.SelectedIndex = -1;
			fifthArgumentTextBox.Text = "";

			secondArgumentComboBox.Enabled = false;
			thirdArgumentTextBox.Enabled = false;
			fourthArgumentComboBox.Enabled = false;
			fifthArgumentTextBox.Enabled = false;
		}

		private void addConstraintButton_Click(object sender, EventArgs e)
		{
			//check all the three necessary fields
			if(firstArgumentTextBox.Text == "" || featGraph.getFeature(firstArgumentTextBox.Text) == null)
			{
				MessageBox.Show("First argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (secondArgumentComboBox.Text == "")
			{
				MessageBox.Show("Second argument box is empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (thirdArgumentTextBox.Text == "") 
			{
				MessageBox.Show("Third argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (thirdArgumentTextBox.Text != "")
			{
				try
				{
					int result = 0;
					Int32.TryParse(thirdArgumentTextBox.Text, out result);
					if (result <= 0)
					{
						MessageBox.Show("Third argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}
				}
				catch (FormatException)
				{
					if (featGraph.getFeature(thirdArgumentTextBox.Text) == null)
					{
						MessageBox.Show("Third argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}
				}
			}
			string firstArgument = firstArgumentTextBox.Text;
			string secondArgument = secondArgumentComboBox.Text;
			int thirdArgument = 0;
			Int32.TryParse(thirdArgumentTextBox.Text, out thirdArgument);
			string fourthArgument = fourthArgumentComboBox.Text;
			string fifthArgument = fifthArgumentTextBox.Text;
			if (temporalConstraintList==null)
			{
				temporalConstraintList = new List<TemporalConstraint>();
			}
			temporalConstraintList.Add(new TemporalConstraint(firstArgument,secondArgument,thirdArgument,fourthArgument,fifthArgument));
			refreshShowConstraintListBox();
		}

		private void showConstraintListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (showConstraintListBox.SelectedIndex != -1)
			{
				firstArgumentTextBox.Text = temporalConstraintList[showConstraintListBox.SelectedIndex].FirstArgument;
				constriantTypeComboBox.Text = temporalConstraintList[showConstraintListBox.SelectedIndex].getThirdArgumentType();
				secondArgumentComboBox.Text = temporalConstraintList[showConstraintListBox.SelectedIndex].SecondArgument;
				thirdArgumentTextBox.Text = temporalConstraintList[showConstraintListBox.SelectedIndex].ThirdArgument.ToString();
				fourthArgumentComboBox.Text = temporalConstraintList[showConstraintListBox.SelectedIndex].FourthArgument;
				fifthArgumentTextBox.Text = temporalConstraintList[showConstraintListBox.SelectedIndex].FifthArgument;

				if (constriantTypeComboBox.Text == "turn")
				{
					secondArgumentComboBox.Enabled = true;
					thirdArgumentTextBox.Enabled = true;
					fourthArgumentComboBox.Enabled = false;
					fifthArgumentTextBox.Enabled = false;
				}
				else if (constriantTypeComboBox.Text == "topic")
				{
					secondArgumentComboBox.Enabled = false;
					thirdArgumentTextBox.Enabled = true;
					fourthArgumentComboBox.Enabled = true;
					fifthArgumentTextBox.Enabled = true;
				}

			}
		}

		private void editConstraintButton_Click(object sender, EventArgs e)
		{
			if (showConstraintListBox.SelectedIndex == -1)
			{
				MessageBox.Show("You have not select one of the constraint.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (firstArgumentTextBox.Text == "" || featGraph.getFeature(firstArgumentTextBox.Text) == null)
			{
				MessageBox.Show("First argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (secondArgumentComboBox.Text == "")
			{
				MessageBox.Show("Second argument box is empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (thirdArgumentTextBox.Text == "") 
			{
				MessageBox.Show("Third argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else if (thirdArgumentTextBox.Text != "")
			{
				try
				{
					int result = 0;
					Int32.TryParse(thirdArgumentTextBox.Text, out result);
					if (result <= 0)
					{
						MessageBox.Show("Third argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}
				}
				catch (FormatException)
				{
					if (featGraph.getFeature(thirdArgumentTextBox.Text) == null)
					{
						MessageBox.Show("Third argument box is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}
				}
			}
			string firstArgument = firstArgumentTextBox.Text;
			string secondArgument = secondArgumentComboBox.Text;
			int thirdArgument = 0;
			Int32.TryParse(thirdArgumentTextBox.Text, out thirdArgument);
			string fourthArgument = fourthArgumentComboBox.Text;
			string fifthArgument = fifthArgumentTextBox.Text;
			temporalConstraintList.RemoveAt(showConstraintListBox.SelectedIndex);
			temporalConstraintList.Add(new TemporalConstraint(firstArgument, secondArgument, thirdArgument,fourthArgument,fifthArgument));
			refreshShowConstraintListBox();
		}

		private void removeConstraintButton_Click(object sender, EventArgs e)
		{
			if (showConstraintListBox.SelectedIndex == -1)
			{
				MessageBox.Show("You have not select one of the constraint.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			temporalConstraintList.RemoveAt(showConstraintListBox.SelectedIndex);
			refreshShowConstraintListBox();
		}

		private void saveAsConstraint()
		{
			saveFileDialog1.Title = "Save Constraint";
			saveFileDialog1.FileName = "";
			saveFileDialog1.OverwritePrompt = true;
			saveFileDialog1.AddExtension = false;
			saveFileDialog1.Filter = "Text File|*.txt|All Files|*";
			saveFileDialog1.ShowDialog();
			if (saveFileDialog1.FileName != "")
			{
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(saveFileDialog1.FileName))
				{
					for (int x = 0; x < temporalConstraintList.Count(); x++)
					{
						file.WriteLine(temporalConstraintList[x].FirstArgument);
						file.WriteLine(temporalConstraintList[x].SecondArgument);
						file.WriteLine(temporalConstraintList[x].ThirdArgument);
						file.WriteLine(temporalConstraintList[x].FourthArgument);
						file.WriteLine(temporalConstraintList[x].FifthArgument);
					}
				}
			}
		}

		private void saveFileForConstraintButton_Click(object sender, EventArgs e)
		{
			if (currentConstraintFileName != "")
			{
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(currentConstraintFileName))
				{
					for (int x = 0; x < temporalConstraintList.Count(); x++)
					{
						file.WriteLine(temporalConstraintList[x].FirstArgument);
						file.WriteLine(temporalConstraintList[x].SecondArgument);
						file.WriteLine(temporalConstraintList[x].ThirdArgument);
						file.WriteLine(temporalConstraintList[x].FourthArgument);
						file.WriteLine(temporalConstraintList[x].FifthArgument);
					}
				}
			}
			else
			{
				saveAsConstraint();
			}
		}

		private void saveAsForConstraintFileButton_Click(object sender, EventArgs e)
		{
			saveAsConstraint();
		}

		private void openExistingConstraintFile(string fileName)
		{
			currentConstraintFileName = fileName;
			string[] lines = System.IO.File.ReadAllLines(currentConstraintFileName);
			temporalConstraintList = new List<TemporalConstraint>();
			string firstArgument = "", secondArgument = "", fourthArgument ="", fifthArgument = "";
			int thirdArgument = 0;
			for (int x = 0; x < lines.Count(); x++)
			{
				if (x % 5 == 0)
				{
					firstArgument = lines[x];
				}
				else if (x % 5 == 1)
				{
					secondArgument = lines[x];
				}
				else if (x % 5 == 2)
				{
					Int32.TryParse(lines[x], out thirdArgument);
				}
				else if (x % 5 == 3)
				{
					fourthArgument = lines[x];
				}
				else if (x % 5 == 4)
				{
					fifthArgument = lines[x];
					temporalConstraintList.Add(new TemporalConstraint(firstArgument, secondArgument, thirdArgument, fourthArgument, fifthArgument));
				}
			}
			refreshShowConstraintListBox();
		}

		private void openFileForConstraintButton_Click(object sender, EventArgs e)
		{
			openFileDialog1.Title = "Choose the Text file you want to load";
			openFileDialog1.FileName = "";
			openFileDialog1.Multiselect = false;
			openFileDialog1.CheckFileExists = true;
			openFileDialog1.Filter = "Text File|*.txt|All Files|*";
			openFileDialog1.ShowDialog();
			if (openFileDialog1.FileName != "" && openFileDialog1.CheckFileExists)
			{
				openExistingConstraintFile(openFileDialog1.FileName);
			}
		}
		
		private void textBox7_KeyUp(object sender, KeyEventArgs e) {
			if (e.KeyCode == ( Keys.Control| Keys.C)) {
				Menu_Copy(sender, e);
			} else if (e.KeyCode == (Keys.Control | Keys.X)) {
				Menu_Cut(sender, e);
			} else if (e.KeyCode == (Keys.Control | Keys.V)) {
				Menu_Paste(sender, e);
			} else if (e.KeyCode == (Keys.Control | Keys.Z)) {
				Menu_Undo(sender, e);
			}
		}

		private void Menu_Copy(System.Object sender, System.EventArgs e)
		{
			// Ensure that text is selected in the text box.    
			if(textBox7.SelectionLength > 0)
				// Copy the selected text to the Clipboard.
				textBox7.Copy();
		}

		private void Menu_Cut(System.Object sender, System.EventArgs e)
		{   
			// Ensure that text is currently selected in the text box.    
			if(textBox7.SelectedText != "")
				// Cut the selected text in the control and paste it into the Clipboard.
				textBox7.Cut();
		}

		private void Menu_Paste(System.Object sender, System.EventArgs e)
		{
			// Determine if there is any text in the Clipboard to paste into the text box. 
			if(Clipboard.GetDataObject().GetDataPresent(DataFormats.Text) == true)
			{
				// Determine if any text is selected in the text box. 
				if(textBox7.SelectionLength > 0)
				{
					// Ask user if they want to paste over currently selected text. 
					if(MessageBox.Show("Do you want to paste over current selection?", "Cut Example", MessageBoxButtons.YesNo) == DialogResult.No)
						// Move selection to the point after the current selection and paste.
						textBox7.SelectionStart = textBox7.SelectionStart + textBox7.SelectionLength;
				}
				// Paste current text in Clipboard into text box.
				textBox7.Paste();
			}
		}

		private void Menu_Undo(System.Object sender, System.EventArgs e)
		{
			// Determine if last operation can be undone in text box.    
			if(textBox7.CanUndo == true)
			{
				// Undo the last operation.
				textBox7.Undo();
				// Clear the undo buffer to prevent last action from being redone.
				textBox7.ClearUndo();
			}
		}

		private void constriantTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (constriantTypeComboBox.Text == "turn")  
			{
				secondArgumentComboBox.Enabled = true;
				thirdArgumentTextBox.Enabled = true;
				fourthArgumentComboBox.Enabled = false;
				fifthArgumentTextBox.Enabled = false;
			}
			else if (constriantTypeComboBox.Text == "topic") 
			{
				secondArgumentComboBox.SelectedIndex = secondArgumentComboBox.Items.IndexOf(">");
				secondArgumentComboBox.Enabled = false;
				thirdArgumentTextBox.Enabled = true;
				fourthArgumentComboBox.Enabled = true;
				fifthArgumentTextBox.Enabled = true;
			}
		}

	}
}
