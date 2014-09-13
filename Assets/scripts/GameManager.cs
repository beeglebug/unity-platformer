using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

	private static GameObject playerObject;
	private static Player player;

	private string initialScene = "map1";

	static public GameManager _instance;

	void Awake()
	{
		_instance = this;
	
		// set self to hang around between level loads
		DontDestroyOnLoad(transform.gameObject);
		
		// cache stuff
		playerObject = GameObject.Find("player");
		player = playerObject.GetComponent<Player>();
		
		playerObject.transform.position = new Vector3(2,-12,0);
		
		// load the first level
		Application.LoadLevel (initialScene);
	}
	
	public static void PortalTransition(Portal portal)
	{		
		_instance.StartCoroutine(LoadScene(portal));
	}
	
	/**
	 * 
	 */
	static IEnumerator LoadScene(Portal sourcePortal)
	{
		// get current map
		GameObject oldMap = GameObject.FindWithTag("map-container");

		sourcePortal.disabled = true;

		float buffer = 0.1f;

		// start loading the new level
		Application.LoadLevelAdditive(sourcePortal.destinationScene);
		yield return 0;

		Portal destinationPortal = GameObject.Find(sourcePortal.destinationPortal).GetComponent<Portal>();
		
		Vector3 playerCenter = player.transform.position;
		Vector3 portalCenter = sourcePortal.GetComponentInParent<BoxCollider2D>().bounds.center;

		GameObject newMap = GameObject.Find(sourcePortal.destinationScene);

		// delete the old map
		Destroy(oldMap);
		
		// assuming vertical
		
		float x;
		Bounds bounds = destinationPortal.GetComponent<BoxCollider2D>().bounds;
		
		if(playerCenter.x > portalCenter.x) {
			x = bounds.min.x - buffer;
		} else {
			x = bounds.max.x + buffer;
		}
				
		player.transform.position = new Vector3(
			x,
			player.transform.position.y,
			player.transform.position.z
		);
		
		// also snap camera
		CameraFollow camera = GameObject.FindWithTag("MainCamera").GetComponent<CameraFollow>();
		camera.SnapToTarget();
		camera.CalculateBounds(newMap);
		
		//Debug.Log (onRight);
	}
	
	
	
	
}
