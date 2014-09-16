using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

	private static GameObject playerObject;
	private static Player player;

	private string initialScene = "scene1";

	static public GameManager _instance;

	void Awake()
	{
		_instance = this;
	
		// set self to hang around between level loads
		DontDestroyOnLoad(transform.gameObject);
		
		// cache stuff
		playerObject = GameObject.Find("player");
		player = playerObject.GetComponent<Player>();
		
		playerObject.transform.position = new Vector3(28,-12,0);
		
		// load the first level
		Application.LoadLevel (initialScene);
	}
	
	public static void PortalTransition(Portal portal)
	{		
		_instance.StartCoroutine(PortalToNewScene(portal));
	}
	
	/**
	 * 
	 */
	static IEnumerator PortalToNewScene(Portal sourcePortal)
	{
		// get current map
		GameObject oldMap = GameObject.FindWithTag("scene-container");

		sourcePortal.disabled = true;

		Vector2 buffer = new Vector2(0.1f, 0.2f);
		
		// get this pre yield, because the player will move in between
		Vector3 position = player.transform.position;

		// start loading the new level
		Application.LoadLevelAdditive(sourcePortal.destinationScene);
		
		yield return 0;

		GameObject destinationPortalObject = GameObject.Find(sourcePortal.destinationPortal);
		
		Portal destinationPortal = destinationPortalObject.GetComponent<Portal>();

		destinationPortal.disabled = true;

		GameObject newMap = GameObject.Find(sourcePortal.destinationScene);

		// delete the old map
		Destroy(oldMap);
		
		// is it a vertical or horizontal portal?
		Bounds sourceBounds = sourcePortal.GetComponentInParent<BoxCollider2D>().bounds;
		Bounds destinationBounds = destinationPortalObject.GetComponent<BoxCollider2D>().bounds;
				
		if(sourcePortal.direction == Portal.Direction.X) {
		
			if(position.x > sourceBounds.center.x) {
				position.x = destinationBounds.min.x - buffer.x;
			} else {
				position.x = destinationBounds.max.x + buffer.x;
			}
		
			//@todo calculate the offset based on relative sizes
		
			// get the offset position inside the portal
			float offset = player.transform.position.y - sourceBounds.max.y;
			
			// set relative to the new portal
			position.y = destinationBounds.max.y + offset;
					
		} else {
		
			if(position.y > sourceBounds.center.y) {
				position.y = destinationBounds.min.y - buffer.y;
			} else {
				position.y = destinationBounds.max.y + buffer.y;
			}
		
			// get the offset position inside the portal
			float offset = player.transform.position.x - sourceBounds.min.x;
			
			// set relative to the new portal
			position.x = destinationBounds.min.x + offset;
		
		}
				
		player.transform.position = new Vector3(
			position.x,
			position.y,
			player.transform.position.z
		);
		
		// also snap camera
		CameraFollow camera = GameObject.FindWithTag("MainCamera").GetComponent<CameraFollow>();
		camera.SnapToTarget();
		camera.CalculateBounds(newMap);
	}
	
	
	
	
}
