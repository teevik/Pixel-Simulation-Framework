using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public static class ColorExtensions {
	public static bool ApproxEqual(this Color c1, Color c2, float epsilon=0.05f) {
		return (c1[0]-c2[0]) * (c1[0]-c2[0])
			+ (c1[1]-c2[1]) * (c1[1]-c2[1])
				+ (c1[2]-c2[2]) * (c1[2]-c2[2])
				+ (c1[3]-c2[3]) * (c1[3]-c2[3]) < epsilon*epsilon;
	}
	
	public static bool IsGrayscale(this Color c) {
		return c.r == c.g && c.r == c.b;
	}
}
