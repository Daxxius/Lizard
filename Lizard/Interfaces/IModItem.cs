namespace Lizard
{
	public interface IModItem
	{
		string DisplayName { get; }
		string Description { get; }
		string ItemImage { get; }

		void Init();
	}
}
