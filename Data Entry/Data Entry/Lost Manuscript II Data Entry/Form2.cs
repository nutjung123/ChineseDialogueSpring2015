﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dialogue_Data_Entry;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace Dialogue_Data_Entry
{
    public partial class Form2 : Form
    {
        private FeatureGraph featGraph;
        private QueryController myController;
        private QueryHandler myHandler;
        private float featureWeight;
        private float tagKeyWeight;
        private SynchronousSocketListener myServer;


        public Form2(FeatureGraph myGraph)
        {
            InitializeComponent();
            //pre-process shortest distance
            myGraph.getMaxDistance();           
            myController = new QueryController(myGraph);
            featGraph = myGraph;
            //clear discussedAmount
            for (int x = 0; x < featGraph.Features.Count(); x++)
            {
                featGraph.Features[x].DiscussedAmount = 0;
            }
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
            /*if (myController == null)
            {
                myController = new QueryController(featGraph);
            }*/
            if (myHandler == null)
                myHandler = new QueryHandler(featGraph);
            chatBox.AppendText("User: "+query+"\r\n");
            string answer = myHandler.ParseInput(query,false);
            chatBox.AppendText("System:"+answer+"\r\n");
            inputBox.Clear();
        }

        private void ServerModeButton_Click(object sender, EventArgs e)
        {
            myServer = new SynchronousSocketListener();
            chatBox.AppendText("Waiting for client to connet...");
            myServer.StartListening();
            myServer.SendDataToClient("Connected");
            chatBox.AppendText("Connected!");
            //Console.WriteLine("Connected.");

            while (true)
            {
                string query = myServer.ReceieveDataFromClient();
                query = query.Replace("<EOF>", "");
                if (query == "QUIT")
                {
                    break;
                }
                /*if (myController == null)
                {
                    myController = new QueryController(featGraph);
                }*/
                if (myHandler == null)
                    myHandler = new QueryHandler(featGraph);
                //Console.WriteLine("Query: " + query);
                chatBox.AppendText("Client: " + query + "\r\n");
                string answer = myHandler.ParseInput(query, true);
                chatBox.AppendText("System:" + answer + "\r\n");
                //Console.WriteLine("Send: " + answer);
                myServer.SendDataToClient(answer);
            }
            myServer.CloseServer();
        }
    }
}
