using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterererBootlegStuff
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
    
    public interface ITile
    {
        Color GetColor();
    }

    // public interface IWaterFlowable
    // {
    //     
    // }

    public class EmptyTile : ITile
    {
        private static Gradient colorGradient = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.blue, 0f), 
                new GradientColorKey(Color.blue, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 1f), 
            }
        };
        
        public Color GetColor()
        {
            return colorGradient.Evaluate(liquid);
        }

        public int lastUpdateFrame;
        public float liquid;
        public float newLiquid;

        public bool isSettled = false;
        public int settleCount;

        public void UnSettle()
        {
            isSettled = false;
            settleCount = 0;
        }

        public void Settle()
        {
            isSettled = true;
        }
        
        public EmptyTile(float liquid = 0f)
        {
            this.liquid = liquid;
            this.newLiquid = liquid;
        }
    }
    
    public class StaticTile : ITile
    {
        private Color _color; 
        
        public StaticTile(Color color)
        {
            _color = color;
        }
        
        public Color GetColor() => _color;
    }
    
    
    [ExecuteAlways]
    public class NewestPixelSimulation : MonoBehaviour
    {
        [Tooltip("Dimensions of each chunk, in pixels")]
        [SerializeField] private Vector2Int _chunkDimensions;
        
        [Tooltip("Amount of chunks")]
        [SerializeField] private Vector2Int _amountOfChunks;
        
        [Tooltip("Scaling factor between pixels and world units")]
        [SerializeField] private float _pixelsPerUnit = 100;
        
        [Tooltip("Shader to use for rendering")]
        [SerializeField] private Shader _shader;
        
        [SerializeField] private Renderer _rendererPrefab;
        
        private Vector2Int _previousChunkDimensions;
        private Vector2Int _previousAmountOfChunks;
        private float _previousPixelsPerUnit;

        private ITile[,] _tiles;
        private Renderer _renderer;
        private Texture2D _texture;
        private bool needsApply = false;
        
        private Vector2Int GetGridSize() => _amountOfChunks * _chunkDimensions;

        private void Awake()
        {
            Reset();
        }

        // private ITile[] GetNeighbors()
        // {
        //     
        // }

        private HashSet<ValueTuple<Vector2Int, TileType>> GetNeighborsOfType<TileType>(Vector2Int position) where TileType : ITile
        {
            var neighborsOfType = new HashSet<ValueTuple<Vector2Int, TileType>>();
            
            for (var x = -1; x < 2; x++)
            {
                for (var y = -1; y < 2; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var offset = new Vector2Int(x, y);
                    var targetPosition = position + offset;

                    if (IsOutOfBounds(targetPosition)) continue;
                    
                    var targetTile = _tiles[targetPosition.x, targetPosition.y];
                    if (targetTile is TileType targetTileOfType)
                    {
                        neighborsOfType.Add((targetPosition, targetTileOfType));
                    }
                }
            }
        
            return neighborsOfType;
        }
        
        private Dictionary<Side, TileType> GetSideNeighborsOfType<TileType>(Vector2Int position) where TileType : class, ITile
        {
            var neighbors = new Dictionary<Side, TileType>();
            
            foreach (Side side in Enum.GetValues(typeof(Side)))
            {
                var offset = side.SideToVector();
                var targetPosition = position + offset;

                TileType targetTile = null;

                if (!IsOutOfBounds(targetPosition))
                {
                    var atargetTile = _tiles[targetPosition.x, targetPosition.y];
                    if (atargetTile is TileType targetTileOfType)
                    {
                        targetTile = targetTileOfType;
                    }
                }
                
                
                neighbors.Add(side, targetTile);
            }

            return neighbors;
        }

        private Vector2Int[] GetNeighborPositions(Vector2Int position)
        {
            var neighborPositions = new Vector2Int[8];

            var index = 0;
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    if (x == 1 && y == 1) continue;

                    var offset = new Vector2Int(x - 1, y - 1);
                    var targetPosition = position + offset;
                    
                    if (IsOutOfBounds(targetPosition)) continue;

                    neighborPositions[index] = targetPosition;
                    index += 1;
                }
            }

            return neighborPositions;
        }

        private bool IsOutOfBounds(Vector2Int position)
        {
            var gridSize = GetGridSize();
            
            return 
                position.x < 0 || position.x >= gridSize.x ||
                position.y < 0 || position.y >= gridSize.y;
        }
        
        private void Update()
        {
            ResetIfPropsChanged();

            // if (Time.frameCount % 10 != 0) return;
            
            for (int x = 0; x < _tiles.GetLength(0); x++)
            {
                for (int y = 0; y < _tiles.GetLength(1); y++)
                {
                    var tile = _tiles[x, y];

                    if (tile is EmptyTile emptyTile)
                    {
                        var waterTileNeighbors = GetSideNeighborsOfType<EmptyTile>(new Vector2Int(x, y));
                        
                        var maxLiquid = 1.0f;
                        var minLiquid = 0.005f;
                        float maxCompression = 0.25f;
                        float minFlow = 0.005f;
                        float maxFlow = 4f;
                        float flowSpeed = 1f;
                        
                        float CalculateVerticalFlowValue(float remainingLiquid, EmptyTile destination)
                        {
                            float sum = remainingLiquid + destination.liquid;
                            float value = 0;

                            if (sum <= maxLiquid) {
                                value = maxLiquid;
                            } else if (sum < 2 * maxLiquid + maxCompression) {
                                value = (maxLiquid * maxLiquid + sum * maxCompression) / (maxLiquid + maxCompression);
                            } else {
                                value = (sum + maxCompression) / 2f;
                            }

                            return value;
                        }

                        
                        var currentFrame = Time.frameCount;
                        if (emptyTile.lastUpdateFrame != currentFrame)
                        {
                            emptyTile.liquid = emptyTile.newLiquid;
                            emptyTile.lastUpdateFrame = currentFrame;

                            if (emptyTile.liquid < minLiquid)
                            {
                                emptyTile.liquid = 0;
                                emptyTile.UnSettle();
                            }
                        }
                        
                        if (emptyTile.liquid == 0) continue;
                        if (emptyTile.isSettled) continue;


                        // foreach (var a in waterTileNeighbors)
                        // {
                        //     var neighbor = a.Value;
                        //     if (neighbor == null) continue;
                        //
                        //     if (neighbor.lastUpdateFrame != currentFrame)
                        //     {
                        //         neighbor.liquid = neighbor.newLiquid;
                        //         neighbor.lastUpdateFrame = currentFrame;
                        //     }
                        // }    
                        
                        float startValue = emptyTile.liquid;
                        float remainingValue = emptyTile.liquid;
                        float flow = 0;

                        var bottomTile = waterTileNeighbors[Side.Bottom];
                        var rightTile = waterTileNeighbors[Side.Right];
                        var leftTile = waterTileNeighbors[Side.Left];
                        var topTile = waterTileNeighbors[Side.Top];


                        if (bottomTile != null)
                        {
                            flow = CalculateVerticalFlowValue(emptyTile.liquid, bottomTile) - bottomTile.liquid;
                            if (bottomTile.liquid > 0 && flow > minFlow)
                                flow *= flowSpeed; 

                            // Constrain flow
                            flow = Mathf.Max (flow, 0);
                            if (flow > Mathf.Min(maxFlow, emptyTile.liquid)) 
                                flow = Mathf.Min(maxFlow, emptyTile.liquid);

                            // Update temp values
                            if (flow != 0) {
                                remainingValue -= flow;
                                emptyTile.newLiquid -= flow;
                                bottomTile.newLiquid += flow;
                                bottomTile.UnSettle();
                            }
                        }
                        
                        if (remainingValue < minLiquid) {
                            emptyTile.newLiquid -= remainingValue;
                            continue;
                        }
                        
                        if (leftTile != null) {

					        // Calculate flow rate
					        flow = (remainingValue - leftTile.liquid) / 4f;
					        if (flow > minFlow)
						        flow *= flowSpeed;

					        // constrain flow
					        flow = Mathf.Max (flow, 0);
					        if (flow > Mathf.Min(maxFlow, remainingValue)) 
						        flow = Mathf.Min(maxFlow, remainingValue);

					        // Adjust temp values
					        if (flow != 0) {
						        remainingValue -= flow;
						        emptyTile.newLiquid -= flow;
						        leftTile.newLiquid += flow;
						        leftTile.UnSettle();
					        } 
				        }

				        // Check to ensure we still have liquid in this cell
				        if (remainingValue < minLiquid) {
					        emptyTile.liquid -= remainingValue;
					        continue;
				        }
				        
				        // Flow to right cell
				        if (rightTile != null) {

					        // calc flow rate
					        flow = (remainingValue - rightTile.liquid) / 3f;										
					        if (flow > minFlow)
						        flow *= flowSpeed; 

					        // constrain flow
					        flow = Mathf.Max (flow, 0);
					        if (flow > Mathf.Min(maxFlow, remainingValue)) 
						        flow = Mathf.Min(maxFlow, remainingValue);
					        
					        // Adjust temp values
					        if (flow != 0) {
						        remainingValue -= flow;
						        emptyTile.newLiquid -= flow;
						        rightTile.newLiquid += flow;
						        rightTile.UnSettle();
					        } 
				        }

				        // Check to ensure we still have liquid in this cell
				        if (remainingValue < minLiquid) {
					        emptyTile.liquid -= remainingValue;
					        continue;
				        }
				        
				        // Flow to Top cell
				        if (topTile != null) {

					        flow = remainingValue - CalculateVerticalFlowValue (remainingValue, topTile); 
					        if (flow > minFlow)
						        flow *= flowSpeed; 

					        // constrain flow
					        flow = Mathf.Max (flow, 0);
					        if (flow > Mathf.Min(maxFlow, remainingValue)) 
						        flow = Mathf.Min(maxFlow, remainingValue);

					        // Adjust values
					        if (flow != 0) {
						        remainingValue -= flow;
						        emptyTile.newLiquid -= flow;
						        topTile.newLiquid += flow;
						        topTile.UnSettle();
					        } 
				        }

				        // Check to ensure we still have liquid in this cell
				        if (remainingValue < minLiquid) {
					        emptyTile.liquid -= remainingValue;
					        continue;
				        }
                        
                        if (startValue == remainingValue) {
                            emptyTile.settleCount++;
                            if (emptyTile.settleCount >= 10) {
                                emptyTile.Settle();
                            }
                        } else {
                            foreach (var a in waterTileNeighbors)
                            {
                                var neighbor = a.Value;
                                    
                                if (neighbor == null) continue;
                                neighbor.UnSettle();
                            }
                        }


                        // foreach (var a in waterTileNeighbors)
                        // {
                        //     var neighborSide = a.Key;
                        //     var neighbor = a.Value;
                        //         
                        //     
                        // }        


                        
                        //
                        // foreach (var (neighborPosition, neighbor) in waterTileNeighbors)
                        // {
                        //     if (neighbor.lastUpdateFrame != currentFrame)
                        //     {
                        //         neighbor.pressure = neighbor.newPressure;
                        //         neighbor.lastUpdateFrame = currentFrame;
                        //     }
                        // }    
                        //
                        // foreach (var (neighborPosition, neighbor) in waterTileNeighbors)
                        // {
                        //     float Flow;
                        //     
                        //     if ( neighborPosition.y > y )
                        //     {
                        //         if ( ( emptyTile.mass < EmptyTile.maxMass ) ||
                        //              ( neighbor.mass < EmptyTile.maxMass ) )
                        //         {
                        //             Flow = emptyTile.mass - EmptyTile.maxMass;
                        //         }
                        //         else
                        //         {
                        //             Flow = emptyTile.mass - neighbor.mass - EmptyTile.maxCompress;
                        //             Flow *= 0.5f;
                        //         }
                        //     }
                        //     else if ( neighborPosition.y < y )
                        //     {
                        //         if ( ( emptyTile.mass < EmptyTile.maxMass ) ||
                        //              ( neighbor.mass < EmptyTile.maxMass ) )
                        //         {
                        //             Flow = EmptyTile.maxMass - neighbor.mass;
                        //         }
                        //         else
                        //         {
                        //             Flow = emptyTile.mass - neighbor.mass + EmptyTile.maxCompress;
                        //             Flow *= 0.5f; 
                        //         }
                        //     }
                        //     else // neighbour is on same level
                        //     {
                        //         Flow = ( emptyTile.mass - neighbor.mass ) * 0.5f;
                        //     }
                        //     
                        //     var deltaPressure = emptyTile.pressure - neighbor.pressure;
                        //     var flow = Flow * deltaPressure;
                        //     flow = Mathf.Clamp(flow, emptyTile.pressure / 6f, -neighbor.pressure / 6f);
                        //     
                        //     emptyTile.newPressure -= flow;
                        //     neighbor.newPressure += flow;
                        // }
                    }
                    
                    var color = tile.GetColor();

                    if (color != _texture.GetPixel(x, y))
                    {
                        _texture.SetPixel(x, y, color);
                        needsApply = true;
                    }
                }
            }
        }
        
        private void LateUpdate()
        {
            // for (int x = 0; x < _tiles.GetLength(0); x++)
            // {
            //     for (int y = 0; y < _tiles.GetLength(1); y++)
            //     {
            //         var tile = _tiles[x, y];
            //         var color = tile.GetColor();
            //
            //         if (color != _texture.GetPixel(x, y))
            //         {
            //             _texture.SetPixel(x, y, color);
            //             needsApply = true;
            //         }
            //     }
            // }

            if (needsApply)
            {
                _texture.Apply();
                needsApply = false;
            }
            
            // foreach (var chunk in _chunks)
            // {
            //     if (chunk.needsApply)
            //     {
            //         chunk.texture.Apply();
            //         chunk.needsApply = false;
            //     }
            // }
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

        private void ClearGrid()
        {
            if (_renderer != null)
            {
                GameObject.DestroyImmediate(_renderer);
                _renderer = null;
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Renderer"))
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }
        }
        
        private void InitializeGrid()
        {
            var gridSize = GetGridSize();
            var worldGridSize = (Vector2)gridSize / _pixelsPerUnit;

            _tiles = new ITile[gridSize.x, gridSize.y];

            for (var column = 0; column < gridSize.x; column++)
            {
                for (var row = 0; row < gridSize.y; row++)
                {
                    _tiles[column, row] = new EmptyTile();
                }
            }
            
            var texture = new Texture2D(gridSize.x, gridSize.y);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            Color[] colors = new Color[texture.width * texture.height];
            texture.SetPixels(colors);
            texture.Apply();
            
            var material = new Material(_shader);
            material.mainTexture = texture;
            
            var renderer = Instantiate(_rendererPrefab);
            renderer.name = "Renderer";
            renderer.transform.localScale = new Vector3(worldGridSize.x, worldGridSize.y, 1);
            renderer.transform.position = new Vector3(
                worldGridSize.x/2,
                worldGridSize.y/2,
                0);
            renderer.transform.SetParent(transform, false);
            renderer.sharedMaterial = material;

            this._texture = texture;
            this._renderer = renderer;
                
                
            _previousChunkDimensions = _chunkDimensions;
            _previousAmountOfChunks = _amountOfChunks;
            _previousPixelsPerUnit = _pixelsPerUnit;
        }

        
        public void Reset()
        {
            ClearGrid();
            InitializeGrid();
        }
        
        public void SetTileTo(Vector2Int position, ITile tile)
        {
            _tiles[position.x, position.y] = tile;
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
    }
}