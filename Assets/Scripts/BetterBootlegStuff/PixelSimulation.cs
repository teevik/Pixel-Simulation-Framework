using System;
using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace BetterBootlegStuff
{
    public enum Side
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public static class SideExtensions
    {
        public static readonly Dictionary<Side, Vector2Int> sideVectorMap = new Dictionary<Side, Vector2Int>()
        {
            [Side.Top] = Vector2Int.up,
            [Side.Right] = Vector2Int.right,
            [Side.Bottom] = Vector2Int.down,
            [Side.Left] = Vector2Int.left,
        };
        
        public static Vector2Int SideToVector(this Side side)
        {
            return sideVectorMap[side];
        }
    }
    
    public abstract class Tile
    {
        public interface IStart
        {
            void Start();
        }
        
        public interface IUpdate
        {
            void Update();
        }

        private Color _color;
        public Color Color => _color;
        
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
        public Vector2Int position;
        public Renderer renderer;
        public Texture2D texture;
        public Tile[,] tiles; // tiles by column, row
        public Dictionary<Side, Chunk> neighbors;
        public PixelSimulation pixelSimulation;
        public bool needsApply = false;
        public HashSet<Tile.IUpdate> updateableTiles = new HashSet<Tile.IUpdate>();
        public Dictionary<Tile, Vector2Int> tilePositions = new Dictionary<Tile, Vector2Int>();

        private void DrawTile(Tile tile, Vector2Int tilePosition)
        {
            if (texture.GetPixel(tilePosition) != tile.Color)
            {
                texture.SetPixel(tilePosition, tile.Color);
                needsApply = true;
            }
        }
        
        private void DrawClear(Tile tile)
        {
            var tilePosition = tilePositions[tile];
            DrawClear(tilePosition);
        }
        
        private void DrawClear(Vector2Int position)
        {
            texture.SetPixel(position, Color.clear);
            needsApply = true;
        }

        public void SwitchTile(Tile originalTile, Tile newTile)
        {
            var tilePosition = tilePositions[originalTile];
            
            RemoveTile(tilePosition);
            AddTile(newTile, tilePosition);
        }

        public void AddTile(Tile tile, Vector2Int tilePosition)
        {
            if (!TileIsInBounds(tilePosition)) throw new ArgumentOutOfRangeException(nameof(tilePosition));
            if (TileExistsAt(tilePosition)) throw new ArgumentException("There's already a tile here", nameof(tilePosition));

            tiles[tilePosition.x, tilePosition.y] = tile;
            tile.Parent = this;
            tilePositions.Add(tile, tilePosition);
            DrawTile(tile, tilePosition);
            
            if (tile is Tile.IStart startableTile)
            {
                startableTile.Start();
            }
            
            if (tile is Tile.IUpdate updateableTile)
            {
                updateableTiles.Add(updateableTile);
            }
        }

        public void RemoveTile(Tile tile)
        {
            var tilePosition = tilePositions[tile];
            RemoveTile(tilePosition);
        }
        
        public void RemoveTile(Vector2Int tilePosition)
        {
            if (!TileIsInBounds(tilePosition)) throw new ArgumentOutOfRangeException(nameof(tilePosition));
            if (!TileExistsAt(tilePosition)) throw new ArgumentException("There's no tile here", nameof(tilePosition));

            var tile = tiles[tilePosition.x, tilePosition.y];
            
            if (tile is Tile.IUpdate updateableTile)
            {
                updateableTiles.Remove(updateableTile);
            }

            tilePositions.Remove(tile);
            tiles[tilePosition.x, tilePosition.y] = null;
            DrawClear(tilePosition);
        }

        public void MoveTileToThisChunk(Tile originalTile, Vector2Int newPosition)
        {
            if (!TileIsInBounds(newPosition)) throw new ArgumentOutOfRangeException(nameof(newPosition));
            if (TileExistsAt(newPosition)) throw new ArgumentException("There's already a tile here", nameof(newPosition));

            var originalTileChunk = originalTile.Parent;
            if (originalTile is Tile.IUpdate updateableTile)
            {
                originalTileChunk.updateableTiles.Remove(updateableTile);
                updateableTiles.Add(updateableTile);
            }

            var originalTilePosition = originalTileChunk.tilePositions[originalTile];
            originalTileChunk.tiles[originalTilePosition.x, originalTilePosition.y] = null;
            originalTileChunk.tilePositions.Remove(originalTile);
            originalTileChunk.DrawClear(originalTile);
            
            originalTile.Parent = this;
            tiles[newPosition.x, newPosition.y] = originalTile;
            tilePositions.Add(originalTile, newPosition);
            DrawTile(originalTile, newPosition);
        }

        public void MoveTileRelative(Tile tile, Vector2Int moveAmount)
        {
            var oldTilePosition = tilePositions[tile];
            var targetTilePosition = oldTilePosition + moveAmount;
            var chunk = this;
            
            var chunkWidth = pixelSimulation.ChunkDimensions.x;
            var chunkHeight = pixelSimulation.ChunkDimensions.y;
            
            while (targetTilePosition.x < 0)
            {
                chunk = chunk.neighbors[Side.Left];
                if (chunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.x = targetTilePosition.x + (chunkWidth);
            }

            while (targetTilePosition.x >= chunkWidth)
            {
                chunk = chunk.neighbors[Side.Right];
                if (chunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.x = targetTilePosition.x - (chunkWidth);
            }
            
            while (targetTilePosition.y < 0)
            {
                chunk = chunk.neighbors[Side.Bottom];
                if (chunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.y = targetTilePosition.y + (chunkHeight);
            }
            
            while (targetTilePosition.y >= chunkHeight)
            {
                chunk = chunk.neighbors[Side.Top];
                if (chunk == null)
                {
                    throw new Exception("aa");
                }
                
                targetTilePosition.y = targetTilePosition.y - (chunkHeight);
            }
            
            if (chunk.TileExistsAt(targetTilePosition)) throw new ArgumentException("There's already a tile here", nameof(moveAmount));
            
            if (tile is Tile.IUpdate updateableTile)
            {
                updateableTiles.Remove(updateableTile);
                chunk.updateableTiles.Add(updateableTile);
            }

            DrawClear(oldTilePosition);
            tiles[oldTilePosition.x, oldTilePosition.y] = null;
            tilePositions.Remove(tile);
            tile.Parent = chunk;
            chunk.tiles[targetTilePosition.x, targetTilePosition.y] = tile;
            chunk.tilePositions.Add(tile, targetTilePosition);
            chunk.DrawTile(tile, targetTilePosition);
        }

        public bool TileIsInBounds(Vector2Int tilePosition)
        {
            var chunkWidth = pixelSimulation.ChunkDimensions.x;
            var chunkHeight = pixelSimulation.ChunkDimensions.y;

            return 
                tilePosition.x >= 0 && tilePosition.x < chunkWidth && 
                tilePosition.y >= 0 && tilePosition.y < chunkHeight;
        }

        public bool TileExistsAt(Vector2Int tilePosition)
        {
            var alreadyExistsTileAt = tiles[tilePosition.x, tilePosition.y] != null;

            return alreadyExistsTileAt;
        }

        public Tile GetNeighborOfTile(Tile tile, Side side)
        {
            var tilePosition = tilePositions[tile];
            var chunkWidth = pixelSimulation.ChunkDimensions.x;
            var chunkHeight = pixelSimulation.ChunkDimensions.y;
            
            var offset = side.SideToVector();
            var targetPosition = tilePosition + offset;

            Tile targetTile;
            if (TileIsInBounds(targetPosition))
            {
                targetTile = tiles[targetPosition.x, targetPosition.y];
            }
            else
            {
                var neighborChunk = neighbors[side];
                if (neighborChunk == null) return new StaticTile(Color.black);
                
                var newTargetPosition = targetPosition;

                if (side == Side.Left || side == Side.Right)
                {
                    newTargetPosition.x = (chunkWidth - 1) - tilePosition.x;
                } 
                else if (side == Side.Top || side == Side.Bottom)
                {
                    newTargetPosition.y = (chunkHeight - 1) - tilePosition.y;
                }

                targetTile = neighborChunk.tiles[newTargetPosition.x, newTargetPosition.y];
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
                if (chunk.needsApply)
                {
                    chunk.texture.Apply();
                    chunk.needsApply = false;
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
                foreach (var tile in chunk.tiles)
                {
                    if (tile == null) continue;
                    
                    if (tile is Tile.IUpdate updateTile)
                    {
                        if (tile.LastUpdateFrame == currentFrame) continue;

                        updateTile.Update();
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
            var chunkPosition = chunk.position;
            var tilePosition = chunk.tilePositions[tile];

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
            var chunkPosition = chunk.position;
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

                    var chunk = new Chunk()
                    {
                        position = new Vector2Int(chunkColumn, chunkRow),
                        tiles = tiles,
                        texture = texture,
                        renderer = renderer,
                        pixelSimulation = this
                    };

                    _chunks[chunkColumn, chunkRow] = chunk;
                }
            }

            foreach (var chunk in _chunks)
            {
                var neighbors = GetNeighborsOfChunk(chunk);

                chunk.neighbors = neighbors;
            }

            _previousChunkDimensions = _chunkDimensions;
            _previousAmountOfChunks = _amountOfChunks;
            _previousPixelsPerUnit = _pixelsPerUnit;
        }
    }
}