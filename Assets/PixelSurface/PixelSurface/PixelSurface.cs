/// <summary>
/// PixSurf.PixelSurface is the MonoBehaviour that does implements a
/// pixel surface.  It does all the work of creating the tiles, updating
/// their textures, managing live pixels, and so on.
/// </summary>
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;


namespace PixSurf
{

  [ExecuteInEditMode]
  public class PixelSurface : MonoBehaviour
  {

    /// <summary>
    /// Stats is a little struct that contains statistics about a
    /// PixelSurface, for analysis or debugging purposes.  We may
    /// add more fields here in future versions of the library.
    /// </summary>
    public struct Stats
    {
      // How many "stacks" of live pixels do we have?  Or to put 
      // it another way: how many pixel locations have any live
      // pixels on them?
      public int livePixelStackCount;

      // How many live pixels do we have, total?
      public int livePixelCount;

      // How many live-pixel recycling pools do we have?  (Should
      // be one for each type of live pixel we've recycled.)
      public int recyclePoolCount;

      // How many recycled pixels do we have, total?
      public int recycledPixelCount;
    }

    /// <summary>
    /// Helper class used internally to keep track of the set of live
    /// pixels at a particular location on the surface.
    /// </summary>
    class LivePixelStack
    {
      public List<LivePixel> livePixels = new List<LivePixel>();
      public int x;
      public int y;
      public Color backgroundColor;
      public bool needsDraw;

      public Color topColor
      {
        get { return livePixels.Count > 0 ? livePixels[livePixels.Count - 1].color : backgroundColor; }
      }
    }

    // Callback type used with IterateOverLine
    public delegate bool PositionCallback(int x, int y);

    #region Public Properties
    [Tooltip("Width of each tile, in pixels")]
    public int tileWidth = 64;

    [Tooltip("Height of each tile, in pixels")]
    public int tileHeight = 64;

    [Tooltip("Total surface width, in pixels")]
    public int totalWidth = 384;

    [Tooltip("Total surface height, in pixels")]
    public int totalHeight = 256;

    [Tooltip("Scaling factor between pixels and world units")]
    public float pixelsPerUnit = 100;

    [Tooltip("Shader to use for rendering; if null, Sprites/Default is used")]
    public Shader shader;

    [Tooltip("Whether to initialize the surface to a test pattern; if false, it's initialized clear")]
    public bool startWithTestPattern = true;

    public Renderer tilePrefab;

    #endregion
    //--------------------------------------------------------------------------------
    #region Private Properties
    GameObject[,] tileObj;    // tile GameObjects by column, row
    Texture2D[,] tileTex;   // tile textures by column, row
    bool[,] tileNeedsApply;   // whether we need to call Apply for each column, row
    bool[,] tileNeedsLivePixelApply;   // whether we need to call Apply for each column, row
    bool anyNeedsApply;     // whether ANY tile needs to call Apply
    Dictionary<int, LivePixelStack> stackMap; // live pixels for each position index

    Stack<LivePixelStack> lpsRecyclePool; // recycling pool for LivePixelStacks
    Dictionary<System.Type, Stack<LivePixel>> livePixelPool;  // recycling pools for LivePixels

    // The following three properties are simply work-arounds for a Unity bug,
    // where CreatePrimitive fails (in built apps) in a project that doesn't 
    // otherwise reference these three classes.
    // (http://forum.unity3d.com/threads/errors-and-crash-inside-createprimitive-on-ios.414604/)
    private MeshCollider _workaround_1;
    private MeshFilter _workaround_2;
    private MeshRenderer _workaround_3;

    // Copies of our public property values so that we can tell when these
    // have changed, and recreate our tiles accordingly.  (Note that this
    // will of course reset the pixel surface data.)
    int prevTileWidth;
    int prevTileHeight;
    int prevTotalWidth;
    int prevTotalHeight;
    float prevPixelsPerUnit;
    bool prevStartWithTestPattern;

    #endregion
    //--------------------------------------------------------------------------------
    #region MonoBehaviour Events
    void Awake()
    {
      // If no shader is specified, use the standard sprite shader.
      if (shader == null) shader = Shader.Find("Sprites/Default");

      // Reset the pixel surface (creating all needed data structures).
      Reset();
    }

    void Update()
    {

      ResetIfPropsChanged();

      if (Application.isPlaying)
      {
        // Update any live pixels attached to this surface.
        UpdateLivePixels();
      }
    }

