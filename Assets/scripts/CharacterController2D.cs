#define DEBUG_CC2D_RAYS
using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent( typeof( BoxCollider2D ), typeof( Rigidbody2D ))]
public class CharacterController2D : MonoBehaviour
{
	private struct CharacterRaycastOrigins
	{
		public Vector3 topRight;
		public Vector3 topLeft;
		public Vector3 bottomRight;
		public Vector3 bottomLeft;
	}

	public class CharacterCollisionState2D
	{
		public bool right;
		public bool left;
		public bool above;
		public bool below;
		public bool becameGroundedThisFrame;
		public bool wasGroundedLastFrame;
		public bool movingDownSlope;
		public float slopeAngle;

		public void reset()
		{
			right = left = above = below = becameGroundedThisFrame = movingDownSlope = false;
			slopeAngle = 0f;
		}
	}

    // defines how far in from the edges of the collider rays are cast from. If cast with a 0 extent it will often result in ray hits that are not desired
	[SerializeField]
	[Range( 0.001f, 0.3f )]
	public float skinWidth = 0.02f;

	// mask with all layers that the player should interact with
	public LayerMask platformMask = 0;

	// mask with all layers that should act as one-way platforms
	[SerializeField]
	public LayerMask oneWayPlatformMask = 0;


	/// the max slope angle that the CC2D can climb
	[Range( 0, 90f )]
	public float slopeLimit = 45f;

	[Range( 2, 20 )]
	public int totalHorizontalRays = 8;

	[Range( 2, 20 )]
	public int totalVerticalRays = 4;


	/// this is used to calculate the downward ray that is cast to check for slopes. We use the somewhat arbitrary value 75 degrees
	/// to calculate the length of the ray that checks for slopes.
	private float _slopeLimitTangent = Mathf.Tan( 75f * Mathf.Deg2Rad );

	[HideInInspector][NonSerialized]
	public new Transform transform;
	[HideInInspector][NonSerialized]
	public BoxCollider2D boxCollider;
	[HideInInspector][NonSerialized]
	public Rigidbody2D rigidBody2D;
	[HideInInspector][NonSerialized]
	public CharacterCollisionState2D collisionState = new CharacterCollisionState2D();
	[HideInInspector][NonSerialized]
	public Vector3 velocity;

	public bool isGrounded {
		get {
			return collisionState.below;
		}
	}

	private const float kSkinWidthFloatFudgeFactor = 0.001f;

	/// holder for our raycast origin corners (TR, TL, BR, BL)
	private CharacterRaycastOrigins _raycastOrigins;

	/// stores our raycast hit during movement
	private RaycastHit2D _raycastHit;

	/// stores any raycast hits that occur this frame. we have to store them in case we get a hit moving
	/// horizontally and vertically so that we can send the events after all collision state is set
	private List<RaycastHit2D> _raycastHitsThisFrame = new List<RaycastHit2D>( 2 );

	// horizontal/vertical movement data
	private float _verticalDistanceBetweenRays;
	private float _horizontalDistanceBetweenRays;

	// we use this flag to mark the case where we are travelling up a slope and we modified our delta.y to allow the climb to occur.
	// the reason is so that if we reach the end of the slope we can make an adjustment to stay grounded
	private bool _isGoingUpSlope = false;

	// keep track of going through platforms
	private bool _hasdroppedThroughPlatform;
	private float _droppedThroughPlatformTime;
	private float _platformDropDelay = 0.1f;

	// ladder stuff
	public bool isTouchingLadder;
	public bool isTouchingLadderTop;
	public bool isOnLadder;

	void Awake()
	{
		// add our one-way platforms to our normal platform mask so that we can land on them from above
		platformMask |= oneWayPlatformMask;

		// cache some components
		transform = GetComponent<Transform>();
		boxCollider = GetComponent<BoxCollider2D>();
		rigidBody2D = GetComponent<Rigidbody2D>();

		// set up initial ray distances
		recalculateDistanceBetweenRays();
	}

	[System.Diagnostics.Conditional( "DEBUG_CC2D_RAYS" )]
	private void DrawRay( Vector3 start, Vector3 dir, Color color )
	{
		Debug.DrawRay( start, dir, color );
	}


