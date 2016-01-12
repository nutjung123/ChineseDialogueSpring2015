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
    public partial class TreeFrom : Form
    {
        private bool flag;
        private bool canDisplay;
        private FeatureGraph featGraph;

        public TreeFrom(FeatureGraph featGraph, bool flag)
        {
            this.canDisplay = true;
            this.featGraph = featGraph;
            this.flag = flag;
            InitializeComponent();
            refreshTreeView();
        }

        public void refreshTreeView()
        {
            treeView1.Nodes.Clear();
            if (flag)
            {
                List<Feature> tmp = featGraph.Features;
                treeDrillFill(treeView1, tmp);
            }
            else
            {
                if (featGraph.Root == null)
                {
                    MessageBox.Show("There is no root set.\nYou need to set a root to view the tree trunk.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    this.canDisplay = false;
                    return;
                }
                bool[] checkEntry = new bool[featGraph.Count];
                treeDrillFillHelper(treeView1.Nodes.Add(featGraph.Root.Name), featGraph.Root, checkEntry);
            }
            treeView1.Refresh();
        }

        private void treeDrillFill(TreeView toRefresh, List<Feature> toFill)
        {
            if (toFill.Count == 0)
            {
                toRefresh.Nodes.Add("EMPTY FEATURE GRAPH");
                return;
            }
            for (int x = 0; x < toFill.Count; x++)
            {
                bool[] checkEntry = new bool[featGraph.Count];
                treeDrillFillHelper(toRefresh.Nodes.Add(toFill[x].Name), toFill[x], checkEntry);
            }
        }
        private void treeDrillFillHelper(TreeNode toRefresh, Feature toFill, bool[] checkEntry)
        {
            int index = featGraph.getFeatureIndex(toFill.Id);
            if (checkEntry[index])
            {
                return;
            }
            checkEntry[index] = true;
            for (int x = 0; x < toFill.Neighbors.Count; x++)
            {
                if (toRefresh.Parent == null || toRefresh.Parent.Text != toFill.Neighbors[x].Item1.Name)
                {
                    treeDrillFillHelper(toRefresh.Nodes.Add(toFill.Neighbors[x].Item1.Name), toFill.Neighbors[x].Item1, checkEntry);
                }
                else
                {
                    toRefresh.Nodes.Add(toFill.Neighbors[x].Item1.Id + "... (Infinite Relation)");
                }
            }
            return;
        }
        private TreeNode getTreeNode(TreeView toSearch, string Id)
        {
            for (int x = 0; x < toSearch.Nodes.Count; x++)
            {
                if (toSearch.Nodes[x].Text == Id) { return toSearch.Nodes[x]; }
            }
            return null;
        }
        public bool CanDisplay
        {
            get
            {
                return this.canDisplay;
            }
        }
    }
}
