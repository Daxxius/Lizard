using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using StringBuilder = System.Text.StringBuilder;

namespace Lizard
{
	public static class ModCompiler
	{
		public class FailedCompileException : System.Exception
		{
			public FailedCompileException(string message) : base(message) { }
		}

		private static CSharpCodeProvider Provider = new CSharpCodeProvider();
		private static CompilerParameters Parameters = new CompilerParameters();

		static ModCompiler()
		{
			Parameters.GenerateExecutable = false;
			Parameters.GenerateInMemory = false;
			Parameters.IncludeDebugInformation = false;
			Parameters.ReferencedAssemblies.Add("System.dll");
			Parameters.ReferencedAssemblies.Add("System.Core.dll");
			Parameters.ReferencedAssemblies.Add(Path.Combine(Program.ManagedFolder, "UnityEngine.dll"));
			Parameters.ReferencedAssemblies.Add(Path.Combine(Program.ManagedFolder, "UnityEngine.UI.dll"));
			Parameters.ReferencedAssemblies.Add(Path.Combine(Program.ManagedFolder, "Assembly-CSharp.dll"));
			Parameters.ReferencedAssemblies.Add(Path.Combine(Program.ManagedFolder, "Lizard.dll"));
		}

		private static string GetSafeName(string name)
		{
			StringBuilder safeName = new StringBuilder(name.Length);

			for (int i = 0; i < name.Length; i++)
			{
				if (name[i] >= '0' && name[i] <= '9' || name[i] >= 'A' && name[i] <= 'Z' || name[i] >= 'a' && name[i] <= 'z')
					safeName.Append(name[i]);
				else
					safeName.Append('_');
			}

			return safeName.ToString();
		}

		public static void CompileMods()
		{
			string[] modDirs = Directory.GetDirectories(Program.ModFolder);

			foreach (string dir in modDirs)
				CompileFolder(dir);
		}

		private static System.Reflection.Assembly CompileFolder(string dir)
		{
			string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

			if (files == null || files.Length == 0)
				return null;

			string dirName = Path.GetFileName(dir);
			string modName = "Mod_" + GetSafeName(dirName);
			string usingFile = Path.Combine(dir, "Usings.cs");

			Parameters.OutputAssembly = Path.Combine(dir, dirName + ".dll");

			StringBuilder code = new StringBuilder();

			if (File.Exists(usingFile))
				code.AppendLine(File.ReadAllText(usingFile));

			code.AppendLine(string.Format("namespace Lizard.{0}\n{{", modName));

			foreach (string file in files)
				if (!file.EndsWith("Usings.cs"))
					code.AppendLine(File.ReadAllText(file));

			code.Append("\n}");

			CompilerResults results = Provider.CompileAssemblyFromSource(Parameters, code.ToString());

			if (results.Errors.HasErrors)
			{
				StringBuilder errorText = new StringBuilder();

				foreach (CompilerError error in results.Errors)
					errorText.AppendLine(string.Format("Error {0}: {1}", error.ErrorNumber, error.ErrorText));

				throw new FailedCompileException(errorText.ToString());
			}

			return results.CompiledAssembly;
		}
	}
}
