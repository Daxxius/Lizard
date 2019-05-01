using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Inject;
using System;
using System.Collections.Generic;
using System.Linq;
using ParameterCollection = Mono.Collections.Generic.Collection<Mono.Cecil.ParameterDefinition>;

namespace Lizard
{
	public static class Cecil
    {
        public static AssemblyDefinition ReadAssembly(string assemblyPath)
        {
            try
            {
				DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Program.ManagedFolder);

                return AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { AssemblyResolver = resolver });
            }
            catch (Exception ex)
            {
				Externs.MessageBox(ex.GetType().Name, ex.ToString());
				return null;
            }
        }

        public class ReturnData
        {
            public TypeDefinition typeDefinition;
            public MethodDefinition methodDefinition;

            public ReturnData(TypeDefinition typeDef, MethodDefinition methodDef)
            {
                typeDefinition = typeDef;
				methodDefinition = methodDef;
            }
        }

		public static bool MethodExists(TypeDefinition type, string methodName)
		{
			return type.Methods.Any(x => x.Name == methodName || x.FullName == methodName);
		}

		public static bool InjectNewMethod(TypeDefinition type, AssemblyDefinition assembly, Dictionary<string, TypeDefinition> typeDefinitions, string methodName, HookData hook)
		{
			try
			{
				MethodDefinition methodDef = new MethodDefinition(methodName, MethodAttributes.Public, assembly.MainModule.TypeSystem.Void)
				{
					IsStatic = type.IsSealed
				};

				type.Methods.Add(methodDef);

				ILProcessor il = methodDef.Body.GetILProcessor();

				methodDef.Body.Instructions.Add(il.Create(OpCodes.Call, assembly.MainModule.Import(hook.Method)));
				methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

				return true;
			}
			catch (Exception ex)
			{
				Externs.MessageBox(ex.GetType().Name, ex.ToString());

				return false;
			}
		}

		public static TypeDefinition ConvertStringToClass(string className, AssemblyDefinition assembly, Dictionary<string, TypeDefinition> typeDefinitions)
		{
			if (!typeDefinitions.TryGetValue(className, out TypeDefinition typeDef))
			{
				string desc = string.Format("Could not find class {0} in assembly {1}", className, assembly.Name);
				Externs.MessageBox("Could not find class!", desc);

				return null;
			}

			return typeDef;
		}

		private static MethodDefinition GetHook(List<MethodDefinition> methodDefs, HookData hookData, string className, string methodName)
		{
			MethodDefinition methodDef = methodDefs[0];

			if (methodDefs.Count == 1)
				return methodDef;

			ParameterCollection hookTypes = hookData.Method.Parameters;
			bool hookStatic = hookData.Method.Parameters.Count == 0 || hookData.Method.Parameters[0].ParameterType.Name != className;

			for (int i = 0; i < methodDefs.Count; i++)
				if (methodDefs[i].IsStatic != hookStatic)
					methodDefs.RemoveAt(i--);

			if (hookTypes.Count == 0 || !hookStatic && hookTypes.Count == 1)
			{
				for (int i = 0; i < methodDefs.Count; i++)
					if (methodDefs[i].Parameters.Count == 0)
						return methodDefs[i];

				return methodDef;
			}

			for (int i = 0; i < methodDefs.Count; i++)
				if (!SafeMatch(methodDefs[i], hookData))
					methodDefs.RemoveAt(i--);

			if (methodDefs.Count > 1)
			{
				System.Text.StringBuilder hooks = new System.Text.StringBuilder("Could not determine which function to hook:\n");

				foreach (MethodDefinition md in methodDefs)
				{
					hooks.Append($"{className}.{md.Name}(");

					if (md.Parameters.Count > 0)
					{
						hooks.Append(md.Parameters[0].ParameterType.Name);

						for (int i = 1; i < md.Parameters.Count; i++)
							hooks.Append($", {md.Parameters[i].ParameterType.Name}");
					}

					hooks.Append(")\n");
				}

				Externs.MessageBox("Ambiguous hook!", hooks.ToString());
			}

			if (methodDefs.Count > 0)
				methodDef = methodDefs[0];

			return methodDef;
		}

		private static bool CompareTypes(ParameterDefinition paramDef1, ParameterDefinition paramDef2)
		{
			return paramDef1.ParameterType.FullName == paramDef2.ParameterType.FullName;
		}

		private static bool CompareTypes(ParameterDefinition paramDef, TypeReference typeRef)
		{
			return paramDef.ParameterType.FullName == typeRef.FullName;
		}

		private static bool SafeMatch(MethodDefinition methodDef, HookData hookData)
		{
			int returnArg = methodDef.IsStatic ? 0 : 1;
			int reqReturnAddArgs = methodDef.IsStatic ? 1 : 2;

			int maxArgs = methodDef.Parameters.Count + reqReturnAddArgs;

			if (hookData.Method.Parameters.Count > maxArgs)
				return false;

			if (hookData.Method.Parameters.Count == maxArgs)
			{
				// has return argument
				if (!CompareTypes(hookData.Method.Parameters[returnArg], methodDef.ReturnType))
					return false;

				for (int i = returnArg + 1; i < hookData.Method.Parameters.Count; i++)
					if (!CompareTypes(hookData.Method.Parameters[i], methodDef.Parameters[i - reqReturnAddArgs]))
						return false;
			}
			else
			{
				// has no return argument
				for (int i = returnArg; i < hookData.Method.Parameters.Count; i++)
					if (!CompareTypes(hookData.Method.Parameters[i], methodDef.Parameters[i - returnArg]))
						return false;
			}

			return true;
		}

        public static ReturnData ConvertStringToClassAndMethod(string str, AssemblyDefinition assembly, Dictionary<string, TypeDefinition> typeDefinitions, HookData hookData)
        {
            string[] _strSplit = str.Split('.');
            string className = str.Substring(0, str.Substring(0, str.Length - 1).LastIndexOf('.'));
            string methodName = _strSplit.Last();

			if (!typeDefinitions.TryGetValue(className, out TypeDefinition typeDef))
			{
				string desc = string.Format("Could not find class {0} in assembly {1}", className, assembly.Name);
				Externs.MessageBox("Could not find class!", desc);

				return null;
			}

			List<MethodDefinition> methodDefs = typeDef.GetMethods(methodName).ToList();

			if (methodDefs.Count == 0)
			{
				string desc = string.Format("Could not find method {0} in type {1}", methodName, className);
				Externs.MessageBox("Could not find method!", desc);

				return null;
			}

			MethodDefinition methodDef = GetHook(methodDefs, hookData, className, methodName);

            return new ReturnData(typeDef, methodDef);
        }

		private static InjectionDefinition GetInjectionDefinition(ReturnData data, HookData hook)
		{
			InjectFlags flags = 0;
			
			if (!data.methodDefinition.IsStatic)
				flags |= InjectFlags.PassInvokingInstance;
			
			if (data.methodDefinition.HasParameters)
			{
				if (hook.Method.Parameters.Any((x) => x.IsIn))
					flags |= InjectFlags.PassParametersRef;
				else
					flags |= InjectFlags.PassParametersVal;
			}

			if (hook.Method.ReturnType != hook.Assembly.MainModule.TypeSystem.Void)
				flags |= InjectFlags.ModifyReturn;

			return new InjectionDefinition(data.methodDefinition, hook.Method, flags);
		}

		internal static bool Inject(ReturnData data, HookData hook)
		{
            try
            {
				InjectionDefinition injector = GetInjectionDefinition(data, hook);

                if ((bool)hook.Attribute.ConstructorArguments[1].Value)
                    injector.Inject(-1);
                else
                    injector.Inject();

                return true;
            }
            catch (Exception ex)
            {
				Externs.MessageBox(ex.GetType().Name, ex.ToString());
            }

            return false;
        }

        internal static bool WriteChanges(AssemblyDefinition targetAssembly)
        {
            try
            {
				targetAssembly.Write(Program.GameAssembly);

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
