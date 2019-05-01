using System.Collections.Generic;
using UnityEngine;

namespace Lizard.Hooks
{
	public static class Hooks
	{
		[AddCall("GameController.Awake")]
		public static void OnAwakeBegin(GameController gameController)
		{
			try
			{
				if (!Lizard.Initialised)
					Lizard.Init();
			}
			catch (System.Exception ex)
			{
				Externs.MessageBox("Error!", ex.ToString());
			}
		}

		[AddCall("TitleScreen.Awake")]
		public static void TitleScreenOnAwakeBegin(TitleScreen titleScreen)
		{
			Lizard.Try(() => LizardTitleScreen.AwakeBegin(titleScreen));
		}

		[AddCall("TitleScreen.Awake", true)]
		public static void TitleScreenOnAwakeEnd(TitleScreen titleScreen)
		{
			Lizard.Try(() => LizardTitleScreen.AwakeEnd(titleScreen));
		}

		[AddCall("TextManager.LoadUIJSON", true)]
		public static void TextManagerLoadUIJSONEnd()
		{
			LizardTitleScreen.SetUITextDict();
		}

		[AddCall("TitleScreen.ConfirmMenuOption")]
		public static bool TitleScreenOnConfirmMenuOption(TitleScreen titleScreen)
		{
			Lizard.Try(() => LizardTitleScreen.ConfirmMenuOption(titleScreen));

			return true;
		}

		[AddCall("Player.InitFSM", true)]
		public static void OnInitFSM(Player player)
		{
			try
			{
				ModManager.LoadModSkills(player);
			}
			catch (System.Exception ex)
			{
				Externs.MessageBox("Error!", ex.ToString());
			}
		}

		[AddCall("Projectile.Awake")]
		public static void OnAwakeProjectile(Projectile projectile)
		{
			IModProjectile modProjectile = projectile as IModProjectile;

			if (modProjectile == null)
				return;

			GameObject projGO = projectile.gameObject;
			Transform projTr = projGO.transform;

			Animator anim = projGO.AddComponent<Animator>();
			anim.feetPivotActive = 0.0f;
			anim.enabled = modProjectile.Metadata.usesAnimator;

			Rigidbody2D rigidbody = projGO.AddComponent<Rigidbody2D>();
			rigidbody.angularDrag = 0.0f;
			rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
			rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
			rigidbody.freezeRotation = modProjectile.Metadata.fixedRotation;
			rigidbody.gravityScale = 0.0f;
			rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;

			string projId = Lizard.GetID(projectile.GetType());
			SpriteRenderer spriteRenderer = Lizard.CreateProjectileSprite(projId, projTr, modProjectile.Metadata);

			Lizard.CreateAttackBox(projTr, modProjectile.Metadata.attackBoxShape);

			if (modProjectile.Metadata.collidesWithWalls)
				Lizard.CreateWallCollider(projTr, modProjectile.Metadata.wallColliderShape);
		}

		[AddCall("Player/SkillState.SetSkillData")]
		public static void OnSetSkillData(Player.SkillState skill)
		{
			if (Lizard.IsInterface<IModSkill>(skill))
			{
				skill.skillID = Lizard.GetID(skill);
				skill.name = skill.skillID;

				ModManager.LoadModSkillStatData(skill.parent, (IModSkill)skill);
			}
		}

		[AddCall("LootManager.DropLoot")]
		public static void OnDropLoot(string entryName, Vector2 location)
		{
			LootManager.DropItem(location, 1, "Lizard.Mod_ExampleMod.ExampleItem");
			LootManager.DropSkill(location, 1, "Lizard.Mod_ExampleMod.ExampleSkill");
		}

		[AddCall("IconManager.LoadSprites", true)]
		public static void OnLoadSprites(Dictionary<string, Sprite> givenDict, string resourcePath)
		{
			if (resourcePath.EndsWith("ItemIcons.png"))
				ModManager.AddItemImages(givenDict);
			else if (resourcePath.EndsWith("Skills.png"))
				ModManager.AddSkillImages(givenDict);
		}

		[AddCall("TextManager.LoadText", true)]
		public static void OnLoadText(bool forceOverride)
		{
			ModManager.AddItemInfos();
			ModManager.AddSkillInfos();
		}

		[AddCall("Item.CreateItem")]
		public static bool CreateItemStart(out Item item, string givenID)
		{
			item = (Item)System.Activator.CreateInstance(LootManager.completeItemDict[givenID].GetType());

			if (givenID.StartsWith("Lizard.Mod_"))
			{
				item.ID = Lizard.GetID(item);

				IModItem modItem = (IModItem)item;
				modItem.Init();
			}

			return true;
		}
	}
}
