using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PixSurf;

public class DrawStuff : MonoBehaviour {
	#region Public Properties
	public enum Shape {
		Rect,
		Ellipse,
		Line,
		FloodFill
	}

	public PixelSurface overlaySurface;

	public Shape shape;
	public Color color = Color.blue;
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	PixelSurface mainSurface;
	bool drawing;
	Vector2 drawStartPos;
	Vector2 drawEndPos;

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		mainSurface = GetComponent<PixelSurface>();
		if (mainSurface == null || overlaySurface == null) {
			Debug.LogError("DrawStuff requires a PixelSurface component, and a separate overlaySurface");
		}
	}
	
	void Update() {
		if (Input.GetMouseButtonDown(0)) {
			drawing = mainSurface.PixelPosAtScreenPos(Input.mousePosition, out drawStartPos);
			drawEndPos = drawStartPos;
			if (drawing && shape == Shape.FloodFill) {
				mainSurface.FloodFill(drawStartPos, color);
				drawing = false;
			}
		} else if (drawing && Input.GetMouseButton(0)) {
			Vector2 p;
			if (mainSurface.PixelPosAtScreenPos(Input.mousePosition, out p) && p != drawEndPos) {
				drawEndPos = p;
				overlaySurface.Clear();
				DrawInto(overlaySurface);
			}
		} else if (drawing && Input.GetMouseButtonUp(0)) {
			overlaySurface.Clear();
			DrawInto(mainSurface);
			drawing = false;
		}

	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	public void SetShape(string shapeStr) {
		shape = (Shape)System.Enum.Parse(typeof(Shape), shapeStr, true);
	}

	public void SetColorFromToggle(UnityEngine.UI.Toggle toggle) {
		var graphic = toggle.GetComponentInChildren<UnityEngine.UI.Image>();
		color = graphic.color;
	}


	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods
	void DrawInto(PixelSurface surf) {
		if (shape == Shape.Line) {
			surf.DrawLine(drawStartPos, drawEndPos, color);
			return;
		}
		Rect r = new Rect(Mathf.Min(drawStartPos.x, drawEndPos.x),
		                Mathf.Min(drawStartPos.y, drawEndPos.y),
		                Mathf.Abs(drawEndPos.x - drawStartPos.x),
		                Mathf.Abs(drawEndPos.y - drawStartPos.y));
		if (shape == Shape.Ellipse) {
			surf.FillEllipse(r, color);
		} else {
			surf.FillRect(r, color);
		}
	}

	#endregion
}
