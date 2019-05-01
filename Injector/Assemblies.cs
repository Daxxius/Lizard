using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lizard
{
	public static class Assemblies
    {
        public static bool LoadLizard()
        {
            try
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Path.GetFullPath(Program.LizardDll));
                List<HookData> allAttributes = AttributesHelper.GetAllAttributesInAssembly(assembly);
				Dictionary<string, List<HookData>> filteredAttributes = AttributesHelper.FindAndInvokeAllAttributes(allAttributes);

				foreach (KeyValuePair<string, List<HookData>> kvp in filteredAttributes)
					Program.Attributes.Add(kvp.Key, kvp.Value);

				return true;
            }
            catch (Exception ex)
            {
				Externs.MessageBox(ex.GetType().Name, ex.ToString());
            }

			return false;
		}
    }
}
