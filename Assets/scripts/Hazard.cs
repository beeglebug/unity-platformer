using UnityEngine;
using System.Collections;

public class Hazard : MonoBehaviour {

	public enum Type { EnemyCollision };
	
	public Type type;
	
	public int damage = 10;
}