    void LateUpdate()
    {
      // Update the screen with the latest changes to live pixels
      // and/or tiles of static pixels.
      if (stackMap == null) return;
      List<int> condemned = null;
      foreach (LivePixelStack stack in stackMap.Values)
      {
        if (stack.needsDraw)
        {
          DrawStack(stack);
          stack.needsDraw = false;
        }
        if (stack.livePixels.Count == 0)
        {
          // This stack is empty.  Let's remove it.  (OFI: recycle it.)
          if (condemned == null) condemned = new List<int>();
          condemned.Add(KeyForXY(stack.x, stack.y));
        }
      }
      if (condemned != null)
      {
        foreach (int key in condemned)
        {
          lpsRecyclePool.Push(stackMap[key]);
          stackMap.Remove(key);
        }
      }

      if (!anyNeedsApply) return;
      for (int col = 0; col < tileTex.GetLength(0); col++)
      {
        for (int row = 0; row < tileTex.GetLength(1); row++)
        {
          if (tileNeedsApply[col, row])
          {
            tileTex[col, row].Apply();
            tileNeedsApply[col, row] = false;
            if (tileObj[col, row].TryGetComponent<PhysicsTile>(out var physicsTile)) physicsTile.Regenerate(col,row);
            Debug.Log("aa");
          } else if (tileNeedsLivePixelApply[col, row])
          {
            tileTex[col, row].Apply();
            tileNeedsApply[col, row] = false;
          }
        }
      }
      anyNeedsApply = false;
    }

    #endregion
    //--------------------------------------------------------------------------------
    #region Public Methods

    /// <summary>
    /// Reset this pixel surface, clearing all pixel data.
    /// </summary>
    public void Reset()
    {
      stackMap = new Dictionary<int, LivePixelStack>();
      lpsRecyclePool = new Stack<LivePixelStack>();
      livePixelPool = new Dictionary<System.Type, Stack<LivePixel>>();
      ClearTiles();
      CreateTiles();
    }

    /// <summary>
    /// Get the current statistics for this pixel surface,
    /// for debugging or analysis (or just plain curiosity).
    /// </summary>
    /// <returns>The stats.</returns>
    public Stats GetStats()
    {
      Stats stats = new Stats();
      stats.livePixelStackCount = stackMap.Count;
      foreach (LivePixelStack st in stackMap.Values)
      {
        stats.livePixelCount += st.livePixels.Count;
      }
      stats.recyclePoolCount = livePixelPool.Count;
      foreach (Stack<LivePixel> st in livePixelPool.Values)
      {
        stats.recycledPixelCount += st.Count;
      }
      return stats;
    }

    /// <summary>
    /// Get the texture for the given tile.  This is not something you
    /// should need very often; it's mainly for debugging, or for very
    /// special cases like integrating with other code that operates
    /// on Texture2Ds.
    /// </summary>
    /// <param name="column">tile column</param>
    /// <param name="row">tile row</param>
    /// <returns>texture of the given tile</returns>
    public Texture2D GetTileTexture(int column, int row)
    {
      return tileTex[column, row];
    }

    /// <summary>
    /// Fill a rectangular region with a color.  Note that the coordinates
    /// in the rectangle are truncated to the next lower integer.
    /// </summary>
    /// <param name="rect">Rectangle to fill</param>
    /// <param name="color">Fill color.</param>
    public void FillRect(Rect rect, Color color)
    {
      int y0 = Mathf.FloorToInt(rect.yMin);
      if (y0 < 0) y0 = 0; else if (y0 >= totalHeight) y0 = totalHeight - 1;
      int y1 = Mathf.FloorToInt(rect.yMax);
      if (y1 < 0) y1 = 0; else if (y1 >= totalHeight) y1 = totalHeight - 1;

      int x0 = Mathf.FloorToInt(rect.xMin);
      if (x0 < 0) x0 = 0; else if (x0 >= totalWidth) x0 = totalWidth - 1;
      int x1 = Mathf.FloorToInt(rect.xMax);
      if (x1 < 0) x1 = 0; else if (x1 >= totalWidth) y1 = totalWidth - 1;

      for (int y = y0; y <= y1; y++)
      {
        for (int x = x0; x <= x1; x++)
        {
          if (x % tileWidth == 0)
          {
            // We're at the start of a tile... See if the whole tile
            // will be the same color.
            int tileCol = x / tileWidth;
            int tileRow = y / tileHeight;
            if (IsTileWithinRect(tileCol, tileRow, rect))
            {
              // Since it is, we can fill the whole tile (just once)
              // for more efficient texture usage, and then skip it
              // on all subsequent iterations.
              if (y % tileHeight == 0) FillTile(tileCol, tileRow, color);
              x += tileWidth - 1;
              continue;
            }
          }
          SetPixel(x, y, color);
        }
      }
    }

