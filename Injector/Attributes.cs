using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lizard
{
	public enum AddAttributesResponse
	{
		Ok,
		Info,
		Error
	}

	public static class AttributesHelper
	{
		public static Dictionary<string, List<HookData>> FindAndInvokeAllAttributes(List<HookData> allAttributes)
		{
			Type[] typelist = Util.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "Lizard.Attributes");
			Dictionary<string, List<HookData>> returnData = new Dictionary<string, List<HookData>>();

			Type hookType = typeof(Attributes.Hook);

			foreach (Type type in typelist)
			{
				if (type.IsAbstract || !type.IsSubclassOf(hookType))
					continue;

				Attributes.Hook instance = (Attributes.Hook)Activator.CreateInstance(type, new object[] { allAttributes });

				if (instance.AddAllFound() != AddAttributesResponse.Ok)
					continue;

				returnData.Add(type.Name, instance.Attributes);
			}

			return returnData;
		}

		public static List<HookData> GetAllAttributesInAssembly(AssemblyDefinition assembly)
		{
			List<HookData> returnData = new List<HookData>();
			IEnumerable<TypeDefinition> types = assembly.MainModule.GetTypes();

			foreach (TypeDefinition type in types)
			{
				foreach (MethodDefinition method in type.Methods)
				{
					var attributes = method.CustomAttributes;

					if (attributes != null && attributes.Count >= 0)
						for (int i = 0; i < attributes.Count; i++)
							returnData.Add(new HookData(type, method, attributes[i], assembly));
				}
			}

			return returnData;
		}
	}

	public class HookData
	{
		public Cecil.ReturnData TargetData { get; set; }
		public AssemblyDefinition Assembly { get; set; }
		public TypeDefinition Type { get; set; }
		public MethodDefinition Method { get; set; }
		public CustomAttribute Attribute { get; set; }

		public HookData(TypeDefinition type, MethodDefinition method, CustomAttribute attribute, AssemblyDefinition assembly)
		{
			Type = type;
			Method = method;
			Attribute = attribute;
			Assembly = assembly;
		}

		public override string ToString()
		{
			return string.Format("Type: {0} Method: {1}", Type.Name, Method.Name);
		}
	}
}
