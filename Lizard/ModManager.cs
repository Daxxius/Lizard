using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpriteDictionary = System.Collections.Generic.Dictionary<string, UnityEngine.Sprite>;

namespace Lizard
{
	internal static class ModManager
	{
		private struct InfoLink
		{
			public Type infoType;
			public FieldInfo dict;
			public FieldInfo id;
			public FieldInfo displayName;
			public FieldInfo description;
			public FieldInfo empowered;
		}

		internal static readonly string ModFolder = "./Lizard/Mods/";

		// mods are loaded into a separate AppDomain so that they can be unloaded without unloading the entire game
		private static AppDomain ModDomain = null;

		internal static Dictionary<string, Mod> Mods = new Dictionary<string, Mod>();

		internal static SpriteDictionary ModItemImages = new SpriteDictionary();
		internal static SpriteDictionary ModSkillImages = new SpriteDictionary();
		internal static SpriteDictionary ModProjectileImages = new SpriteDictionary();

		internal static Dictionary<string, object> ModItemDescriptions = new Dictionary<string, object>();
		internal static Dictionary<string, object> ModSkillDescriptions = new Dictionary<string, object>();

		internal static Dictionary<string, IModItem> ModItems = new Dictionary<string, IModItem>();
		internal static Dictionary<string, IModSkill> ModSkills = new Dictionary<string, IModSkill>();

		private static InfoLink ItemLink = new InfoLink();
		private static InfoLink SkillLink = new InfoLink();

		private static string LoadingMod = null;

		private static void ModDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
		{
			string caption = "Error in " + Assembly.GetExecutingAssembly().GetName();
			Externs.MessageBox(caption, ex.ExceptionObject.ToString());
		}

		private static Assembly ModDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Logger.Log($"Resolving assembly {args.Name}");

			return Assembly.LoadFile(LoadingMod);
		}

		internal static string GetManagedDirectory()
		{
			DirectoryInfo gameDir = new DirectoryInfo(Directory.GetCurrentDirectory());

			foreach (string directory in Directory.GetDirectories(gameDir.FullName))
				if (directory.EndsWith("_Data", StringComparison.CurrentCulture))
					return Path.Combine(directory, "Managed\\");

			return null;
		}

		internal static void Init()
		{
			Type textMgrType = typeof(TextManager);

			ItemLink.dict = textMgrType.GetField("itemInfoDict", Lizard.NonPublicStatic);
			ItemLink.infoType = textMgrType.GetNestedType("ItemInfo", Lizard.NonPublicInstance);

			ItemLink.id = ItemLink.infoType.GetField("itemID", Lizard.PublicInstance);
			ItemLink.displayName = ItemLink.infoType.GetField("displayName", Lizard.PublicInstance);
			ItemLink.description = ItemLink.infoType.GetField("description", Lizard.PublicInstance);
			ItemLink.empowered = null;

			SkillLink.dict = textMgrType.GetField("skillInfoDict", Lizard.NonPublicStatic);
			SkillLink.infoType = textMgrType.GetNestedType("SkillInfo", Lizard.NonPublicInstance);

			SkillLink.id = SkillLink.infoType.GetField("skillID", Lizard.PublicInstance);
			SkillLink.displayName = SkillLink.infoType.GetField("displayName", Lizard.PublicInstance);
			SkillLink.description = SkillLink.infoType.GetField("description", Lizard.PublicInstance);
			SkillLink.empowered = SkillLink.infoType.GetField("empowered", Lizard.PublicInstance);
		}

		internal static void Reset()
		{
			foreach (KeyValuePair<string, IModItem> kvp in ModItems)
				LootManager.completeItemDict.Remove(kvp.Key);

			Mods.Clear();
			ModItems.Clear();
			ModItemImages.Clear();
			ModSkills.Clear();
			ModSkillImages.Clear();
			ModProjectileImages.Clear();
			ModItemDescriptions.Clear();
			ModSkillDescriptions.Clear();

			Lizard.Reset();
			LizardTitleScreen.Reset();
		}

		internal static object CreateItemInfo()
		{
			return Activator.CreateInstance(ItemLink.infoType);
		}

