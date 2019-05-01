using Mono.Cecil;
using System.Collections.Generic;

namespace Lizard.Attributes
{
	public sealed class AddMethodHook : Hook<AddMethodAttribute>
	{
		public AddMethodHook(List<HookData> attributes) : base(attributes) { }

		public override AddAttributesResponse AddAllFound()
		{
			foreach (HookData hookData in AllAttributes)
				if (hookData.Attribute.AttributeType.Name == nameof(AddMethodAttribute))
					return base.AddAllFound();

			return AddAttributesResponse.Info;
		}

		public static int InjectHooks(Dictionary<string, List<HookData>> attributes, AssemblyDefinition assembly, Dictionary<string, TypeDefinition> typeDefinitions)
		{
			if (!attributes.ContainsKey(nameof(AddMethodHook)))
				return 0;

			int injectedCorrectly = 0;

			foreach (HookData hook in attributes[nameof(AddMethodHook)])
			{
				TypeDefinition typeDef = Cecil.ConvertStringToClass(hook.Attribute.ConstructorArguments[0].Value.ToString(), assembly, typeDefinitions);

				if (typeDef == null)
					continue;

				string methodName = hook.Attribute.ConstructorArguments[1].Value.ToString();

				if (Cecil.MethodExists(typeDef, methodName))
				{
					string desc = string.Format("Method {0} already exists in type {1}", methodName, typeDef.FullName);
					Externs.MessageBox("Method already exists!", desc);
				}
				else if (Cecil.InjectNewMethod(typeDef, assembly, typeDefinitions, methodName, hook))
					++injectedCorrectly;
			}

			return injectedCorrectly;
		}
	}
}
