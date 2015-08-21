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
            this.EnglishRadioButton = new System.Windows.Forms.RadioButton();
            this.ChineseRadioButton = new System.Windows.Forms.RadioButton();
            this.TTSbutton = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
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
            // EnglishRadioButton
            // 
            this.EnglishRadioButton.AutoSize = true;
            this.EnglishRadioButton.Checked = true;
            this.EnglishRadioButton.Location = new System.Drawing.Point(649, 231);
            this.EnglishRadioButton.Name = "EnglishRadioButton";
            this.EnglishRadioButton.Size = new System.Drawing.Size(59, 17);
            this.EnglishRadioButton.TabIndex = 8;
            this.EnglishRadioButton.TabStop = true;
            this.EnglishRadioButton.Text = "English";
            this.EnglishRadioButton.UseVisualStyleBackColor = true;
            // 
            // ChineseRadioButton
            // 
            this.ChineseRadioButton.AutoSize = true;
            this.ChineseRadioButton.Location = new System.Drawing.Point(649, 254);
            this.ChineseRadioButton.Name = "ChineseRadioButton";
            this.ChineseRadioButton.Size = new System.Drawing.Size(63, 17);
            this.ChineseRadioButton.TabIndex = 9;
            this.ChineseRadioButton.Text = "Chinese";
            this.ChineseRadioButton.UseVisualStyleBackColor = true;
            // 
            // TTSbutton
            // 
            this.TTSbutton.Location = new System.Drawing.Point(649, 151);
            this.TTSbutton.Name = "TTSbutton";
            this.TTSbutton.Size = new System.Drawing.Size(75, 23);
            this.TTSbutton.TabIndex = 10;
            this.TTSbutton.Text = "TTS";
            this.TTSbutton.UseVisualStyleBackColor = true;
            this.TTSbutton.Click += new System.EventHandler(this.TTSbutton_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(649, 209);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(83, 17);
            this.checkBox1.TabIndex = 11;
            this.checkBox1.Text = "Same Lang.";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(649, 186);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(89, 17);
            this.checkBox2.TabIndex = 12;
            this.checkBox2.Text = "TTS Enabled";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(736, 370);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.TTSbutton);
            this.Controls.Add(this.ChineseRadioButton);
            this.Controls.Add(this.EnglishRadioButton);
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
        private System.Windows.Forms.RadioButton EnglishRadioButton;
        private System.Windows.Forms.RadioButton ChineseRadioButton;
        private System.Windows.Forms.Button TTSbutton;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
    }
}