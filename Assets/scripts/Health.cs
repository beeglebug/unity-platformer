using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {

	public int max = 100;
	public int current;
	
	void Awake() {
	
		current = max;
	
	}
	
	void OnGUI()
	{
		//GUI.backgroundColor = Color.red;
			
		//GUI.Label (new Rect (10,10,200,100), "Health: " + current);
		
	}
	
	public void Reduce(int amount)
	{
		current -= amount;
		
		if(current <= 0) {
			Destroy(this.gameObject);
		}
	}
	
}