    /// <summary>
    /// Fill an elliptical region with a color.  Note that the coordinates
    /// in the bounds rectangle are truncated to the next lower integer.
    /// </summary>
    /// <param name="rect">Bounds rect within which to inscribe an axis-oriented ellipse.</param>
    /// <param name="color">Fill color.</param>
    public void FillEllipse(Rect rect, Color color)
    {
      int y0 = Mathf.FloorToInt(rect.yMin);
      if (y0 < 0) y0 = 0; else if (y0 >= totalHeight) y0 = totalHeight - 1;
      int y1 = Mathf.FloorToInt(rect.yMax);
      if (y1 < 0) y1 = 0; else if (y1 >= totalHeight) y1 = totalHeight - 1;

      float r = rect.height / 2;
      float rsqr = r * r;
      for (int y = y0; y <= y1; y++)
      {
        float cy = rect.center.y - y;
        float cx = Mathf.Sqrt(rsqr - cy * cy) * rect.width / rect.height;
        int x0 = Mathf.RoundToInt(rect.center.x - cx);
        if (x0 < 0) x0 = 0; else if (x0 >= totalWidth) x0 = totalWidth - 1;
        int x1 = Mathf.RoundToInt(rect.center.x + cx);
        if (x1 < 0) x1 = 0; else if (x1 >= totalWidth) y1 = totalWidth - 1;
        for (int x = x0; x <= x1; x++)
        {
          if (x % tileWidth == 0)
          {
            // We're at the start of a tile... See if the whole tile
            // will be the same color.
            int tileCol = x / tileWidth;
            int tileRow = y / tileHeight;
            if (IsTileWithinEllipse(tileCol, tileRow, rect))
            {
              // Since it is, we can fill the whole tile (just once)
              // for more efficient texture usage, and then skip it
              // on all subsequent iterations.
              if (y % tileHeight == 0) FillTile(tileCol, tileRow, color);
              x += tileWidth - 1;
              continue;
            }
          }
          SetPixel(x, y, color);
        }
      }
    }

    /// <summary>
    /// Fill a circular area with a solid color.  This is just a shortcut for FillEllipse.
    /// </summary>
    /// <param name="x">Circle center x position.</param>
    /// <param name="y">Circle center y position.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="color">Fill color.</param>
    public void FillCircle(float x, float y, float radius, Color color)
    {
      FillEllipse(new Rect(x - radius, y - radius, radius * 2, radius * 2), color);
    }

    /// <summary>
    /// Get the color of the specified pixel.  If out of bounds,
    /// returns Color.clear.  Normally this method ignores any live
    /// pixels at the specified position, but if includeLive=true,
    /// then you will instead get the color of the topmost live pixel.
    /// </summary>
    /// <returns>Pixel color at the given x,y.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="includeLive">If set to <c>true</c> include live pixels.</param>
    public Color GetPixel(int x, int y, bool includeLive = false)
    {
      if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return Color.clear;
      LivePixelStack stack = null;
      if (stackMap.TryGetValue(KeyForXY(x, y), out stack))
      {
        return includeLive ? stack.topColor : stack.backgroundColor;
      }

      int col = x / tileWidth, row = y / tileHeight;

      Material mat = tileObj[col, row].GetComponent<Renderer>().sharedMaterial; // should probably cache these
      if (mat.mainTexture == null) return mat.color;
      return tileTex[col, row].GetPixel(x % tileWidth, y % tileHeight);
    }

    /// <summary>
    /// Get the color of the specified pixel.  If out of bounds,
    /// returns Color.clear.  Normally this method ignores any live
    /// pixels at the specified position, but if includeLive=true,
    /// then you will instead get the color of the topmost live pixel.
    /// </summary>
    /// <returns>Pixel color at the given pixel position (rounded to nearest integers).</returns>
    /// <param name="pixelPos">Pixel position.</param>
    /// <param name="includeLive">If set to <c>true</c> include live.</param>
    public Color GetPixel(Vector2 pixelPos, bool includeLive = false)
    {
      return GetPixel(Mathf.RoundToInt(pixelPos.x), Mathf.RoundToInt(pixelPos.y), includeLive);
    }

    /// <summary>
    /// Set the static pixel color at the given position.  Note that if
    /// there are any live pixels at this position, those are ignored in
    /// this operation; you're only setting the static color here.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="color">Color to set.</param>
    public void SetPixel(int x, int y, Color color)
    {
      if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return;
      LivePixelStack stack = null;
      if (stackMap.TryGetValue(KeyForXY(x, y), out stack))
      {
        // We have a stack at this location, so just update the background
        // color (which will be revealed later when the stack is empty).
        stack.backgroundColor = color;
        return;
      }

      // No stack, so update the actual texture.
      SetPixelIgnoringStacks(x, y, color, false);
    }

    public void SetPixel(Vector2 pixelPos, Color color)
    {
      SetPixel(Mathf.RoundToInt(pixelPos.x), Mathf.RoundToInt(pixelPos.y), color);
    }

