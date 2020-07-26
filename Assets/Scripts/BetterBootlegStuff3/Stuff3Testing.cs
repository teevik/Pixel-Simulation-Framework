using System;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public static Gradient gradient = new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(208f/255f, 191f/255f, 146f/255f), 0f), 
                new GradientColorKey(new Color(222f/255f, 205f/255f, 159f/255f), 1f)
            }
        };
        
        public bool DefaultIsStale => false;
        public Color DefaultColor => _color;

        private readonly Color _color;

        public SandTile(Color color)
        {
            _color = gradient.Evaluate(Random.value);
        }

        public TileUpdateResult? Update(TileUpdateApi api)
        {
            Side? wantedMovement = null;
            bool? wantToGoStale = null;
            
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
                    wantToGoStale = true;
                }
            }
            
            return new TileUpdateResult
            {
                WantedMovement = wantedMovement,
                WantToGoStale = wantToGoStale
            };
        }
    }
    
    public class WaterTile : ITile
    {
        public static Gradient gradient = new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(64f/255f, 167f/255f, 218f/255f), 0f), 
                new GradientColorKey(new Color(81f/255f, 181f/255f, 233f/255f), 1f)
            }
        };

        public bool DefaultIsStale => false;
        public Color DefaultColor => _color;

        private readonly Color _color;

        public WaterTile(Color color)
        {
            _color = gradient.Evaluate(Random.value);
        }

        private int framesSinceChangedColor = 0;

        public TileUpdateResult? Update(TileUpdateApi api)
        {
            Side? wantedMovement = null;
            Color? wantedColor = null;
            bool? wantToGoStale = null;

            framesSinceChangedColor += 1;
            if (framesSinceChangedColor > 10)
            {
                framesSinceChangedColor = 0;
                wantedColor = gradient.Evaluate(Random.value);
            }
            
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
                    else
                    {
                        wantToGoStale = true;
                    }
                }
            }
            
            return new TileUpdateResult
            {
                WantedMovement = wantedMovement,
                WantedColor = wantedColor,
                WantToGoStale = wantToGoStale
            };
        }
    }
}