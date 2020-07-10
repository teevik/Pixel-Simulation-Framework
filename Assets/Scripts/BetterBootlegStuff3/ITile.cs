using UnityEngine;

namespace BetterBootlegStuff3
{
    public struct TileUpdateApi
    {
        public delegate bool TileExistsAtDelegate(Side side);

        public TileExistsAtDelegate TileExistsAt;
    }
    
    public struct TileUpdateResult
    {
        public Color? WantedColor;
        public Side? WantedMovement;
        public bool? WantToGoStale;
    }

    public interface ITile
    {
        Color DefaultColor { get; }
        bool DefaultIsStale { get; }

        TileUpdateResult? Update(TileUpdateApi tileUpdateApi);
    }

    public struct TileData
    {
        public int LastFrameUpdated;
        public Color CurrentColor;
    }
}