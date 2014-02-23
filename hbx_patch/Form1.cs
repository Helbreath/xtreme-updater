using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace hbx_patch
{
	public partial class hbx_patch : Form
	{
		List<FileData> filelist = new List<FileData>();
		List<FileData> updatelist;
		Thread grabupdatelist;
		Thread verifyfiles;
		Thread downloadfiles;
		AutoResetEvent autoEvent = new AutoResetEvent(false);
		string currentts = "0";
		public hbx_patch()
		{
			InitializeComponent();
			grabupdatelist = new Thread(new ThreadStart(GetUpdateList));
			grabupdatelist.Start();
			button2.Visible = false;
			try
			{
				currentts = System.IO.File.ReadAllText(@".\ts");
			}
			catch (System.IO.FileNotFoundException e)
			{
				currentts = "0";
			}
			System.IO.Directory.CreateDirectory(@".\bin");
			System.IO.Directory.CreateDirectory(@".\data");
			System.IO.Directory.CreateDirectory(@".\fonts");
			System.IO.Directory.CreateDirectory(@".\screenshots");
			System.IO.Directory.CreateDirectory(@".\sprites");
			System.IO.Directory.CreateDirectory(@".\data\lang");
			System.IO.Directory.CreateDirectory(@".\data\mapdata");
			System.IO.Directory.CreateDirectory(@".\data\music");
			System.IO.Directory.CreateDirectory(@".\data\shops");
			System.IO.Directory.CreateDirectory(@".\data\sounds");
			System.IO.Directory.CreateDirectory(@".\data\shops\friends");
			System.IO.Directory.CreateDirectory(@".\data\shops\itemconfigs");
			System.IO.Directory.CreateDirectory(@".\data\shops\mutes");
			System.IO.Directory.CreateDirectory(@".\data\shops\shop");
		}

		public static void Decompress(System.IO.FileInfo fi, string path)
		{
			// Get the stream of the source file. 
			using (System.IO.FileStream inFile = fi.OpenRead())
			{
				// Get original file extension, for example "doc" from report.doc.gz.
				string curFile = fi.FullName;
				string origName = curFile.Remove(curFile.Length - fi.Extension.Length);

				//Create the decompressed file. 
				using (System.IO.FileStream outFile = System.IO.File.Create(origName))
				{
					using (System.IO.Compression.GZipStream Decompress = new System.IO.Compression.GZipStream(inFile,
							System.IO.Compression.CompressionMode.Decompress))
					{
						//Copy the decompression stream into the output file.
						byte[] buffer = new byte[4096];
						int numRead;
						while ((numRead = Decompress.Read(buffer, 0, buffer.Length)) != 0)
						{
							outFile.Write(buffer, 0, numRead);
						}
						Console.WriteLine("Decompressed: {0}", fi.Name);

					}
				}
			}
			fi.Delete();
		}

		private void LoadDataList()
		{
			//load
			string[] lines = System.IO.File.ReadAllLines(@".\update.lst");
			foreach (string line in lines)
			{
				string[] entries = line.Split('|');
				FileData fd = new FileData();
				fd.path = entries[0];
				fd.md5 = entries[1];
				fd.timestamp = entries[2];
				fd.Checked = true;
				filelist.Add(fd);
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			//Get latest files
			IVisible(progressBar1, true);
			IVisible(progressBar2, true);
			IVisible(button1, false);
			IVisible(button2, false);
			IVisible(button3, false);
			progressBar1.Minimum = 0;
			progressBar1.Maximum = 100;

			progressBar2.Minimum = 0;
			progressBar2.Maximum = 100;
			//build file collection list and start downloading
			updatelist = filelist.FindAll(x => (Convert.ToInt64(x.timestamp) > Convert.ToInt64(currentts)));
			downloadfiles = new Thread(new ThreadStart(StartUpdate));
			downloadfiles.Start();
		}
		delegate void ProgressMaxCallback(ProgressBar btn, Int32 val);
		public void IMaxProgress(ProgressBar btn, Int32 val)
		{
			btn.Maximum = val;
		}
		public void StartUpdate()
		{
			ProgressMaxCallback d = new ProgressMaxCallback(IMaxProgress);
			this.Invoke(d, new object[] { progressBar2, updatelist.Count });
			foreach (FileData fd in updatelist)
			{
				IStaticText(label1, "File: " + fd.path.Substring(2));
				GrabFile("http://www.helbreathx.net/patch/" + fd.path.Substring(2) + ".gz", fd.path + ".gz");
				if (Convert.ToInt64(fd.timestamp) > Convert.ToInt64(currentts))
					currentts = fd.timestamp;
				autoEvent.WaitOne();
				Decompress(new System.IO.FileInfo(fd.path + ".gz"), fd.path);
			}
			IStaticText(label1, "");
			System.IO.File.WriteAllText(@".\ts", currentts);
			IVisible(button2, true);
			IVisible(progressBar1, false);
			IVisible(progressBar2, false);
			IDisable(button2, true);
		}
		public void GetUpdateList()
		{
			WebClient client = new WebClient();
			Uri uri = new Uri("http://www.helbreathx.net/patch/update.lst");
			client.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateListReceived);
			client.DownloadFileAsync(uri, "update.lst");
		}
		public void GrabFile(string address, string to)
		{
			progressBar1.Minimum = 0;
			progressBar1.Maximum = 100;
			WebClient client = new WebClient();
			Uri uri = new Uri(address);
			client.DownloadFileAsync(uri, to);
			client.DownloadFileCompleted += new AsyncCompletedEventHandler(FileReceived);
			client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
		}
		delegate void DisableCallback(Button btn, bool toggle);
		public void IDisable(Button btn, bool toggle)
		{
			if (this.InvokeRequired)
			{
				DisableCallback d = new DisableCallback(IDisable);
				this.Invoke(d, new object[] { btn, toggle });
			}
			else
			{
				btn.Enabled = toggle;
			}
		}
		delegate void VisibleCallback(Button btn, bool toggle);
		public void IVisible(Button btn, bool toggle)
		{
			if (this.InvokeRequired)
			{
				VisibleCallback d = new VisibleCallback(IVisible);
				this.Invoke(d, new object[] { btn, toggle });
			}
			else
			{
				btn.Visible = toggle;
			}
		}
		delegate void VisiblePBCallback(ProgressBar btn, bool toggle);
		public void IVisible(ProgressBar btn, bool toggle)
		{
			if (this.InvokeRequired)
			{
				VisiblePBCallback d = new VisiblePBCallback(IVisible);
				this.Invoke(d, new object[] { btn, toggle });
			}
			else
			{
				btn.Visible = toggle;
			}
		}
		delegate void StaticTextCallback(Label btn, string text);
		public void IStaticText(Label btn, string text)
		{
			if (this.InvokeRequired)
			{
				StaticTextCallback d = new StaticTextCallback(IStaticText);
				this.Invoke(d, new object[] { btn, text });
			}
			else
			{
				btn.Visible = true;
				btn.Text = text;
			}
		}
		delegate void BtnTextCallback(Button btn, string text);
		public void IBtnText(Button btn, string text)
		{
			if (this.InvokeRequired)
			{
				BtnTextCallback d = new BtnTextCallback(IBtnText);
				this.Invoke(d, new object[] { btn, text });
			}
			else
			{
				btn.Text = text;
			}
		}
		delegate void ProgressCallback(ProgressBar btn, Int32 val);
		public void IProgress(ProgressBar btn, Int32 val)
		{
			if (this.InvokeRequired)
			{
				ProgressCallback d = new ProgressCallback(IProgress);
				this.Invoke(d, new object[] { btn, val });
			}
			else
			{
				btn.Value = val;
			}
		}
		public void UpdateListReceived(object sender, AsyncCompletedEventArgs e)
		{
			//check data
			LoadDataList();


			//needs update
			if (filelist.Exists(x => (Convert.ToInt64(x.timestamp) > Convert.ToInt64(currentts))))
			{
				IDisable(button1, true);
				IDisable(button3, false);
				IBtnText(button1, "PATCH");
			}
			else
			{
				//can play
				IDisable(button2, true);
				IVisible(button1, false);
				IVisible(button2, true);
			}
		}
		public void FileReceived(object sender, AsyncCompletedEventArgs e)
		{
			IProgress(progressBar2, progressBar2.Value + 1);
			autoEvent.Set();
		}
		public void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
		{
			double bytesIn = double.Parse(e.BytesReceived.ToString());
			double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
			double percentage = bytesIn / totalBytes * 100;
			//progressBar1.Value = int.Parse(Math.Truncate(percentage).ToString());

			//IProgress(progressBar1, Convert.ToInt32((100 / e.TotalBytesToReceive) * e.BytesReceived));
			IProgress(progressBar1, int.Parse(Math.Truncate(percentage).ToString()));
		}

		private void button2_Click(object sender, EventArgs e)
		{
			//Process.Start("bin\\Client.exe", "--start");
			ProcessStartInfo si = new ProcessStartInfo("Client.exe", "--start");
			si.WorkingDirectory = ".\\bin";
			Process.Start(si);
			this.Close();
		}

		private void button3_Click(object sender, EventArgs e)
		{
			verifyfiles = new Thread(new ThreadStart(VerifyFiles));
			verifyfiles.Start();
			IVisible(button1, false);
			IVisible(button2, false);
			IDisable(button3, false);
		}
		private void VerifyFiles()
		{

		}
	}
	public class FileData
	{
		public string timestamp = "";
		public string md5 = "";
		public string name = "";
		public string path = "";
		public bool updatedthispass = false;
		public bool Checked = false;
		public FileData()
		{

		}
	}
}
