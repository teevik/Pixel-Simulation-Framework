using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PixSurf;
using System.Collections.Generic;

public class StatsDisplay : MonoBehaviour {
	#region Public Properties
	public Text text;
	public PixelSurface pixelSurface;

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		if (text == null) text = GetComponent<Text>();
		if (pixelSurface == null) pixelSurface = GetComponent<PixelSurface>();
	}
	
	void Update() {
		if (text == null || pixelSurface == null) return;
		PixelSurface.Stats stats = pixelSurface.GetStats();
		text.text = string.Format(
			"Live Pixel Stacks: {0}\n" +
			"Total Live Pixels: {1}\n" + 
			"Recycle Pools: {2}\n" +
			"Recycled Pixels: {3}",
			stats.livePixelStackCount,
			stats.livePixelCount,
			stats.recyclePoolCount,
			stats.recycledPixelCount);
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods

	#endregion
}