	/// attempts to move the character to position + deltaMovement. Any colliders in the way will cause the movement to
	/// stop when run into.
	public void move( Vector3 deltaMovement )
	{
		// save our current grounded state which we will use for wasGroundedLastFrame and becameGroundedThisFrame
		collisionState.wasGroundedLastFrame = isGrounded;

		// clear our state
		collisionState.reset();
		_raycastHitsThisFrame.Clear();
		_isGoingUpSlope = false;

		var desiredPosition = transform.position + deltaMovement;
		primeRaycastOrigins( desiredPosition, deltaMovement );


		// first, we check for a slope below us before moving
		// only check slopes if we are going down and grounded
		if( deltaMovement.y < 0 && collisionState.wasGroundedLastFrame )
			handleVerticalSlope( ref deltaMovement );

		// now we check movement in the horizontal dir
		if( deltaMovement.x != 0 )
			moveHorizontally( ref deltaMovement );

		// next, check movement in the vertical dir
		if( deltaMovement.y != 0 )
			moveVertically( ref deltaMovement );

		transform.Translate( deltaMovement, Space.World );

		// only calculate velocity if we have a non-zero deltaTime
		if( Time.deltaTime > 0 )
			velocity = deltaMovement / Time.deltaTime;

		// set our becameGrounded state based on the previous and current collision state
		if( !collisionState.wasGroundedLastFrame && collisionState.below ) {
			collisionState.becameGroundedThisFrame = true;
			isOnLadder = false;
		}

		// if we are going up a slope we artificially set a y velocity so we need to zero it out here
		if( _isGoingUpSlope )
			velocity.y = 0;

		// re-enable 1 way platforms if we just dropped down
		if (_hasdroppedThroughPlatform && _droppedThroughPlatformTime < Time.time - _platformDropDelay)
		{
			_hasdroppedThroughPlatform = false;
			platformMask |= oneWayPlatformMask;
		}


	}

	/**
	 * temporarily disable 1 way platform collision
	 * so the character can jump down through them
	 * will be re-enabled in 100 milliseconds
	 */
	public void dropThroughPlatform()
	{
		platformMask ^= oneWayPlatformMask;
		_hasdroppedThroughPlatform = true;
		_droppedThroughPlatformTime = Time.time;
	}

	/// <summary>
	/// this should be called anytime you have to modify the BoxCollider2D at runtime. It will recalculate the distance between the rays used for collision detection.
	/// It is also used in the skinWidth setter in case it is changed at runtime.
	/// </summary>
	public void recalculateDistanceBetweenRays()
	{
		// figure out the distance between our rays in both directions
		// horizontal
		var colliderUseableHeight = boxCollider.size.y * Mathf.Abs( transform.localScale.y ) - ( 2f * skinWidth );
		_verticalDistanceBetweenRays = colliderUseableHeight / ( totalHorizontalRays - 1 );

		// vertical
		var colliderUseableWidth = boxCollider.size.x * Mathf.Abs( transform.localScale.x ) - ( 2f * skinWidth );
		_horizontalDistanceBetweenRays = colliderUseableWidth / ( totalVerticalRays - 1 );
	}

	/// <summary>
	/// resets the raycastOrigins to the current extents of the box collider inset by the skinWidth. It is inset
	/// to avoid casting a ray from a position directly touching another collider which results in wonky normal data.
	/// </summary>
	/// <param name="futurePosition">Future position.</param>
	/// <param name="deltaMovement">Delta movement.</param>
	private void primeRaycastOrigins( Vector3 futurePosition, Vector3 deltaMovement )
	{
		var scaledColliderSize = new Vector2( boxCollider.size.x * Mathf.Abs( transform.localScale.x ), boxCollider.size.y * Mathf.Abs( transform.localScale.y ) ) / 2;
		var scaledCenter = new Vector2( boxCollider.center.x * transform.localScale.x, boxCollider.center.y * transform.localScale.y );

		_raycastOrigins.topRight = transform.position + new Vector3( scaledCenter.x + scaledColliderSize.x, scaledCenter.y + scaledColliderSize.y );
		_raycastOrigins.topRight.x -= skinWidth;
		_raycastOrigins.topRight.y -= skinWidth;

		_raycastOrigins.topLeft = transform.position + new Vector3( scaledCenter.x - scaledColliderSize.x, scaledCenter.y + scaledColliderSize.y );
		_raycastOrigins.topLeft.x += skinWidth;
		_raycastOrigins.topLeft.y -= skinWidth;

		_raycastOrigins.bottomRight = transform.position + new Vector3( scaledCenter.x + scaledColliderSize.x, scaledCenter.y -scaledColliderSize.y );
		_raycastOrigins.bottomRight.x -= skinWidth;
		_raycastOrigins.bottomRight.y += skinWidth;

		_raycastOrigins.bottomLeft = transform.position + new Vector3( scaledCenter.x - scaledColliderSize.x, scaledCenter.y -scaledColliderSize.y );
		_raycastOrigins.bottomLeft.x += skinWidth;
		_raycastOrigins.bottomLeft.y += skinWidth;
	}


