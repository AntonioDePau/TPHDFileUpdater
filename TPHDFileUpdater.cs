using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Globalization;
using System.Text;

public class DecompressedFileSizeEntry{
	[XmlIgnore]
	public string Filepath;
	
	[XmlElement("name")]
	public string Name;
	
	[XmlIgnore]
	public string CleanName;
	
	[XmlElement("size")]
	public long Size;
	
	[XmlElement("decompressedSize")]
	public long DecompressedSize;

	[XmlIgnore]
	public bool Updated = false;
	
	[XmlElement("ratio")]
	public string Ratio = "";

	public DecompressedFileSizeEntry(string root, string file, string filepath, long size, long decompressedSize, string ratio){
		Filepath = filepath;
		Name = file.Replace(root, "");
		CleanName = Name.Replace("./DVDRoot", "content").Replace("/", "\\");
		Size = size;
		DecompressedSize = decompressedSize;
		Ratio = ratio;
	}

    public void UpdateInfo(){
        CleanName = Name.Replace("./DVDRoot/", "").Replace("/", "\\");
	}

	public DecompressedFileSizeEntry(){}

	override public string ToString(){
		return $"{Size}\0{DecompressedSize}\0{Ratio}\0{Name}\0";
	}
}

public class DecompressedFileSize{
	public string FilePath = null;
	public List<DecompressedFileSizeEntry> Entries = new List<DecompressedFileSizeEntry>();

	public void Serialize(string xmlPath = "DecompressedSizeListEntries.xml"){
		using(var writer = new FileStream(xmlPath, FileMode.Create)){
        	XmlSerializer ser = new XmlSerializer(typeof(List<DecompressedFileSizeEntry>), new XmlRootAttribute("DecompressedSizeListEntries"));
			ser.Serialize(writer, Entries);
		}
	}

	public void Deserialize(string text){
		using(var reader = new StringReader(text)){
        	XmlSerializer deserializer = new XmlSerializer(typeof(List<DecompressedFileSizeEntry>),  
            	new XmlRootAttribute("DecompressedSizeListEntries"));
        	Entries = (List<DecompressedFileSizeEntry>)deserializer.Deserialize(reader);
			Entries.ForEach(entry => {
				entry.UpdateInfo();
			});
		}
	}

	public DecompressedFileSize(string xmlPath){
		FilePath = xmlPath;

		string text = File.ReadAllText(FilePath);
		
		Deserialize(text);
	}

	public DecompressedFileSize(Func<string> function){
		string text = function();

		Deserialize(text);
	}

	public DecompressedFileSize(string DecompressedFileSize, string root){
		FilePath = DecompressedFileSize;

		string text = File.ReadAllText(FilePath);
		string[] parts = text.Split((char)0x0);

		for(int i = 0; i < parts.Length; i += 4){
			if(i + 1 >= parts.Length) break;

			string filesize = parts[i];
			string decompressedfilesize = parts[i + 1];
			string compressionratio = parts[i + 2];
			string filename = parts[i + 3];
			string filepath = Path.Combine(root, filename);

			long size = long.Parse(filesize);
			long decompressedSize = long.Parse(decompressedfilesize);

			Entries.Add(new DecompressedFileSizeEntry(root, filename, filepath, size, decompressedSize, compressionratio));
		}

		Console.WriteLine("\nDeompressedFileSize entries found: " + Entries.Count);
	}

	public void Update(string root = null){
		if(root != null) Entries.ForEach(entry => {
			string filepath = Path.Combine(root, entry.CleanName);
			bool exists = File.Exists(filepath);

			if(File.Exists(filepath + ".gz")){
				exists = true;
				filepath = filepath + ".gz";
				entry.Filepath = filepath;
			}
			entry.Updated = exists;
		});

		List<DecompressedFileSizeEntry> updated = Entries.Where(entry => entry.Updated == true).ToList();
		updated.ForEach(updatedFile => {
			string filepath = updatedFile.Filepath;
			byte[] bytes = File.ReadAllBytes(filepath);
			updatedFile.Size = bytes.Length;

			using(MemoryStream ms = new MemoryStream(bytes))
			using(BinaryReader br = new BinaryReader(ms)){
				ms.Seek(-4, SeekOrigin.End);
				updatedFile.DecompressedSize = (long)br.ReadUInt32();
			}

			double ratio = (1 - ((double)updatedFile.Size / (double)updatedFile.DecompressedSize));
			string percent = ratio.ToString("P2", new NumberFormatInfo{
				PercentPositivePattern = 1,
				PercentNegativePattern = 1
			});

			updatedFile.Ratio = percent;
			
			Console.WriteLine("Updated [DFSE]: " + updatedFile.ToString());
		});
	}

	public void Save(){
		StringBuilder sb = new StringBuilder();
		Entries.ForEach(entry => {
			sb.Append(entry.ToString());
		});
		File.WriteAllText(FilePath, sb.ToString());
	}

	override public string ToString(){
		StringBuilder sb = new StringBuilder();

		Entries.ForEach(entry => {
			sb.Append(entry.ToString());
		});

		return sb.ToString();
	}
}

public class FileSizeEntry{
	[XmlIgnore]
	public string Filepath;

	[XmlElement("name")]
	public string Name;

	[XmlIgnore]
	public string CleanName;