    /// <summary>
    /// Create (or recycle) a LivePixel at the given position.  The new
    /// pixel is attached to this pixel surface at the given position,
    /// and its Start method is called so it can initialize itself.
    /// </summary>
    /// <returns>The newly created (or recycled) live pixel.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="color">Optional color to set.</param>
    /// <typeparam name="T">Specific type of LivePixel to create.</typeparam>
    public T CreateLivePixel<T>(int x, int y, Color color = default(Color)) where T : LivePixel, new()
    {
      Stack<LivePixel> pool = null;
      livePixelPool.TryGetValue(typeof(T), out pool);

      LivePixel noob;
      if (pool == null || pool.Count == 0)
      {
        // Recycling pool is empty; create a new LivePixel from scratch.
        noob = new T();
      }
      else
      {
        // Recycle an old live pixel (hooray!).
        noob = pool.Pop();
        noob.dead = false;
      }

      noob.position = new Vector2(x, y);
      noob.color = color;
      AddToStack(noob, x, y);
      noob.Start(this);
      return (T)noob;
    }

    /// <summary>
    /// Clear all live pixels at the given location.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public void ClearLivePixels(int x, int y)
    {
      if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return;
      LivePixelStack stack = null;
      if (stackMap.TryGetValue(KeyForXY(x, y), out stack))
      {
        if (stack.livePixels.Count > 0)
        {
          foreach (LivePixel lp in stack.livePixels) Recycle(lp);
          stack.livePixels.Clear();
          stack.needsDraw = true;
        }
      }
    }

    /// <summary>
    /// Clear the whole surface (to Color.clear).  Also clears
    /// all live pixels.
    /// </summary>
    public void Clear()
    {
      Clear(Color.clear);
    }

    /// <summary>
    /// Clear the entire surface to the given color.  Also clears
    /// all live pixels.
    /// </summary>
    /// <param name="color">Color to fill the surface with.</param>
    public void Clear(Color color)
    {
      foreach (GameObject gob in tileObj)
      {
        Material mat = gob.GetComponent<Renderer>().sharedMaterial;
        mat.mainTexture = null;
        mat.color = color;
      }
      stackMap.Clear();
      livePixelPool.Clear();
    }

    /// <summary>
    /// Draw a texture into the surface.  You can draw any rectangular
    /// portion of the source texture into any rectangular area of the
    /// PixelSurface.
    /// </summary>
    /// <param name="src">Source texture to draw.</param>
    /// <param name="destRect">Where to draw the texture in this surface.</param>
    /// <param name="srcRect">What part of the source texture to draw.</param>
    public void DrawTexture(Texture2D src, Rect destRect, Rect srcRect)
    {
      int x0 = Mathf.FloorToInt(destRect.xMin);
      int y0 = Mathf.FloorToInt(destRect.yMin);
      int x1 = Mathf.FloorToInt(destRect.xMax);
      int y1 = Mathf.FloorToInt(destRect.yMax);

      // OFI: this could probably be made a lot more efficient with Get/SetPixels...
      float srcy = srcRect.yMin;
      float dsrcy = (float)srcRect.height / destRect.height;
      for (int y = y0; y <= y1; y++)
      {
        float srcx = srcRect.xMin;
        float dsrcx = (float)srcRect.width / destRect.width;
        for (int x = x0; x <= x1; x++)
        {
          Color c = src.GetPixel((int)srcx, (int)srcy);
          SetPixel(x, y, c);
          srcx += dsrcx;
        }
        srcy += dsrcy;
      }
    }

    /// <summary>
    /// Draw a texture into the surface.  This version draws the entire
    /// source texture into any rectangular area of the PixelSurface.
    /// </summary>
    /// <param name="src">Source texture to draw.</param>
    /// <param name="destRect">Where to draw the texture in this surface.</param>
    public void DrawTexture(Texture2D src, Rect destRect)
    {
      DrawTexture(src, destRect, new Rect(0, 0, src.width, src.height));
    }

    /// <summary>
    /// Draw a texture into the surface.  This version fills the entire
    /// PixelSurface with the entire given texture.
    /// </summary>
    /// <param name="src">Source texture to draw.</param>
    public void DrawTexture(Texture2D src)
    {
      DrawTexture(src, new Rect(0, 0, totalWidth, totalHeight));
    }

    /// <summary>
    /// Draw a 1-pixel-thick line between the given points in the surface.
    /// </summary>
    /// <param name="p1">Pixel coordinates of one end of the line.</param>
    /// <param name="p2">Pixel coordinates of the other end of the line.</param>
    /// <param name="color">Color to draw.</param>
    public void DrawLine(Vector2 p1, Vector2 p2, Color color)
    {
      DrawLine(Mathf.RoundToInt(p1.x),
               Mathf.RoundToInt(p1.y),
               Mathf.RoundToInt(p2.x),
               Mathf.RoundToInt(p2.y),
               color);
    }

