using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {

	private CharacterController2D _controller;
	private Vector3 _velocity;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;
	public float runSpeed = 8f;
	public float gravity = -25f;
	
	[HideInInspector]
	private float normalizedHorizontalSpeed = 0;
	
	public enum Direction { Right, Left };
	public Direction movementDirection = Direction.Left;
	
	void Awake ()
	{
		_controller = GetComponent<CharacterController2D>();
	}
	
	void Update ()
	{
		_velocity = _controller.velocity;
	
		if( _controller.isGrounded )
		{
			_velocity.y = 0;
		}
	
		if(_controller.collisionState.left) {
			movementDirection = Direction.Right;
		} else if(_controller.collisionState.right) {
			movementDirection = Direction.Right;
		}
	
		if( movementDirection == Direction.Right )
		{
			normalizedHorizontalSpeed = 1;
			if( transform.localScale.x < 0f ) {
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
			}

		} else if( movementDirection == Direction.Left )
		{
			normalizedHorizontalSpeed = -1;
			if( transform.localScale.x > 0f ) {
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
			}
		}
		else
		{
			normalizedHorizontalSpeed = 0;
		}
		
		// apply horizontal speed smoothing it
		var smoothedMovementFactor = _controller.isGrounded || _controller.isOnLadder ? groundDamping : inAirDamping; // how fast do we change direction?
		_velocity.x = Mathf.Lerp( _velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor );
	
		_velocity.y += gravity * Time.deltaTime;
		_controller.move( _velocity * Time.deltaTime );
	
	}
}
