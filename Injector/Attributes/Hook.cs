using System.Collections.Generic;

namespace Lizard.Attributes
{
	public abstract class Hook
	{
		protected List<HookData> AllAttributes { get; set; }
		public List<HookData> Attributes { get; set; }
		public int Count => Attributes.Count;

		public abstract AddAttributesResponse AddAllFound();

		protected AddAttributesResponse AddAllFound(System.Type hookType)
		{
			System.Text.StringBuilder errorText = new System.Text.StringBuilder();

			foreach (HookData hookData in AllAttributes)
				if (!hookData.Type.IsPublic)
					errorText.AppendLine(hookData.Type.Name);

			if (errorText.Length > 0)
			{
				Externs.MessageBox("Hook(s) are not public!", "Following type(s) are not public\n" + errorText.ToString());

				return AddAttributesResponse.Error;
			}

			Attributes = new List<HookData>();

			string typeName = hookType.FullName;

			foreach (HookData hookData in AllAttributes)
				if (hookData.Attribute.AttributeType.FullName == typeName)
					Attributes.Add(hookData);

			return AddAttributesResponse.Ok;
		}
	}

	public abstract class Hook<T> : Hook where T : System.Attribute
	{
		public override AddAttributesResponse AddAllFound() => AddAllFound(typeof(T));

		protected Hook(List<HookData> attributes)
		{
			AllAttributes = attributes;
		}
	}
}
