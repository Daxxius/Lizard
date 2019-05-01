using UnityEngine;

namespace Lizard
{
	public interface IModProjectile
	{
		string ProjectileImage { get; }
		ProjectileMetadata Metadata { get; }
	}

	public struct ProjectileMetadata
	{
		public static readonly ProjectileMetadata Default = new ProjectileMetadata
		{
			attackBoxShape = new CollisionBox(Vector2.one),
			wallColliderShape = new CollisionCircle(0.1f),
			spriteSize = new Vector2(1.5f, 1.5f),
			spriteOffset = Vector2.zero,
			spriteRotationOffset = 0.0f,
			usesAnimator = false,
			collidesWithWalls = true,
			fixedRotation = true
		};

		public CollisionShape attackBoxShape;
		public CollisionShape wallColliderShape;

		public Vector2 spriteSize;
		public Vector2 spriteOffset;
		public float spriteRotationOffset;

		public bool usesAnimator;
		public bool collidesWithWalls;
		public bool fixedRotation;
	}
}
