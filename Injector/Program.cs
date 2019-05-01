using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lizard
{
	class Program
    {
		public static Dictionary<string, List<HookData>> Attributes = null;

		public static string ManagedFolder;
        public static string GameAssembly = "Assembly-CSharp.dll";
        public const string ModFolder = "Mods/";
		public const string LizardDll = "Lizard.dll";
		public const string AttributesDll = "HookAttribute.dll";

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
		{
			Externs.MessageBox("Error!", ex.ExceptionObject.ToString());
		}

		static void Main(string[] args)
        {
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			Attributes = new Dictionary<string, List<HookData>>();

			if (!Directory.Exists(ModFolder))
				Directory.CreateDirectory(ModFolder);

			ManagedFolder = Util.GetManagedDirectory();

			if (ManagedFolder == null)
			{
				Externs.MessageBox("Managed folder missing!", "Could not find the Managed folder");

				Environment.Exit(-1);
			}

            GameAssembly = Path.Combine(ManagedFolder, GameAssembly);

			if (!Assemblies.LoadLizard())
				Environment.Exit(-1);

			string assemblyClean = GameAssembly + ".clean";
			string lizard = Path.Combine(ManagedFolder, LizardDll);
			string attributes = Path.Combine(ManagedFolder, AttributesDll);

			Unpatch(GameAssembly, assemblyClean, lizard, attributes);

			// no hooks
			if (Attributes.Sum(x => x.Value.Count) == 0)
			{
				RunGame();

				Environment.Exit(0);
            }

			Patch(GameAssembly, assemblyClean, lizard, attributes);

			RunGame();
        }

		private static void Unpatch(string assembly, string assemblyClean, string lizard, string attributes)
		{
			if (File.Exists(assemblyClean))
			{
				File.Delete(assembly);
				File.Copy(assemblyClean, assembly);
			}

			if (File.Exists(lizard))
				File.Delete(lizard);

			if (File.Exists(attributes))
				File.Delete(attributes);
		}

		private static void Patch(string assembly, string assemblyClean, string lizard, string attributes)
		{
			// keep original assembly before modification
			if (!File.Exists(assemblyClean))
				File.Copy(assembly, assemblyClean);

			File.Copy(LizardDll, lizard);
			File.Copy(AttributesDll, attributes);

			Mono.Cecil.AssemblyDefinition assemblyDefinition = Cecil.ReadAssembly(assembly);

			if (assemblyDefinition == null)
			{
				Externs.MessageBox("Assembly is null", "Failed to load assembly " + assembly);

				Environment.Exit(-1);
			}

			int injectedCorrectly = Util.InjectAllHooks(Attributes, assemblyDefinition);

			if (injectedCorrectly > 0)
				Cecil.WriteChanges(assemblyDefinition);

			ModCompiler.CompileMods();
		}

		private static void RunGame()
		{
			DirectoryInfo gameDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent;
			
			string[] files = Directory.GetFiles(gameDir.FullName, "*.exe");
			string gameFile = files.Length > 0 ? files[0] : null;

			if (gameFile == null)
				Externs.MessageBox("Game executable missing!", "Failed to find game executable");
			else
			{
				System.Diagnostics.Process proc = new System.Diagnostics.Process
				{
					StartInfo = new System.Diagnostics.ProcessStartInfo(gameFile)
				};

				proc.Start();
			}
		}
    }
}