    /// <summary>
    /// Draw a 1-pixel-thick line between the given points in the surface.
    /// </summary>
    /// <param name="x1">The first x value.</param>
    /// <param name="y1">The first y value.</param>
    /// <param name="x2">The second x value.</param>
    /// <param name="y2">The second y value.</param>
    /// <param name="color">Color to draw.</param>
    public void DrawLine(int x1, int y1, int x2, int y2, Color color)
    {
      // Bresenham's line algorithm
      int dx = x2 - x1;
      int dy = y2 - y1;
      int absDx = dx < 0 ? -dx : dx;
      int absDy = dy < 0 ? -dy : dy;

      bool steep = (absDy > absDx);
      if (steep)
      {
        Swap(ref x1, ref y1);
        Swap(ref x2, ref y2);
      }

      if (x1 > x2)
      {
        Swap(ref x1, ref x2);
        Swap(ref y1, ref y2);
      }

      dx = x2 - x1;
      dy = y2 - y1;
      // not needed anymore: int absDx = dx < 0 ? -dx : dx;
      absDy = dy < 0 ? -dy : dy;

      int error = dx / 2;
      int ystep = (y1 < y2) ? 1 : -1;
      int y = y1;

      int maxX = (int)x2;

      for (int x = (int)x1; x < maxX; x++)
      {
        if (steep) SetPixel(y, x, color); else SetPixel(x, y, color);

        error -= absDy;
        if (error < 0)
        {
          y += ystep;
          error += dx;
        }
      }
    }

    /// <summary>
    /// Iterate over all the positions from x1,y1 to x2,y2, invoking the callback
    /// on each position, until that callback returns false (or we reach x2,y2).
    /// </summary>
    /// <param name="callback">function called on each position: return false to terminate</param>
    public void IterateOverLine(int x1, int y1, int x2, int y2, PositionCallback callback)
    {
      // Bresenham's line algorithm
      int dx = x2 - x1;
      int dy = y2 - y1;
      int absDx = dx < 0 ? -dx : dx;
      int absDy = dy < 0 ? -dy : dy;

      bool steep = (absDy > absDx);
      if (steep)
      {
        Swap(ref x1, ref y1);
        Swap(ref x2, ref y2);
        dx = x2 - x1;
        dy = y2 - y1;
        absDx = dx < 0 ? -dx : dx;
        absDy = dy < 0 ? -dy : dy;
      }

      int error = absDx / 2;
      int ystep = (y1 < y2) ? 1 : -1;
      int y = y1;
      int xstep = (dx > 0 ? 1 : -1);

      for (int x = (int)x1; x != x2; x += xstep)
      {
        if (steep)
        {
          if (!callback(y, x)) return;
        }
        else
        {
          if (!callback(x, y)) return;
        }

        error -= absDy;
        if (error < 0)
        {
          y += ystep;
          error += absDx;
        }
      }
    }

    /// <summary>
    /// Do a "paint bucket" type fill at the given position.  That is,
    /// recursively find all neighboring pixels which are the same color
    /// as the current color at the given position, and change them to
    /// the new color.
    /// </summary>
    /// <param name="position">Position at which to fill.</param>
    /// <param name="color">Color to fill with.</param>
    public void FloodFill(Vector2 position, Color color)
    {
      FloodFill(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), color);
    }

    /// <summary>
    /// Do a "paint bucket" type fill at the given position.  That is,
    /// recursively find all neighboring pixels which are the same color
    /// as the current color at the given position, and change them to
    /// the new color.
    /// </summary>
    /// <param name="x">X coordinate at which to fill.</param>
    /// <param name="y">Y coordinate at which to fill.</param>
    /// <param name="color">Color to fill with.</param>
    public void FloodFill(int x, int y, Color color)
    {
      Color colorToFill = GetPixel(x, y);
      if (colorToFill == color) return;
      Stack<int> toDo = new Stack<int>();
      toDo.Push(KeyForXY(x, y));
      while (toDo.Count > 0)
      {
        int key = toDo.Pop();
        XYForKey(key, out x, out y);
        SetPixel(x, y, color);
        if (x > 0 && Floods(GetPixel(x - 1, y), colorToFill)) toDo.Push(KeyForXY(x - 1, y));
        if (y > 0 && Floods(GetPixel(x, y - 1), colorToFill)) toDo.Push(KeyForXY(x, y - 1));
        if (x + 1 < totalWidth && Floods(GetPixel(x + 1, y), colorToFill)) toDo.Push(KeyForXY(x + 1, y));
        if (y + 1 < totalHeight && Floods(GetPixel(x, y + 1), colorToFill)) toDo.Push(KeyForXY(x, y + 1));
      }
    }


