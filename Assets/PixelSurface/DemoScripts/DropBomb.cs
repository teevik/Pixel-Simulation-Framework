using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PixSurf;

public class DropBomb : MonoBehaviour {
	#region Public Properties
	public GameObject bombPrototype;

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	PixelSurface surf;
	
	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		surf = GetComponent<PixelSurface>();
		bombPrototype.SetActive(false);
	}
	
	void Update() {
		if (!Input.GetMouseButtonDown(0)) return;
		Vector2 pixelPos;
		if (surf.PixelPosAtScreenPos(Input.mousePosition, out pixelPos)) {
			DropBombAt(Mathf.RoundToInt(pixelPos.x), surf.totalHeight);
		}
	}
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	void DropBombAt(int x, int y) {
		// Somebody set up us the bomb.
		GameObject bomb = GameObject.Instantiate(bombPrototype);
		bomb.transform.position = surf.WorldPosAtPixelPos(new Vector2(x, y));
		bomb.name = "Bomb";
		bomb.SetActive(true);
		// (You have no chance to survive make your time.)
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods
	void Splode(int x, int y, int radius=20) {
		Vector2 center = new Vector2(x, y);
		Rect r = new Rect(x-radius, y-radius, radius*2, radius*2);
		float radSqr = radius*radius;
		for (int j = (int)r.yMin; j < (int)r.yMax; j++) {
			for (int i = (int)r.xMin; i < (int)r.xMax; i++) {
				float dsqr = (i-x)*(i-x) + (j-y)*(j-y);
				if (dsqr > radSqr) continue;					// out of circle bounds
				
				surf.ClearLivePixels(i, j);						// clear any live pixels (poof!)
				
				if (Random.Range(0f, 1f) > 0.2f) continue;		// random subset
				
				Color c = surf.GetPixel(i, j);
				if (c.a == 0f) continue;						// no solid pixel here
				
				
				// Create a new live pixel out of this color!
				PixParticle ej = surf.CreateLivePixel<PixParticle>(i, j, c);
				ej.velocity = (ej.position - center) * 7f;
			}
		}
		surf.FillEllipse(r, Color.clear);
	}
	

	#endregion
}
