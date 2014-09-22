using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Tiled2Unity.CustomTiledImporter]
class TiledImporter : Tiled2Unity.ICustomTiledImporter
{
	public void HandleCustomProperties(GameObject gameObject, IDictionary<string, string> properties)
	{

	}

	
	public void CustomizePrefab(GameObject prefab)
	{
		prefab.tag = "map";
	
		ImportLadders (prefab);		
		ImportSpikes (prefab);		
	}

	public void ImportSpikes(GameObject prefab)
	{
		// find all the polygon colliders
		Component[] polygonColliders = prefab.GetComponentsInChildren<PolygonCollider2D>();
		
		if (polygonColliders == null)
			return;
		
		// find all *ladder* polygon colliders
		int mask = LayerMask.NameToLayer("spikes");
		var polygons = from polygon in polygonColliders
			where polygon.gameObject.layer == mask
				select polygon;
		
		if (polygons == null)
			return;
		
		foreach (PolygonCollider2D poly in polygons)
		{		
			Hazard hazard = poly.gameObject.AddComponent<Hazard>();
		}
	}
	
	
	public void ImportLadders(GameObject prefab)
	{

		// find all the polygon colliders
		Component[] polygonColliders = prefab.GetComponentsInChildren<PolygonCollider2D>();

		if (polygonColliders == null)
			return;
	
		// find all *ladder* polygon colliders
		int mask = LayerMask.NameToLayer("ladders");
		var polygons = from polygon in polygonColliders
			where polygon.gameObject.layer == mask
			select polygon;

		if (polygons == null)
			return;

		foreach (PolygonCollider2D poly in polygons)
		{
			// make the top object
			GameObject top = new GameObject("top");

			// set some tags
			top.tag = "ladder-top";
			poly.gameObject.tag = "ladder";

			// set as triggers
			poly.isTrigger = true;

			// Create edge colliders for each path
			for (int p = 0; p < poly.pathCount; ++p)
			{
				Vector2[] points = poly.GetPath(p);
				
				float xMin = points.Min(pt => pt.x);
				float xMax = points.Max(pt => pt.x);
				float y = points.Max(pt => pt.y);

				// make a collider
				EdgeCollider2D topEdgeCollider2D = top.AddComponent<EdgeCollider2D>();
				topEdgeCollider2D.points = new Vector2[]
				{
					new Vector2(xMin, y),
					new Vector2(xMax, y),
				};

				topEdgeCollider2D.isTrigger = true;

			}

			// parent the new component
			top.transform.parent = poly.gameObject.transform;
		}
	}
}