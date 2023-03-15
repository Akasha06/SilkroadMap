using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

class MainClass {
	// Client Language
	private static byte LanguageIndex = 9;


	/// <summary>
	/// Name references from the game.
	/// </summary>
	static Dictionary<string, string> NameReferences;
	/// <summary>
	/// Models (NPC,Mob,Char,..) used by the game.
	/// </summary>
	static Dictionary<uint,Model> Models;
	/// <summary>
	/// Raw of data about Teleport linking
	/// </summary>
	static Dictionary<string, string[]> TeleportData,TeleportAndBuildings;
	/// <summary>
	/// Region references from the game.
	/// </summary>
	static Dictionary<string, string> RegionReferences;
	/// <summary>
	/// Teleport links from game;
	/// </summary>
	static Dictionary<uint,Teleport> TeleportsLinks;
	static Dictionary<uint,Teleport> StoreLinks;
  public static void Main (string[] args) {
		Console.WriteLine("Loading name references..");
    LoadNameReferences();
		Console.WriteLine("Loading models..");
		LoadModels();
		Console.WriteLine("Generating NPC's..");
		GenerateNPCs();
		Console.WriteLine("NPCs.js generated successfully!");
		Console.WriteLine("Loading TeleportData..");
		LoadTeleportData();
		Console.WriteLine("Loading zonename references..");
		LoadRegions();
		Console.WriteLine("Creating Teleport Links..");
		LoadTeleportLinks();
		Console.WriteLine("Generating Teleports..");
		GenerateTeleportLinks();
		Console.WriteLine("TPs.js & NPCsLinked.js generated successfully!");
  }
	private static void LoadNameReferences(){
		NameReferences = new Dictionary<string, string>();
		
		string[] files = new string[] { "textdata_equip&skill_all.txt","textdata_object_all.txt"};

		string[] data;
		string line;
		foreach (string file in files)
		{
			// Keep memory safe
			using (StreamReader reader = new StreamReader(file))
			{
				while (!reader.EndOfStream)
				{
					if ((line = reader.ReadLine()) == null)
						continue;
					
					// Data enabled in game
					if (line.StartsWith("1\t"))
					{
						data = line.Split("\t", StringSplitOptions.None);

						if (data.Length > LanguageIndex && data[LanguageIndex] != "0"){
							int dummy;
              if(int.TryParse(data[1],out dummy)) // iSRO file type
                NameReferences[data[2]] = data[LanguageIndex];
              else
							  NameReferences[data[1]] = data[LanguageIndex];
						}
					}
				}
			}
		}
	}
	private static string GetNameReference(string ServerName)
	{
		if (NameReferences.ContainsKey(ServerName))
			return NameReferences[ServerName];
		return "";
	}
	private static void LoadModels()
	{
		Models = new Dictionary<uint,Model>();
		string[] data;
		string line;
		// Keep memory safe
		using (StreamReader reader = new StreamReader("characterdata_all.txt"))
		{
			while (!reader.EndOfStream)
			{
				if ((line = reader.ReadLine()) == null)
					continue;
				// Data is enabled in game
				if (line.StartsWith("1\t"))
				{
					data = line.Split("\t", StringSplitOptions.None);
					// Extract name if has one
					string name = "";
					if (data[5] != "xxx")
						name = GetNameReference(data[5]);
					// INSERT OR UPDATE
					Model model = new Model();
					model.ID = uint.Parse(data[1]);
					model.ServerName = data[2];
					model.Name = name;
					model.tid2 = byte.Parse(data[10]);
					model.tid3 = byte.Parse(data[11]);
					model.tid4 = byte.Parse(data[12]);
					Models[model.ID] = model;
				}
			}
		}
	}
	class Model: IComparable
	{
		public uint ID;
		public string ServerName;
		public string Name;
		public byte tid2,tid3,tid4;
		// keep it as string
		public string Region,X,Y,Z;
		// Quick, avoid generate another class
		public List<Teleport> Links = new List<Teleport>();
		public Model(){
			
		}
		public Model(Model m){
			ID = m.ID;
			ServerName = m.ServerName;
			Name = m.Name;
			tid2 = m.tid2;
			tid3 = m.tid3;
			tid4 = m.tid4;
		}
		public int CompareTo(object obj) {
			Model m = obj as Model;
      return String.Compare(Name,m.Name,StringComparison.OrdinalIgnoreCase);
    }
	}
	static List<Model> modelNpcs;
	private static void GenerateNPCs(){
		modelNpcs = new List<Model>();

		string[] data;
		string line;
		using (StreamReader reader = new StreamReader("npcpos.txt"))
		{
			while (!reader.EndOfStream)
			{
				if ((line = reader.ReadLine()) == null)
					continue;
				data = line.Split("\t", StringSplitOptions.None);

				Model m = Models[uint.Parse(data[0])];
				// filter
				//  NPC
				if(m.tid2 == 2){
					// Guide
					if(m.tid3 == 2)
					{
						if(m.Name != ""){
							Model model = new Model(m);
							model.Region = data[1];
							model.X = data[2];
							model.Z = data[3];
							model.Y = data[4];
							modelNpcs.Add(model);
						}
					}
				}
			}
		}
		modelNpcs.Sort();
		// Print out .js
		string fileOutput = "var NPCs=[";
		for(int i=0;i<modelNpcs.Count;i++){
			Model m = modelNpcs[i];

			// links
			string foDestinations = "";
			if(m.Links.Count > 0){
				foreach(Teleport d in m.Links){
					foDestinations+="{'name':'"+d.Name.Replace("'",@"\'")+"','region':"+d.Region+",'x':"+d.X+",'z':"+d.Z+",'y':"+d.Y+"},";
				}
				// remove last comma
				foDestinations = foDestinations.Remove(foDestinations.Length-1);
			}
			
			fileOutput += "{'name':'"+m.Name.Replace("'",@"\'")+"','region':"+m.Region+",'x':"+m.X+",'z':"+m.Z+",'y':"+m.Y+"},";
		}
		// remove last comma
		fileOutput = fileOutput.Remove(fileOutput.Length-1); 
		fileOutput += "];";
		File.WriteAllText("NPCs.js", fileOutput);
	}
	private static void LoadTeleportData(){
		TeleportData = new Dictionary<string, string[]>();

		string line;
		string[] data;
		using (StreamReader reader = new StreamReader("teleportdata.txt"))
		{
			while (!reader.EndOfStream)
			{
				if ((line = reader.ReadLine()) == null)
					continue;
				// Data is enabled in game
				if (line.StartsWith("1\t")){
					data = line.Split("\t", StringSplitOptions.None);
					TeleportData[data[1]] = data;
				}
			}
		}
		TeleportAndBuildings = new Dictionary<string, string[]>();
		using (StreamReader reader = new StreamReader("teleportbuilding.txt"))
		{
			while (!reader.EndOfStream)
			{
				if ((line = reader.ReadLine()) == null)
					continue;
				// Data is enabled in game
				if (line.StartsWith("1\t")){
					data = line.Split("\t", StringSplitOptions.None);
					TeleportAndBuildings[data[1]] = data;
				}
			}
		}
	}
	private static void LoadRegions(){
		RegionReferences = new Dictionary<string, string>();
		
		string line;
		string []data;
		using (StreamReader reader = new StreamReader("textzonename_all.txt"))
		{
			while (!reader.EndOfStream)
			{
				if ((line = reader.ReadLine()) == null)
					continue;

				// Data is enabled on the game
				if (line.StartsWith("1\t"))
				{
					data = line.Split("\t", StringSplitOptions.None);
					
					if (data[LanguageIndex] != "0")
						RegionReferences[data[1]] = data[LanguageIndex];
				}
			}
		}
	}
	private static string GetRegionReference(string ServerName)
	{
		if (RegionReferences.ContainsKey(ServerName))
			return RegionReferences[ServerName];
		return "";
	}
	private static void LoadTeleportLinks(){
		TeleportsLinks = new Dictionary<uint, Teleport>();
		StoreLinks = new Dictionary<uint, Teleport>();

		string line;
		string[] data;
		using (StreamReader reader = new StreamReader("teleportlink.txt"))
		{
			while (!reader.EndOfStream)
			{
				if ((line = reader.ReadLine()) == null)
					continue;
				// Data is enabled on the game
				if (line.StartsWith("1\t"))
				{
					data = line.Split("\t", StringSplitOptions.None);

					// Load or create
					uint SourceID = uint.Parse(data[1]);

					if(TeleportsLinks.ContainsKey(SourceID)){
						// Its created, add link only
						Teleport _tp = TeleportsLinks[SourceID];
						// add new link
						Teleport.Destination _d = new Teleport.Destination();
						// destination name
						uint _dID = uint.Parse(TeleportData[data[2]][3]);
						if(Models.ContainsKey(_dID)){
							_d.Name = Models[_dID].Name;
						}
						else{
							_d.Name = GetNameReference(TeleportData[data[2]][4]);
						}
						if(_d.Name == ""){
							_d.Name = TeleportData[data[2]][2];
						}
						_d.Region = TeleportData[data[2]][5];
						_d.X = TeleportData[data[2]][6];
						_d.Z = TeleportData[data[2]][7];
						_d.Y = TeleportData[data[2]][8];	
						_tp.Links.Add(_d);
						continue;
					}
					Teleport tp = new Teleport();
					TeleportsLinks[SourceID] = tp;

					tp.ID = uint.Parse(TeleportData[data[1]][3]);
					tp.SourceID = SourceID;
					StoreLinks[tp.ID] = tp;
					// Extract name
					if(Models.ContainsKey(tp.ID)){
						tp.Name = Models[tp.ID].Name;
					}
					else{
						tp.Name = GetNameReference(TeleportData[data[1]][4]);
					}
					if(tp.Name == ""){
						tp.Name = TeleportData[data[1]][2];
					}
					tp.ServerName = TeleportData[data[1]][2];
				
					tp.Region = TeleportData[data[1]][5];
					tp.X = TeleportData[data[1]][6];
					tp.Z = TeleportData[data[1]][7];
					tp.Y = TeleportData[data[1]][8];
					// link
					Teleport.Destination d = new Teleport.Destination();
					// destination name
					uint dID = uint.Parse(TeleportData[data[2]][3]);
					if(Models.ContainsKey(dID)){
						d.Name = Models[dID].Name;
					}
					else
					{
						d.Name = GetNameReference(TeleportData[data[2]][4]);
					}
					if(d.Name == ""){
						d.Name = TeleportData[data[2]][2];
					}
					d.Region = TeleportData[data[2]][5];
					d.X = TeleportData[data[2]][6];
					d.Z = TeleportData[data[2]][7];
					d.Y = TeleportData[data[2]][8];	
					tp.Links.Add(d);
				}
			}
		}
	}
	class Teleport{
		public uint ID;
		public uint SourceID;
		public string Name;
		public string ServerName;
		// keep it as string
		public string X,Y,Z,Region;
		public List<Destination> Links = new List<Destination>();
		public class Destination{
			public string Name;
			public string X,Y,Z,Region;
		}
	}
	private static void GenerateTeleportLinks(){
		// Print out TPs.js
		string fileOutput = "var TPs=[";
		foreach(string key in TeleportAndBuildings.Keys){
			uint id = uint.Parse(key);
			if(StoreLinks.ContainsKey(id))
			{
				string[] data = TeleportAndBuildings[key];

				string type = data[12];
				Teleport tp = TeleportsLinks[StoreLinks[id].SourceID];
				
				// links
				string foDestinations = "";
				if(tp.Links.Count > 0){
					foreach(Teleport.Destination d in tp.Links){
						foDestinations+="{'name':'"+d.Name.Replace("'",@"\'")+"','region':"+d.Region+",'x':"+d.X+",'z':"+d.Z+",'y':"+d.Y+"},";
					}
					// remove last comma
					foDestinations = foDestinations.Remove(foDestinations.Length-1);
				}
				tp.Name = GetNameReference(data[5]);
				if(tp.Name == "")
					tp.Name = tp.ServerName;

				fileOutput += "{'name':'"+tp.Name.Replace("'",@"\'")+"','region':"+data[41]+",'x':"+data[43]+",'z':"+data[44]+",'y':"+data[45]+",'type':"+type+",'teleport':["+foDestinations+"]},";

				TeleportsLinks.Remove(StoreLinks[id].SourceID);
				StoreLinks.Remove(id);
			}
		}

		// Print out NPCsLinked.js
		string NPCFileOutput = "var NPCs=[";
		foreach(Model m in modelNpcs){
			// links
			string npcFODestinations = "";
			if(StoreLinks.ContainsKey(m.ID)){
				Teleport tp = StoreLinks[m.ID];
				
				foreach(Teleport.Destination d in tp.Links){
					npcFODestinations+="{'name':'"+d.Name.Replace("'",@"\'")+"','region':"+d.Region+",'x':"+d.X+",'z':"+d.Z+",'y':"+d.Y+"},";
				}
				// remove last comma
				npcFODestinations = npcFODestinations.Remove(npcFODestinations.Length-1);

				TeleportsLinks.Remove(tp.SourceID);
			}
			
			NPCFileOutput += "{'name':'"+m.Name.Replace("'",@"\'")+"','region':"+m.Region+",'x':"+m.X+",'z':"+m.Z+",'y':"+m.Y+",'teleport':["+npcFODestinations+"]},";
		}
		// remove last comma
		NPCFileOutput = NPCFileOutput.Remove(NPCFileOutput.Length-1); 
		NPCFileOutput += "];";
		File.WriteAllText("NPCsLinked.js", NPCFileOutput);

		foreach(uint id in TeleportsLinks.Keys){
			Teleport tp = TeleportsLinks[id];
			
			// links
			string foDestinations = "";
			foreach(Teleport.Destination d in tp.Links){
				foDestinations+="{'name':'"+d.Name.Replace("'",@"\'")+"','region':"+d.Region+",'x':"+d.X+",'z':"+d.Z+",'y':"+d.Y+"},";
			}
			// remove last comma
			foDestinations = foDestinations.Remove(foDestinations.Length-1);
			
			fileOutput += "{'name':'"+tp.Name.Replace("'",@"\'")+"','region':"+tp.Region+",'x':"+tp.X+",'z':"+tp.Z+",'y':"+tp.Y+",'type':5,'teleport':["+foDestinations+"]},";
		}

		// remove last comma
		fileOutput = fileOutput.Remove(fileOutput.Length-1); 
		fileOutput += "];";
		File.WriteAllText("TPs.js", fileOutput);
	}
}