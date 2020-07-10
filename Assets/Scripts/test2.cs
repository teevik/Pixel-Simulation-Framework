using PixSurf;
using UnityEngine;

[RequireComponent(typeof(PixelSurface))]
public class test2 : MonoBehaviour
{
  public Texture2D background;

  void Start()
  {
    var surface = GetComponent<PixelSurface>();

    surface.DrawTexture(
      background,
      new Rect(0, 0, surface.totalWidth, surface.totalHeight),
      new Rect(0, 0, surface.totalWidth, surface.totalHeight)
    );
  }
}