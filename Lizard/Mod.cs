using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Lizard
{
	public class Mod
	{
		public Assembly Assembly { get; private set; }

		private readonly Type[] types;
		private readonly List<Type> itemTypes;
		private readonly List<Type> skillTypes;
		private readonly List<Type> projectileTypes;

		public Mod(Assembly assembly)
		{
			Assembly = assembly;
			types = assembly.GetTypes();

			itemTypes = Load<Item, IModItem>();
			skillTypes = Load<Player.SkillState, IModSkill>();
			projectileTypes = Load<MonoBehaviour, IModProjectile>();
		}

		internal void LoadItems()
		{
			if (itemTypes.Count == 0)
			{
				Logger.Log("No items to load\n");
				return;
			}

			Logger.Log($"Loading {itemTypes.Count} item type(s)\n");

			foreach (Type itemType in itemTypes)
			{
				Logger.Log($"Loading item {itemType.FullName}");

				Item item = (Item)Activator.CreateInstance(itemType);
				item.ID = Lizard.GetID(itemType);

				Sprite sprite = LoadItemSprite(item);

				ModManager.AddModItem(item, sprite);
			}

			Logger.Log("");
		}

		internal void LoadSkills(Player player)
		{
			if (skillTypes.Count == 0)
				return;

			Logger.Log($"Loading {skillTypes.Count} skill type(s) for player {player.playerID + 1}\n");

			foreach (Type skillType in skillTypes)
			{
				Logger.Log($"Loading skill {skillType.FullName} for player {player.playerID + 1}");

				try
				{
					Player.SkillState skill = (Player.SkillState)Activator.CreateInstance(skillType, player.fsm, player);

					Sprite sprite = LoadSkillSprite(skill);

					ModManager.AddModSkill(skill, sprite);
					player.fsm.AddState(skill);
				}
				catch (Exception ex)
				{
					Externs.MessageBox("Error!", ex.ToString());
				}
			}

			Logger.Log("");
		}

		internal void LoadProjectiles()
		{
			if (projectileTypes.Count == 0)
			{
				Logger.Log("No projectiles to load\n\n");
				return;
			}

			Logger.Log($"Loading {projectileTypes.Count} projectile type(s)\n");

			foreach (Type projectileType in projectileTypes)
			{
				Logger.Log($"Loading projectile {projectileType.FullName}");

				GameObject dummy = new GameObject();
				dummy.SetActive(false);

				MonoBehaviour proj = (MonoBehaviour)dummy.AddComponent(projectileType);

				Sprite sprite = LoadProjectileSprite(proj);
				
				ModManager.AddModProjectile(Lizard.GetID(projectileType), sprite);

				UnityEngine.Object.Destroy(dummy);
			}

			Logger.Log("");
		}

		internal void InitModSkillInfos()
		{
			foreach (Type skillType in skillTypes)
				ModManager.ModSkillDescriptions.Add(Lizard.GetID(skillType), ModManager.CreateSkillInfo());
		}

		private List<Type> Load<IT>(Type objectType, bool includeAbstract)
		{
			List<Type> result = new List<Type>();

			Type interfaceType = typeof(IT);

			foreach (Type type in types)
				if (includeAbstract || !type.IsAbstract)
					if (Lizard.IsInterface(interfaceType, type) && type.IsSubclassOf(objectType))
						result.Add(type);

			return result;
		}

		public List<Type> Load<OT, IT>(bool includeAbstract = false)
		{
			return Load<IT>(typeof(OT), includeAbstract);
		}

		private static string GetModFolder(object obj)
		{
			return Path.GetDirectoryName(obj.GetType().Assembly.Location);
		}

		private static Sprite LoadSprite(string path)
		{
			try
			{
				Logger.Log($"Loading sprite {path}");

				byte[] textureBytes = null;

				if (!File.Exists(path))
				{
					Logger.Log($"Could not find texture {path}");
					return null;
				}
				else
					textureBytes = File.ReadAllBytes(path);

				Texture2D tex = new Texture2D(1, 1);

				if (tex.LoadImage(textureBytes))
					return Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);

				Externs.MessageBox("Failed loading texture!", $"Failed to load texture {path}");
			}
			catch (Exception ex)
			{
				Externs.MessageBox("Error loading sprite!", ex.ToString());
			}

			return null;
		}

		private static Sprite LoadObjectSprite(object obj, string id, string type, string image)
		{
			Sprite sprite = null;

			string lcType = type.ToLower();

			try
			{
				string modDir = GetModFolder(obj);
				string objDir = Path.Combine(modDir, type + 's');
				string iconPath = Path.GetFullPath(Path.Combine(objDir, image));

				Logger.Log($"Loading {lcType} image {id}");

				sprite = LoadSprite(iconPath);

				if (sprite != null)
				{
					sprite.name = id;
					sprite.texture.name = id;
				}
				else
					Logger.Log($"Failed to load {lcType} image {id}");
			}
			catch (Exception ex)
			{
				Externs.MessageBox($"Loading {lcType} image failed", ex.ToString());
			}

			return sprite;
		}

		private static Sprite LoadItemSprite(Item item)
		{
			IModItem modItem = (IModItem)item;

			return LoadObjectSprite(item, item.ID, "Item", modItem.ItemImage);
		}

		private static Sprite LoadSkillSprite(Player.SkillState skill)
		{
			IModSkill modSkill = (IModSkill)skill;

			return LoadObjectSprite(skill, skill.skillID, "Skill", modSkill.SkillImage);
		}

		private static Sprite LoadProjectileSprite(MonoBehaviour projectile)
		{
			IModProjectile modProj = (IModProjectile)projectile;
			string projID = Lizard.GetID(projectile);

			return LoadObjectSprite(projectile, projID, "Projectile", modProj.ProjectileImage);
		}
	}
}
