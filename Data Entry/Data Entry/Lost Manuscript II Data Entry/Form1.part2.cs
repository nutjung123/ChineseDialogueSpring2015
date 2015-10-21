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
using Newtonsoft.Json;

namespace Dialogue_Data_Entry
{
    public partial class Form1
    {
        List<Constraint> constraintList;
        private Constraint selectedConstraint;
        private Clause selectedClause;
        private string constraintJsonFilename = @"\constraint.json";

        // Open the default constraint file
        private void openDefaultConstraintJsonFile()
        {
            string defaultConstraintFile = Directory.GetCurrentDirectory() + constraintJsonFilename;
            if (File.Exists(defaultConstraintFile))
            {
                openExistingConstraintJsonFile(defaultConstraintFile);
            }
        }

        // Read a constraint file
        private void openExistingConstraintJsonFile(string fileName)
        {
            string json = File.ReadLines(Directory.GetCurrentDirectory() + constraintJsonFilename).First();
            constraintList = JsonConvert.DeserializeObject<List<Constraint>>(json);

            refreshShowNewConstraintListBox();
        }

        // Refresh the list of constraints
        private void refreshShowNewConstraintListBox()
        {
            showNewConstraintListBox.Items.Clear();
            if (constraintList != null)
            {
                for (int x = 0; x < constraintList.Count(); x++)
                {
                    showNewConstraintListBox.Items.Add(constraintList[x].name);
                }
            }
        }

        // Refresh the list of clauses
        private void refreshClauseListBox()
        {
            clauseListBox.Items.Clear();
            foreach (Clause clause in selectedConstraint.clauses)
            {
                string toAdd = "";

                if (clause.getOuterRelationshipId() == 0)
                {
                    toAdd += "^ ";
                }
                else if (clause.getOuterRelationshipId() == 1)
                {
                    toAdd += "v ";
                }

                toAdd += "( " + clause.getName1();

                if (clause.getInnerRelationshipId() == 0)
                {
                    toAdd += " > ";
                }
                else if (clause.getInnerRelationshipId() == 1)
                {
                    toAdd += " < ";
                }

                toAdd += clause.getName2() + " )";

                if (clause.getNot() == true)
                {
                    toAdd += " !";
                }

                clauseListBox.Items.Add(toAdd);
            }
        }

        // Find the selected constraint
        private void showNewConstraintListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedConstraint = constraintList[showNewConstraintListBox.SelectedIndex];
            refreshClauseListBox();
            clauseListBox.SelectedIndex = 0;
            refreshClauseEditor();
        }

        private void featureForNewConstraintListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void refreshClauseEditor()
        {
            if (selectedClause != null)
            {
                notCheckBox.Checked = selectedClause.getNot();
                topic1TextBox.Text = selectedClause.getName1();
                topic2TextBox.Text = selectedClause.getName2();

                if (selectedClause.getOuterRelationshipId() == 0)
                {
                    outerComboBox.Text = "AND";
                }
                else
                {
                    outerComboBox.Text = "OR";
                }

                if (selectedClause.getInnerRelationshipId() == 0)
                {
                    innerComboBox.Text = ">";
                }
                else
                {
                    innerComboBox.Text = "<";
                }
            }
        }

        private void clauseListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedClause = selectedConstraint.clauses[clauseListBox.SelectedIndex];
            refreshClauseEditor();
        }

        private void openConstraintsJsonButton_Click(object sender, EventArgs e)
        {
            openExistingConstraintJsonFile(Directory.GetCurrentDirectory() + constraintJsonFilename);
        }

        private void saveCostaintsJsonButton_Click(object sender, EventArgs e)
        {
            string json = JsonConvert.SerializeObject(constraintList);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(Directory.GetCurrentDirectory() + constraintJsonFilename))
            {
                file.WriteLine(json);
            }
        }

        private void updateClause(Clause clause)
        {
            if (clause != null)
            {
                clause.setNot(notCheckBox.Checked);
                clause.setName1(topic1TextBox.Text);
                clause.setName2(topic2TextBox.Text);

                if (outerComboBox.Text == "AND")
                {
                    clause.setOuterRelationshipId(0);
                }
                else
                {
                    clause.setOuterRelationshipId(1);
                }

                if (innerComboBox.Text == ">")
                {
                    clause.setInnerRelationshipId(0);
                }
                else
                {
                    clause.setInnerRelationshipId(1);
                }

                refreshClauseListBox();
            }
        }

        private void saveClauseButton_Click(object sender, EventArgs e)
        {
            int index = clauseListBox.SelectedIndex;
            updateClause(selectedClause);
            clauseListBox.SelectedIndex = index;
        }

        private void newClauseButton_Click(object sender, EventArgs e)
        {
            Clause newClause = new Clause();
            selectedConstraint.clauses.Add(newClause);
            updateClause(newClause);
            clauseListBox.SelectedIndex = selectedConstraint.clauses.Count - 1;
        }

        private void deleteClauseButton_Click(object sender, EventArgs e)
        {
            selectedConstraint.clauses.RemoveAt(clauseListBox.SelectedIndex);
            refreshClauseListBox();
            clauseListBox.SelectedIndex = 0;
        }

        private void deleteConstraintButton_Click(object sender, EventArgs e)
        {
            constraintList.RemoveAt(showNewConstraintListBox.SelectedIndex);
            refreshShowNewConstraintListBox();
            showNewConstraintListBox.SelectedIndex = 0;
            refreshClauseListBox();
        }

        private void newConstraintButton_Click(object sender, EventArgs e)
        {
            List<Clause> newList = new List<Clause>();
            newList.Add(new Clause());
            constraintList.Add(new Constraint(constraintNameTextBox.Text, newList));
            refreshShowNewConstraintListBox();
            showNewConstraintListBox.SelectedIndex = constraintList.Count - 1;
            refreshClauseListBox();
            clauseListBox.SelectedIndex = 0;
        }

    }
}
