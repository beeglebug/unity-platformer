using UnityEngine;
using System.Collections;
using UnityEditor;

public class CameraFollow : MonoBehaviour {

	public GameObject target;
	public float xSmooth = 5f;
	public float ySmooth = 5f;

    public Vector2 min;
    public Vector2 max;
    
	void Awake () {
	
		GameObject lol = GameObject.FindGameObjectWithTag ("Player");
        
		Selection.activeObject = lol;
		Debug.Log (lol);
	}
	

	void Update () {

		TrackTarget ();
        ConstrainToBounds ();        
	
    }

	void OnLevelWasLoaded(int level) {
	
		CalculateBounds (GameObject.FindGameObjectWithTag ("map"));
	
	}

	public void SnapToTarget()
	{
		transform.position = new Vector3(
			target.transform.position.x,
			target.transform.position.y,
			transform.position.z
		);
		
	}

	void TrackTarget() {

		float targetX = target.transform.position.x;
		float targetY = target.transform.position.y;

		targetX = Mathf.Lerp (transform.position.x, target.transform.position.x, xSmooth * Time.deltaTime);
		targetY = Mathf.Lerp (transform.position.y, target.transform.position.y, ySmooth * Time.deltaTime);
	
		transform.position = new Vector3(targetX, targetY, transform.position.z);

    }

    
    public void CalculateBounds(GameObject mapObject)
    { 
		float cameraHeight = 2f * camera.orthographicSize;
        float cameraWidth = camera.aspect * cameraHeight;
        
		TiledMap map = mapObject.GetComponent<TiledMap>();

		min = new Vector2(
			map.bounds.min.x + (cameraWidth / 2f),
			map.bounds.min.y + (cameraHeight / 2f)
		);
        
		max = new Vector2(
			map.bounds.max.x - (cameraWidth / 2f),
			map.bounds.max.y - (cameraHeight / 2f)
		);

		ConstrainToBounds();

    }
    
    void ConstrainToBounds() {
        
		transform.position = new Vector3(
        	Mathf.Clamp(transform.position.x, min.x, max.x),
        	Mathf.Clamp(transform.position.y, min.y, max.y),
        	transform.position.z
        );

    }
    
}