	[XmlElement("size")]
	public long Size;

	[XmlIgnore]
	public bool Updated = false;

	public FileSizeEntry(string root, string file, string cleanname, string filepath, long size){
		Filepath = filepath;
		Name = file.Replace(root, "");
		CleanName = cleanname.Replace(root, "");
		Size = size;
	}

	public void UpdateInfo(){
		CleanName = Name;
	}

	public FileSizeEntry(){}

	override public string ToString(){
		return $"{Name}\0{Size}\0";
	}
}

public class FileSize{
	public string FilePath = null;
	public List<FileSizeEntry> Entries = new List<FileSizeEntry>();

	public void Serialize(string xmlPath = "FileSizeListEntries.xml"){
		using(var writer = new FileStream(xmlPath, FileMode.Create)){
        	XmlSerializer ser = new XmlSerializer(typeof(List<FileSizeEntry>), new XmlRootAttribute("FileSizeListEntries"));
			ser.Serialize(writer, Entries);
		}
	}

	public void Deserialize(string text){
		using(var reader = new StringReader(text)){
        	XmlSerializer deserializer = new XmlSerializer(typeof(List<FileSizeEntry>),  
            	new XmlRootAttribute("FileSizeListEntries"));
        	Entries = (List<FileSizeEntry>)deserializer.Deserialize(reader);
			Entries.ForEach(entry => {
				entry.UpdateInfo();
			});
		}
	}

	public FileSize(string xmlPath){
		FilePath = xmlPath;

		string text = File.ReadAllText(FilePath);
		
		Deserialize(text);
	}

	public FileSize(Func<string> function){
		string text = function();

		Deserialize(text);
	}

	public FileSize(string FileSizeList, string root){
		FilePath = FileSizeList;

		string text = File.ReadAllText(FilePath);
		string[] parts = text.Split((char)0x0);

		for(int i = 0; i < parts.Length; i += 2){
			if(i + 1 >= parts.Length) break;

			string filename = parts[i];
			string cleanname = filename.Replace("/", "\\");
			string filesize = parts[i + 1];
			string filepath = Path.Combine(root, "content", cleanname);

			long size = long.Parse(filesize);

			Entries.Add(new FileSizeEntry(root, filename, cleanname, filepath, size));
		}

		Console.WriteLine("\nFileSize entries found: " + Entries.Count);
	}

	public void Update(string root = null, Func<FileSizeEntry, bool> entryCallback = null){
		if(root != null) Entries.ForEach(entry => {
			string filepath = Path.Combine(root, entry.CleanName);
			bool exists = File.Exists(filepath);

			if(exists) entry.Filepath = filepath;

			if(entryCallback != null && entryCallback(entry)) return;

			entry.Updated = exists;
		});

		List<FileSizeEntry> updated = Entries.Where(entry => entry.Updated == true).ToList();
		updated.ForEach(updatedFile => {
			string filepath = updatedFile.Filepath;
			byte[] bytes = File.ReadAllBytes(filepath);
			updatedFile.Size = bytes.Length;
			Console.WriteLine("Updated [FSLE]: " + updatedFile.ToString());
		});
	}

	public void Save(){
		StringBuilder sb = new StringBuilder();
		Entries.ForEach(entry => {
			sb.Append(entry.ToString());
		});
		File.WriteAllText(FilePath, sb.ToString());
	}
}

public class TPHDHelper{
	static void UpdateFileData(List<string> moddedFiles, string fileSizeList, string decompressedFileSizePath, string root){
		var decompressedFileSize = new DecompressedFileSize(decompressedFileSizePath, root);
		decompressedFileSize.Update();
		decompressedFileSize.Save();

		var fileSize = new FileSize(fileSizeList, root);
		fileSize.Update();
		fileSize.Save();
	}

	static void InitParsing(string root){
		string FileSizeList = null;
		string DecompressedSizeList = null;

		List<string> moddedFiles = new List<string>();
		List<string> files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories).ToList();

		files.ForEach(file => {
			if(file == Path.Combine(root, @"content\FileSizeList.txt")){
				FileSizeList = file;
				return;
			}

			if(file == Path.Combine(root, @"content\DecompressedSizeList.txt")){
				DecompressedSizeList = file;
				return;
			}

			moddedFiles.Add(file);
		});

		if(FileSizeList == null){
			Console.WriteLine("Could not find FileSizeList.txt!");
			return;
		}

		if(DecompressedSizeList == null){
			Console.WriteLine("Could not find DecompressedSizeList.txt!");
			return;
		}

		Console.WriteLine("FileSizeList file: " + FileSizeList);
		Console.WriteLine("DecompressedSizeList file: " + DecompressedSizeList);

		UpdateFileData(moddedFiles, FileSizeList, DecompressedSizeList, root);
	}

	static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
	static string version = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location).ProductVersion;
	static string title = ExecutingAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

	static void Main(string[] args){
		Console.WriteLine($"{title} - v{version}\n");

		string root = Directory.GetCurrentDirectory();

		if(args.Length > 0 && args[0] != null && Directory.Exists(args[0])){
			root = args[0];
		}

		Console.WriteLine("Active directory: " + root + "\n");

		InitParsing(root);

		Console.WriteLine("\nDone!");
    	Console.ReadLine();
	}
}