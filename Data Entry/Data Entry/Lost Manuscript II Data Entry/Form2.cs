using System;
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
using System.Threading;

namespace Dialogue_Data_Entry
{
    public partial class Form2 : Form
    {
        private FeatureGraph featGraph;
        private QueryHandler myHandler;
        private float featureWeight;
        private float tagKeyWeight;
        private SynchronousSocketListener myServer = null;
        private Thread serverThread = null;
        private volatile bool _shouldStop = false;
        private List<TemporalConstraint> temporalConstraintList;

        public Form2(FeatureGraph myGraph, List<TemporalConstraint> myTemporalConstraintList)
        {
            InitializeComponent();
            //pre-process shortest distance
            myGraph.getMaxDistance();           
            this.featGraph = myGraph;
            this.temporalConstraintList = myTemporalConstraintList;
            //clear discussedAmount
            for (int x = 0; x < featGraph.Features.Count(); x++)
            {
                featGraph.Features[x].DiscussedAmount = 0;
            }
            featureWeight = .6f;
            tagKeyWeight = .2f;
            chatBox.AppendText("Hello, and Welcome to the Query. \r\n");
            inputBox.KeyDown += new KeyEventHandler(this.inputBox_KeyDown);
            this.FormClosing += Window_Closing;
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
            if (myHandler == null)
                myHandler = new QueryHandler(featGraph, temporalConstraintList);
            chatBox.AppendText("User: "+query+"\r\n");
            string answer = myHandler.ParseInput(query,false);
            chatBox.AppendText("System:"+answer+"\r\n");
            inputBox.Clear();
        }

        private void ServerModeButton_Click(object sender, EventArgs e)
        {
            //Start new thread for server
            this.serverThread = new Thread(this.DoWork);
            this.serverThread.Start();
        }

        public void DoWork()
        {
            myServer = new SynchronousSocketListener();
            
            this.Invoke((MethodInvoker)delegate {
                chatBox.AppendText("Waiting for client to connect...");
            });

            myServer.StartListening();
            //myServer.SendDataToClient("Connected");
            
            this.Invoke((MethodInvoker)delegate
            {
                chatBox.AppendText("\nConnected!");
            });
            this._shouldStop = false;
            //Console.WriteLine("Connected.");
            while (!this._shouldStop)
            {
                string query = myServer.ReceieveDataFromClient();
                query = query.Replace("<EOF>", "");
                if (query == "QUIT")
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        chatBox.AppendText("Client: " + query + "\r\n");
                    });
                    break;
                }
                if (myHandler == null)
                    myHandler = new QueryHandler(featGraph, temporalConstraintList);
                //Console.WriteLine("Query: " + query);
                
                this.Invoke((MethodInvoker)delegate
                {
                    chatBox.AppendText("Client: " + query + "\r\n");
                });
                
                string answer = myHandler.ParseInput(query, true);
                
                this.Invoke((MethodInvoker)delegate
                {
                    chatBox.AppendText("System:" + answer + "\r\n");
                });

                //Console.WriteLine("Send: " + answer);
                myServer.SendDataToClient(answer);
            }
            myServer.CloseServer();
            myServer = null;
        }

        public void RequestDoWorkStop()
        {
            this._shouldStop = true;
        }

        private void StopServerbutton_Click(object sender, EventArgs e)
        {
            if (myServer != null)
            {
                //(Doesn't seem to stop the loop)
                this.RequestDoWorkStop();
                myServer.CloseServer();
                this.serverThread.Abort(); //To Do: Not use Abort and terminate by existing function DoWork
                this.serverThread.Join();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopServerbutton_Click(sender, e);
        }

    }
}
