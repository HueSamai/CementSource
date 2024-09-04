using System;

namespace CementGB.Installer
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.infoText = new System.Windows.Forms.Label();
            this.retryButton = new System.Windows.Forms.Button();
            this.downloadTimer = new System.Windows.Forms.Timer(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // infoText
            // 
            this.infoText.BackColor = System.Drawing.Color.Transparent;
            this.infoText.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.infoText.ForeColor = System.Drawing.Color.White;
            this.infoText.Location = new System.Drawing.Point(14, 219);
            this.infoText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.infoText.Name = "infoText";
            this.infoText.Size = new System.Drawing.Size(556, 104);
            this.infoText.TabIndex = 1;
            this.infoText.Text = "Couldn\'t find Gang Beasts directory! Make sure Gang Beasts is open.";
            this.infoText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // retryButton
            // 
            this.retryButton.BackColor = System.Drawing.Color.Chartreuse;
            this.retryButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.retryButton.FlatAppearance.BorderSize = 0;
            this.retryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.retryButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.retryButton.ForeColor = System.Drawing.Color.White;
            this.retryButton.Location = new System.Drawing.Point(175, 326);
            this.retryButton.Margin = new System.Windows.Forms.Padding(4);
            this.retryButton.Name = "retryButton";
            this.retryButton.Size = new System.Drawing.Size(234, 58);
            this.retryButton.TabIndex = 2;
            this.retryButton.Text = "Retry";
            this.retryButton.UseVisualStyleBackColor = false;
            this.retryButton.Visible = false;
            this.retryButton.Click += new System.EventHandler(this.Button1_Click);
            // 
            // downloadTimer
            // 
            this.downloadTimer.Interval = 500;
            this.downloadTimer.Tick += new System.EventHandler(this.DownloadTimer_Tick);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = global::CementGB.Installer.Properties.Resources.cement_logo_alpha;
            this.pictureBox1.Location = new System.Drawing.Point(188, 14);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(210, 208);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.CadetBlue;
            this.BackgroundImage = global::CementGB.Installer.Properties.Resources.cement_background;
            this.ClientSize = new System.Drawing.Size(584, 416);
            this.Controls.Add(this.retryButton);
            this.Controls.Add(this.infoText);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cement Installer";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        private void DownloadTimer_Tick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label infoText;
        private System.Windows.Forms.Button retryButton;
        private System.Windows.Forms.Timer downloadTimer;
    }
}

