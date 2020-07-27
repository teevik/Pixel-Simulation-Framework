using System.Collections.Generic;
using UnityEngine;

namespace PixelSimulationFramework
{
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
        public static readonly PairedDictionary<Side, Vector2Int> sideVectorMap = new PairedDictionary<Side, Vector2Int>()
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
        
        public static Vector2Int SideAsVector(this Side side)
        {
            return sideVectorMap[side];
        }
    
        public static Side VectorAsSide(this Vector2Int vector)
        {
            return sideVectorMap[vector];
        }
    }
}