using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {

	public int max = 100;
	public int current;
	public bool invulnerable = false;
	private float _invulnerableLength = 1.0f;
	private float _invulnerableTime;
	
	void Awake() {
	
		current = max;
	
	}
	
	void Update()
	{
		// reset invulnerability
		if(invulnerable && _invulnerableTime < Time.time - _invulnerableLength)
		{
			invulnerable = false;
		}
	
	}
	
	public void TakeDamage(Hazard source)
	{
		if(invulnerable) { return; }
		
		current -= source.damage;
		
		invulnerable = true;
		_invulnerableTime = Time.time;
		
		if(current <= 0) {
			Destroy(this.gameObject);
		}
	}
	
}