		internal static object CreateSkillInfo()
		{
			return Activator.CreateInstance(SkillLink.infoType);
		}

		internal static void LoadMods()
		{
			UnloadMods();

			AppDomainSetup setup = new AppDomainSetup()
			{
				ApplicationBase = Environment.CurrentDirectory
			};

			System.Security.Policy.Evidence evidence = AppDomain.CurrentDomain.Evidence;

			ModDomain = AppDomain.CreateDomain("Mods", evidence, setup);
			//ModDomain.FirstChanceException += ModDomain_FirstChanceException;
			ModDomain.UnhandledException += ModDomain_UnhandledException;
			ModDomain.AssemblyResolve += ModDomain_AssemblyResolve;
			AppDomain.CurrentDomain.AssemblyResolve += ModDomain_AssemblyResolve;

			if (Directory.Exists(ModFolder))
			{
				string[] dirs = Directory.GetDirectories(ModFolder);
				string[] dlls = new string[dirs.Length];

				int count = 0;

				for (int i = 0; i < dirs.Length; i++)
				{
					string folderName = Path.GetFileName(dirs[i]);
					string path = Path.Combine(dirs[i], folderName + ".dll");

					if (File.Exists(path))
						dlls[count++] = path;
				}

				if (count == 0)
				{
					Logger.Log("No mods found");
					return;
				}

				Assembly lizard = Assembly.GetCallingAssembly();
				Type marshalType = typeof(MarshalProxy);

				Logger.Log($"Loading {count} mod(s)\n");

				foreach (string file in dlls)
				{
					if (file == null)
						break;

					string path = Path.GetFullPath(file);
					LoadingMod = path;

					Logger.Log($"Loading mod {path}");

					Assembly assembly = LoadAssembly(lizard, marshalType, path);

					if (assembly != null)
					{
						Mod mod = new Mod(assembly);

						Mods.Add(Path.GetFileNameWithoutExtension(path), mod);
					}
					else
						Logger.Log($"Failed to load mod {path}\n");
				}
			}
			else
				Directory.CreateDirectory(ModFolder);

			Logger.Log("\nSkills will be loaded on player spawn\n");
		}

		internal static void AddModItem(Item item, UnityEngine.Sprite sprite)
		{
			IModItem modItem = (IModItem)item;
			object itemInfo = CreateItemInfo();

			ItemLink.id.SetValue(itemInfo, item.ID);
			ItemLink.displayName.SetValue(itemInfo, modItem.DisplayName);
			ItemLink.description.SetValue(itemInfo, modItem.Description);

			ModItems.Add(item.ID, modItem);
			ModItemImages.Add(item.ID, sprite);
			ModItemDescriptions.Add(item.ID, itemInfo);

			LootManager.completeItemDict.Add(item.ID, item);
		}

		internal static void AddModSkill(Player.SkillState skill, UnityEngine.Sprite sprite)
		{
			IModSkill modSkill = (IModSkill)skill;

			ModSkills.Add(skill.skillID, modSkill);
			ModSkillImages.Add(skill.skillID, sprite);

			foreach (int tier in modSkill.Tiers)
				LootManager.skillTierDict[tier].Add(skill.skillID);

			UpdateModDescription(skill.skillID, modSkill);
		}

		private static void UpdateModDescription(string skillID, IModSkill modSkill)
		{
			object skillInfo = ModSkillDescriptions[skillID];

			SkillLink.id.SetValue(skillInfo, skillID);
			SkillLink.displayName.SetValue(skillInfo, modSkill.DisplayName);
			SkillLink.description.SetValue(skillInfo, modSkill.Description);
			SkillLink.empowered.SetValue(skillInfo, modSkill.Empowered);

			ModSkillDescriptions[skillID] = skillInfo;

			System.Collections.IDictionary skillInfoDict = (System.Collections.IDictionary)SkillLink.dict.GetValue(null);
			skillInfoDict[skillID] = skillInfo;
		}

