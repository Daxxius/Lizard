// MonoBehaviour is a Unity3D class that functions as an attachable component for game objects
// MonoBehaviours can only be attached to UnityEngine.GameObject instances
public class ExampleProjectile : Projectile, IModProjectile
{
	public virtual string ProjectileImage { get { return "ExampleProjectile.png"; } }
	
	public virtual ProjectileMetadata Metadata
	{
		get
		{
			return new ProjectileMetadata
			{
				attackBoxShape = new CollisionCircle(0.5f),
				wallColliderShape = new CollisionCircle(0.1f),
				spriteSize = new Vector2(2.5f, 2.5f),
				spriteOffset = Vector2.zero,
				spriteRotationOffset = 0.0f,
				usesAnimator = false,
				collidesWithWalls = true,
				fixedRotation = false
			};
		}
	}
	
	protected ParticleEffect fireEmitter = null;
	protected float emissionRate = 15.0f;
	protected string endAudioID = "FireBlast";
	protected string explodeID = "FlameRing";
	
	protected float rotSpeed = 180.0f;
	protected float curAngle = 0.0f;
	
	public Transform target = null;
	
	public override void Awake()
	{
		base.Awake();
		impactAudioID = "ImpactFire";
	}
	
	public override void Start()
	{
		base.Start();
		fireEmitter = PoolManager.GetPoolItem<ParticleEffect>("SmokeEmitter");
		fireEmitter.reqFollowTrans = true;
		fireEmitter.followTransform = transform;
		fireEmitter.Play(emissionRate, null, null, lifeTime, null, null, 0.0f, true);
		
		rotation = true;
		curAngle = Mathf.Atan2(moveVector.y, moveVector.x) * Mathf.Rad2Deg;
	}
	
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		
		if (target == null)
			return;
		
		Vector2 diff = target.position - transform.position;
		
		float distSq = Vector2.Dot(diff, diff);
		float minDist = moveSpeed * Time.fixedDeltaTime;
		
		if (distSq > minDist * minDist)
		{
			float targetAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
			float turnAmount = rotSpeed * Time.fixedDeltaTime;
			float angleDiff = targetAngle - curAngle;
			
			if (angleDiff < -180.0f)
				angleDiff += 360.0f;
			else if (angleDiff > 180.0f)
				angleDiff -= 360.0f;
			
			if (Mathf.Abs(angleDiff) < turnAmount)
				curAngle = targetAngle;
			else
				curAngle += turnAmount * Mathf.Sign(angleDiff);
			
			float angleRad = curAngle * Mathf.Deg2Rad;
			
			moveVector = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
		}
		else
			ResetProjectile();
	}
	
	public override void ResetProjectile()
	{
		base.ResetProjectile();
		
		SpawnParticles();
		
		PlayAudio(endAudioID);
		
		if (fireEmitter != null)
			fireEmitter.Stop();
		
		base.ResetProjectile();
	}
	
	protected virtual void SpawnParticles()
	{
		ParticleEffect explodeEffect = PoolManager.GetPoolItem<ParticleEffect>(explodeID);
		
		int particleCount = 5;
		Vector3? spawnPos = transform.position;
		ParticleSystemOverride overrides = null;
		Vector3? spawnRot = null;
		float spawnDelay = 0.0f;
		float? sortYOffset = null;
		Transform followTransform = null;
		
		explodeEffect.Emit(particleCount, spawnPos, overrides, spawnRot, spawnDelay, sortYOffset, followTransform);
	}
	
	protected virtual void PlayAudio(string audioID)
	{
		SoundManager.PlayAudioWithDistance(audioID, null, transform, 24.0f, -1.0f, 1.0f, false);
	}
}