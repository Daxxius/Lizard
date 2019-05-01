public class ExampleSkill : Player.ProjectileAttackState, IModSkill
{
	public virtual string DisplayName { get { return "Example Skill"; } }						// name shown in menus
	public virtual string Description { get { return "Example skill description."; } }			// description show in menus
	public virtual string SkillImage { get { return "ExampleSkill.png"; } }						// image for the skill icon/card
	public virtual string Empowered { get { return "Example skill empowered description"; } }	// description for the empowered version of the skill if there is one
	
	// max of 5 tiers
	// this skill only has 1 tier and is added to tier table zero
	
	// add this skill to the following tier tables (for drops, shops, etc)
	public virtual int[] Tiers { get { return new int[] { 0 }; } }
	
	// stats for each tier of the spell
	// last value in each array is copied to fill the remaining slots (total of 5 slots)
	public virtual SkillStats Stats
	{
		get
		{
			return new SkillStats()
			{
				ID = new string[1] { skillID },						// id of the skill per tier, typically the same for all tiers
				burnChance = new float[1] { 0.3f },					// 30% chance to inflict burning
				cooldown = new float[1] { 1.0f },					// 3 second cooldown
				damage = new int[1] { 20 },							// damage each projectile causes
				elementType = new string[1] { "Fire" },				// damage element type
				sameAttackImmunityTime = new float[1] { 0.0625f },	// minimum delay between each tick of damage from projectiles
			};
		}
	}
	
	protected float moveSpeed = 12.0f;						// how quickly each projectile will move
	protected float projFlyTime = 2.0f;						// duration that a projectile will fly for before resetting or returning to the player
	
	protected string skillAudioID = "FlameLight";			// audio to play when skill is used
	protected string effectID = "FlameRing";				// particle effect to spawn when the projectiles are shot
	
	const float MAX_TARGET_DISTANCE = 20.0f;
	
	// determines what player animation should play on cast
	public override string OnEnterAnimStr
	{
		get
		{
			if (parent.facingDirection != FacingDirection.Right)
				return Lizard.GetForehandAnimStr(this);
			else
				return Lizard.GetBackhandAnimStr(this);
		}
	}
	
	public ExampleSkill(FSM parentFSM, Player parentEntity) : base("", parentFSM, parentEntity)
	{
		// animation times for player when using the skill (animation time factor => 0.0 = start, 1.0 = finish)
		float startTime = 0.1f;
		float holdTime = 0.2f;
		float executeTime = 0.3f;
		float cancelTime = 0.6f;
		float runTime = 0.8f;
		float exitTime = 1.0f;
		
		SetAnimTimes(startTime, holdTime, executeTime, cancelTime, runTime, exitTime);
	}
	
	public override void OnEnter()
	{
		base.OnEnter(); 	// MUST call base.OnEnter()
		
		SetSkillLevel(1);	// set skill data to stats in first slot
		
		// things to do when entering this skill state
		parent.movement.moveVector = -inputVector;
		parent.movement.MoveToMoveVector(moveSpeed, false);
		
		CreateProjectiles();
	}
	
	protected void PlaySound(string audioID)
	{
		Vector2? soundOrigin = null;					// custom sound origin
		Transform originTransform = parent.transform;	// if transform is not null, use the transform position as the sound origin
		float maxDist = 24.0f;							// maximum distance the sound will travel from origin
		float volume = -1.0f;							// override volume with value, a negative value will not override volume
		float pitch = SoundManager.StandardPitchRange;	// override pitch with value, a negative value will not override pitch
		bool ignoreTimeRestriction = false;				// sound won't play if the same sound was played less than 50 milliseconds ago
		
		SoundManager.PlayAudioWithDistance(audioID, soundOrigin, originTransform, maxDist, volume, pitch, ignoreTimeRestriction);
	}
	
	protected void SpawnParticles(int? _particleCount = null, string _effectID = null)
	{
		// get a particle effect system
		ParticleEffect effect;

		if (_effectID == null)
			effect = PoolManager.GetPoolItem<ParticleEffect>();
		else
			effect = PoolManager.GetPoolItem<ParticleEffect>(_effectID);
		
		Vector3? spawnPos = parent.transform.position;	// spawn position
		ParticleSystemOverride overrides = null;		// particle system overrides, can override things like particle lifetime, spawn rate, and particle size
		Vector3? localRotation = null;					// optional rotation to spawn
		float delayTime = 0.0f;							// time to wait before spawning
		float? sortYOffset = null;						// bias for rendering particles above other objects based on vertical position
		Transform followTransform = null;				// an object the particles should move towards, will set emitter position to the objects position
		
		effect.Emit(_particleCount, spawnPos, overrides, localRotation, delayTime, sortYOffset, followTransform);
	}
	
	protected void CreateProjectiles()
	{
		float maxTargetDistanceSq = MAX_TARGET_DISTANCE * MAX_TARGET_DISTANCE;
		
		int shotCount = 0;
		
		foreach (GameObject enemy in GameController.enemies)
		{
			Transform enemyTr = enemy.transform;
			Vector2 diff = enemyTr.position - parent.transform.position;
			float distSq = Vector2.Dot(diff, diff);
		
			if (distSq <= maxTargetDistanceSq)
			{
				// skill, setAttackInfo = true
				ExampleProjectile proj = Lizard.CreateProjectile<ExampleProjectile>(this);
				proj.target = enemyTr;
				
				Vector2 moveVector = Vector2.right;
				
				if (distSq > 0.0f)
					moveVector = diff / Mathf.Sqrt(distSq);	// normalised direction to target
				
				bool setKnockbackVector = true;
				
				// Sets all the skill data on the projectile and sets its travel direction
				// FireProjectile is a function in ProjectileAttackState, MeleeAttackState skills cannot use it
				FireProjectile(proj, moveVector, setKnockbackVector, moveSpeed, projFlyTime);
				
				shotCount++;
			}
		}
		
		if (shotCount > 0)
		{
			SpawnParticles(2, effectID);
			PlaySound(skillAudioID);
		}
	}
	
	protected void ShakeScreen()
	{
		float shakeIntensity = 0.2f;		// the amount to shake the screen
		bool ignoreShakeCount = false;		// if the screen should shake even if it is already shaking
		
		CameraController.ShakeCamera(shakeIntensity, ignoreShakeCount);
	}
}