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
    public partial class Form1
    {
        List<Constraint> constraintList;
        private string newconstraintFilename = @"\newconstraint.txt";

        // Open the default constraint file
        private void openDefaultNewConstraintFile() {
            string defaultConstraintFile = Directory.GetCurrentDirectory() + newconstraintFilename;
            if (File.Exists(defaultConstraintFile)) {
                openExistingNewConstraintFile(defaultConstraintFile);
            }
        }

        // Read a constraint file
        private void openExistingNewConstraintFile(string fileName) {
            currentConstraintFileName = fileName;
            string[] lines = System.IO.File.ReadAllLines(currentConstraintFileName);
            constraintList = new List<Constraint>();

            string constraintName = "";
            bool isNewClause = true;
            List<Clause> clauseList = new List<Clause>();

            foreach (string line in lines) {
                if (isNewClause) {
                    clauseList = new List<Clause>();
                    constraintName = line;
                    isNewClause = false;
                } else if (line == "") {
                    constraintList.Add(new Constraint(constraintName, clauseList));
                    isNewClause = true;
                } else {
                    string[] items = line.Split(' ');

                    string n1 = items[1];
                    string n2 = items[3];

                    int in_rel_id = 0;
                    if (items[0] == "AND") {
                        in_rel_id = 0;
                    }
                    else if (items[0] == "OR") {
                        in_rel_id = 1;
                    }

                    int out_rel_id = 0;
                    if (items[2] == ">") {
                        out_rel_id = 0;
                    }
                    else if (items[2] == "<") {
                        out_rel_id = 1;
                    }

                    clauseList.Add(new Clause(n1, n2, in_rel_id, out_rel_id, false));
                }
            }

            refreshShowNewConstraintListBox();
        }

        // Refresh the list of constraints
        private void refreshShowNewConstraintListBox() {
            showNewConstraintListBox.Items.Clear();
            if (constraintList != null) {
                for (int x = 0; x < constraintList.Count(); x++) {
                    showNewConstraintListBox.Items.Add(constraintList[x].name);
                }
            }
        }

        // Refresh the list of clauses
        private void refreshClauseListBox() {
            clauseListBox.Items.Clear();
            foreach (Clause clause in selectedConstraint.clauses) {
                string toAdd = "";

                if (clause.getOuterRelationshipId() == 0) {
                    toAdd += "^ ";
                }
                else if (clause.getOuterRelationshipId() == 1) {
                    toAdd += "v ";
                }

                toAdd += "( " + clause.getName1();

                if (clause.getInnerRelationshipId() == 0) {
                    toAdd += " > ";
                }
                else if (clause.getInnerRelationshipId() == 1) {
                    toAdd += " < ";
                }

                toAdd += clause.getName2() + " )";

                clauseListBox.Items.Add(toAdd);
            }
        }

        // Find the selected constraint
        private void showNewConstraintListBox_SelectedIndexChanged(object sender, EventArgs e) {
            string name = showNewConstraintListBox.SelectedItem.ToString();
            foreach (Constraint constraint in constraintList) {
                if (constraint.name == name) {
                    selectedConstraint = constraint;
                }
            }
            refreshClauseListBox();
        }

        private void featureForNewConstraintListBox_SelectedIndexChanged(object sender, EventArgs e) {

        }

        private void clauseListBox_SelectedIndexChanged(object sender, EventArgs e) {

        }
    }
}
