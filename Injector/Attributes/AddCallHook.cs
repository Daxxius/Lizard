using Mono.Cecil;
using System.Collections.Generic;

namespace Lizard.Attributes
{
	public sealed class AddCallHook : Hook<AddCallAttribute>
	{
		public AddCallHook(List<HookData> attributes) : base(attributes) { }

		public static int InjectHooks(Dictionary<string, List<HookData>> attributes, AssemblyDefinition assembly, Dictionary<string, TypeDefinition> typeDefinitions)
		{
			if (!attributes.ContainsKey(nameof(AddCallHook)))
				return 0;

			int injectedCorrectly = 0;

			foreach (HookData hook in attributes[nameof(AddCallHook)])
			{
				if (hook.Attribute.ConstructorArguments.Count > 0)
				{
					hook.TargetData = Cecil.ConvertStringToClassAndMethod(hook.Attribute.ConstructorArguments[0].Value.ToString(), assembly, typeDefinitions, hook);

					if (hook.TargetData != null && Cecil.Inject(hook.TargetData, hook))
						++injectedCorrectly;
				}
			}

			return injectedCorrectly;
		}
	}
}