	/// <summary>
	/// we have to use a bit of trickery in this one. The rays must be cast from a small distance inside of our
	/// collider (skinWidth) to avoid zero distance rays which will get the wrong normal. Because of this small offset
	/// we have to increase the ray distance skinWidth then remember to remove skinWidth from deltaMovement before
	/// actually moving the player
	/// </summary>
	private void moveHorizontally( ref Vector3 deltaMovement )
	{
		var isGoingRight = deltaMovement.x > 0;
		var rayDistance = Mathf.Abs( deltaMovement.x ) + skinWidth;
		var rayDirection = isGoingRight ? Vector2.right : -Vector2.right;
		var initialRayOrigin = isGoingRight ? _raycastOrigins.bottomRight : _raycastOrigins.bottomLeft;

		for( var i = 0; i < totalHorizontalRays; i++ )
		{
			var ray = new Vector2( initialRayOrigin.x, initialRayOrigin.y + i * _verticalDistanceBetweenRays );

			DrawRay( ray, rayDirection * rayDistance, Color.red );

			// if we are grounded we will include oneWayPlatforms only on the first ray (the bottom one). this will allow us to
			// walk up sloped oneWayPlatforms
			if( i == 0 && collisionState.wasGroundedLastFrame )
				_raycastHit = Physics2D.Raycast( ray, rayDirection, rayDistance, platformMask );
			else
				_raycastHit = Physics2D.Raycast( ray, rayDirection, rayDistance, platformMask & ~oneWayPlatformMask );

			if( _raycastHit )
			{
				// the bottom ray can hit slopes but no other ray can so we have special handling for those cases
				if( i == 0 && handleHorizontalSlope( ref deltaMovement, Vector2.Angle( _raycastHit.normal, Vector2.up ) ) )
				{
					_raycastHitsThisFrame.Add( _raycastHit );
					break;
				}

				// set our new deltaMovement and recalculate the rayDistance taking it into account
				deltaMovement.x = _raycastHit.point.x - ray.x;
				rayDistance = Mathf.Abs( deltaMovement.x );

				// remember to remove the skinWidth from our deltaMovement
				if( isGoingRight )
				{
					deltaMovement.x -= skinWidth;
					collisionState.right = true;
				}
				else
				{
					deltaMovement.x += skinWidth;
					collisionState.left = true;
				}

				_raycastHitsThisFrame.Add( _raycastHit );

				// we add a small fudge factor for the float operations here. if our rayDistance is smaller
				// than the width + fudge bail out because we have a direct impact
				if( rayDistance < skinWidth + kSkinWidthFloatFudgeFactor )
					break;
			}
		}
	}


	/// <summary>
	/// handles adjusting deltaMovement if we are going up a slope.
	/// </summary>
	/// <returns><c>true</c>, if horizontal slope was handled, <c>false</c> otherwise.</returns>
	/// <param name="deltaMovement">Delta movement.</param>
	/// <param name="angle">Angle.</param>
	private bool handleHorizontalSlope( ref Vector3 deltaMovement, float angle )
	{
		// disregard 90 degree angles (walls)
		if( Mathf.RoundToInt( angle ) == 90 ) return false; 

		if( angle < slopeLimit )
		{
			// we only need to adjust the deltaMovement if we are not jumping
			// TODO: this uses a magic number which isn't ideal!
			if( deltaMovement.y < 0.07f )
			{
				// we dont set collisions on the sides for this since a slope is not technically a side collision
				// smooth y movement when we climb. we make the y movement equivalent to the actual y location that corresponds
				// to our new x location using our good friend Pythagoras
				deltaMovement.y = Mathf.Abs( Mathf.Tan( angle * Mathf.Deg2Rad ) * deltaMovement.x );
				_isGoingUpSlope = true;
				collisionState.below = true;
			}
		
		} else {
			// too steep. get out of here
			deltaMovement.x = 0;
		}

		return true;
	}

