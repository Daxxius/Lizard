using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Lizard
{
	public static class Lizard
	{
		internal const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
		internal const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;
		internal const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
		internal const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

		internal static FieldInfo PlayForehandAnim = null;
		internal static FieldInfo ForehandAnimPlayed = null;

		internal static readonly int AttackBoxLayer = LayerMask.NameToLayer("Attack");
		internal static readonly int WallColliderLayer = LayerMask.NameToLayer("WallOnly");

		internal static bool Initialised { get; private set; }

		internal static void Init()
		{
			Logger.Init();

			System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			System.Type playerType = typeof(Player);
			PlayForehandAnim = playerType.GetField("playForehandAnim", NonPublicInstance);
			ForehandAnimPlayed = playerType.GetField("forehandAnimPlayed", NonPublicInstance);

			ModManager.Init();
			ModManager.LoadMods();
			ModManager.LoadModItems();
			ModManager.InitModSkillInfos();
			ModManager.LoadModProjectiles();

			Initialised = true;
		}

		internal static void Reset()
		{
			Initialised = false;
		}

		internal static void Try(System.Action action)
		{
			try
			{
				action();
			}
			catch (System.Exception ex)
			{
				MessageBox("Error!", ex.ToString());
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs ex)
		{
			Externs.MessageBox("Error!", ex.ExceptionObject.ToString());
		}

		internal static string GetID(System.Type type)
		{
			return type.FullName;
		}

		internal static string GetID(object obj)
		{
			return obj.GetType().FullName;
		}

		internal static bool IsInterface(System.Type interfaceType, System.Type objectType)
		{
			return interfaceType.IsAssignableFrom(objectType);
		}

		internal static bool IsInterface<IT>(System.Type objectType)
		{
			return IsInterface(typeof(IT), objectType);
		}

		internal static bool IsInterface<IT>(object obj)
		{
			return IsInterface(typeof(IT), obj.GetType());
		}

		public static string GetBackhandAnimStr(Player.SkillState skill)
		{
			PlayForehandAnim.SetValue(skill.parent, true);
			ForehandAnimPlayed.SetValue(skill.parent, false);

			return "Backhand" + skill.parent.facingDirection.ToString();
		}

		public static string GetForehandAnimStr(Player.SkillState skill)
		{
			PlayForehandAnim.SetValue(skill.parent, false);
			ForehandAnimPlayed.SetValue(skill.parent, true);

			return "Forehand" + skill.parent.facingDirection.ToString();
		}

		internal static SpriteRenderer CreateProjectileSprite(string projID, Transform parent, ProjectileMetadata metadata)
		{
			GameObject projectileGO = new GameObject("ProjectileSprite");
			Transform tr = projectileGO.transform;
			tr.parent = parent;
			tr.localPosition = metadata.spriteOffset;
			tr.localRotation = Quaternion.Euler(0.0f, 0.0f, metadata.spriteRotationOffset);

			SpriteRenderer spriteRenderer = projectileGO.AddComponent<SpriteRenderer>();

			spriteRenderer.adaptiveModeThreshold = 0.5f;
			spriteRenderer.drawMode = SpriteDrawMode.Simple;
			spriteRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
			spriteRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
			spriteRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			spriteRenderer.size = metadata.spriteSize;
			spriteRenderer.sortingLayerName = "Effect";
			spriteRenderer.sortingLayerID = -1276599315;
			spriteRenderer.sortingOrder = 0;
			spriteRenderer.tileMode = SpriteTileMode.Continuous;
			
			Sprite sprite;
			if (ModManager.ModProjectileImages.TryGetValue(projID, out sprite))
			{
				spriteRenderer.sprite = sprite;
				spriteRenderer.material.mainTexture = sprite.texture;
			}
			else
				Logger.Log($"Could not find projectile image for {projID}");

			return spriteRenderer;
		}

		internal static Collider2D CreateShapeCollider(GameObject gameObject, CollisionShape shape)
		{
			Collider2D collider = null;

			if (shape.shape == CollisionShapes.Box)
			{
				BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>();
				bc.size = ((CollisionBox)shape).size;
				collider = bc;
			}
			else
			{
				CircleCollider2D cc = gameObject.AddComponent<CircleCollider2D>();
				cc.radius = ((CollisionCircle)shape).size;
				collider = cc;
			}

			collider.isTrigger = true;

			return collider;
		}

		internal static Attack CreateAttackBox(Transform parent, CollisionShape shape)
		{
			GameObject attackBox = new GameObject("AttackBox")
			{
				layer = AttackBoxLayer
			};

			Transform tr = attackBox.transform;
			tr.parent = parent;
			tr.localPosition = Vector3.zero;
			tr.localRotation = Quaternion.identity;

			CreateShapeCollider(attackBox, shape);

			return attackBox.AddComponent<Attack>();
		}

		internal static Collider2D CreateWallCollider(Transform parent, CollisionShape shape)
		{
			GameObject wallCollider = new GameObject("WallCollider")
			{
				layer = WallColliderLayer
			};

			Transform tr = wallCollider.transform;
			tr.parent = parent;
			tr.localPosition = Vector3.zero;
			tr.localRotation = Quaternion.identity;

			ProjectileWallDetector wallDetector = wallCollider.AddComponent<ProjectileWallDetector>();

			return CreateShapeCollider(wallCollider, shape);
		}

		public static Transform GetClosestTarget(Projectile proj)
		{
			if (proj.targetGroup.Count == 0)
				return null;

			Transform projTr = proj.transform;
			Transform closest = null;

			float closestDistSq = float.MaxValue;

			foreach (GameObject target in proj.targetGroup)
			{
				Transform targetTr = target.transform;

				Vector2 diff = targetTr.position - projTr.position;

				float distSq = Vector2.Dot(diff, diff);

				if (distSq < closestDistSq)
				{
					closest = targetTr;
					distSq = closestDistSq;
				}
			}

			return closest;
		}

		public static T CreateProjectile<T>(Player.SkillState skill, bool setAttackInfo = true) where T : Projectile
		{
			try
			{
				GameObject projGO = new GameObject(typeof(T).FullName);

				Transform projTr = projGO.transform;
				Transform ownerTr = skill.parent.transform;

				projTr.position = ownerTr.position;
				projTr.rotation = ownerTr.rotation;

				T proj = projGO.AddComponent<T>();
				proj.destroyOnDisable = true;
				proj.pointTowardMoveVector = true;

				if (proj.impactAudioID == null)
					proj.impactAudioID = "";

				if (proj.particleEffectName == null)
					proj.particleEffectName = "";
				
				proj.parentObject = skill.parent.gameObject;
				proj.parentEntity = skill.parent;

				if (setAttackInfo)
				{
					proj.attackBox.SetAttackInfo(skill.parent.skillCategory, skill.skillID);
					proj.UpdateCalculatedDamage(true);
				}

				return proj;
			}
			catch (System.Exception ex)
			{
				Externs.MessageBox("Error!", ex.ToString());
			}

			return null;
		}

		internal static FieldInfo GetFieldInfo(System.Type type, string fieldName, bool isStatic, bool isPublic)
		{
			BindingFlags flags = BindingFlags.GetField;
			flags |= (isStatic ? BindingFlags.Static : BindingFlags.Instance);
			flags |= (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);

			return type.GetField(fieldName, flags);
		}

		internal static FieldInfo GetFieldInfo<T>(string fieldName, bool isStatic, bool isPublic)
		{
			return GetFieldInfo(typeof(T), fieldName, isStatic, isPublic);
		}

		internal static T GetField<T>(object inst, string fieldName, bool isPublic)
		{
			FieldInfo info = GetFieldInfo(inst.GetType(), fieldName, false, isPublic);

			return (T)info.GetValue(inst);
		}

		internal static T GetField<T>(System.Type type, string fieldName, bool isPublic)
		{
			FieldInfo info = GetFieldInfo(type, fieldName, false, isPublic);

			return (T)info.GetValue(null);
		}

		internal static T GetStaticProperty<T>(System.Type type, string propertyName, bool isPublic)
		{
			BindingFlags flags = BindingFlags.Static | BindingFlags.GetProperty;
			flags |= (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);

			PropertyInfo info = type.GetProperty(propertyName, flags);

			return (T)info.GetValue(null, null);
		}

		private static List<MemberInfo> GetSortedMembers(System.Type type, BindingFlags flags)
		{
			List<MemberInfo> members = new List<MemberInfo>();

			members.AddRange(type.GetFields(flags));
			members.AddRange(type.GetProperties(flags));

			members.Sort((x, y) => x.Name.CompareTo(y.Name));

			return members;
		}

		internal static void OutputGameObject(GameObject gameObject)
		{
			System.IO.StreamWriter writer = new System.IO.StreamWriter($"Lizard/{gameObject.name}.log");

			OutputGameObject(gameObject, new System.Text.StringBuilder(), writer);

			writer.Close();
		}

		private static void OutputGameObject(GameObject gameObject, System.Text.StringBuilder stringBuilder, System.IO.StreamWriter writer)
		{
			Transform transform = gameObject.transform;
			Component[] components = gameObject.GetComponents<Component>();

			writer.WriteLine($"{stringBuilder.ToString()}[{gameObject.name}]");

			writer.WriteLine(stringBuilder.ToString() + "{");

			stringBuilder.Append('\t');

			OutputClass(gameObject, stringBuilder, writer);

			foreach (Component component in components)
				OutputClass(component, stringBuilder, writer);

			writer.WriteLine(stringBuilder.ToString());

			foreach (Transform tr in transform)
				OutputGameObject(tr.gameObject, stringBuilder, writer);

			stringBuilder.Length--;

			writer.WriteLine(stringBuilder.ToString() + "}\n\n");
		}

		internal static void OutputClass<T>(T obj) where T : class
		{
			System.Type type = obj?.GetType() ?? typeof(T);

			System.IO.StreamWriter writer = new System.IO.StreamWriter($"Lizard/{type.FullName}.log");

			OutputClass(obj, new System.Text.StringBuilder(), writer);

			writer.Close();
		}

		private static void OutputClass<T>(T obj, System.Text.StringBuilder stringBuilder, System.IO.StreamWriter writer) where T : class
		{
			const BindingFlags flags = PublicStatic | NonPublicInstance;

			System.Type type = obj?.GetType() ?? typeof(T);

			List<MemberInfo> members = GetSortedMembers(type, flags);

			System.Func<object, MemberInfo, object> getFI = (inst, mi) => ((FieldInfo)mi).GetValue(inst);
			System.Func<object, MemberInfo, object> getPI = (inst, mi) => ((PropertyInfo)mi).GetValue(inst, null);

			writer.WriteLine($"{stringBuilder.ToString()}({type.Name})");

			writer.WriteLine(stringBuilder.ToString() + "{");

			stringBuilder.Append('\t');

			foreach (MemberInfo mi in members)
			{
				System.Func<object, MemberInfo, object> getVal;

				if (mi.MemberType == MemberTypes.Field)
					getVal = getFI;
				else
					getVal = getPI;

				object val = getVal(obj, mi);
				string valString = val?.ToString().Replace("\n", " | ") ?? "(null)";

				writer.WriteLine($"{stringBuilder.ToString()}{mi.Name} {valString}");

				System.Collections.IList list = val as System.Collections.IList;

				if (list != null)
				{
					stringBuilder.Append('\t');

					foreach (object el in list)
					{
						string elString = el?.ToString().Replace("\n", " | ") ?? "(null)";

						writer.WriteLine($"{stringBuilder.ToString()}{elString}");
					}

					stringBuilder.Length--;
				}
			}

			stringBuilder.Length--;

			writer.WriteLine(stringBuilder.ToString() + "}");
		}

		internal static void OutputStatData(StatData statData)
		{
			Logger.Log("SkillStats");

			foreach (KeyValuePair<string, object> kvp in statData.statDict)
			{
				System.Collections.IList list = (System.Collections.IList)kvp.Value;

				Logger.Log("-------");
				Logger.Log("\t" + kvp.Key);
				Logger.Log("{");

				foreach (object obj in list)
					Logger.Log("\t\t" + (obj == null ? "(null)" : obj.ToString()));

				Logger.Log("}");
			}
		}

		internal static void OutputArray(System.Array array)
		{
			Logger.Log("{");

			foreach (object val in array)
				Logger.Log(val);

			Logger.Log("}");
		}

		public static void Log(string text) => Logger.Log(text);
		public static void Log(object obj) => Logger.Log(obj);
		public static void MessageBox(string title, string description) => Externs.MessageBox(title, description);
	}
}