    /// <summary>
    /// Get the pixel position at a given screen position (relative to
    /// the given camera, or Camera.main if none is specified).  Handy
    /// for finding the pixel position clicked, for example.
    /// </summary>
    /// <returns><c>true</c>, if screen position is in bounds; <c>false</c> otherwise.</returns>
    /// <param name="screenPos">Screen position of interest.</param>
    /// <param name="pixelPos">Receives the corresponding pixel position.</param>
    /// <param name="camera">Optional camera of interest (uses Camera.main by default).</param>
    public bool PixelPosAtScreenPos(Vector3 screenPos, out Vector2 pixelPos, Camera camera = null)
    {
      if (camera == null) camera = Camera.main;
      Ray ray = camera.ScreenPointToRay(screenPos);
      RaycastHit hit;
      if (Physics.Raycast(ray, out hit))
      {
        Vector3 localPt = transform.InverseTransformPoint(hit.point);
        pixelPos = localPt * pixelsPerUnit;
        return true;
      }
      pixelPos = Vector2.zero;
      return false;
    }

    /// <summary>
    /// Reduce our current RAM usage as much as we can by freeing
    /// recycled objects, etc.
    /// </summary>
    public void ReduceRAM()
    {
      lpsRecyclePool.Clear();
      livePixelPool.Clear();
    }

    /// <summary>
    /// Get the position in the world of a given pixel position.
    /// </summary>
    /// <returns>World coordinates of given pixel position.</returns>
    /// <param name="pixelPos">Pixel position of interest.</param>
    public Vector3 WorldPosAtPixelPos(Vector2 pixelPos)
    {
      return transform.TransformPoint(new Vector3(pixelPos.x / pixelsPerUnit,
                                                           pixelPos.y / pixelsPerUnit,
                                                           0));
    }

    /// <summary>
    /// Get the pixel position at a given world position (ignoring the
    /// local Z direction).
    /// </summary>
    /// <returns>Pixel position corresponding to the given world position.</returns>
    /// <param name="worldPos">World position of interest.</param>
    public Vector2 PixelPosAtWorldPos(Vector3 worldPos)
    {
      return transform.InverseTransformPoint(worldPos) * pixelsPerUnit;
    }

    /// <summary>
    /// Return whether the given pixel position is within bounds of
    /// this pixel surface.
    /// </summary>
    /// <returns><c>true</c>, if within bounds, <c>false</c> otherwise.</returns>
    /// <param name="pixelPos">Pixel position of interest.</param>
    public bool InBounds(Vector2 pixelPos)
    {
      return pixelPos.x >= 0 && pixelPos.x < totalWidth
        && pixelPos.y >= 0 && pixelPos.y < totalHeight;
    }

    #endregion
    //--------------------------------------------------------------------------------
    #region Private Methods

    void ClearTiles()
    {
      // Destroy all the tiles we know about.
      if (tileObj != null)
      {
        foreach (GameObject gob in tileObj)
        {
          GameObject.DestroyImmediate(gob);
        }
        tileObj = null;
        tileTex = null;
      }

      // In addition to that, let's also search our immediate children
      // for things that look like tiles, and destroy them.  This is 
      // needed, for example, in cases where there were some tiles
      // created in the editor, but not part of our tileObj array.
      for (int i = transform.childCount - 1; i >= 0; i--)
      {
        Transform t = transform.GetChild(i);
        if (t.name.StartsWith("Tile ") && t.GetComponent<MeshFilter>() != null)
        {
          GameObject.DestroyImmediate(t.gameObject);
        }
      }
    }

    void CreateTiles()
    {
      if (shader == null) shader = Shader.Find("Sprites/Default");
      int tileCols = Mathf.CeilToInt(totalWidth / tileWidth);
      int tileRows = Mathf.CeilToInt(totalHeight / tileHeight);
      tileObj = new GameObject[tileCols, tileRows];
      tileTex = new Texture2D[tileCols, tileRows];
      tileNeedsApply = new bool[tileCols, tileRows];
      tileNeedsLivePixelApply = new bool[tileCols, tileRows];

      float worldTileWidth = tileWidth / pixelsPerUnit;
      float worldTileHeight = tileHeight / pixelsPerUnit;
      for (int row = 0; row < tileRows; row++)
      {
        for (int col = 0; col < tileCols; col++)
        {
          var quad = Instantiate(tilePrefab);
          quad.name = "Tile " + col + ", " + row;
          quad.transform.localScale = new Vector3(worldTileWidth, worldTileHeight, 1);
          quad.transform.position = new Vector3(
            worldTileWidth * (col + 0.5f),
            worldTileHeight * (row + 0.5f),
            0);
          quad.transform.SetParent(transform, false);

          Material mat = new Material(shader);
          quad.sharedMaterial = mat;
          Texture2D tex = new Texture2D(tileWidth, tileHeight);
          tex.wrapMode = TextureWrapMode.Clamp;
          tex.filterMode = FilterMode.Point;
          mat.mainTexture = tex;
          tileTex[col, row] = tex;

          if (startWithTestPattern)
          {
            for (int y = 0; y < tex.height; y++)
            {
              for (int x = 0; x < tex.width; x++)
              {
                Color color = ((x & y) != 0 ? Color.white : Color.gray);
                tex.SetPixel(x, y, color);
              }
            }
            tex.Apply();
          }
          else
          {
            mat.mainTexture = null;
            mat.color = Color.clear;
          }

          tileObj[col, row] = quad.gameObject;
        }
      }

      prevTileWidth = tileWidth;
      prevTileHeight = tileHeight;
      prevTotalWidth = totalWidth;
      prevTotalHeight = totalHeight;
      prevPixelsPerUnit = pixelsPerUnit;
      prevStartWithTestPattern = startWithTestPattern;
    }

