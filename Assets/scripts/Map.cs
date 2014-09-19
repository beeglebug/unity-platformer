using UnityEngine;
using System.Collections;

public class Map : MonoBehaviour {

	public Bounds bounds;

	void Awake () {
	
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		
		foreach (Renderer renderer in renderers) {
			bounds.Encapsulate(renderer.bounds);
		}
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
