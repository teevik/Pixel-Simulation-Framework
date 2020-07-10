using System;
using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace BettererBootlegStuff
{
    public abstract class Pixel
    {
        /// <summary>
        /// Last frame on which this pixel was updated.  For internal use;
        /// do not muck with this property.
        /// </summary>
        public int _lastUpdateFrame;
        
        public interface IStartablePixel
        {
            void Start(Vector2Int position, NewPixelSimulation pixelSimulation);
        }
        
        public interface IUpdateablePixel
        {
            void Update(Vector2Int position, NewPixelSimulation pixelSimulation);
        }
        
        public Color color = Color.clear;
    }

    public class StaticPixel : Pixel
    {
    }

    public class Chunk
    {
        public Vector2Int position;
        public Renderer renderer;
        public Texture2D texture;
        public bool needsApply = false;
        
        public void DrawPixel(Vector2Int pixelPosition, Color color)
        {
            if (texture.GetPixel(pixelPosition) != color)
            {
                texture.SetPixel(pixelPosition, color);
                needsApply = true;
            }
        }
    }

    [ExecuteAlways]
    public class NewPixelSimulation : MonoBehaviour
    {
        [Tooltip("Dimensions of each chunk, in pixels")]
        [SerializeField] private Vector2Int _chunkDimensions;
        
        [Tooltip("Amount of chunks")]
        [SerializeField] private Vector2Int _amountOfChunks;
        
        [Tooltip("Scaling factor between pixels and world units")]
        [SerializeField] private float _pixelsPerUnit = 100;
        
        [Tooltip("Shader to use for rendering")]
        [SerializeField] private Shader _shader;
        
        [SerializeField] private Renderer _rendererPrefab;

        [SerializeField] private bool _saveStats;

        private Vector2Int _previousChunkDimensions;
        private Vector2Int _previousAmountOfChunks;
        private float _previousPixelsPerUnit;
        
        private Chunk[,] _chunks;
        private Pixel[,] _pixels;
        private Dictionary<Pixel, Vector2Int> _pixelPositions;
        private HashSet<Pixel.IUpdateablePixel> _updateablePixels;
            
        private Vector2Int GetTotalGridSize() => _amountOfChunks * _chunkDimensions;

        private void Awake()
        {
            Reset();
        }

        private void Update()
        {
            ResetIfPropsChanged();
            
            // if (Application.isPlaying)
            // {
            //     UpdateTiles();
            // }
        }
        
        private void LateUpdate()
        {
            foreach (var chunk in _chunks)
            {
                if (chunk.needsApply)
                {
                    chunk.texture.Apply();
                    chunk.needsApply = false;
                }
            }
        }
        
        public void Reset()
        {
            ClearChunks();
            InitializeChunks();
        }
        
        private void ResetIfPropsChanged()
        {
            if (_chunkDimensions != _previousChunkDimensions
                || _amountOfChunks != _previousAmountOfChunks
                || _pixelsPerUnit != _previousPixelsPerUnit)
            {
                Reset();
            }
        }

        private void ClearChunks()
        {
            // Destroy all the chunks we know about.
            if (_chunks != null)
            {
                foreach (var chunk in _chunks)
                {
                    GameObject.DestroyImmediate(chunk.renderer.gameObject);
                }
                
                _chunks = null;
            }

            // In addition to that, let's also search our immediate children
            // for things that look like chunks, and destroy them.
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Chunk "))
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }
        }
        
        private void InitializeChunks()
        {
            _chunks = new Chunk[_amountOfChunks.x, _amountOfChunks.y];
            
            var worldChunkDimensions = (Vector2)_chunkDimensions / _pixelsPerUnit;

            for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
            {
                for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
                {
                    var texture = new Texture2D(_chunkDimensions.x, _chunkDimensions.y);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Point;
                    Color[] colors = new Color[texture.width * texture.height];
                    texture.SetPixels(colors);
                    texture.Apply();
                    
                    var material = new Material(_shader);
                    material.mainTexture = texture;
                    
                    var renderer = Instantiate(_rendererPrefab);
                    renderer.name = "Chunk " + chunkColumn + ", " + chunkRow;
                    renderer.transform.localScale = new Vector3(worldChunkDimensions.x, worldChunkDimensions.y, 1);
                    renderer.transform.position = new Vector3(
                        worldChunkDimensions.x * (chunkColumn + 0.5f),
                        worldChunkDimensions.y * (chunkRow + 0.5f),
                        0);
                    renderer.transform.SetParent(transform, false);
                    renderer.sharedMaterial = material;

                    var chunk = new Chunk()
                    {
                        position = new Vector2Int(chunkColumn, chunkRow),
                        texture = texture,
                        renderer = renderer
                    };

                    _chunks[chunkColumn, chunkRow] = chunk;
                }
            }
            
            _previousChunkDimensions = _chunkDimensions;
            _previousAmountOfChunks = _amountOfChunks;
            _previousPixelsPerUnit = _pixelsPerUnit;
        }

        private void GetChunkPositionFromGridPosition(Vector2Int gridPosition, out Chunk chunk, out Vector2Int chunkPixelPosition)
        {
            var chunkPosition = ((Vector2) gridPosition / (Vector2) _chunkDimensions).FloorToVector2Int();
            chunk = _chunks[chunkPosition.x, chunkPosition.y];
            chunkPixelPosition = gridPosition - (chunkPosition * _chunkDimensions);
        }
        
        private void DrawPixel(Vector2Int gridPosition, Color color)
        {
            GetChunkPositionFromGridPosition(gridPosition, out var chunk, out var chunkPixelPosition);
            chunk.DrawPixel(chunkPixelPosition, color);
        }

        public PixelType AddPixel<PixelType>(Vector2Int position) where PixelType : Pixel, new()
        {
            if (!IsInBoundsOfGrid(position)) throw new ArgumentOutOfRangeException(nameof(position));
            if (TileExistsAt(position)) throw new ArgumentException(nameof(position));
            
            var pixel = new PixelType();

            _pixels[position.x, position.y] = pixel;

            if (pixel is Pixel.IStartablePixel startablePixel)
            {
                startablePixel.Start(position, this);
            }

            if (pixel is Pixel.IUpdateablePixel updateablePixel)
            {
                _updateablePixels.Add(updateablePixel);
            }
            
            DrawPixel(position, pixel.color);

            return pixel;
        }

        public void RemovePixel(Vector2Int position)
        {
            if (!IsInBoundsOfGrid(position)) throw new ArgumentOutOfRangeException(nameof(position));
            if (!TileExistsAt(position)) throw new ArgumentException(nameof(position));

            var pixel = _pixels[position.x, position.y];
            
            if (pixel is Pixel.IUpdateablePixel updateablePixel)
            {
                _updateablePixels.Remove(updateablePixel);
            }

            _pixelPositions.Remove(pixel);
        }

        private bool IsInBoundsOfGrid(Vector2Int position)
        {
            var totalGridSize = GetTotalGridSize();
            
            return position.x >= 0 || position.y >= 0 || position.x < totalGridSize.x || position.y < totalGridSize.y;
        }
        
        private bool TileExistsAt(Vector2Int position)
        {
            return _pixels[position.x, position.y] != null;
        }

        private void OnDrawGizmosSelected()
        {
            var worldChunkDimensions = (Vector2)_chunkDimensions / _pixelsPerUnit;

            for (int x = 0; x < _amountOfChunks.x; x++)
            {
                for (int y = 0; y < _amountOfChunks.y; y++)
                {
                    var offset = new Vector2(x, y);
                    
                    var center = worldChunkDimensions * offset + worldChunkDimensions / 2;
                    var size = worldChunkDimensions / 2;

                    Gizmos.DrawWireCube((Vector3)center + transform.position, worldChunkDimensions);
                }
            }
        }
    }
}