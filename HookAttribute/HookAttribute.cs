using System;

namespace Lizard
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AddCallAttribute : Attribute
    {
        bool AddToEnd { get; set; }
        string FullName { get; set; }

		public string GetName() => FullName;
		public bool PlaceAtEnd() => AddToEnd;

		public AddCallAttribute(string fullName, bool addToEnd = false)
        {
            FullName = fullName;
            AddToEnd = addToEnd;
        }
    }
}
