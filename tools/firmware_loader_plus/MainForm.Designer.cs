using System.ComponentModel.Design;
using System.Windows.Forms;
using System;
namespace GD77_FirmwareLoaderPlus
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
			this.btnOpenFile = new System.Windows.Forms.Button();
			this.lblMessage = new System.Windows.Forms.Label();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.SuspendLayout();
			// 
			// btnOpenFile
			// 
			this.btnOpenFile.Location = new System.Drawing.Point(88, 78);
			this.btnOpenFile.Name = "btnOpenFile";
			this.btnOpenFile.Size = new System.Drawing.Size(144, 23);
			this.btnOpenFile.TabIndex = 0;
			this.btnOpenFile.Text = "Select file and upload";
			this.btnOpenFile.UseVisualStyleBackColor = true;
			this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
			// 
			// lblMessage
			// 
			this.lblMessage.Location = new System.Drawing.Point(13, 13);
			this.lblMessage.Name = "lblMessage";
			this.lblMessage.Size = new System.Drawing.Size(289, 31);
			this.lblMessage.TabIndex = 1;
			this.lblMessage.Text = "Please select a firmware file";
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(12, 47);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(290, 15);
			this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar1.TabIndex = 2;
			this.progressBar1.Visible = false;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(314, 113);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.lblMessage);
			this.Controls.Add(this.btnOpenFile);
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.Text = "GD-77 Firmware Loader Plus";
			this.ResumeLayout(false);

		}

		#endregion

		private Button btnOpenFile;
		private Label lblMessage;
		private ProgressBar progressBar1;


	}
}

