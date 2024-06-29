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

public class GraphicPack{
	public string Name;
	public bool Active;
	public string FullPath;
	public string Rules;

	public void Toggle(){
		Active = !Active;
	}

	public void Update(){
		string rulePath = Active ? Rules + ".disabled" : Rules;
		string newRulePath = Active ? Rules : Rules + ".disabled";

		if(File.Exists(newRulePath)) return;

		if(!File.Exists(rulePath) && !File.Exists(newRulePath)){
			throw new Exception("Could not find: " + rulePath);
		}

		File.Move(rulePath, newRulePath);
	}

	public GraphicPack(string path){
		FullPath = path;
		Name = Path.GetFileName(FullPath);
		Rules = Path.Combine(FullPath, "rules.txt");

		Active = File.Exists(Rules);
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
	static string ToolTitle = $"{title} - v{version}";

	static void RefreshUI(int index, List<GraphicPack> graphicPacks){
		Console.Clear();
		Console.WriteLine($"{ToolTitle}\n");

		int graphicPackIndex = 0;
		graphicPacks.ForEach(graphicPack => {
			string selected = index == graphicPackIndex ? " -> " : "    ";
			string active = graphicPack.Active ? "x" : " ";

			Console.WriteLine($"{selected} [{active}] {graphicPack.Name}");

			graphicPackIndex++;
		});

		Console.WriteLine("\nUse up and down arrow keys to change the selection. Use Enter to toggle a mod on and off. Use Spacebar to continue.\n");
	}

	static void UI(string root, List<GraphicPack> graphicPacks){
		int index = 0;
		int maxIndex = graphicPacks.Count;

		ConsoleKeyInfo userInput;
		do{
			RefreshUI(index, graphicPacks);
			userInput = Console.ReadKey();

			switch(userInput.Key){
				case ConsoleKey.Enter:
					graphicPacks[index].Toggle();
					break;

				case ConsoleKey.UpArrow:
					index = index > 0 ? index - 1 : maxIndex - 1;
					break;

				case ConsoleKey.DownArrow:
					index = index < maxIndex - 1 ? index + 1 : 0;
					break;
			}
		}while(userInput.Key != ConsoleKey.Spacebar);

		Console.Clear();
		Console.WriteLine($"{ToolTitle}\n");

		graphicPacks.ForEach(graphicPack => {
			graphicPack.Update();

			if(!graphicPack.Active) return;
			
			decompressedFileSize.Update(Path.Combine(graphicPack.FullPath, "content"));
			fileSize.Update(Path.Combine(root, "content"), (entry) => {
				if(entry.Name != "DecompressedSizeList.txt" && entry.Name != "FileSizeList.txt") return false;

				return true;
			});

			string decompressedfilepath = Path.Combine(graphicPack.FullPath, "content", "DecompressedSizeList.txt");
			string filepath = Path.Combine(graphicPack.FullPath, "content", "FileSizeList.txt");

			if(File.Exists(decompressedfilepath)) File.Move(decompressedfilepath, decompressedfilepath + ".back");
			if(File.Exists(filepath)) File.Move(filepath, filepath + ".back");
		});

		string DummyPackPath = Path.Combine(root, CombinedFileSizeModName);
		Directory.CreateDirectory(DummyPackPath);
		File.WriteAllText(Path.Combine(DummyPackPath, "rules.txt"), @"
[Definition]
titleIds = 000500001019E500,000500001019E600,000500001019C800
name = Combined File Size Mod
path = ""The Legend of Zelda: Twilight Princess HD/Mods/Combined File Size Mode""
description = Overrides all active mods' FileSizeList.txt and DecompressedFileSize.txt files by merging the relevant sizes.
version = 7
		");

		string DummyPackContentPath = Path.Combine(DummyPackPath, "content");
		Directory.CreateDirectory(DummyPackContentPath);

		decompressedFileSize.FilePath = Path.Combine(DummyPackContentPath, "DecompressedSizeList.txt");
		decompressedFileSize.Save();

		fileSize.FilePath = Path.Combine(DummyPackContentPath, "FileSizeList.txt");
		var decompressedFileSizeEntry = fileSize.Entries.Single(entry => entry.Name == "DecompressedSizeList.txt");
		if(decompressedFileSizeEntry == null){
			throw new Exception("Could not update the DecompressedSizeList.txt size entry in FileSizeList.txt");
		}
		decompressedFileSizeEntry.Size = decompressedFileSize.ToString().Length;
		fileSize.Save();
	}

	static List<GraphicPack> GetGraphicPacks(string root){
		List<string> folders = Directory.GetDirectories(root).ToList();

		List<GraphicPack> graphicPacks = new List<GraphicPack>();

		string packNameFilter = "TwilightPrincessHD_";

		folders.ForEach(folder => {
			if(!Path.GetFileName(folder).StartsWith(packNameFilter)) return;
			if(Path.GetFileName(folder) == CombinedFileSizeModName) return;

			string Rules = Path.Combine(folder, "rules.txt");
			if(!File.Exists(Rules) && !File.Exists(Rules + ".disabled")) return;

			if(!Directory.Exists(Path.Combine(folder, "content"))) return;

			graphicPacks.Add(new GraphicPack(folder));
		});

		return graphicPacks;
	}

	static string ReadEmbeddedTextFile(string name){
		string resourceName = ExecutingAssembly.GetManifestResourceNames().Single(str => {
			return str == name;
		});
		using(Stream stream = ExecutingAssembly.GetManifestResourceStream(resourceName))
		using(StreamReader reader = new StreamReader(stream)){
    		string result = reader.ReadToEnd();
			return result;
		}
	}

	static void SerialiseFileSize(string root){
		var decompressedFileSizeListTxt = new DecompressedFileSize("DecompressedSizeList.txt", root);
		decompressedFileSizeListTxt.Serialize();

		var fileSizeListTxt = new FileSize("FileSizeList.txt", root);
		fileSizeListTxt.Serialize();
	}

	public static DecompressedFileSize decompressedFileSize;
	public static FileSize fileSize;
	public static string CombinedFileSizeModName = "TwilightPrincessHD_CombinedFileSizeMod";

	static void Main(){
		Console.WriteLine($"{ToolTitle}\n");

		string root = Directory.GetCurrentDirectory();

		if(File.Exists(Path.Combine(root, "Cemu.exe"))){
			root = Path.Combine(root, "graphicPacks");
		}

		if(!root.EndsWith("graphicPacks") || !Directory.Exists(root)){
			Console.WriteLine("Please run this executable inside your Cemu folder (next to the executable) or inside the graphicPacks folder!");
    		Console.ReadLine();
			return;
		}

		decompressedFileSize = new DecompressedFileSize(() => {
			return ReadEmbeddedTextFile("DecompressedSizeListEntries.xml");
		});

		fileSize = new FileSize(() => {
			return ReadEmbeddedTextFile("FileSizeListEntries.xml");
		});

		List<GraphicPack> graphicPacks = GetGraphicPacks(root);

		UI(root, graphicPacks);

		Console.WriteLine("\nDone!");
    	Console.ReadLine();
	}
}