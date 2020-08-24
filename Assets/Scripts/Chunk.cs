using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSimulationFramework
{
    public struct DirtyRect
    {
        public Vector2Int StartTilePosition;
        public Vector2Int EndTilePosition;
    }
    
    public class Chunk
    {
        private readonly Vector2Int _chunkDimensions;
        private readonly Texture2D _texture;
        private Dictionary<Side, Chunk> _neighbors;
        private readonly ITile[,] _tiles;
        private readonly Dictionary<ITile, Vector2Int?> _tilePositions;
        private readonly Dictionary<ITile, TileData?> _tileDatas;
        private readonly HashSet<Vector2Int> _unstaleTilePositions;
        private bool _needsApply = false;
        private DirtyRect? _dirtyRect = null;

        public DirtyRect? DirtyRect => _dirtyRect;
        
        public Chunk(Texture2D texture, Vector2Int chunkDimensions)
        {
            _chunkDimensions = chunkDimensions;
            _texture = texture;
            _tiles = new ITile[chunkDimensions.x, chunkDimensions.y];
            _tilePositions = new Dictionary<ITile, Vector2Int?>();
            _tileDatas = new Dictionary<ITile, TileData?>();
            _unstaleTilePositions = new HashSet<Vector2Int>();
            _dirtyRect = new DirtyRect
            {
                StartTilePosition = Vector2Int.zero, 
                EndTilePosition = Vector2Int.zero
            };
        }
        
        public void FixedUpdate()
        {
            if (_dirtyRect is DirtyRect dirtyRect)
            {
                for (var x = Math.Max(0, dirtyRect.StartTilePosition.x); x < Math.Min(_chunkDimensions.x, dirtyRect.EndTilePosition.x); x++)
                {
                    for (var y = Math.Max(0, dirtyRect.StartTilePosition.y); y < Math.Min(_chunkDimensions.y, dirtyRect.EndTilePosition.y); y++)
                    {
                        var tile = _tiles[x, y];
                        var currentTilePosition = new Vector2Int(x, y);
                        
                        if (tile == null) continue;
                        var tileData = _tileDatas[tile].Value;
                        
                        if (tileData.LastFrameUpdated == Time.frameCount) continue;

                        var newTileData = tileData;
                            newTileData.LastFrameUpdated = Time.frameCount;

                        bool ApiTileExistsAt(Side side)
                        {
                            if (GetNeighboringTilePosition(currentTilePosition, side, out var neighborTilePosition, out var neighborChunk))
                            {
                                return neighborChunk.TileExistsAt(neighborTilePosition);
                            }

                            return true;
                        }
                        
                        ITile ApiGetTileAtSide(Side side)
                        {
                            if (GetNeighboringTilePosition(currentTilePosition, side, out var neighborTilePosition, out var neighborChunk))
                            {
                                if (neighborChunk.TileExistsAt(neighborTilePosition))
                                    return neighborChunk._tiles[neighborTilePosition.x, neighborTilePosition.y];
                            }

                            return null;
                        }
                            
                        var updateResult = tile.Update(new TileUpdateApi
                        {
                            TileExistsAt = ApiTileExistsAt,
                            GetTileAtSide = ApiGetTileAtSide
                        });
                        
                        if (updateResult == null) continue;

                        if (updateResult.Value.WantToGoStale ?? false)
                        {
                            _unstaleTilePositions.Remove(currentTilePosition);
                            _tileDatas[tile] = newTileData;
                        }
                        else
                        {
                            _tileDatas[tile] = newTileData;

                            if (updateResult.Value.WantedMovement != null)
                            {
                                TryMoveTileRelative(currentTilePosition, updateResult.Value.WantedMovement.Value);
                            }
                        }
                    } 
                }
            }
            
            // if (_unstaleTilePositions.Count == 0)
            // {
            //     _dirtyRect = null;
            // }
            // else
            // {
            var startDirtyRectZone = Vector2Int.zero;
            var endDirtyRectZone = Vector2Int.zero;

            if (_unstaleTilePositions.Count > 0)
            {
                startDirtyRectZone = _chunkDimensions;
                endDirtyRectZone = Vector2Int.zero;
                
                foreach (var unstaleTilePosition in _unstaleTilePositions)
                {
                    if (unstaleTilePosition.x > endDirtyRectZone.x) endDirtyRectZone.x = unstaleTilePosition.x;
                    if (unstaleTilePosition.x < startDirtyRectZone.x) startDirtyRectZone.x = unstaleTilePosition.x;
                    if (unstaleTilePosition.y > endDirtyRectZone.y) endDirtyRectZone.y = unstaleTilePosition.y;
                    if (unstaleTilePosition.y < startDirtyRectZone.y) startDirtyRectZone.y = unstaleTilePosition.y;
                }

                startDirtyRectZone -= Vector2Int.one * 2;
                endDirtyRectZone += Vector2Int.one * 2;
            } 


            foreach (var sideNeighborsPair in _neighbors)
            {
                var (neighborSide, neighborChunk) = (sideNeighborsPair.Key, sideNeighborsPair.Value);
            
                var sideAsVector = neighborSide.SideAsVector();
                

                var neighborDirtyRect = neighborChunk._dirtyRect.Value;
                
                var relativeStartTilePosition = neighborDirtyRect.StartTilePosition + (sideAsVector * _chunkDimensions);
                var relativeEndTilePosition = neighborDirtyRect.EndTilePosition + (sideAsVector * _chunkDimensions);

                if (TilePositionIsInBounds(relativeStartTilePosition))
                {
                    
                }

                
                foreach (var dirtyRectPart in new[] { neighborDirtyRect.StartTilePosition, neighborDirtyRect.EndTilePosition })
                {
                    var relativePosition = dirtyRectPart + (sideAsVector * _chunkDimensions);
                    // Debug.Log(relativePosition);

                    // if (dirtyRectPart.x < 0 && sideAsVector.x < 0)
                    // {
                    //     if (dirtyRectPart.y < 0 && sideAsVector.y < 0)
                    //     {
                    //         // Left Up
                    //         if (s)
                    //     } else if (dirtyRectPart.y > _chunkDimensions.y && sideAsVector.y > 0)
                    //     {
                    //         // Left Down
                    //     }
                    //     else
                    //     {
                    //         // Left
                    //     }
                    // }
                    // else if (dirtyRectPart.x > _chunkDimensions.x && sideAsVector.x > 0)
                    // {
                    //     if (dirtyRectPart.y < 0 && sideAsVector.y < 0)
                    //     {
                    //         // Right Up
                    //     } else if (dirtyRectPart.y > _chunkDimensions.y && sideAsVector.y > 0)
                    //     {
                    //         // Right Down
                    //     }
                    //     else
                    //     {
                    //         // Right
                    //     }
                    // }
                    // else
                    // {
                    //     if (dirtyRectPart.y < 0 && sideAsVector.y < 0)
                    //     {
                    //         // Up
                    //     } else if (dirtyRectPart.y > _chunkDimensions.y && sideAsVector.y > 0)
                    //     {
                    //         // Down
                    //     }
                    //     else
                    //     {
                    //         // Center?
                    //     }
                    // }
                }
            }
            
            
            startDirtyRectZone.Clamp(-Vector2Int.one * 2, _chunkDimensions + Vector2Int.one * 2);
            endDirtyRectZone.Clamp(-Vector2Int.one * 2, _chunkDimensions + Vector2Int.one * 2);

            _dirtyRect = new DirtyRect
            {
                StartTilePosition = startDirtyRectZone,
                EndTilePosition = endDirtyRectZone
            };
            // }
        }

        public void LateUpdate()
        {
            if (_needsApply)
            {
                _texture.Apply();
                _needsApply = false;
            }
        }

        private void DrawTile(Vector2Int tilePosition, Color color)
        {
            _texture.SetPixel(tilePosition.x, tilePosition.y, color);
            _needsApply = true;
        }
        
        private void DrawClearTile(Vector2Int tilePosition)
        {
            DrawTile(tilePosition, Color.clear);
        }

        public void SetNeighbors(Dictionary<Side, Chunk> neighbors)
        {
            _neighbors = neighbors;
        }

        public bool TileExistsAt(Vector2Int tilePosition)
        {
            if (!TilePositionIsInBounds(tilePosition)) throw new ArgumentException("Out of bounds", nameof(tilePosition));
            
            var tileExists = _tiles[tilePosition.x, tilePosition.y] != null;
            return tileExists;
        }
        
        public bool TilePositionIsInBounds(Vector2Int tilePosition)
        {
            var chunkWidth = _chunkDimensions.x;
            var chunkHeight = _chunkDimensions.y;

            var tilePositionIsInBounds = 
                tilePosition.x >= 0 && tilePosition.x < chunkWidth && 
                tilePosition.y >= 0 && tilePosition.y < chunkHeight;

            return tilePositionIsInBounds;
        }
        
        public void InstantiateTile(ITile tile, Vector2Int tilePosition)
        {
            if (!TilePositionIsInBounds(tilePosition)) throw new ArgumentException("Out of bounds", nameof(tilePosition));
            if (TileExistsAt(tilePosition)) throw new ArgumentException("Tile already exists here", nameof(tilePosition));

            var tileData = new TileData()
            {
                CurrentColor = tile.DefaultColor,
                LastFrameUpdated = 0
            };
            
            AddTile(tile, tilePosition, tileData);
            DrawTile(tilePosition, tileData.CurrentColor);
        }
        
        private void AddTile(ITile tile, Vector2Int tilePosition, TileData tileData)
        {
            _tiles[tilePosition.x, tilePosition.y] = tile;
            _tilePositions[tile] = tilePosition;
            _tileDatas[tile] = tileData;
            
            if (!tile.DefaultIsStale) _unstaleTilePositions.Add(tilePosition);
            
            DrawTile(tilePosition, tileData.CurrentColor);
        }

        private void RemoveTile(Vector2Int tilePosition)
        {
            var tile = _tiles[tilePosition.x, tilePosition.y];
            _tiles[tilePosition.x, tilePosition.y] = null;
            _tilePositions[tile] = null;
            _tileDatas[tile] = null;
            _unstaleTilePositions.Remove(tilePosition);

            DrawClearTile(tilePosition);
        }

        private bool GetNeighboringTilePosition(Vector2Int tilePosition, Side side, out Vector2Int neighborTilePosition, out Chunk neighborChunk)
        {
            var chunkWidth = _chunkDimensions.x;
            var chunkHeight = _chunkDimensions.y;

            var sideOffset = side.SideAsVector();
            var targetTilePosition = tilePosition + sideOffset;
            
            if (TilePositionIsInBounds(targetTilePosition))
            {
                neighborTilePosition = targetTilePosition;
                neighborChunk = this;
                return true;
            }
            
            var sideDirection = Vector2Int.zero;

            if (targetTilePosition.x >= chunkWidth)
            {
                sideDirection.x = 1;
            } else if (targetTilePosition.x < 0)
            {
                sideDirection.x = -1;
            }
            
            if (targetTilePosition.y >= chunkHeight)
            {
                sideDirection.y = 1;
            } else if (targetTilePosition.y < 0)
            {
                sideDirection.y = -1;
            }

            var chunkNeighborSide = sideDirection.VectorAsSide();
            
            if (!_neighbors.TryGetValue(chunkNeighborSide, out neighborChunk))
            {
                neighborTilePosition = Vector2Int.zero;
                return false;
            }
            
            neighborTilePosition = targetTilePosition;
            
            if (sideDirection.x != 0)
            {
                neighborTilePosition.x = (chunkWidth - 1) - tilePosition.x;
            } 
            if (sideDirection.y != 0)
            {
                neighborTilePosition.y = (chunkHeight - 1) - tilePosition.y;
            }
            
            return true;
        }
        
        public bool NeighborExistsAt(Vector2Int tilePosition, Side side)
        {
            if (!TilePositionIsInBounds(tilePosition)) throw new ArgumentException("Out of bounds", nameof(tilePosition));

            if (GetNeighboringTilePosition(tilePosition, side, out var neighborTilePosition, out var neighborChunk))
            {
                if (neighborChunk.TileExistsAt(neighborTilePosition)) return true;
                else return false;
            }

            return true;
        }

        private void TryMoveTileRelative(Vector2Int tilePosition, Side side)
        {
            var tile = _tiles[tilePosition.x, tilePosition.y];
            var tileData = _tileDatas[tile].Value;

            if (GetNeighboringTilePosition(tilePosition, side, out var neighborTilePosition, out var neighborChunk))
            {
                if (neighborChunk.TileExistsAt(neighborTilePosition))
                {
                    Debug.Log("Tile exists here");
                    return;
                }
                
                RemoveTile(tilePosition);
                
                neighborChunk.AddTile(tile, neighborTilePosition, tileData);
            }
        }

        private void MoveTile(Vector2Int oldTilePosition, Vector2Int newTilePosition)
        {
            var tile = _tiles[oldTilePosition.x, oldTilePosition.y];
            var tileData = _tileDatas[tile].Value;
            
            RemoveTile(oldTilePosition);
            AddTile(tile, newTilePosition, tileData);
        }
    }
}