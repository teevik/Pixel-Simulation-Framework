using BootlegStuff;
using UnityEngine;

public class BetterSnowFall : MonoBehaviour {
    #region Public Properties
    public float probability = 10;	// probability of creating new snowflake on each frame

    #endregion
    //--------------------------------------------------------------------------------
    #region Private Properties
    BootlegPixelSurface surf;

    #endregion
    //--------------------------------------------------------------------------------
    #region MonoBehaviour Events
    void Start() {
        surf = GetComponent<BootlegPixelSurface>();
    }
	
    void Update() {
        if (Random.Range(0, 100) < probability) {
            int x = Random.Range(0, surf.totalWidth);
            surf.AddLivePixel(new SnowLivePixel(new Vector2Int(x, surf.totalHeight)));
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


public class SnowLivePixel : LivePixel {
    public SnowLivePixel(Vector2Int position) : base(position)
    {
        color = Color.white;
    }
    
    bool ClearAt(BootlegPixelSurface surf, Vector2Int position) {
        Color c = surf.GetStaticPixel(position);
        return c.a == 0;
    }
	
    public override void Update(BootlegPixelSurface surf) {
        float r = Random.Range(0.5f, 1f);
        color = new Color(r, r, r);
		
        int oldy = roundedPosition.y;
        position += Vector2.down * (Time.deltaTime * 10f);
        if (roundedPosition.y != oldy) position += Vector2.right * Random.Range(-1f,1f);
		
        if (!ClearAt(surf, roundedPosition)) {
            // We've hit something.  See if it's clear to the sides.
            bool clearLeft = ClearAt(surf, roundedPosition + Vector2Int.left);
            bool clearRight = ClearAt(surf, roundedPosition + Vector2Int.right);
            if (clearLeft && clearRight) {
                if (Random.Range(0f,1f) > 0.5f) position += Vector2.left;
                else position += Vector2.right;
            } else if (clearLeft) {
                position += Vector2.left;
            } else if (clearRight) {
                position += Vector2.right;
            } else {
                // Couldn't find a clear spot.  Move back up 1 space, and die.
                position += Vector2.up;
                Die();
            }
        }
    }
}