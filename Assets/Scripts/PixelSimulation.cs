using System;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PixelSimulationFramework
{
    public class PixelSimulation : SerializedMonoBehaviour
    {        
        [DisableIf("@UnityEngine.Application.isPlaying")]
        [ValidateInput("@(_chunkDimensions.x > 0 && _chunkDimensions.y > 0)", "Needs to be larger than 0")]
        [OnValueChanged(nameof(ResetChunkRenderers))]
        [Tooltip("Dimensions of each chunk, in pixels")]
        [SerializeField] private Vector2Int _chunkDimensions;
        
        [DisableIf("@UnityEngine.Application.isPlaying")]
        [ValidateInput("@(_amountOfChunks.x > 0 && _amountOfChunks.y > 0)", "Needs to be larger than 0")]
        [OnValueChanged(nameof(ResetChunkRenderers))]
        [Tooltip("Amount of chunks")]
        [SerializeField] private Vector2Int _amountOfChunks;
        
        [DisableIf("@UnityEngine.Application.isPlaying")]
        [ValidateInput("@(_pixelsPerUnit > 0)", "Needs to be larger than 0")]
        [OnValueChanged(nameof(ResetChunkRenderers))]
        [Tooltip("Scaling factor between pixels and world units")]
        [SerializeField] private float _pixelsPerUnit = 100;
        
        [Required]
        [Tooltip("Shader to use for rendering")]
        [SerializeField] private Shader _shader;

        [Required]
        [OnValueChanged(nameof(ResetChunkRenderers))]
        [Tooltip("Prefab for rendering each chunk")]
        [SerializeField] private Renderer _chunkRendererPrefab;

        [FoldoutGroup("Chunk Renderers")]
        [TableMatrix(HorizontalTitle = "Square Celled Matrix", SquareCells = true)]
        [TableList]
        [SerializeField] private Renderer[,] _chunkRenderers;

        private Chunk[,] _chunks;
        
        private void Awake()
        {
            _chunks = new Chunk[_amountOfChunks.x, _amountOfChunks.y];

            for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
            {
                for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
                {
                    var chunkTexture = _chunkRenderers[chunkColumn, chunkRow].sharedMaterial.mainTexture as Texture2D;
                    
                    _chunks[chunkColumn, chunkRow] = new Chunk(chunkTexture, _chunkDimensions);
                }
            }

            for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
            {
                for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
                {
                    var chunk = _chunks[chunkColumn, chunkRow];
                    var chunkNeighbors = GetNeighborsOfChunk(new Vector2Int(chunkColumn, chunkRow));
                    chunk.SetNeighbors(chunkNeighbors);
                }
            }
        }

        private void FixedUpdate()
        {
            foreach (var chunk in _chunks)
            {
                chunk.FixedUpdate();
            }
        }
        
        private void LateUpdate()
        {
            foreach (var chunk in _chunks)
            {
                chunk.LateUpdate();
            }
        }
        
        public Vector3 WorldPositionAtGlobalTilePosition(Vector2Int globalTilePosition)
        {
            return transform.TransformPoint(new Vector3(globalTilePosition.x / _pixelsPerUnit,
                globalTilePosition.y / _pixelsPerUnit,
                0));
        }
        
        public Vector2Int GlobalTilePositionAtWorldPosition(Vector3 worldPosition)
        {
            var position = transform.InverseTransformPoint(worldPosition);
            var x = Mathf.RoundToInt(position.x * _pixelsPerUnit);
            var y = Mathf.RoundToInt(position.y * _pixelsPerUnit);
            
            return new Vector2Int(x, y);
        }

        public bool FindTilePosition(Vector2Int globalTilePosition, out Chunk chunk, out Vector2Int tilePosition)
        {
            var chunkPosition = ((Vector2) globalTilePosition / (Vector2) _chunkDimensions).FloorToVector2Int();

            if (ChunkIsInBounds(chunkPosition))
            {
                chunk = _chunks[chunkPosition.x, chunkPosition.y];
                tilePosition = globalTilePosition - (chunkPosition * _chunkDimensions);
                return true;
            }

            chunk = null;
            tilePosition = default;
            return false;
        }

        private bool ChunkIsInBounds(Vector2Int chunkPosition)
        {
            return 
                chunkPosition.x >= 0 && chunkPosition.x < _amountOfChunks.x && 
                chunkPosition.y >= 0 && chunkPosition.y < _amountOfChunks.y;
        }
        
        private Dictionary<Side, Chunk> GetNeighborsOfChunk(Vector2Int chunkPosition)
        {
            var chunkNeighbors = new Dictionary<Side, Chunk>();
            
            foreach (Side side in Enum.GetValues(typeof(Side)))
            {
                var offset = side.SideAsVector();
                var targetPosition = chunkPosition + offset;

                if (ChunkIsInBounds(targetPosition))
                {
                    var targetChunk = _chunks[targetPosition.x, targetPosition.y];
                    chunkNeighbors.Add(side, targetChunk);
                }
            }

            return chunkNeighbors;
        }

        [Button]
        private void ResetChunkRenderers()
        {
            if (Application.isPlaying) return;
            if (_amountOfChunks.x <= 0 || _amountOfChunks.y <= 0) return;
            if (_chunkDimensions.x <= 0 || _chunkDimensions.y <= 0) return;

            var worldChunkDimensions = (Vector2)_chunkDimensions / _pixelsPerUnit;

            if (_chunkRenderers != null)
            {
                foreach (var chunkRenderer in _chunkRenderers)
                {
                    if (chunkRenderer != null) DestroyImmediate(chunkRenderer.gameObject);
                }
            }

            _chunkRenderers = new Renderer[_amountOfChunks.x,_amountOfChunks.y];
            
            for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
            {
                for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
                {
                    var renderer = Instantiate(_chunkRendererPrefab);

                    var texture = new Texture2D(_chunkDimensions.x, _chunkDimensions.y)
                    {
                        wrapMode = TextureWrapMode.Clamp, 
                        filterMode = FilterMode.Point
                    };
                    var colors = new Color[texture.width * texture.height];
                    texture.SetPixels(colors);
                    texture.Apply();

                    var material = new Material(_shader)
                    {
                        mainTexture = texture
                    };
                    
                    renderer.name = $"Chunk {chunkColumn}, {chunkRow}";
                    renderer.transform.localScale = new Vector3(worldChunkDimensions.x, worldChunkDimensions.y, 1);
                    renderer.transform.position = new Vector3(
                        worldChunkDimensions.x * (chunkColumn + 0.5f),
                        worldChunkDimensions.y * (chunkRow + 0.5f),
                        0);
                    renderer.transform.SetParent(transform, false);
                    renderer.sharedMaterial = material;

                    _chunkRenderers[chunkColumn, chunkRow] = renderer;
                }
            }
        }

        private void OnDrawGizmos()
        {
            for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
            {
                for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
                {
                    var chunkPosition = new Vector2Int(chunkColumn, chunkRow);
                    var chunkRenderer = _chunkRenderers[chunkColumn, chunkRow];
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(chunkRenderer.transform.position, chunkRenderer.transform.lossyScale);

                    if (_chunks == null) continue;
                    
                    var chunk = _chunks[chunkColumn, chunkRow];

                    if (!(chunk.DirtyRect is DirtyRect dirtyRect)) continue;
                    
                    var startGlobalPosition = 
                        WorldPositionAtGlobalTilePosition(chunkPosition * _chunkDimensions + dirtyRect.StartTilePosition);
                    var endGlobalPosition =
                        WorldPositionAtGlobalTilePosition(chunkPosition * _chunkDimensions + dirtyRect.EndTilePosition + Vector2Int.one);

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(startGlobalPosition, new Vector2(startGlobalPosition.x, endGlobalPosition.y));
                    Gizmos.DrawLine(startGlobalPosition, new Vector2(endGlobalPosition.x, startGlobalPosition.y));
                    Gizmos.DrawLine(endGlobalPosition, new Vector2(startGlobalPosition.x, endGlobalPosition.y));
                    Gizmos.DrawLine(endGlobalPosition, new Vector2(endGlobalPosition.x, startGlobalPosition.y));
                }
            }
        }
    }
}