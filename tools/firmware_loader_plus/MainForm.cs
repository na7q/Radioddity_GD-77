/*
 * Copyright (C)2019 Roger Clark. VK3KYY
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *   1. Redistributions of source code must retain the above copyright notice,
 *      this list of conditions and the following disclaimer.
 *   2. Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *   3. The name of the author may not be used to endorse or promote products
 *      derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.IO;
using UsbLibrary;
using System.Threading;

namespace GD77_FirmwareLoaderPlus
{
	public partial class MainForm : Form
	{
		private string _filename="";
		public MainForm(string[] args)
		{
			InitializeComponent();
			this.CenterToScreen();
			this.BringToFront();
			this.Refresh();
			if (args.Length > 0)
			{
				try
				{
					if (File.Exists(args[0]))
					{
						if (Path.GetExtension(args[0]) == ".bin" || Path.GetExtension(args[0]) == ".sgl")
						{
							uploadFile(args[0]);
						}
						else
						{
							lblMessage.Text = "Only .sgl or .bin files can be uploaded";
						}
					}
				}
				catch(Exception)
				{
					lblMessage.Text = "Sorry. There has been an error";
				}
			}
		}

		public void SetLabel(string txt)
		{
			if (this.lblMessage.InvokeRequired)
			{
				lblMessage.Invoke((MethodInvoker)(() => lblMessage.Text = txt));
				//this.Refresh();
			}
			else
			{
				this.lblMessage.Text = txt;
			}
		}

		public void SetProgressPercentage(int perc)
		{
			if (this.lblMessage.InvokeRequired)
			{
				progressBar1.Invoke((MethodInvoker)(() => progressBar1.Value = perc));
			}
			else
			{
				progressBar1.Value = perc;
			}

		}

		private void uploadFile(string filename)
		{
			try
			{
				progressBar1.Visible = true;
				_filename = filename;
				Thread th = new Thread(new ThreadStart(firmwareLoaderStart));

				th.Start();
				;
			}
			catch (Exception)
			{
				lblMessage.Text = "Error while uploading the firmware";
			}
		}
		private void firmwareLoaderStart()
		{
			FirmwareLoader.UploadFirmare(_filename, this);
		}

		private void btnOpenFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog1 = new OpenFileDialog();
			openFileDialog1.Filter = "firmware files (*.sgl;*.bin)|*.sgl;*.bin|All files (*.*)|*.*";
			openFileDialog1.RestoreDirectory = true;

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				uploadFile(openFileDialog1.FileName);
			}
		}
	}
}