	private void moveVertically( ref Vector3 deltaMovement )
	{
		var isGoingUp = deltaMovement.y > 0;
		var rayDistance = Mathf.Abs( deltaMovement.y ) + skinWidth;
		var rayDirection = isGoingUp ? Vector2.up : -Vector2.up;
		var initialRayOrigin = isGoingUp ? _raycastOrigins.topLeft : _raycastOrigins.bottomLeft;

		// apply our horizontal deltaMovement here so that we do our raycast from the actual position we would be in if we had moved
		initialRayOrigin.x += deltaMovement.x;

		// if we are moving up, we should ignore the layers in oneWayPlatformMask
		var mask = platformMask;
		if( isGoingUp && !collisionState.wasGroundedLastFrame )
			mask &= ~oneWayPlatformMask;

		for( var i = 0; i < totalVerticalRays; i++ )
		{
			var ray = new Vector2( initialRayOrigin.x + i * _horizontalDistanceBetweenRays, initialRayOrigin.y );

			DrawRay( ray, rayDirection * rayDistance, Color.red );
			_raycastHit = Physics2D.Raycast( ray, rayDirection, rayDistance, mask );
			if( _raycastHit )
			{
				// set our new deltaMovement and recalculate the rayDistance taking it into account
				deltaMovement.y = _raycastHit.point.y - ray.y;
				rayDistance = Mathf.Abs( deltaMovement.y );

				// remember to remove the skinWidth from our deltaMovement
				if( isGoingUp )
				{
					deltaMovement.y -= skinWidth;
					collisionState.above = true;
				} else {
					deltaMovement.y += skinWidth;
					collisionState.below = true;
				}

				_raycastHitsThisFrame.Add( _raycastHit );

				// this is a hack to deal with the top of slopes. if we walk up a slope and reach the apex we can get in a situation
				// where our ray gets a hit that is less then skinWidth causing us to be ungrounded the next frame due to residual velocity.
				if( !isGoingUp && deltaMovement.y > 0.00001f ) _isGoingUpSlope = true;

				// we add a small fudge factor for the float operations here. if our rayDistance is smaller
				// than the width + fudge bail out because we have a direct impact
				if( rayDistance < skinWidth + kSkinWidthFloatFudgeFactor ) return;
			}
		}
	}

	/**
	 * checks the center point under the BoxCollider2D for a slope. 
	 * If it finds one then the deltaMovement is adjusted so that the player stays grounded
	 */
	private void handleVerticalSlope( ref Vector3 deltaMovement )
	{
		// slope check from the center of our collider
		var centerOfCollider = ( _raycastOrigins.bottomLeft.x + _raycastOrigins.bottomRight.x ) * 0.5f;
		var rayDirection = -Vector2.up;

		// the ray distance is based on our slopeLimit
		var slopeCheckRayDistance = _slopeLimitTangent * ( _raycastOrigins.bottomRight.x - centerOfCollider );

		var slopeRay = new Vector2( centerOfCollider, _raycastOrigins.bottomLeft.y );

		DrawRay( slopeRay, rayDirection * slopeCheckRayDistance, Color.yellow );
		_raycastHit = Physics2D.Raycast( slopeRay, rayDirection, slopeCheckRayDistance, platformMask );

		if( _raycastHit )
		{
			// bail out if we have no slope
			var angle = Vector2.Angle( _raycastHit.normal, Vector2.up );
			if( angle == 0 ) return;

			// we are moving down the slope if our normal and movement direction are in the same x direction
			var isMovingDownSlope = Mathf.Sign( _raycastHit.normal.x ) == Mathf.Sign( deltaMovement.x );
			if( isMovingDownSlope )
			{
				deltaMovement.y = _raycastHit.point.y - slopeRay.y - skinWidth;
				collisionState.movingDownSlope = true;
				collisionState.slopeAngle = angle;
			}
		}
	}

	/**
	 * handle various triggers the player might collide with
	 */
	void OnTriggerEnter2D(Collider2D collision)
	{
		switch(collision.gameObject.tag)
		{
			case "ladder":
				isTouchingLadder = true;
				break;
			case "ladder-top":
				isTouchingLadderTop = true;
				break;
		}
	}

	/**
	 * handle various triggers the player might collide with
	 */
	void OnTriggerExit2D(Collider2D collision)
	{
		switch(collision.gameObject.tag)
		{
			case "ladder":
				isTouchingLadder = false;
				isOnLadder = false;
				break;
			case "ladder-top":
				isTouchingLadderTop = false;
				break;
		}
	}

}
