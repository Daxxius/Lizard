using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lizard.Attributes;

namespace Lizard
{
	public static class Util
    {
        public static string GetManagedDirectory()
        {
			DirectoryInfo gameDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent;

            foreach (string directory in Directory.GetDirectories(gameDir.FullName))
                if (directory.EndsWith("_Data", StringComparison.CurrentCulture))
                    return Path.Combine(directory, "Managed\\");

            return null;
        }

		private static bool NamespaceType(Type t, string ns)
		{
			return string.Equals(t.Namespace, ns, StringComparison.Ordinal);
		}

		public static Type[] GetTypesInNamespace(Assembly assembly, string ns)
		{
			return assembly.GetTypes().Where(t => NamespaceType(t, ns)).ToArray();
		}

		public static int InjectAllHooks(Dictionary<string, List<HookData>> attributes, AssemblyDefinition assembly)
		{
			Dictionary<string, TypeDefinition> typeDefinitions = new Dictionary<string, TypeDefinition>();

			foreach (TypeDefinition typeDef in assembly.MainModule.Types)
				RecursiveAddTypes(typeDef, typeDefinitions);

			int hooks = AddCallHook.InjectHooks(attributes, assembly, typeDefinitions);
			int adds = AddMethodHook.InjectHooks(attributes, assembly, typeDefinitions);

			return hooks + adds;
		}

		private static void RecursiveAddTypes(TypeDefinition typeDef, Dictionary<string, TypeDefinition> typeDefinitions)
		{
			typeDefinitions.Add(typeDef.FullName, typeDef);

			if (typeDef.HasNestedTypes)
				foreach (TypeDefinition nested in typeDef.NestedTypes)
					RecursiveAddTypes(nested, typeDefinitions);
		}
	}
}
