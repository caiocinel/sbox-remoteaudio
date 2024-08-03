using Sandbox.Citizen;

public class PlayerController : Component
{
	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );

	public Vector3 WishVelocity { get; private set; }

	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }

	[Sync]
	public Angles EyeAngles { get; set; }

	[Sync]
	public bool IsRunning { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( IsProxy )
			return;

		var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		if ( cam is not null )
		{
			var ee = cam.Transform.Rotation.Angles();
			ee.roll = 0;
			EyeAngles = ee;
		}
	}

	protected override void OnUpdate()
	{
		// Eye input
		if ( !IsProxy )
		{
			var ee = EyeAngles;
			ee += Input.AnalogLook * 0.5f;
			ee.roll = 0;
			EyeAngles = ee;

			var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
			var lookDir = EyeAngles.ToRotation();

			cam.Transform.Rotation = lookDir;
		
			IsRunning = Input.Down( "Run" );
		}
	}

	[Broadcast]
	public void OnJump( float floatValue, string dataString, object[] objects, Vector3 position )
	{
		AnimationHelper?.TriggerJump();
	}

	float fJumps;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishVelocity();

		var cc = GameObject.Components.Get<CharacterController>();

		if ( cc.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 268.3281572999747f * 1.2f;
			//if ( Duck.IsActive )
			//	flMul *= 0.8f;

			cc.Punch( Vector3.Up * flMul * flGroundFactor );
			//	cc.IsOnGround = false;

			OnJump( fJumps, "Hello", new object[] { Time.Now.ToString(), 43.0f }, Vector3.Random );

			fJumps += 1.0f;

		}

		if ( cc.IsOnGround )
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
			cc.Accelerate( WishVelocity );
			cc.ApplyFriction( 4.0f );
		}
		else
		{
			cc.Velocity -= Gravity * Time.Delta * 0.5f;
			cc.Accelerate( WishVelocity.ClampLength( 50 ) );
			cc.ApplyFriction( 0.1f );
		}

		cc.Move();

		if ( !cc.IsOnGround )
		{
			cc.Velocity -= Gravity * Time.Delta * 0.5f;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}
	}

	public void BuildWishVelocity()
	{
		var rot = EyeAngles.ToRotation();

		WishVelocity = rot * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( Input.Down( "Run" ) ) WishVelocity *= 320.0f;
		else WishVelocity *= 110.0f;
	}
}
