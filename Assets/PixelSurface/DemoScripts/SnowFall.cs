using UnityEngine;
using PixSurf;

public class Snow : LivePixel {
	bool ClearAt(PixelSurface surf, int x, int y) {
		Color c = surf.GetPixel(x, y);
		return c == Color.black || c.a == 0;
	}
	
	public override void Update(PixelSurface surf) {
		float r = Random.Range(0.5f, 1f);
		color = new Color(r, r, r);
		
		int oldy = y;
		position.y -= Time.deltaTime * 10f;
		if (y != oldy) position.x += Random.Range(-1f,1f);
		
		if (!ClearAt(surf, x, y)) {
			// We've hit something.  See if it's clear to the sides.
			bool clearLeft = ClearAt(surf, x-1, y);
			bool clearRight = ClearAt(surf, x+1, y);
			if (clearLeft && clearRight) {
				if (Random.Range(0f,1f) > 0.5f) position.x--;
				else position.x++;
			} else if (clearLeft) {
				position.x--;
			} else if (clearRight) {
				position.x++;
			} else {
				// Couldn't find a clear spot.  Move back up 1 space, and die.
				position.y++;
				Die();
			}
		}
	}
}

public class SnowFall : MonoBehaviour {
	#region Public Properties
	public float probability = 10;	// probability of creating new snowflake on each frame

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	PixelSurface surf;

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		surf = GetComponent<PixelSurface>();
	}
	
	void Update() {
		if (Random.Range(0, 100) < probability) {
			int x = Random.Range(0, surf.totalWidth);
			surf.CreateLivePixel<Snow>(x, surf.totalHeight, Color.white);
		}
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods

	#endregion
}
