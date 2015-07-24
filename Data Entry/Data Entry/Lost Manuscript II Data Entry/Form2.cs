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
                if (query == "Start Recording")
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        StartRecording();
                    });
                    //myServer.SendDataToClient("success");
                    continue;
                }
                if (query.Contains("Stop Recording:"))
                {
                    // parse the string, last substring as the language, assume that the input string is correct
                    string language = query.Split(':')[1];
                    this.Invoke((MethodInvoker)delegate
                    {
                        StopRecording();
                    });
                    string translated_query = null;
                    this.Invoke((MethodInvoker)delegate
                    {
                        translated_query = XunfeiFunction.IatModeTranslate("audio/temp.wav", language);
                    });
                    //MessageBox.Show(translated_query);
                    if (translated_query != null)
                    {
                        myServer.SendDataToClient(translated_query);
                    }
                    else
                    {
                        myServer.SendDataToClient("Recording stopped: No speech detected.");
                    }
                    continue;
                }
                if (query.Contains("TTS#"))
                {
                    string language = query.Split('#')[1];
                    string preferred_sex = query.Split('#')[2];
                    query = query.Split('#')[3];
                    this.Invoke((MethodInvoker)delegate
                    {
                        XunfeiFunction.ProcessVoice(query, "audio/out.wav", language, preferred_sex);
                    });
                    this.Invoke((MethodInvoker)delegate
                    {
                        Play_TTS_file("audio/out.wav");
                    });
                    myServer.SendDataToClient("TTS completed.");
                    continue;
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

        NAudio.Wave.WaveIn sourceStream = null;
        //NAudio.Wave.DirectSoundOut waveOut = null;
        NAudio.Wave.WaveFileWriter waveWriter = null;

        private void sourceStream_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            if (waveWriter == null) return;

            waveWriter.WriteData(e.Buffer, 0, e.BytesRecorded);
            waveWriter.Flush();
        }

        private void StartRecording()
        {
            // Check if there are sources for input sound
            int numResource = NAudio.Wave.WaveIn.DeviceCount;
            if (numResource == 0) return;

            // Use the first source as default
            sourceStream = new NAudio.Wave.WaveIn();
            // Set wave format
            sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(16000, 16, 1);

            NAudio.Wave.WaveInProvider waveIn = new NAudio.Wave.WaveInProvider(sourceStream);

            //waveOut = new NAudio.Wave.DirectSoundOut();
            //waveOut.Init(waveIn);

            sourceStream.StartRecording();
            //waveOut.Play(); // plays the audio, serve as demo, can be deleted

            sourceStream.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(sourceStream_DataAvailable);
            // Save the file temporarily in the audio folder, note that previous recording will be overwritten
            waveWriter = new NAudio.Wave.WaveFileWriter("audio/temp.wav", sourceStream.WaveFormat);
        }

        private void StopRecording()
        {
            /*
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
            */
            if (sourceStream != null)
            {
                sourceStream.StopRecording();
                sourceStream.Dispose();
                sourceStream = null;
            }
            if (waveWriter != null)
            {
                waveWriter.Dispose();
                waveWriter = null;
            }
        }

        private void StartSpeakingbutton_Click(object sender, EventArgs e)
        {
            StartRecording();
        }

        private void StopSpeakingbutton_Click(object sender, EventArgs e)
        {
            StopRecording();
            
            if (EnglishRadioButton.Checked)
            {
                inputBox.Text = XunfeiFunction.IatModeTranslate("audio/temp.wav", "english");
            }
            else if (ChineseRadioButton.Checked)
            {
                inputBox.Text = XunfeiFunction.IatModeTranslate("audio/temp.wav", "chinese");
            }
            else { }
            
        }

        private void TTSbutton_Click(object sender, EventArgs e)
        {
            string filename = "audio/out.wav";

            if (EnglishRadioButton.Checked)
            {
                XunfeiFunction.ProcessVoice(inputBox.Text, filename, "english", "male");
            }
            else if (ChineseRadioButton.Checked)
            {
                XunfeiFunction.ProcessVoice(inputBox.Text, filename, "chinese");
            }
            else { }

            Play_TTS_file(filename);
        }
        private void Play_TTS_file(string filename)
        {
            NAudio.Wave.WaveFileReader audio = new NAudio.Wave.WaveFileReader(filename);
            NAudio.Wave.IWavePlayer player = new NAudio.Wave.WaveOut(NAudio.Wave.WaveCallbackInfo.FunctionCallback());
            player.Init(audio);
            player.Play();
            while (true)
            {
                if (player.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
                {
                    player.Dispose();
                    //MessageBox.Show("disposed");
                    audio.Close();
                    audio.Dispose();
                    break;
                }
            };
        }
    }
}
