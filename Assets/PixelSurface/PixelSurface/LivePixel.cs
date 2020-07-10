/// <summary>
/// The LivePixel class represents a dynamic pixel, one that is moving around
/// on a PixelSurface.  It has a position and color, and as long as it is
/// owned by an active PixelSurface, its Update method will be called on each
/// frame.  You will generally want to subclass this, and override Start and
/// Update as needed to create the particular behavior your live pixels need.
/// </summary>
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace PixSurf {

	public class LivePixel {
		#region Public Properties
		// Position of this pixel on its surface
		public Vector2 position;

		// Color of this pixel
		public Color color;

		// Position x (column) and y (row) integer accessors
		public int x { get { return Mathf.RoundToInt(position.x); } }
		public int y { get { return Mathf.RoundToInt(position.y); } }

		// Whether this pixel is now dead.  Set this to true to have it
		// removed from the surface in the next turn.
		public bool dead = false;

		// Last frame on which this pixel was updated.  For internal use;
		// do not muck with this property.
		public int _lastUpdateFrame;

		#endregion
		//--------------------------------------------------------------------------------
		#region Virtual Methods (override these!)

		/// <summary>
		/// This is called when a LivePixel is created/recycled, and added
		/// to a PixelSurface.  Override this to initialize your pixel data.
		/// </summary>
		/// <param name="surf">PixelSurface to which this pixel is attached.</param>
		public virtual void Start(PixelSurface surf) {
		
		}

		/// <summary>
		/// Update is called once on each frame, as long as this LivePixel
		/// is alive (dead==false) and attached to an active PixelSurface.
		/// Here you can update .position or .color to animate your pixel.
		/// </summary>
		/// <param name="surf">PixelSurface to which this pixel is attached.</param>
		public virtual void Update(PixelSurface surf) {
		
		}

		#endregion
		//--------------------------------------------------------------------------------
		#region Public Methods

		/// <summary>
		/// Become a "dead" (i.e. static) pixel, baking our color into the pixel surface.
		/// </summary>
		public void Die() {
			dead = true;
		}

		/// <summary>
		/// Delete this live pixel WITHOUT baking its color into the pixel surface.
		/// </summary>
		public void DieClear() {
			color = Color.clear;
			dead = true;
		}
		#endregion
	}
}
