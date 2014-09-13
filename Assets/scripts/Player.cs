using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
	

	void OnTriggerStay2D(Collider2D collidedWith)
	{		
		Portal portal = collidedWith.GetComponentInParent<Portal>();
		
		if(portal && !portal.disabled) {
		
			if(collidedWith.bounds.Contains (transform.position)){
				
				GameManager.PortalTransition(portal);
			}
			
		}
	}

}
