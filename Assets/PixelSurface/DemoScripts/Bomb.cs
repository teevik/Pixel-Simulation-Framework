using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PixSurf;

public class Bomb : MonoBehaviour {
	#region Public Properties
	public Vector2 speed = new Vector2(1, -2);
	public int boomRadius = 30;
	public int fireBits = 20;
	public PixelSurface surface;
	public GoBoom goBoom;
	public MakeFire makeFire;

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		if (surface == null) surface = GetComponentInParent<PixelSurface>();
		if (surface == null) {
			Debug.LogError("Bomb needs a PixelSurface to work!");
		}
	}
	
	void Update() {
		// Update our position (i.e., fall!)
		transform.position += (Vector3)speed * Time.deltaTime;

		// Check for hitting something (or falling off the bottom!)
		CheckHit();
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods
	void CheckHit() {
		Vector2 pixelPos = surface.PixelPosAtWorldPos(transform.position);
		if (pixelPos.y < -32) {
			// We've fallen off the bottom.
			GameObject.Destroy(gameObject);
			return;
		}

		if (!surface.InBounds(pixelPos)) return;

		Color c = surface.GetPixel(pixelPos);
		if (c.a > 0.1f) {
			// We've hit something!  Do our explosion effects.
			if (goBoom != null) goBoom.Splode(Mathf.RoundToInt(pixelPos.x), Mathf.RoundToInt(pixelPos.y), boomRadius);

			if (makeFire != null) {
				for (float ang=0; ang<360; ang+=5) {
					float radians = ang * Mathf.Deg2Rad;
					Vector2 p = new Vector2(pixelPos.x + Mathf.Cos(radians) * (boomRadius + 2),
					                        pixelPos.y + Mathf.Sin(radians) * (boomRadius + 2));
					if (makeFire.IsFlammable(surface.GetPixel(p))) {
						Fire.CreateFlameAt(surface, p);
					}
				}
			}

			// And, destroy this bomb.
			GameObject.Destroy(gameObject);
		}
	}
	#endregion
}
