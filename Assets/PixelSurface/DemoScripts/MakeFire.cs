using UnityEngine;
using PixSurf;

public class Fire : LivePixel {
	static public float bouyancy = 20f;
	static public float lifeTime = 1f;

	float startTime;
	
	public override void Start(PixelSurface surf) {
		startTime = Time.time;
		color = MakeFire.instance.fireStartColor;
	}
	
	public override void Update(PixelSurface surf) {
		// Adjust position and color
		position.y += bouyancy * Time.deltaTime;
		float t = (Time.time - startTime) / lifeTime;
		color = Color.Lerp(MakeFire.instance.fireStartColor, MakeFire.instance.fireEndColor, t);
		if (t > 1) DieClear();
		
		// Spawn a new fire pixel now and then
		if (Random.Range(0,100) < 10) {
			surf.CreateLivePixel<Fire>(x, y-1).startTime = startTime-0.3f;
		}
		
		// If we touch wood, create an ember (sometimes)
		Color c = surf.GetPixel(x, y, false);
		if (MakeFire.instance.IsFlammable(c) && Random.Range(0,100) < 2) {
			surf.CreateLivePixel<Ember>(x, y);
		}
		
		// If we touch snow, clear it
		if (c.IsGrayscale()) {
			surf.SetPixel(x, y, Color.clear);
		}
	}
	
	public static void CreateFlameAt(PixelSurface surf, Vector2 position) {
		int x = Mathf.RoundToInt(position.x);
		int y = Mathf.RoundToInt(position.y);
		for (int i=-1; i<=1; i++) {
			for (int j=-1; j<=1; j++) {
				if ((i==j || -i==j) && i!=0) continue;
				surf.CreateLivePixel<Fire>(x + i, y + j);
			}
		}
	}
	
}

public class Ember : LivePixel {
	float nextFireTime;
	float dieTime;
	
	public override void Start (PixelSurface surf) {
		dieTime = Time.time + Random.Range(1f,10f);
		color = new Color(1f, Random.Range(0f, 0.5f), 0);
		surf.SetPixel(x, y, Color.black);
		nextFireTime = Time.time + Random.Range(0, 0.4f);
	}
	
	public override void Update(PixelSurface surf) {
		if (Time.time > nextFireTime) {
			Fire.CreateFlameAt(surf, position + Random.insideUnitCircle * 2f);
			nextFireTime += 0.4f;
		}
		if (Time.time > dieTime) {
			surf.SetPixel(x, y, Color.clear);
			DieClear();
		}
	}
}

public class MakeFire : MonoBehaviour {
	#region Public Properties

	public Color fireStartColor = new Color(1f, 0.7f, 0.3f, 1f);
	public Color fireEndColor = new Color(0.7f, 0.7f, 0, 0.0f);
	public Color[] flammableColors = new Color[] { new Color(101f/255, 69f/255, 27f/255) };

	// The pixels need a way to get our color settings, so they can use:
	public static MakeFire instance {
		get { return _instance; }
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	PixelSurface surf;
	static MakeFire _instance;
	
	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Awake() {
		_instance = this;
	}

	void Start() {
		surf = GetComponent<PixelSurface>();
	}
	
	void Update() {
		if (!Input.GetMouseButton(0)) return;
		Vector2 pixelPos;
		if (surf.PixelPosAtScreenPos(Input.mousePosition, out pixelPos)) {
			Fire.CreateFlameAt(surf, pixelPos + Random.insideUnitCircle * 5f);
		}

	}
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	public bool IsFlammable(Color c) {
		if (c.a < 0.1f) return false;	// quickly bail out on clear colors
		foreach (Color flammableColor in flammableColors) {
			if (c.ApproxEqual(flammableColor)) return true;
		}
		return false;
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods
	
	#endregion
}
