using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {

	public float gravity = -25f;
	public float runSpeed = 2f;
	public float climbSpeed = 4f;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;
	public float jumpHeight = 3f;
	
	[HideInInspector]
	private float normalizedHorizontalSpeed = 0;
	//private float normalizedVerticalSpeed = 0;
	
	private CharacterController2D _controller;
	private Animator _animator;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;
		
	void Awake () {

		_controller = GetComponent<CharacterController2D>();
			
	}
	
	// Update is called once per frame
	void Update () {
	
		_velocity = _controller.velocity;
		
		if( _controller.isGrounded )
		{
			_velocity.y = 0;
		}
		
		bool moveRight = false;
		bool moveLeft = false;

		GameObject player = GameObject.FindGameObjectWithTag("Player");
		
		if(player.transform.position.x < transform.position.x) {
			moveLeft = true;
			moveRight = false;
		} else {
			moveLeft = false;
			moveRight = true;
		}
		
		if( moveRight)
		{
			normalizedHorizontalSpeed = 1;
			if( transform.localScale.x < 0f ) {
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
			}
			
			if( _controller.isGrounded ) {
				//_animator.Play( Animator.StringToHash( "run" ) );
			}
		}
		else if( moveLeft )
		{
			normalizedHorizontalSpeed = -1;
			if( transform.localScale.x > 0f ) {
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
			}
			
			if( _controller.isGrounded ) {
				//_animator.Play( Animator.StringToHash( "run" ) );
			}
		}
		else
		{
			normalizedHorizontalSpeed = 0;
			
			if( _controller.isGrounded ) {
				//_animator.Play( Animator.StringToHash( "idle" ) );
			}
		}
		
		
		// apply horizontal speed smoothing it
		var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
		_velocity.x = Mathf.Lerp( _velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor );
		
		_velocity.y += gravity * Time.deltaTime;
		
		_controller.move( _velocity * Time.deltaTime );

	}
}