    bool Floods(Color curColor, Color colorToMatch)
    {
      if (curColor == colorToMatch) return true;    // exact match
      if (colorToMatch.a == 0 && curColor.a == 0) return true;  // both clear
      return false;
    }

    /// <summary>
    /// Fill the given tile with a solid color.  This removes the texture
    /// from the material, and uses just the color.
    /// </summary>
    /// <param name="col">Tile column.</param>
    /// <param name="row">Tile row.</param>
    /// <param name="color">Fill color.</param>
    void FillTile(int col, int row, Color color)
    {
      Debug.Log("Setting tile " + col + ", " + row + " to " + color);
      Material mat = tileObj[col, row].GetComponent<Renderer>().sharedMaterial;
      mat.mainTexture = null;
      mat.color = color;

      // We've updated the texture, but we need to also update the background
      // color of any live pixel stacks we may have.
      if (stackMap.Count > 0)
      {
        LivePixelStack stack = null;
        int y0 = row * tileHeight;
        int x0 = col * tileWidth;
        int y1 = y0 + tileHeight;
        int x1 = x0 + tileWidth;
        for (int y = y0; y < y1; y++)
        {
          for (int x = x0; x < x1; x++)
          {
            if (stackMap.TryGetValue(KeyForXY(x, y), out stack))
            {
              stack.backgroundColor = color;
            }
          }
        }
      }
    }

    /// <summary>
    /// Figure out whether the given tile is completely contained
    /// in an ellipse inscribed in the given rectangle.
    /// </summary>
    /// <param name="col">tile column</param>
    /// <param name="row">tile row</param>
    /// <param name="ellipse">ellipse bounds</param>
    /// <returns>true if tile is within ellipse; false otherwise</returns>
    bool IsTileWithinEllipse(int col, int row, Rect ellipse)
    {
      int x = col * tileWidth;
      int y = row * tileHeight;
      return IsPointWithinEllipse(x, y, ellipse)
        && IsPointWithinEllipse(x + tileWidth, y, ellipse)
        && IsPointWithinEllipse(x + tileWidth, y + tileHeight, ellipse)
        && IsPointWithinEllipse(x, y + tileHeight, ellipse);
    }

    /// <summary>
    /// Figure out whether the given tile is completely contained
    /// in the given rectangle.
    /// </summary>
    /// <param name="col">tile column</param>
    /// <param name="row">tile row</param>
    /// <param name="rect">rectangle bounds</param>
    /// <returns>true if tile is within rect; false otherwise</returns>
    bool IsTileWithinRect(int col, int row, Rect rect)
    {
      int x = col * tileWidth;
      int y = row * tileHeight;
      if (x < rect.xMin || y < rect.yMin) return false;
      if (x + tileWidth > rect.xMax || y + tileHeight > rect.yMax) return false;
      return true;
    }


    /// <summary>
    /// Return whether the given point is within the axis-aligned
    /// ellipse inscribed in the given rectangle.
    /// </summary>
    /// <param name="x">point of interest, x coordinate</param>
    /// <param name="y">point of interest, y coordinate</param>
    /// <param name="ellipse">ellipse bounds</param>
    /// <returns>true if point is within the ellipse; false otherwise</returns>
    static bool IsPointWithinEllipse(float x, float y, Rect ellipse)
    {
      float halfWidth = ellipse.width / 2;
      float dx = x - ellipse.center.x;
      float term1 = (dx * dx) / (halfWidth * halfWidth);

      float halfHeight = ellipse.height / 2;
      float dy = y - ellipse.center.y;
      float term2 = (dy * dy) / (halfHeight * halfHeight);

      return term1 + term2 <= 1;
    }

    void NeedApplyAtTile(int col, int row, bool isFromLivePixel)
    {
      if (isFromLivePixel)
      {
        tileNeedsLivePixelApply[col, row] = true;
      }
      else
      {
        tileNeedsApply[col, row] = true;
      }
      anyNeedsApply = true;
    }

    void NeedApplyAtXY(int x, int y)
    {
      tileNeedsApply[x / tileWidth, y / tileHeight] = true;
      anyNeedsApply = true;
    }

    Texture2D TexAtXY(int x, int y)
    {
      return tileTex[x / tileWidth, y / tileHeight];
    }

    void Recycle(LivePixel p)
    {
      Stack<LivePixel> pool = null;
      if (!livePixelPool.TryGetValue(p.GetType(), out pool))
      {
        pool = new Stack<LivePixel>();
        livePixelPool[p.GetType()] = pool;
      }
      pool.Push(p);
    }

