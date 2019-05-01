using Vector2 = UnityEngine.Vector2;

namespace Lizard
{
	public enum CollisionShapes
	{
		Box,
		Circle
	}

	public abstract class CollisionShape
	{
		public readonly CollisionShapes shape;

		private CollisionShape() { }

		internal CollisionShape(CollisionShapes _shape)
		{
			shape = _shape;
		}
	}

	public abstract class CollisionShape<T> : CollisionShape
	{
		public readonly T size;

		internal CollisionShape(CollisionShapes _shape, T _size) : base(_shape)
		{
			size = _size;
		}
	}

	public sealed class CollisionBox : CollisionShape<Vector2>
	{
		public CollisionBox(Vector2 _size) : base(CollisionShapes.Box, _size) { }

		public CollisionBox(float width, float height) : base(CollisionShapes.Box, new Vector2(width, height)) { }
	}

	public sealed class CollisionCircle : CollisionShape<float>
	{
		public CollisionCircle(float _size) : base(CollisionShapes.Circle, _size) { }
	}
}
