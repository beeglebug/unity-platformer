﻿using UnityEngine;
using System.Collections;

public class Hazard : MonoBehaviour {

	public enum Type { EnemyCollision, MeleeWeapon, EnvironmentSpike };
	
	public Type type;
	
	public int damage = 10;
	
	public void OnTriggerEnter2D(Collider2D collider)
	{
		CauseDamage(collider);
	}
	
	public void OnTriggerStay2D(Collider2D collider)
	{
		CauseDamage(collider);
	}

	public void CauseDamage(Collider2D collider)
	{
		if(collider.gameObject.HasComponent<Health>()) {
			
			Health health = collider.gameObject.GetComponent<Health>();
			
			health.TakeDamage(this);
		}
	}

}
