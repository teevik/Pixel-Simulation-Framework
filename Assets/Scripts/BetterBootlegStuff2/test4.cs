﻿using System;
using System.Collections.Generic;
using System.Linq;
using BetterBootlegStuff;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BetterBootlegStuff2
{
    [RequireComponent(typeof(BetterBootlegStuff2.PixelSimulation))]
    public class test4 : MonoBehaviour
    {
        private BetterBootlegStuff2.PixelSimulation _pixelSimulation;

        private void Awake()
        {
            _pixelSimulation = GetComponent<BetterBootlegStuff2.PixelSimulation>();
        }

        private void Update()
        {
            var mousePosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var gridPosition = _pixelSimulation.GridPositionAtWorldPosition(mousePosition);

            if (!_pixelSimulation.FindTilePosition(gridPosition, out var chunk, out var tilePosition)) return;
            
            if (Input.GetMouseButton(0))
            {
                if (chunk.TileExistsAt(tilePosition)) return;

                chunk.AddTile(new BetterBootlegStuff2.StaticTile(Color.gray), tilePosition);
            }
            else if (Input.GetMouseButton(1))
            {
                // if (chunk.TileExistsAt(tilePosition)) return;
                // chunk.AddTile(new ParticleTile(Vector2.zero), tilePosition);
            
                // if (chunk.TileExistsAt(tilePosition)) return;
                // chunk.AddTile(new WaterTile(), tilePosition);
            
                if (chunk.TileExistsAt(tilePosition)) return;
    
                chunk.AddTile(new SandTile(Color.yellow), tilePosition);
            }
        }
    }

    public class SandTile : BetterBootlegStuff2.Tile
    {
        private const int maxTimeBeingStale = 1;
        
        public SandTile(Color color) : base(color)
        {
        }

        // private float timeBeingStale = 0;

        public override void Update(float deltaTime)
        {
            if (!Parent.NeighborExistsAt(this, BetterBootlegStuff2.Side.South))
            {
                Parent.MoveTileRelative(this, BetterBootlegStuff2.Side.South.SideToVector());
                // timeBeingStale = 0;
            }
            else
            {
                var randomBool = UnityEngine.Random.value > 0.5f;

                var randomCornerSide1 = randomBool ? BetterBootlegStuff2.Side.SouthWest : BetterBootlegStuff2.Side.SouthEast;
                var randomCornerSide2 = randomBool ? BetterBootlegStuff2.Side.SouthEast : BetterBootlegStuff2.Side.SouthWest;

                if (!Parent.NeighborExistsAt(this, randomCornerSide1))
                {
                    Parent.MoveTileRelative(this, randomCornerSide1.SideToVector());
                } else if (!Parent.NeighborExistsAt(this, randomCornerSide2))
                {
                    Parent.MoveTileRelative(this, randomCornerSide2.SideToVector());
                }
            }


            // else
            // {
            //     timeBeingStale += Time.deltaTime;
            // }
            //
            // if (timeBeingStale > maxTimeBeingStale)
            // {
            //     Parent.SwitchTile(this, new BetterBootlegStuff2.StaticTile(Color));
            // }
        }
    }

    public class ParticleTile : BetterBootlegStuff2.Tile
    {
        private const float gravity = 50f;
        private Vector2 _velocity;
        private Vector2 relativePositionWanted = Vector2.zero;

        public ParticleTile(Vector2 velocity) : base(Color.yellow)
        {
            _velocity = velocity;
        }

        public override void Update(float deltaTime)
        {
            _velocity.y -= gravity * Time.deltaTime;
            _velocity *= 1f - 0.1f * Time.deltaTime;

            relativePositionWanted += _velocity * Time.deltaTime;

            bool IsClearLeft() => !Parent.NeighborExistsAt(this, BetterBootlegStuff2.Side.West);
            bool IsClearRight() => !Parent.NeighborExistsAt(this, BetterBootlegStuff2.Side.East);
            bool IsClearBottom() => !Parent.NeighborExistsAt(this, BetterBootlegStuff2.Side.South);
            bool IsClearTop() => !Parent.NeighborExistsAt(this, BetterBootlegStuff2.Side.North);

            if (_velocity.y < 0 && !IsClearBottom())
            {
                var bottomTile = Parent.GetNeighborOfTile(this, BetterBootlegStuff2.Side.South);
                if (bottomTile is BetterBootlegStuff2.StaticTile || bottomTile is SandTile)
                {
                    if (IsClearLeft() && IsClearRight())
                    {
                        if (Random.Range(0f,1f) > 0.5f) Parent.MoveTileRelative(this, Vector2Int.left);
                        else Parent.MoveTileRelative(this, Vector2Int.right);
                    }
                    else if (IsClearLeft())
                    {
                        Parent.MoveTileRelative(this, Vector2Int.left);
                    } 
                    else if (IsClearRight())
                    {
                        Parent.MoveTileRelative(this, Vector2Int.right);
                    }

                    if (!IsClearBottom())
                    {
                        Parent.SwitchTile(this, new SandTile(Color));
                    }
                
                    return;
                }
            }

            if (relativePositionWanted.x >= 1)
            {
                if (IsClearRight())
                {
                    Parent.MoveTileRelative(this, Vector2Int.right);
                }
                relativePositionWanted.x -= 1;
            } 
            else if (relativePositionWanted.x <= -1)
            {
                if (IsClearLeft())
                {
                    Parent.MoveTileRelative(this, Vector2Int.left);
                }
                relativePositionWanted.x += 1;
            }
        
            if (relativePositionWanted.y >= 1)
            {
                if (IsClearTop())
                {
                    Parent.MoveTileRelative(this, Vector2Int.up);
                }
                relativePositionWanted.y -= 1;
            } 
            else if (relativePositionWanted.y <= -1)
            {
                if (IsClearBottom())
                {
                    Parent.MoveTileRelative(this, Vector2Int.down);
                }
            
                relativePositionWanted.y += 1;
            }
        }
    }

// public class WaterTile : Tile, Tile.IStart, Tile.IUpdate
// {
//     private bool _isBeingUpdatedByOther = false;
//     private HashSet<WaterTile> _connectedWaterTiles = null;
//     
//     public WaterTile() : base(Color.blue)
//     {
//     }
//
//     // private Dictionary<WaterTile, Vector2Int> FloodFill(WaterTile tile)
//     // {
//     //     var connectedWaterTiles = new Dictionary<WaterTile, Vector2Int>();
//     //     connectedWaterTiles.Add(tile, chunk.pixelSimulation.GetGridPositionOfTile(tile));
//     //     
//     //     return FloodFill(tile, connectedWaterTiles);
//     // }
//     
//     // private Dictionary<WaterTile, Vector2Int> FloodFill(WaterTile tile, Dictionary<WaterTile, Vector2Int> connectedWaterTiles)
//     // {
//     //     var neighbors = tile.chunk.GetNeighborsOfTile(tile);
//     //
//     //     foreach (var neighbor in neighbors)
//     //     {
//     //         var neighborTile = neighbor.Value;
//     //
//     //         if (neighborTile is WaterTile neighborWaterTile)
//     //         {
//     //             if (neighborWaterTile.isBeingUpdatedByOther == false && !connectedWaterTiles.ContainsKey(neighborWaterTile))
//     //             {
//     //                 neighborWaterTile.isBeingUpdatedByOther = true;
//     //                 connectedWaterTiles.Add(neighborWaterTile, chunk.pixelSimulation.GetGridPositionOfTile(neighborWaterTile));
//     //                 FloodFill(neighborWaterTile, connectedWaterTiles);
//     //             }
//     //         }
//     //     }
//     //
//     //     return connectedWaterTiles;
//     // }
//
//     private void FindHighestTile(Dictionary<WaterTile, Vector2Int> connectedWaterTiles, out WaterTile highestTile, out Vector2Int highestPosition)
//     {
//         highestTile = null;
//         highestPosition = default;
//
//         foreach (var waterTileWithGridPosition in connectedWaterTiles)
//         {
//             var waterTile = waterTileWithGridPosition.Key;
//             var gridPosition = waterTileWithGridPosition.Value;
//
//             if (highestTile == null)
//             {
//                 highestTile = waterTile;
//                 highestPosition = gridPosition;
//                 continue;
//             }
//             
//             if (gridPosition.y > highestPosition.y)
//             {
//                 highestTile = waterTile;
//                 highestPosition = gridPosition;
//             }
//         }
//     }
//
//     public void Start()
//     { 
//         var neighbors = chunk.GetNeighborsOfTile(this);
//         
//         var neighboringConnectedWaterTiles = new HashSet<HashSet<WaterTile>>();
//
//         foreach (var neighbor in neighbors)
//         {
//             var neighborSide = neighbor.Key;
//             var neighborTile = neighbor.Value;
//
//             if (neighborTile is WaterTile neighborWaterTile)
//             {
//                 if (neighborWaterTile._connectedWaterTiles != null)
//                 {
//                     neighboringConnectedWaterTiles.Add(neighborWaterTile._connectedWaterTiles);
//                 }
//                 // if (neighborWaterTile._connectedWaterTiles != null)
//                 // {
//                 //     if (this._connectedWaterTiles == null)
//                 //     {
//                 //         this._connectedWaterTiles = neighborWaterTile._connectedWaterTiles;
//                 //         this._connectedWaterTiles.Add(this);
//                 //     }
//                 //     else
//                 //     {
//                 //         if (this._connectedWaterTiles != neighborWaterTile)
//                 //     }
//                 // }
//                 // else
//                 // {
//                 //     if (this._connectedWaterTiles == null)
//                 //     {
//                 //         var newConnectedWaterTiles = new HashSet<WaterTile>();
//                 //         newConnectedWaterTiles.Add(this);
//                 //         newConnectedWaterTiles.Add(neighborWaterTile);
//                 //
//                 //         this._connectedWaterTiles = newConnectedWaterTiles;
//                 //         neighborWaterTile._connectedWaterTiles = newConnectedWaterTiles;
//                 //     }
//                 //     else
//                 //     {
//                 //         
//                 //     }
//                 // }
//             }
//         }
//
//         if (neighboringConnectedWaterTiles.Count > 1)
//         {
//             
//         }
//         else if (neighboringConnectedWaterTiles.Count == 1)
//         {
//             var neighboringConnectedWaterTile = neighboringConnectedWaterTiles.First();
//
//             if (this._connectedWaterTiles != null && this._connectedWaterTiles != neighboringConnectedWaterTile)
//             {
//                 this._co
//             }
//             
//             this._connectedWaterTiles = neighboringConnectedWaterTile;
//             this._connectedWaterTiles.Add(this);
//         }
//         else
//         {
//             
//         }
//     }
//
//     public void Update()
//     {
//         if (_isBeingUpdatedByOther)
//         {
//             _isBeingUpdatedByOther = false;
//             return;
//         }
//
//         var connectedWaterTiles = FloodFill(this);
//         
//         if (connectedWaterTiles.Count == 1)
//         {
//             if (!chunk.NeighborExistsAt(this, Side.Bottom))
//             {
//                 chunk.MoveTileRelative(this, Vector2Int.down);
//             }
//             return;
//         }
//
//         FindHighestTile(connectedWaterTiles, out var highestTile, out var highestPosition);
//
//         foreach (var lowestNeighbor in connectedWaterTiles.OrderBy(waterTile => waterTile.Value.y).ThenBy(_ => Random.value))
//         {
//             var lowestTile = lowestNeighbor.Key;
//             var lowestTilePosition = lowestNeighbor.Value;
//             var lowestTileNeighbors = lowestTile.chunk.GetNeighborsOfTile(lowestTile);
//             var neighborBottom = lowestTileNeighbors[Side.Bottom];
//             var neighborLeft = lowestTileNeighbors[Side.Left];
//             var neighborRight = lowestTileNeighbors[Side.Right];
//             var neighborTop = lowestTileNeighbors[Side.Top];
//
//             void MoveToSide(Side side)
//             {
//                 chunk.pixelSimulation.FindTilePosition(lowestTilePosition + side.SideToVector(), out var newPositionChunk, out var tilePosition);
//                 
//                 newPositionChunk.MoveTileToThisChunk(highestTile, tilePosition);
//             }
//
//             if (neighborBottom == null)
//             {
//                 MoveToSide(Side.Bottom);
//                 return;
//             } 
//             else if (neighborLeft == null && neighborRight == null)
//             {
//                 MoveToSide((Random.Range(0f, 1f) > 0.5f) ? Side.Left : Side.Right);
//                 return;
//             }
//             else if (neighborLeft == null)
//             {
//                 MoveToSide(Side.Left);
//                 return;
//
//             } else if (neighborRight == null)
//             {
//                 MoveToSide(Side.Right);
//                 return;
//             } else if (neighborTop == null)
//             {
//                 MoveToSide(Side.Top);
//                 return;
//             }
//         }
//     }
// }
//
}