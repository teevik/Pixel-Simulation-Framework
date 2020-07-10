using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace BootlegStuff
{
    public abstract class LivePixel
    {
        private Vector2 _actualPosition;
        private Vector2Int _roundedPosition;

        public Vector2 position
        {
            get => _actualPosition;
            set
            {
                _actualPosition = value;
                _roundedPosition = _actualPosition.RoundToVector2Int();
            }
        }

        public Vector2Int roundedPosition => _roundedPosition;
        
        public Color color;
        public bool isDead = false;
        
        // Last frame on which this pixel was updated.  For internal use;
        // do not muck with this property.
        public int _lastUpdateFrame;

        protected LivePixel(Vector2Int position)
        {
            this.position = position;
        }

        /// <summary>
        /// This is called when a LivePixel is created/recycled, and added
        /// to a PixelSurface.  Override this to initialize your pixel data.
        /// </summary>
        /// <param name="pixelSurface">PixelSurface to which this pixel is attached.</param>
        public virtual void Start(BootlegPixelSurface pixelSurface) {
		
        }

        /// <summary>
        /// Update is called once on each frame, as long as this LivePixel
        /// is alive (dead==false) and attached to an active PixelSurface.
        /// Here you can update .position or .color to animate your pixel.
        /// </summary>
        /// <param name="pixelSurface">PixelSurface to which this pixel is attached.</param>
        public virtual void Update(BootlegPixelSurface pixelSurface) {
		
        }
         
        public void Die() {
            isDead = true;
        }

        public void DieClear() {
            color = Color.clear;
            isDead = true;
        }
    }

    
    [ExecuteInEditMode]
    public class BootlegPixelSurface : MonoBehaviour
    {
        private class LivePixelStack
        {
            public List<LivePixel> livePixels = new List<LivePixel>();
            public bool needsDraw = false;
            public Vector2Int position;

            public LivePixelStack(Vector2Int position)
            {
                this.position = position;
            }

            public Color GetTopColor() => livePixels.Count > 0 ? livePixels[livePixels.Count - 1].color : Color.clear;
        }

        private class Tile
        {
            public GameObject gameObject;
            public Texture2D staticPixelTexture;
            public Texture2D livePixelTexture;

            public Tile(GameObject gameObject, Texture2D staticPixelTexture, Texture2D livePixelTexture, Material staticPixelMaterial)
            {
                this.gameObject = gameObject;
                this.staticPixelTexture = staticPixelTexture;
                this.livePixelTexture = livePixelTexture;
            }
        }

        private struct TileNeedingApply
        {
            public enum ApplyType
            {
                Static,
                Live
            }

            public ApplyType applyType;
            public Tile tile;

            public TileNeedingApply(ApplyType applyType, Tile tile)
            {
                this.applyType = applyType;
                this.tile = tile;
            }
        }

        [Tooltip("Width of each tile, in pixels")]
        [SerializeField] public int tileWidth = 64;

        [Tooltip("Height of each tile, in pixels")]
        [SerializeField] public int tileHeight = 64;

        [Tooltip("Total surface width, in pixels")]
        [SerializeField] public int totalWidth = 384;

        [Tooltip("Total surface height, in pixels")]
        [SerializeField] public int totalHeight = 256;

        [Tooltip("Scaling factor between pixels and world units")]
        [SerializeField] public float pixelsPerUnit = 100;

        [Tooltip("Shader to use for rendering")]
        [SerializeField] private Shader shader;
        
        [SerializeField] private Renderer tilePrefab;
        
        private Tile[,] _tiles; // tiles by column, row
        private HashSet<TileNeedingApply> _tilesThatNeedApply;
        private Dictionary<int, LivePixelStack> _livePixelStacks;

        private int _prevTileWidth;
        private int _prevTileHeight;
        private int _prevTotalWidth;
        private int _prevTotalHeight;
        private float _prevPixelsPerUnit;

        private void Awake()
        {
            Reset();
        }
        
        private void Update()
        {

            ResetIfPropsChanged();
            
            if (Application.isPlaying)
            {
                UpdateLivePixels();
            }
        }
        
        void UpdateLivePixels()
        {
            // if (stackMap == null) return;
            int currentFrame = Time.frameCount;
            // // OFI: find some way to avoid copying the stack list, even though we
            // // may be adding new stacks as we go.  Maybe we need to keep our own
            // // list and iterate over it by index?
            var stacks = new List<LivePixelStack>(_livePixelStacks.Values);

            foreach (var stack in stacks)
            {
                var livePixels = stack.livePixels;
                
                if (livePixels.Count == 0) continue;
                
                var stackKey = KeyForPosition(stack.position);
                var lastTopColor = stack.GetTopColor();

                for (int i = livePixels.Count - 1; i >= 0; i--)
                {
                    var livePixel = livePixels[i];
                    if (livePixel._lastUpdateFrame == currentFrame) continue;
                    livePixel.Update(this);
                    livePixel._lastUpdateFrame = currentFrame;

                    if (i >= livePixels.Count || livePixels[i] != livePixel)
                    {
                        // The pixel we were working on disappeared from our list during Update...
                        // perhaps it was transferred or destroyed.  In either case, we should
                        // not try to deal with it any further here.
                        continue;
                    }

                    var newStackKey = KeyForPosition(livePixel.roundedPosition);

                    if (livePixel.isDead)
                    {
                        // This pixel has died.  Remove it from the old stack,
                        // and apply its color to the background color (unless it's clear).
                        livePixels.RemoveAt(i);
                        if (livePixel.color.a > 0f)
                        {
                            SetStaticPixel(livePixel.roundedPosition, livePixel.color);
                        }
                    }
                    else if (newStackKey != stackKey)
                    {
                        // This pixel has moved.  Remove it from the old stack,
                        // and add it to the new one.
                        livePixels.RemoveAt(i);
                        
                        if (IsInBounds(livePixel.position)) AddLivePixel(livePixel);
                    }
                }

                if (livePixels.Count == 0)
                {
                    DrawStack(stack);
                    _livePixelStacks.Remove(stackKey);
                }
                else
                {
                    var currentTopColor = stack.GetTopColor();
                    if (currentTopColor != lastTopColor) stack.needsDraw = true;
                }
                
            }
        }

        public void AddLivePixel(LivePixel livePixel)
        {
            var position = livePixel.roundedPosition;
            var key = KeyForPosition(position);
            
            livePixel.Start(this);

            LivePixelStack livePixelStack;
            
            if (!_livePixelStacks.TryGetValue(key, out livePixelStack))
            {
                livePixelStack = new LivePixelStack(position);
                _livePixelStacks.Add(key, livePixelStack);
            }
            
            livePixelStack.livePixels.Add(livePixel);
            livePixelStack.needsDraw = true;
        }

        private void DrawStack(LivePixelStack stack)
        {
            var position = stack.position;
            var x = position.x;
            var y = position.y;
            
            if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return;
            
            var tile = TileForPosition(position);
            var color = stack.GetTopColor();
                
            var tx = position.x % tileWidth;
            var ty = position.y % tileHeight;
            if (tile.livePixelTexture.GetPixel(tx, ty) == color) return;  // already the right color
            tile.livePixelTexture.SetPixel(tx, ty, color);

            stack.needsDraw = false;
            _tilesThatNeedApply.Add(new TileNeedingApply(TileNeedingApply.ApplyType.Live, tile));
        }
        
        private void LateUpdate()
        {
            var stacks = new List<LivePixelStack>(_livePixelStacks.Values);
            
            foreach (var stack in stacks)
            {
                if (stack.needsDraw) DrawStack(stack);
            }
            
            if (_tilesThatNeedApply.Count > 0)
            {
                foreach (var tileNeedingApply in _tilesThatNeedApply)
                {
                    var applyType = tileNeedingApply.applyType;
                    var tile = tileNeedingApply.tile;
                    
                    if (applyType == TileNeedingApply.ApplyType.Static)
                    {
                        tile.staticPixelTexture.Apply();
                    }
                    
                    if (applyType == TileNeedingApply.ApplyType.Live)
                    {
                        tile.livePixelTexture.Apply();
                    }
                }

                _tilesThatNeedApply.Clear();
            }
        }
        
        /// <summary>
        /// Set the static pixel color at the given position.  Note that if
        /// there are any live pixels at this position, those are ignored in
        /// this operation; you're only setting the static color here.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="color">Color to set.</param>
        public void SetStaticPixel(Vector2Int position, Color color)
        {
            var x = position.x;
            var y = position.y;
            
            if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return;

            var tile = TileForPosition(new Vector2Int(x, y));

            var staticPixelTexture = tile.staticPixelTexture;

            int tx = x % tileWidth;
            int ty = y % tileHeight;
            if (staticPixelTexture.GetPixel(tx, ty) == color) return;  // already the right color

            staticPixelTexture.SetPixel(tx, ty, color);
            _tilesThatNeedApply.Add(new TileNeedingApply(TileNeedingApply.ApplyType.Static, tile));
        }

        public List<T> GetLivePixels<T>(Vector2Int position) where T : class
        {
            var results = new List<T>();
            
            var key = KeyForPosition(position);
            if (_livePixelStacks.TryGetValue(key, out var livePixelStack))
            {
                foreach (var livePixel in livePixelStack.livePixels)
                {
                    if (livePixel is T result) results.Add(result); 
                }
            }

            return results;
        }
        
        public Color GetStaticPixel(Vector2Int position)
        {
            var x = position.x;
            var y = position.y;
            
            if (x < 0 || y < 0 || x >= totalWidth || y >= totalHeight) return Color.clear;

            var tile = TileForPosition(position);

            return tile.staticPixelTexture.GetPixel(x % tileWidth, y % tileHeight);
        }
        
        /// <summary>
        /// Get the position in the world of a given pixel position.
        /// </summary>
        /// <returns>World coordinates of given pixel position.</returns>
        /// <param name="pixelPos">Pixel position of interest.</param>
        public Vector3 WorldPosAtPixelPos(Vector2Int pixelPos)
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
        public Vector2Int PixelPosAtWorldPos(Vector3 worldPos)
        {
            var position = transform.InverseTransformPoint(worldPos);
            var x = Mathf.RoundToInt(position.x * pixelsPerUnit);
            var y = Mathf.RoundToInt(position.y * pixelsPerUnit);
            
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Return whether the given pixel position is within bounds of
        /// this pixel surface.
        /// </summary>
        /// <returns><c>true</c>, if within bounds, <c>false</c> otherwise.</returns>
        /// <param name="pixelPos">Pixel position of interest.</param>
        public bool IsInBounds(Vector2 pixelPos)
        {
            return pixelPos.x >= 0 && pixelPos.x < totalWidth
                                   && pixelPos.y >= 0 && pixelPos.y < totalHeight;
        }

        private void ClearTiles()
        {
            // Destroy all the tiles we know about.
            if (_tiles != null)
            {
                foreach (var tile in _tiles)
                {
                    GameObject.DestroyImmediate(tile.gameObject);
                }
                
                _tiles = null;
            }

            // In addition to that, let's also search our immediate children
            // for things that look like tiles, and destroy them.  This is 
            // needed, for example, in cases where there were some tiles
            // created in the editor, but not part of our tileObj array.
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Tile "))
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }
        }
        
        private static Texture2D CreateClearTexture2D(int width, int height)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.clear);
            texture.Resize(width, height);
            return texture;
        }

        private void CreateTiles()
        {
            var tileColumns = Mathf.CeilToInt(totalWidth / tileWidth);
            var tileRows = Mathf.CeilToInt(totalHeight / tileHeight);

            _tiles = new Tile[tileColumns, tileRows];

            var worldTileWidth = tileWidth / pixelsPerUnit;
            var worldTileHeight = tileHeight / pixelsPerUnit;
            
            for (var row = 0; row < tileRows; row++)
            {
                for (var column = 0; column < tileColumns; column++)
                {
                    var containter = new GameObject();
                    containter.name = "Tile " + column + ", " + row;
                    containter.transform.localScale = new Vector3(worldTileWidth, worldTileHeight, 1);
                    containter.transform.position = new Vector3(
                        worldTileWidth * (column + 0.5f),
                        worldTileHeight * (row + 0.5f),
                        0);
                    containter.transform.SetParent(transform, false);

                    var staticPixelTile = Instantiate(tilePrefab, containter.transform, false);
                    var livePixelTile = Instantiate(tilePrefab, containter.transform, false);
                    livePixelTile.sortingOrder = 1;

                    var staticPixelMaterial = new Material(shader);
                    var livePixelMaterial = new Material(shader);
                    var staticPixelTexture = new Texture2D(tileWidth, tileHeight);
                    var livePixelTexture = new Texture2D(tileWidth, tileHeight);
                    
                    for (int j = 0; j < tileHeight; j++)
                    {
                        for (int i = 0; i < tileWidth; i++)
                        {
                            staticPixelTexture.SetPixel(i, j, Color.clear);
                            livePixelTexture.SetPixel(i, j, Color.clear);
                        }
                    }

                    staticPixelTexture.wrapMode = TextureWrapMode.Clamp;
                    staticPixelTexture.filterMode = FilterMode.Point;
                    livePixelTexture.wrapMode = TextureWrapMode.Clamp;
                    livePixelTexture.filterMode = FilterMode.Point;

                    staticPixelMaterial.mainTexture = staticPixelTexture;
                    livePixelMaterial.mainTexture = livePixelTexture;
                    
                    staticPixelMaterial.color = Color.clear;
                    livePixelMaterial.color = Color.clear;

                    staticPixelTile.sharedMaterial = staticPixelMaterial;
                    livePixelTile.sharedMaterial = livePixelMaterial;

                    var tile = new Tile(containter, staticPixelTexture, livePixelTexture, staticPixelMaterial);
                    _tiles[column, row] = tile;
                }
            }

            _prevTileWidth = tileWidth;
            _prevTileHeight = tileHeight;
            _prevTotalWidth = totalWidth;
            _prevTotalHeight = totalHeight;
            _prevPixelsPerUnit = pixelsPerUnit;
        }

        /// <summary>
        /// Reset this pixel surface, clearing all pixel data.
        /// </summary>
        public void Reset()
        {
            _tilesThatNeedApply = new HashSet<TileNeedingApply>();
            _livePixelStacks = new Dictionary<int, LivePixelStack>();
            
            ClearTiles();
            CreateTiles();
        }
        
        /// <summary>
        /// Check whether any of our public properties have changed to
        /// necessitate a reset of our tiles.  If so, do that now.
        /// (This is how we keep up with property changes in the editor.)
        /// </summary>
        private void ResetIfPropsChanged()
        {
            if (tileWidth != _prevTileWidth
                || tileHeight != _prevTileHeight
                || totalWidth != _prevTotalWidth
                || totalHeight != _prevTotalHeight
                || pixelsPerUnit != _prevPixelsPerUnit)
            {
                Reset();
            }
        }
        
        private int KeyForPosition(Vector2Int position)
        {
            return position.y * totalWidth + position.x;
        }

        private Vector2Int PositionForKey(int key)
        {
            var y = key / totalWidth;
            var x = key % totalWidth;
            
            return new Vector2Int(x, y);
        }

        private Tile TileForPosition(Vector2Int position)
        {
            var column = position.x / tileWidth;
            var row = position.y / tileHeight;

            return _tiles[column, row];
        }
    }
}