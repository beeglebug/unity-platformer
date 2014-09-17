using UnityEngine;
using System.Collections;

public class CharacterInputController : MonoBehaviour
{
	public float gravity = -25f;
	public float runSpeed = 8f;
	public float climbSpeed = 4f;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;
	public float jumpHeight = 3f;

	[HideInInspector]
	private float normalizedHorizontalSpeed = 0;
	private float normalizedVerticalSpeed = 0;

	private CharacterController2D _controller;
	private Animator _animator;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;

	private KeyCode jumpKey = KeyCode.Space;

	void Awake()
	{
		_animator = GetComponent<Animator>();
		_controller = GetComponent<CharacterController2D>();
	}
	
	void Update()
	{
		// grab our current _velocity to use as a base for all calculations
		_velocity = _controller.velocity;

		if( _controller.isGrounded )
		{
			_velocity.y = 0;
		}

		if( Input.GetKey( KeyCode.RightArrow ) )
		{
			normalizedHorizontalSpeed = 1;
			if( transform.localScale.x < 0f )
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );

			if( _controller.isGrounded )
				_animator.Play( Animator.StringToHash( "run" ) );
		}
		else if( Input.GetKey( KeyCode.LeftArrow ) )
		{
			normalizedHorizontalSpeed = -1;
			if( transform.localScale.x > 0f )
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );

			if( _controller.isGrounded )
				_animator.Play( Animator.StringToHash( "run" ) );
		}
		else
		{
			normalizedHorizontalSpeed = 0;

			if( _controller.isGrounded ) {
				//_animator.Play( Animator.StringToHash( "idle" ) );
			}
		}

		/**
		 * ladders
		 */
		if (Input.GetKeyDown (KeyCode.UpArrow)) {

			if (_controller.isTouchingLadder) {

				if (!_controller.isOnLadder) {

					Debug.Log ("start climbing up ladder");
					_controller.isOnLadder = true;
				}

				normalizedVerticalSpeed = 1;
				_animator.Play( Animator.StringToHash( "climb" ) );

			}

		} else {
					
			normalizedVerticalSpeed = 0;

		}

		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			
			if (_controller.isTouchingLadder) {
				
				// only allow drop down if touching the top collider
				if (!_controller.isOnLadder && _controller.isTouchingLadderTop) {
					Debug.Log ("start climbing down ladder");
					_controller.isOnLadder = true;
					_controller.dropThroughPlatform();
				}
			}
			
			if(_controller.isOnLadder) {
				normalizedVerticalSpeed = -1;
				_animator.Play( Animator.StringToHash( "climb" ) );
			}
			
			
		} else {
			normalizedVerticalSpeed = 0;
		}

		// already climbing
		if (Input.GetKey (KeyCode.UpArrow)) {

			if(_controller.isOnLadder) {
				normalizedVerticalSpeed = 1;
			}

		} else if (Input.GetKey (KeyCode.DownArrow)) {
		
			if(_controller.isOnLadder) {
				normalizedVerticalSpeed = -1;
			}

		}

		// we can only jump whilst grounded
		if( _controller.isGrounded && Input.GetKeyDown( jumpKey ) )
		{
			if(Input.GetKey(KeyCode.DownArrow))
			{
				_controller.dropThroughPlatform();
				_animator.Play( Animator.StringToHash( "fall" ) );
			}
			else
			{
				_velocity.y = Mathf.Sqrt( 2f * jumpHeight * -gravity );
				_animator.Play( Animator.StringToHash( "jump" ) );
			}
		}

		// apply horizontal speed smoothing it
		var smoothedMovementFactor = _controller.isGrounded || _controller.isOnLadder ? groundDamping : inAirDamping; // how fast do we change direction?
		_velocity.x = Mathf.Lerp( _velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor );

		// apply gravity before moving (if not on ladder)
		if (_controller.isOnLadder) {
		
			_velocity.y = Mathf.Lerp( _velocity.y, normalizedVerticalSpeed * climbSpeed, Time.deltaTime * smoothedMovementFactor );

		}
		else
		{
			_velocity.y += gravity * Time.deltaTime;
		}

		_controller.move( _velocity * Time.deltaTime );
	}

}
