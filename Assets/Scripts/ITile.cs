using UnityEngine;

namespace PixelSimulationFramework
{
    public struct TileUpdateApi
    {
        public delegate bool TileExistsAtDelegate(Side side);

        public TileExistsAtDelegate TileExistsAt;
        
        public delegate ITile GetTileAtSideDelegate(Side side);

        public GetTileAtSideDelegate GetTileAtSide;
    }
    
    public struct TileUpdateResult
    {
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