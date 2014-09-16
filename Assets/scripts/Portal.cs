using UnityEngine;
using System.Collections;

public class Portal : MonoBehaviour {

	public string destinationScene;
	public string destinationPortal;

	public bool disabled;

	public enum Direction { X, Y };

	public Direction direction = Direction.X;

}
