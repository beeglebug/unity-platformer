using UnityEngine;
using System.Collections;

public static class ExtensionMethods {

	/// <summary>
	/// Checks whether a game object has a component of type T attached.
	/// </summary>
	/// <param name="gameObject">Game object.</param>
	/// <returns>True when component is attached.</returns>
	public static bool HasComponent<T> (this GameObject gameObject) where T : Component
	{
		return gameObject.GetComponent<T>() != null;
	}
	
	
}
