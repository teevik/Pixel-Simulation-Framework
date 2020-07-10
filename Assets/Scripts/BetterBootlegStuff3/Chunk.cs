using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterBootlegStuff3
{
    public struct ChunkTiles
    {
        private Tile[,] _tiles;
        private Dictionary<Tile, Vector2Int> _tilePositions;
        private Dictionary<Tile, TileData> _tileDatas;
        private Dictionary<Side, ChunkTiles> _neighbors;

        public ChunkTiles(Vector2Int chunkDimensions, Dictionary<Side, ChunkTiles> neighbors)
        {
            _tiles = new Tile[chunkDimensions.x, chunkDimensions.y];
            _tilePositions = new Dictionary<Tile, Vector2Int>();
            _tileDatas = new Dictionary<Tile, TileData>();
            _neighbors = neighbors;
        }
    }
    
    public class Chunk
    {
        private readonly Texture2D _texture;
        private readonly Vector2Int _chunkDimensions;
        private ChunkTiles _tiles;
        private Dictionary<Side, Chunk> _neighbors;

        public Chunk(Texture2D texture, Vector2Int chunkDimensions)
        {
            _texture = texture;
            _chunkDimensions = chunkDimensions;
        }

        public void SetNeighbors(Dictionary<Side, Chunk> neighbors)
        {
            _neighbors = neighbors;
            _tiles = new ChunkTiles(
                _chunkDimensions,
                neighbors.Select(a => new KeyValuePair<Side, ChunkTiles>(a.Key, a.Value._tiles))
                    .ToDictionary(x => x.Key, x => x.Value));
        }
    }
}