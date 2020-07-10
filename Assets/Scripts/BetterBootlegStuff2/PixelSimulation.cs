using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Extensions;
using Sirenix.Utilities;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace BetterBootlegStuff2
{
    
    public struct DirtyRect
    {
        private Vector2Int _startTilePosition;
        private Vector2Int _endTilePosition;
        
        public DirtyRect(Vector2Int startTilePosition, Vector2Int endTilePosition)
        {
            _startTilePosition = startTilePosition;
            _endTilePosition = endTilePosition;
        }
    }
    
    public enum Side
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public static class SideExtensions
    {
        public static readonly Dictionary<Side, Vector2Int> sideVectorMap = new Dictionary<Side, Vector2Int>()
        {
            [Side.North] = Vector2Int.up,
            [Side.NorthEast] = Vector2Int.up + Vector2Int.right,
            [Side.East] = Vector2Int.right,
            [Side.SouthEast] = Vector2Int.right + Vector2Int.down,
            [Side.South] = Vector2Int.down,
            [Side.SouthWest] = Vector2Int.down + Vector2Int.left,
            [Side.West] = Vector2Int.left,
            [Side.NorthWest] = Vector2Int.left + Vector2Int.up
        };
        
        public static Vector2Int SideToVector(this Side side)
        {
            return sideVectorMap[side];
        }
    }
    
    public abstract class Tile
    {
        public virtual void Start()
        {
            
        }

        public virtual void Update(float deltaTime)
        {
            
        }

        protected bool _isStale = false;
        public bool IsStale => _isStale;

        protected Color _color;
        public Color Color => _color;

        protected Vector2Int _wantedMovement = Vector2Int.zero;
        public Vector2Int WantedMovement => _wantedMovement;

        // Last frame on which this pixel was updated.  For internal use;
        // do not muck with this property.
        public int LastUpdateFrame { get; set; }

        public Chunk Parent { get; set; }


        protected Tile(Color color)
        {
            _color = color;
        }
    }

    public class StaticTile : Tile
    {
        public StaticTile(Color color) : base(color)
        {
        }
    }

    public class Chunk
    {
        private readonly Vector2Int _position;
        private Renderer _renderer;
        private readonly Texture2D _texture;
        private readonly Tile[,] _tiles; // tiles by column, row
        private Dictionary<Side, Chunk> _neighbors;
        private readonly PixelSimulation _pixelSimulation;
        public bool NeedsApply { get; set; } = false;
        private readonly Dictionary<Tile, Vector2Int> _tilePositions = new Dictionary<Tile, Vector2Int>();
        private DirtyRect _dirtyRect;
        private HashSet<Tile> _unstaleTiles;

        public Texture2D Texture => _texture;
        public Tile[,] Tiles => _tiles;
        public Vector2Int Position => _position;
        public Dictionary<Tile, Vector2Int> TilePositions => _tilePositions;
        public Renderer Renderer => _renderer;
        
        public Chunk(PixelSimulation pixelSimulation, Vector2Int position, Tile[,] tiles, Texture2D texture, Renderer renderer)
        {
            _pixelSimulation = pixelSimulation;
            _position = position;
            _tiles = tiles;
            _texture = texture;
            _renderer = renderer;

            _unstaleTiles = _tiles.Cast<Tile>().ToHashSet();
            _dirtyRect = new DirtyRect(startTilePosition: Vector2Int.zero, endTilePosition: _pixelSimulation.ChunkDimensions);
        }

        public void SetNeighbors(Dictionary<Side, Chunk> neighbors)
        {
            _neighbors = neighbors;
        }

        private void DrawTile(Tile tile, Vector2Int tilePosition)
        {
            if (_texture.GetPixel(tilePosition) != tile.Color)
            {
                _texture.SetPixel(tilePosition, tile.Color);
                NeedsApply = true;
            }
        }
        
        private void DrawClear(Tile tile)
        {
            var tilePosition = _tilePositions[tile];
            DrawClear(tilePosition);
        }
        
        private void DrawClear(Vector2Int position)
        {
            _texture.SetPixel(position, Color.clear);
            NeedsApply = true;
        }

        public void SwitchTile(Tile originalTile, Tile newTile)
        {
            var tilePosition = _tilePositions[originalTile];
            
            RemoveTile(tilePosition);
            AddTile(newTile, tilePosition);
        }

        public void AddTile(Tile tile, Vector2Int tilePosition)
        {
            if (!TileIsInBounds(tilePosition)) throw new ArgumentOutOfRangeException(nameof(tilePosition));
            if (TileExistsAt(tilePosition)) throw new ArgumentException("There's already a tile here", nameof(tilePosition));

            _tiles[tilePosition.x, tilePosition.y] = tile;
            tile.Parent = this;
            _tilePositions.Add(tile, tilePosition);
            if (!tile.IsStale) _unstaleTiles.Add(tile);
            DrawTile(tile, tilePosition);
            
            tile.Start();
        }

        public void RemoveTile(Tile tile)
        {
            var tilePosition = _tilePositions[tile];
            RemoveTile(tilePosition);
        }
        
        public void RemoveTile(Vector2Int tilePosition)
        {
            if (!TileIsInBounds(tilePosition)) throw new ArgumentOutOfRangeException(nameof(tilePosition));
            if (!TileExistsAt(tilePosition)) throw new ArgumentException("There's no tile here", nameof(tilePosition));

            var tile = _tiles[tilePosition.x, tilePosition.y];
            
            if (!tile.IsStale) _unstaleTiles.Remove(tile);

            _tilePositions.Remove(tile);
            _tiles[tilePosition.x, tilePosition.y] = null;
            DrawClear(tilePosition);
        }

        public void MoveTileToThisChunk(Tile originalTile, Vector2Int newPosition)
        {
            if (!TileIsInBounds(newPosition)) throw new ArgumentOutOfRangeException(nameof(newPosition));
            if (TileExistsAt(newPosition)) throw new ArgumentException("There's already a tile here", nameof(newPosition));

            var originalTileChunk = originalTile.Parent;
            if (!originalTile.IsStale)
            {
                originalTileChunk._unstaleTiles.Remove(originalTile);
                _unstaleTiles.Add(originalTile);
            }

            var originalTilePosition = originalTileChunk._tilePositions[originalTile];
            originalTileChunk._tiles[originalTilePosition.x, originalTilePosition.y] = null;
            originalTileChunk._tilePositions.Remove(originalTile);
            originalTileChunk.DrawClear(originalTilePosition);
            
            originalTile.Parent = this;
            _tiles[newPosition.x, newPosition.y] = originalTile;
            _tilePositions.Add(originalTile, newPosition);
            DrawTile(originalTile, newPosition);
        }

        public void MoveTileRelative(Tile tile, Vector2Int moveAmount)
        {
            var oldTilePosition = _tilePositions[tile];
            var targetTilePosition = oldTilePosition + moveAmount;
            var targetChunk = this;
            
            var chunkWidth = _pixelSimulation.ChunkDimensions.x;
            var chunkHeight = _pixelSimulation.ChunkDimensions.y;
            
            while (targetTilePosition.x < 0)
            {
                targetChunk = targetChunk._neighbors[Side.West];
                if (targetChunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.x += chunkWidth;
            }

            while (targetTilePosition.x >= chunkWidth)
            {
                targetChunk = targetChunk._neighbors[Side.East];
                if (targetChunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.x -= chunkWidth;
            }
            
            while (targetTilePosition.y < 0)
            {
                targetChunk = targetChunk._neighbors[Side.South];
                if (targetChunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.y += chunkHeight;
            }
            
            while (targetTilePosition.y >= chunkHeight)
            {
                targetChunk = targetChunk._neighbors[Side.North];
                if (targetChunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.y -= chunkHeight;
            }

            if (targetChunk == this)
            {
                // if (!TileIsInBounds(newPosition)) throw new ArgumentOutOfRangeException(nameof(newPosition));
                // if (TileExistsAt(newPosition)) throw new ArgumentException("There's already a tile here", nameof(newPosition));
                
                DrawClear(oldTilePosition);
                
                _tiles[oldTilePosition.x, oldTilePosition.y] = null;
                _tiles[targetTilePosition.x, targetTilePosition.y] = tile;

                _tilePositions.Remove(tile);
                _tilePositions.Add(tile, targetTilePosition);
                
                DrawTile(tile, targetTilePosition);
            }
            else
            {
                targetChunk.MoveTileToThisChunk(tile, targetTilePosition);
            }
        }

        public bool TileIsInBounds(Vector2Int tilePosition)
        {
            var chunkWidth = _pixelSimulation.ChunkDimensions.x;
            var chunkHeight = _pixelSimulation.ChunkDimensions.y;

            return 
                tilePosition.x >= 0 && tilePosition.x < chunkWidth && 
                tilePosition.y >= 0 && tilePosition.y < chunkHeight;
        }

        public bool TileExistsAt(Vector2Int tilePosition)
        {
            var alreadyExistsTileAt = _tiles[tilePosition.x, tilePosition.y] != null;

            return alreadyExistsTileAt;
        }

        public Tile GetNeighborOfTile(Tile tile, Side side)
        {
            var tilePosition = _tilePositions[tile];
            var chunkWidth = _pixelSimulation.ChunkDimensions.x;
            var chunkHeight = _pixelSimulation.ChunkDimensions.y;
            
            var offset = side.SideToVector();
            var targetPosition = tilePosition + offset;

            Tile targetTile;
            if (TileIsInBounds(targetPosition))
            {
                targetTile = _tiles[targetPosition.x, targetPosition.y];
            }
            else
            {
                var neighborChunk = _neighbors[side];
                if (neighborChunk == null) return new StaticTile(Color.black);
                
                var newTargetPosition = targetPosition;

                if (side == Side.West || side == Side.East)
                {
                    newTargetPosition.x = (chunkWidth - 1) - tilePosition.x;
                } 
                else if (side == Side.North || side == Side.South)
                {
                    newTargetPosition.y = (chunkHeight - 1) - tilePosition.y;
                }

                targetTile = neighborChunk._tiles[newTargetPosition.x, newTargetPosition.y];
            }

            return targetTile;
        }

        public bool NeighborExistsAt(Tile tile, Side side)
        {
            var neighbor = GetNeighborOfTile(tile, side);
            return neighbor != null;
        }

        
        public Dictionary<Side, Tile> GetNeighborsOfTile(Tile tile)
        {
            var tileNeighbors = new Dictionary<Side, Tile>();
            
            foreach (Side side in Enum.GetValues(typeof(Side)))
            {
                var targetTile = GetNeighborOfTile(tile, side);
                tileNeighbors.Add(side, targetTile);
            }

            return tileNeighbors;
        }
    }

    [ExecuteAlways]
    public class PixelSimulation : MonoBehaviour
    {
        public class PixelSimulationStats
        {
            public int staticPixels;
            public int updatePixels;
        }
        
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
        
        public Vector2Int ChunkDimensions => _chunkDimensions;
        public Vector2Int AmountOfChunks => _amountOfChunks;
        public float PixelsPerUnit => _pixelsPerUnit;
        
        private Vector2Int _previousChunkDimensions;
        private Vector2Int _previousAmountOfChunks;
        private float _previousPixelsPerUnit;
        public readonly PixelSimulationStats stats = new PixelSimulationStats();
        
        private Chunk[,] _chunks; // chunks by column, row

        private void Awake()
        {
            Reset();
        }

        private void Update()
        {
            ResetIfPropsChanged();
            
            if (Application.isPlaying)
            {
                UpdateTiles();
            }
        }

        private void LateUpdate()
        {
            foreach (var chunk in _chunks)
            {
                if (chunk.NeedsApply)
                {
                    chunk.Texture.Apply();
                    chunk.NeedsApply = false;
                }
            }
        }

        private void UpdateTiles()
        {
            var currentFrame = Time.frameCount;
            stats.staticPixels = 0;
            stats.updatePixels = 0;

            foreach (var chunk in _chunks)
            {
                foreach (var tile in chunk.Tiles)
                {
                    if (tile == null) continue;
                    
                    if (!tile.IsStale)
                    {
                        if (tile.LastUpdateFrame == currentFrame) continue;

                        tile.Update(Time.deltaTime);
                        tile.LastUpdateFrame = currentFrame;
                        if (_saveStats) stats.updatePixels += 1;
                    } 
                    else if (_saveStats)
                    {
                        stats.staticPixels += 1;
                    }
                } 
            }
        }

        public Vector2Int GetGridPositionOfTile(Tile tile)
        {
            var chunk = tile.Parent;
            var chunkPosition = chunk.Position;
            var tilePosition = chunk.TilePositions[tile];

            var gridPosition = chunkPosition * _chunkDimensions + tilePosition;
            return gridPosition;
        }

        public Vector3 WorldPositionAtGridPosition(Vector2Int pixelPos)
        {
            return transform.TransformPoint(new Vector3(pixelPos.x / _pixelsPerUnit,
                pixelPos.y / _pixelsPerUnit,
                0));
        }
        
        public Vector2Int GridPositionAtWorldPosition(Vector3 worldPos)
        {
            var position = transform.InverseTransformPoint(worldPos);
            var x = Mathf.RoundToInt(position.x * _pixelsPerUnit);
            var y = Mathf.RoundToInt(position.y * _pixelsPerUnit);
            
            return new Vector2Int(x, y);
        }

        public bool FindTilePosition(Vector2Int gridPosition, out Chunk chunk, out Vector2Int tilePosition)
        {
            var chunkPosition = ((Vector2) gridPosition / (Vector2) _chunkDimensions).FloorToVector2Int();

            if (ChunkIsInBounds(chunkPosition))
            {
                chunk = _chunks[chunkPosition.x, chunkPosition.y];
                tilePosition = gridPosition - (chunkPosition * _chunkDimensions);
                return true;
            }

            chunk = null;
            tilePosition = default;
            return false;
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

        public void Reset()
        {
            ClearChunks();
            InitializeChunks();
        }
        
        private void ClearChunks()
        {
            // Destroy all the chunks we know about.
            if (_chunks != null)
            {
                foreach (var chunk in _chunks)
                {
                    GameObject.DestroyImmediate(chunk.Renderer.gameObject);
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

        private bool ChunkIsInBounds(Vector2Int position)
        {
            var columns = _amountOfChunks.x;
            var rows = _amountOfChunks.y;

            return 
                position.x >= 0 && position.x < columns && 
                position.y >= 0 && position.y < rows;
        }
        
        public Dictionary<Side, Chunk> GetNeighborsOfChunk(Chunk chunk)
        {
            var chunkPosition = chunk.Position;
            var chunkNeighbors = new Dictionary<Side, Chunk>();
            
            foreach (Side side in Enum.GetValues(typeof(Side)))
            {
                var offset = side.SideToVector();
                var targetPosition = chunkPosition + offset;

                if (ChunkIsInBounds(targetPosition))
                {
                    var targetChunk = _chunks[targetPosition.x, targetPosition.y];
                    chunkNeighbors.Add(side, targetChunk);
                }
                else
                {
                    chunkNeighbors.Add(side, null);
                }
            }

            return chunkNeighbors;
        }


        private void InitializeChunks()
        {
            _chunks = new Chunk[_amountOfChunks.x, _amountOfChunks.y];
            
            var worldChunkDimensions = (Vector2)_chunkDimensions / _pixelsPerUnit;

            for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
            {
                for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
                {
                    var tiles = new Tile[_chunkDimensions.x, _chunkDimensions.y];
                    
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

                    var chunk = new Chunk(pixelSimulation: this, position: new Vector2Int(chunkColumn, chunkRow),
                        tiles: tiles, texture: texture, renderer: renderer);

                    _chunks[chunkColumn, chunkRow] = chunk;
                }
            }

            foreach (var chunk in _chunks)
            {
                var neighbors = GetNeighborsOfChunk(chunk);

                chunk.SetNeighbors(neighbors);
            }

            _previousChunkDimensions = _chunkDimensions;
            _previousAmountOfChunks = _amountOfChunks;
            _previousPixelsPerUnit = _pixelsPerUnit;
        }
    }
}