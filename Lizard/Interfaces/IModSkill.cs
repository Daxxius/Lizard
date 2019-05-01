namespace Lizard
{
	public interface IModSkill
	{
		string DisplayName { get; }
		string Description { get; }
		string SkillImage { get; }
		string Empowered { get; }

		int[] Tiers { get; }

		SkillStats Stats { get; }
	}
}