    void SetPixelIgnoringStacks(int x, int y, Color color, bool isFromLivePixel)
    {
      if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return;
      int col = x / tileWidth, row = y / tileHeight;

      // Make sure we are actually using the texture for the given tile!
      Material mat = tileObj[col, row].GetComponent<Renderer>().sharedMaterial; // should probably cache these
      if (mat.mainTexture == null)
      {
        // Whoops, we weren't; fill & apply this texture!  (Unless it's a color match.)
        if (mat.color == color) return;
        for (int j = 0; j < tileHeight; j++)
        {
          for (int i = 0; i < tileWidth; i++)
          {
            tileTex[col, row].SetPixel(i, j, mat.color);
          }
        }
        mat.mainTexture = tileTex[col, row];
        mat.color = Color.white;
      }

      Texture2D tex = tileTex[col, row];
      int tx = x % tileWidth;
      int ty = y % tileHeight;
      if (tex.GetPixel(tx, ty) == color) return;  // already the right color

      tex.SetPixel(tx, ty, color);
      NeedApplyAtTile(col, row, isFromLivePixel);
    }

    int KeyForXY(int x, int y)
    {
      return y * totalWidth + x;
    }

    void XYForKey(int key, out int x, out int y)
    {
      y = key / totalWidth;
      x = key % totalWidth;
    }

    void AddToStack(LivePixel lp, int x, int y)
    {
      int positionKey = KeyForXY(x, y);
      LivePixelStack stack;
      if (!stackMap.TryGetValue(positionKey, out stack))
      {
        if (lpsRecyclePool.Count == 0)
        {
          stack = new LivePixelStack();
        }
        else
        {
          stack = lpsRecyclePool.Pop();
        }
        stack.x = x;
        stack.y = y;
        stack.backgroundColor = GetPixel(x, y);
        stackMap[positionKey] = stack;
      }
      stack.livePixels.Add(lp);
      stack.needsDraw = true;
    }

    void DrawStack(LivePixelStack stack)
    {
      // We can't just call SetPixel here, because that would simply update
      // the background color of the stack.  We need to draw directly to
      // the texture.
      SetPixelIgnoringStacks(stack.x, stack.y, stack.topColor, true);
    }

    void UpdateLivePixels()
    {
      if (stackMap == null) return;
      int curFrame = Time.frameCount;
      // OFI: find some way to avoid copying the stack list, even though we
      // may be adding new stacks as we go.  Maybe we need to keep our own
      // list and iterate over it by index?
      List<LivePixelStack> stacks = new List<LivePixelStack>(stackMap.Values);
      foreach (LivePixelStack stack in stacks)
      {
        List<LivePixel> pixels = stack.livePixels;
        if (pixels.Count == 0) continue;
        Color lastTopColor = stack.topColor;
        int stackKey = KeyForXY(stack.x, stack.y);

        for (int i = pixels.Count - 1; i >= 0; i--)
        {
          LivePixel lp = pixels[i];
          if (lp._lastUpdateFrame == curFrame) continue;
          lp.Update(this);
          lp._lastUpdateFrame = curFrame;
          if (i >= pixels.Count || pixels[i] != lp)
          {
            // The pixel we were working on disappeared from our list during Update...
            // perhaps it was transferred or destroyed.  In either case, we should
            // not try to deal with it any further here.
            continue;
          }

          int newKey = KeyForXY(lp.x, lp.y);

          if (lp.dead)
          {
            // This pixel has died.  Remove it from the old stack,
            // and apply its color to the background color (unless it's clear).
            pixels.RemoveAt(i);
            if (lp.color.a > 0f)
            {
              SetPixel(lp.x, lp.y, lp.color);
            }
            Recycle(lp);
          }
          else if (newKey != stackKey)
          {
            // This pixel has moved.  Remove it from the old stack,
            // and add it to the new one.
            pixels.RemoveAt(i);
            AddToStack(lp, lp.x, lp.y);
          }
        }

        if (stack.topColor != lastTopColor) stack.needsDraw = true;
      }
    }

    void Swap(ref int a, ref int b)
    {
      int temp = a;
      a = b;
      b = temp;
    }

    /// <summary>
    /// Check whether any of our public properties have changed to
    /// necessitate a reset of our tiles.  If so, do that now.
    /// (This is how we keep up with property changes in the editor.)
    /// </summary>
    void ResetIfPropsChanged()
    {
      if (tileWidth != prevTileWidth
        || tileHeight != prevTileHeight
        || totalWidth != prevTotalWidth
        || totalHeight != prevTotalHeight
        || pixelsPerUnit != prevPixelsPerUnit
        || startWithTestPattern != prevStartWithTestPattern)
      {

        Reset();
      }
    }

    #endregion
  }
}
