using System;

namespace Lizard
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class AddMethodAttribute : Attribute
	{
		string FullClassName { get; set; }
		string MethodName { get; set; }

		public string GetName() => FullClassName;
		public string GetMethodName() => MethodName;

		public AddMethodAttribute(string fullClassName, string methodName)
		{
			FullClassName = fullClassName;
			MethodName = methodName;
		}
	}
}
