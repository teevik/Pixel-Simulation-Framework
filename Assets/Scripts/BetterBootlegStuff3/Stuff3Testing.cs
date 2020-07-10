﻿using System;
using UnityEngine;

namespace BetterBootlegStuff3
{
    [RequireComponent(typeof(PixelSimulation))]
    public class Stuff3Testing : MonoBehaviour
    {
        private PixelSimulation _pixelSimulation;

        private void Awake()
        {
            _pixelSimulation = GetComponent<PixelSimulation>();
        }

        private void Update()
        {
            var mousePosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var globalTilePosition = _pixelSimulation.GlobalTilePositionAtWorldPosition(mousePosition);
            
            if (Input.GetMouseButton(0))
            {
                for (var x = -1; x < 2; x++)
                {
                    for (var y = -1; y < 2; y++)
                    {
                        var offset = new Vector2Int(x, y);
                        var offsetedGlobalTilePosition = globalTilePosition + offset;
                        
                        if (_pixelSimulation.FindTilePosition(offsetedGlobalTilePosition, out var chunk, out var tilePosition))
                        {
                            if (!chunk.TileExistsAt(tilePosition)) chunk.InstantiateTile(new StaticTile(Color.gray), tilePosition);
                        }
                    }
                }
                
            }
            else if (Input.GetMouseButton(1))
            {
                if (_pixelSimulation.FindTilePosition(globalTilePosition, out var chunk, out var tilePosition))
                {
                    if (!chunk.TileExistsAt(tilePosition)) chunk.InstantiateTile(new SandTile(Color.yellow), tilePosition);
                }
            }
            else if (Input.GetMouseButton(2))
            {
                if (_pixelSimulation.FindTilePosition(globalTilePosition, out var chunk, out var tilePosition))
                {
                    if (!chunk.TileExistsAt(tilePosition)) chunk.InstantiateTile(new WaterTile(Color.cyan), tilePosition);
                }
            }
        }
    }

    public class StaticTile : ITile
    {
        public bool DefaultIsStale => true;
        public Color DefaultColor => _color;

        private readonly Color _color;

        public StaticTile(Color color)
        {
            _color = color;
        }

        public TileUpdateResult? Update(TileUpdateApi api) => null;
    }
    
    public class SandTile : ITile
    {
        public bool DefaultIsStale => false;
        public Color DefaultColor => _color;

        private readonly Color _color;

        public SandTile(Color color)
        {
            _color = color;
        }

        public TileUpdateResult? Update(TileUpdateApi api)
        {
            Side? wantedMovement = null;
            
            if (!api.TileExistsAt(Side.South))
            {
                wantedMovement = Side.South;
            }
            else
            {
                var randomBool = UnityEngine.Random.value > 0.5f;
                var randomCornerSide1 = randomBool ? Side.SouthWest : Side.SouthEast;
                var randomCornerSide2 = randomBool ? Side.SouthEast : Side.SouthWest;
                
                if (!api.TileExistsAt(randomCornerSide1))
                {
                    wantedMovement = randomCornerSide1;
                } else if (!api.TileExistsAt(randomCornerSide2))
                {
                    wantedMovement = randomCornerSide2;
                }
            }
            
            return new TileUpdateResult
            {
                WantedMovement = wantedMovement
            };
        }
    }
    
    public class WaterTile : ITile
    {
        public bool DefaultIsStale => false;
        public Color DefaultColor => _color;

        private readonly Color _color;

        public WaterTile(Color color)
        {
            _color = color;
        }

        public TileUpdateResult? Update(TileUpdateApi api)
        {
            Side? wantedMovement = null;
            
            if (!api.TileExistsAt(Side.South))
            {
                wantedMovement = Side.South;
            }
            else
            {
                var randomBool = UnityEngine.Random.value > 0.5f;
                var randomCornerSide1 = randomBool ? Side.SouthWest : Side.SouthEast;
                var randomCornerSide2 = randomBool ? Side.SouthEast : Side.SouthWest;
                
                if (!api.TileExistsAt(randomCornerSide1))
                {
                    wantedMovement = randomCornerSide1;
                } else if (!api.TileExistsAt(randomCornerSide2))
                {
                    wantedMovement = randomCornerSide2;
                }
                else
                {
                    var randomSide1 = randomBool ? Side.West : Side.East;
                    var randomSide2 = randomBool ? Side.East : Side.West;

                    if (!api.TileExistsAt(randomSide1))
                    {
                        wantedMovement = randomSide1;
                    } else if (!api.TileExistsAt(randomSide2))
                    {
                        wantedMovement = randomSide2;
                    }
                }
            }
            
            return new TileUpdateResult
            {
                WantedMovement = wantedMovement
            };
        }
    }
}