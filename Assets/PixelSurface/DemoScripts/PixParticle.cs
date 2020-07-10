using UnityEngine;
using PixSurf;

public class PixParticle : LivePixel {
	static public float gravity = 50f;
	
	public Vector2 velocity;
	
	bool ClearAt(PixelSurface surf, int x, int y) {
		Color c = surf.GetPixel(x, y);
		return c == Color.black || c.a == 0;
	}
	
	public override void Update(PixelSurface surf) {
		
		velocity.y -= gravity * Time.deltaTime;
		velocity *= 1f - 0.1f * Time.deltaTime;
		position += velocity * Time.deltaTime;
		
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
			} else if (velocity.y < 0) {
				// Falling down, and couldn't find a clear spot.  Move back up 1 space, and die.
				position.y++;
				Die();
			}
			velocity = Vector2.zero;
		}
	}
	
}