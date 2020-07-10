using UnityEngine;

namespace BetterBootlegStuff3
{
    public struct TileApi
    {
        
    }

    public interface Tile
    {
        void Update(TileApi tileApi);
    }

    public struct TileData
    {
        public int LastFrameUpdated;
        public Color CurrentColor;
        public bool IsStale;
        public Vector2Int WantedMovement;
    }
}