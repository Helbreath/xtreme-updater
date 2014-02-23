using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

namespace hbx_patch_generator
{
	public partial class Form1 : Form
	{
		List<FileData> filelist = new List<FileData>();
		List<string> datalist = new List<string>();
		List<string> hashgenlist = new List<string>();
		string timestamp = "";
		public Form1()
		{
			InitializeComponent();

			//String[] allfiles = System.IO.Directory.GetFiles(".", "*.*", System.IO.SearchOption.AllDirectories);
			
			try
			{
				LoadDataList();
			}
			catch (System.IO.FileNotFoundException ex)
			{
				
			}

			//node.Name;
			//treeView1.Nodes.Add();
			treeView1.CheckBoxes = true;


			TreeNode node = TreeScan(".");

			treeView1.Nodes.Add(node);
			treeView1.ExpandAll();
			treeView1.AfterCheck += node_AfterCheck;

		}

		private void node_AfterCheck(object sender, TreeViewEventArgs e)
		{
			// The code only executes if the user caused the checked state to change. 
			if (e.Action != TreeViewAction.Unknown)
			{
				if (e.Node.Nodes.Count > 0)
				{
					/* Calls the CheckAllChildNodes method, passing in the current 
					Checked value of the TreeNode whose checked state changed. */
					this.CheckAllChildNodes(e.Node, e.Node.Checked);
				}
			}
		}

		private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
		{
			foreach (TreeNode node in treeNode.Nodes)
			{
				node.Checked = nodeChecked;
				if (node.Nodes.Count > 0)
				{
					// If the current node has child nodes, call the CheckAllChildsNodes method recursively. 
					this.CheckAllChildNodes(node, nodeChecked);
				}
			}
		}

		private TreeNode TreeScan(string sDir)
		{
			TreeNode node = new TreeNode(sDir);
			foreach (string f in Directory.GetFiles(sDir))
			{
 				string[] tokens = f.Split('\\');
				TreeNode temp = node.Nodes.Add(tokens[tokens.Length - 1], f);
				temp.Checked = filelist.Exists(x => x.path == f);
				
			}
			foreach (string d in Directory.GetDirectories(sDir))
			{
				node.Nodes.Add(TreeScan(d));
			}
			return node;
		}

		private void LoadDataList()
		{
			//load
			string[] lines = System.IO.File.ReadAllLines(@".\datalist.txt");
			foreach (string line in lines)
			{
				string[] entries = line.Split('|');
				FileData fd = new FileData();
				fd.path = entries[0];
				fd.name = entries[1];
				fd.md5 = entries[2];
				fd.timestamp = entries[3];
				fd.Checked = true;
				filelist.Add(fd);
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			//save
			long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
			ticks /= 10000000; //Convert windows ticks to seconds
			timestamp = ticks.ToString();

			List<string> linewrite = new List<string>();
			hashgenlist.Clear();
			DumpTrees(treeView1.Nodes);

			//generate hash
			foreach (FileData fd in filelist)
			{
				string temp = "";

				temp = fd.path + "|" + fd.md5 + "|" + fd.timestamp;
				hashgenlist.Add(temp);
				temp = fd.path + "|" + fd.name + "|" + fd.md5 + "|" + fd.timestamp;
				linewrite.Add(temp);
				Compress(new FileInfo(fd.path), fd.path);
			}
			System.IO.File.WriteAllLines(@".\datalist.txt", linewrite.ToArray());
			System.IO.File.WriteAllLines(@".\update.lst", hashgenlist.ToArray());
		}

		public void Compress(FileInfo fi, string path)
		{
			// Get the stream of the source file. 
			using (FileStream inFile = fi.OpenRead())
			{
				// Prevent compressing hidden and already compressed files. 
				if ((File.GetAttributes(fi.FullName) & FileAttributes.Hidden)
						!= FileAttributes.Hidden & fi.Extension != ".gz")
				{
					// Create the compressed file. 
					using (FileStream outFile = File.Create("D:/hbx/" + path + ".gz"))
					{
						using (System.IO.Compression.GZipStream Compress = new System.IO.Compression.GZipStream(outFile,
								System.IO.Compression.CompressionMode.Compress))
						{
							// Copy the source file into the compression stream.
							byte[] buffer = new byte[4096];
							int numRead;
							while ((numRead = inFile.Read(buffer, 0, buffer.Length)) != 0)
							{
								Compress.Write(buffer, 0, numRead);
							}
							Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
								fi.Name, fi.Length.ToString(), outFile.Length.ToString());
						}
					}
				}
			}
		}

		private void DumpTrees(TreeNodeCollection masternode)
		{
			foreach (TreeNode node in masternode)
			{
				if (node.Nodes.Count > 0)
					DumpTrees(node.Nodes);
				else
				{
					if (node.Checked && node.Nodes.Count == 0)
					{
						//add hash
						using (var md5 = MD5.Create())
						{
							using (var stream = File.OpenRead(node.Text))
							{
								string hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","").ToLower();
								if (filelist.Exists(x => (x.path == node.Text)))
								{
									FileData tempfd = filelist.Find(x => (x.path == node.Text));
									if (tempfd.md5 != hash)
									{
										tempfd.updatedthispass = true;
										tempfd.timestamp = timestamp;
									}
								}
								else
								{
									FileData fd = new FileData();
									fd.md5 = hash;
									fd.timestamp = timestamp;
									fd.path = node.Text;
									fd.name = node.Name;
									fd.updatedthispass = true;
									filelist.Add(fd);
								}
							}
						}
					}
				}
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			//load
			try
			{
				LoadDataList();
			}
			catch (System.IO.FileNotFoundException ex)
			{
				
			}
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
