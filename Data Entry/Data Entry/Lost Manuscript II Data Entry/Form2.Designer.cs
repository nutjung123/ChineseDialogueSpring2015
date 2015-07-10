namespace Dialogue_Data_Entry
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.inputBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.queryButton = new System.Windows.Forms.Button();
            this.chatBox = new System.Windows.Forms.TextBox();
            this.StartServerButton = new System.Windows.Forms.Button();
            this.StopServerbutton = new System.Windows.Forms.Button();
            this.StartSpeakingbutton = new System.Windows.Forms.Button();
            this.StopSpeakingbutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // inputBox
            // 
            this.inputBox.Location = new System.Drawing.Point(16, 335);
            this.inputBox.Name = "inputBox";
            this.inputBox.Size = new System.Drawing.Size(614, 20);
            this.inputBox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 319);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(489, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Please enter your query below. our query will be executed with respect to the cur" +
    "rent data that is open.";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // queryButton
            // 
            this.queryButton.Location = new System.Drawing.Point(649, 335);
            this.queryButton.Name = "queryButton";
            this.queryButton.Size = new System.Drawing.Size(75, 23);
            this.queryButton.TabIndex = 2;
            this.queryButton.Text = "Query";
            this.queryButton.UseVisualStyleBackColor = true;
            this.queryButton.Click += new System.EventHandler(this.query_Click);
            // 
            // chatBox
            // 
            this.chatBox.Location = new System.Drawing.Point(16, 18);
            this.chatBox.Multiline = true;
            this.chatBox.Name = "chatBox";
            this.chatBox.ReadOnly = true;
            this.chatBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.chatBox.Size = new System.Drawing.Size(614, 288);
            this.chatBox.TabIndex = 3;
            // 
            // StartServerButton
            // 
            this.StartServerButton.Location = new System.Drawing.Point(649, 18);
            this.StartServerButton.Name = "StartServerButton";
            this.StartServerButton.Size = new System.Drawing.Size(75, 46);
            this.StartServerButton.TabIndex = 4;
            this.StartServerButton.Text = "Start Server";
            this.StartServerButton.UseVisualStyleBackColor = true;
            this.StartServerButton.Click += new System.EventHandler(this.ServerModeButton_Click);
            // 
            // StopServerbutton
            // 
            this.StopServerbutton.Location = new System.Drawing.Point(649, 81);
            this.StopServerbutton.Name = "StopServerbutton";
            this.StopServerbutton.Size = new System.Drawing.Size(74, 45);
            this.StopServerbutton.TabIndex = 5;
            this.StopServerbutton.Text = "Stop Server";
            this.StopServerbutton.UseVisualStyleBackColor = true;
            this.StopServerbutton.Click += new System.EventHandler(this.StopServerbutton_Click);
            // 
            // StartSpeakingbutton
            // 
            this.StartSpeakingbutton.Location = new System.Drawing.Point(649, 277);
            this.StartSpeakingbutton.Name = "StartSpeakingbutton";
            this.StartSpeakingbutton.Size = new System.Drawing.Size(75, 23);
            this.StartSpeakingbutton.TabIndex = 6;
            this.StartSpeakingbutton.Text = "Speak";
            this.StartSpeakingbutton.UseVisualStyleBackColor = true;
            this.StartSpeakingbutton.Click += new System.EventHandler(this.StartSpeakingbutton_Click);
            // 
            // StopSpeakingbutton
            // 
            this.StopSpeakingbutton.Location = new System.Drawing.Point(649, 306);
            this.StopSpeakingbutton.Name = "StopSpeakingbutton";
            this.StopSpeakingbutton.Size = new System.Drawing.Size(75, 23);
            this.StopSpeakingbutton.TabIndex = 7;
            this.StopSpeakingbutton.Text = "Stop";
            this.StopSpeakingbutton.UseVisualStyleBackColor = true;
            this.StopSpeakingbutton.Click += new System.EventHandler(this.StopSpeakingbutton_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(736, 370);
            this.Controls.Add(this.StopSpeakingbutton);
            this.Controls.Add(this.StartSpeakingbutton);
            this.Controls.Add(this.StopServerbutton);
            this.Controls.Add(this.StartServerButton);
            this.Controls.Add(this.chatBox);
            this.Controls.Add(this.queryButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.inputBox);
            this.MaximizeBox = false;
            this.Name = "Form2";
            this.Text = "Query";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox inputBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button queryButton;
        private System.Windows.Forms.TextBox chatBox;
        private System.Windows.Forms.Button StartServerButton;
        private System.Windows.Forms.Button StopServerbutton;
        private System.Windows.Forms.Button StartSpeakingbutton;
        private System.Windows.Forms.Button StopSpeakingbutton;
    }
}