		internal static void LoadModSkillStatData(Player player, IModSkill modSkill)
		{
			const string statId = "Skills";

			Logger.Log($"Loading skill stat data for {modSkill.DisplayName} for player {player.playerID + 1}");

			Dictionary<string, StatData> dictionary = StatManager.data[statId][player.skillCategory];

			SkillStats skillStats = modSkill.Stats;
			skillStats.Initialize();

			skillStats.targetNames[0] = "EnemyHurtBox";
			skillStats.targetNames[1] = "DestructibleHurtBox";
			skillStats.targetNames[4] = "FFAHurtBox";

			StatData statData = new StatData(skillStats, player.skillCategory);

			List<string> val = statData.GetValue<List<string>>("targetNames", -1);

			if (val.Contains(Globals.allyHBStr) || val.Contains(Globals.enemyHBStr))
				val.Add(Globals.ffaHBStr);

			if (val.Contains(Globals.allyFCStr) || val.Contains(Globals.enemyFCStr))
				val.Add(Globals.ffaFCStr);

			string id = statData.GetValue<string>("ID", -1);

			dictionary[id] = statData;
			StatManager.globalSkillData[id] = statData;
		}

		internal static void AddModProjectile(string id, UnityEngine.Sprite sprite)
		{
			ModProjectileImages.Add(id, sprite);
		}

		internal static void AddItemImages(Dictionary<string, UnityEngine.Sprite> givenDict)
		{
			foreach (KeyValuePair<string, UnityEngine.Sprite> kvp in ModItemImages)
				givenDict.Add(kvp.Key, kvp.Value);
		}

		internal static void AddSkillImages(Dictionary<string, UnityEngine.Sprite> givenDict)
		{
			foreach (KeyValuePair<string, UnityEngine.Sprite> kvp in ModSkillImages)
				givenDict.Add(kvp.Key, kvp.Value);
		}

		internal static void LoadModItems()
		{
			foreach (KeyValuePair<string, Mod> kvp in Mods)
				kvp.Value.LoadItems();
		}

		internal static void LoadModSkills(Player player)
		{
			foreach (KeyValuePair<string, Mod> kvp in Mods)
				kvp.Value.LoadSkills(player);
		}

		internal static void LoadModProjectiles()
		{
			foreach (KeyValuePair<string, Mod> kvp in Mods)
				kvp.Value.LoadProjectiles();
		}

		internal static void InitModSkillInfos()
		{
			foreach (KeyValuePair<string, Mod> kvp in Mods)
				kvp.Value.InitModSkillInfos();
		}

		internal static void AddItemInfos()
		{
			System.Collections.IDictionary itemInfoDict = (System.Collections.IDictionary)ItemLink.dict.GetValue(null);

			foreach (KeyValuePair<string, object> kvp in ModItemDescriptions)
				itemInfoDict.Add(kvp.Key, kvp.Value);
		}

		internal static void AddSkillInfos()
		{
			System.Collections.IDictionary skillInfoDict = (System.Collections.IDictionary)SkillLink.dict.GetValue(null);

			foreach (KeyValuePair<string, object> kvp in ModSkillDescriptions)
				skillInfoDict.Add(kvp.Key, kvp.Value);
		}

		private static Assembly LoadAssembly(Assembly lizardAssembly, Type marshalType, string file)
		{
			MarshalProxy marshal = (MarshalProxy)ModDomain.CreateInstanceAndUnwrap(lizardAssembly.FullName, marshalType.FullName);

			return marshal.GetAssembly(file);
		}

		internal static void UnloadMods()
		{
			Reset();

			if (ModDomain != null)
			{
				AppDomain.Unload(ModDomain);
				ModDomain = null;
			}
		}

		private class MarshalProxy : MarshalByRefObject
		{
			public Assembly GetAssembly(string assemblyPath)
			{
				try
				{
					Assembly assembly = Assembly.LoadFile(assemblyPath);

					HashSet<string> loadedReferences = new HashSet<string>();

					foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
						LoadReference(reference, loadedReferences);

					return assembly;
				}
				catch (Exception ex)
				{
					Externs.MessageBox("Failed to load assembly", ex.ToString());
				}

				return null;
			}

			private void LoadReference(AssemblyName name, HashSet<string> loaded)
			{
				if (loaded.Contains(name.FullName))
					return;

				Assembly assembly = Assembly.Load(name);

				loaded.Add(name.FullName);

				foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
					LoadReference(name, loaded);
			}
		}
	}
}
