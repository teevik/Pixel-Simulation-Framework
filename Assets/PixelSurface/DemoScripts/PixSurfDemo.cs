using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PixSurf;

[RequireComponent(typeof(PixelSurface))]

public class PixSurfDemo : MonoBehaviour {
	#region Public Properties
	public enum Mode {
		DropSand,
		Explode,
		Fire,
		DropBomb,
		Draw
	}
	public Mode mode = Mode.DropSand;

	public Texture2D background;

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	PixelSurface surf;

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		surf = GetComponent<PixelSurface>();

		surf.DrawTexture(background, new Rect(0, 0, surf.totalWidth, surf.totalHeight),
		                 new Rect(0, 0, surf.totalWidth, surf.totalHeight));

		SetMode(Mode.DropSand);
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	public void SetMode(string mode) {
		SetMode((Mode)System.Enum.Parse(typeof(Mode), mode, true));
	}

	public void SetMode(Mode mode) {
		this.mode = mode;

		GetComponent<DropSand>().enabled = (mode == Mode.DropSand);
		GetComponent<GoBoom>().enabled = (mode == Mode.Explode);
		GetComponent<MakeFire>().enabled = (mode == Mode.Fire);
		GetComponent<DropBomb>().enabled = (mode == Mode.DropBomb);
		GetComponent<DrawStuff>().enabled = (mode == Mode.Draw);
	}
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods

	#endregion
}
