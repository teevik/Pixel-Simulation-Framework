using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PixSurf;

public class DropSand : MonoBehaviour {
	#region Public Properties
	public Color sandColor = new Color(0.9f, 0.6f, 0.3f);

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
		if (!Input.GetMouseButton(0)) return;
		Vector2 pixelPos;
		if (surf.PixelPosAtScreenPos(Input.mousePosition, out pixelPos)) {
			surf.CreateLivePixel<PixParticle>(Mathf.RoundToInt(pixelPos.x), Mathf.RoundToInt(pixelPos.y),
			                                  sandColor).velocity = 20f * Random.insideUnitCircle;
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
