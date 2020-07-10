// using System;
// using System.Collections.Generic;
// using NativeQuadTree;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace BetterBootlegStuff4
// {
//     public class PixelSimulation : SerializedMonoBehaviour
//     {        
//         [DisableIf("@UnityEngine.Application.isPlaying")]
//         [ValidateInput("@(_chunkDimensions.x > 0 && _chunkDimensions.y > 0)", "Needs to be larger than 0")]
//         [OnValueChanged(nameof(ResetChunkRenderers))]
//         [Tooltip("Dimensions of each chunk, in pixels")]
//         [SerializeField] private Vector2Int _chunkDimensions;
//         
//         [DisableIf("@UnityEngine.Application.isPlaying")]
//         [ValidateInput("@(_amountOfChunks.x > 0 && _amountOfChunks.y > 0)", "Needs to be larger than 0")]
//         [OnValueChanged(nameof(ResetChunkRenderers))]
//         [Tooltip("Amount of chunks")]
//         [SerializeField] private Vector2Int _amountOfChunks;
//         
//         [DisableIf("@UnityEngine.Application.isPlaying")]
//         [ValidateInput("@(_pixelsPerUnit > 0)", "Needs to be larger than 0")]
//         [OnValueChanged(nameof(ResetChunkRenderers))]
//         [Tooltip("Scaling factor between pixels and world units")]
//         [SerializeField] private float _pixelsPerUnit = 100;
//         
//         [Required]
//         [Tooltip("Shader to use for rendering")]
//         [SerializeField] private Shader _shader;
//
//         [Required]
//         [OnValueChanged(nameof(ResetChunkRenderers))]
//         [Tooltip("Prefab for rendering each chunk")]
//         [SerializeField] private Renderer _chunkRendererPrefab;
//
//         [FoldoutGroup("Chunk Renderers")]
//         [TableMatrix(HorizontalTitle = "Square Celled Matrix", SquareCells = true)]
//         [TableList]
//         [SerializeField] private Renderer[,] _chunkRenderers;
//
//         // private Chunk[,] _chunks;
//
//         private void Awake()
//         {
//             var bounds = new AABB2D(0, 1024);
//             var quadTree = new NativeQuadTree<bool>(bounds);
//
//             quadTree.
//         }
//
//         // private void Awake()
//         // {
//         //     _chunks = new Chunk[_amountOfChunks.x, _amountOfChunks.y];
//         //
//         //     for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
//         //     {
//         //         for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
//         //         {
//         //             var chunkTexture = _chunkRenderers[chunkColumn, chunkRow].sharedMaterial.mainTexture as Texture2D;
//         //             
//         //             _chunks[chunkColumn, chunkRow] = new Chunk(chunkTexture, _chunkDimensions);
//         //         }
//         //     }
//         //
//         //     for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
//         //     {
//         //         for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
//         //         {
//         //             var chunk = _chunks[chunkColumn, chunkRow];
//         //             var chunkNeighbors = GetNeighborsOfChunk(new Vector2Int(chunkColumn, chunkRow));
//         //             chunk.SetNeighbors(chunkNeighbors);
//         //         }
//         //     }
//         // }
//         //
//         // private bool ChunkIsInBounds(Vector2Int chunkPosition)
//         // {
//         //     var columns = _amountOfChunks.x;
//         //     var rows = _amountOfChunks.y;
//         //
//         //     return 
//         //         chunkPosition.x >= 0 && chunkPosition.x < columns && 
//         //         chunkPosition.y >= 0 && chunkPosition.y < rows;
//         // }
//         //
//         // private Dictionary<Side, Chunk> GetNeighborsOfChunk(Vector2Int chunkPosition)
//         // {
//         //     var chunkNeighbors = new Dictionary<Side, Chunk>();
//         //     
//         //     foreach (Side side in Enum.GetValues(typeof(Side)))
//         //     {
//         //         var offset = side.SideAsVector();
//         //         var targetPosition = chunkPosition + offset;
//         //
//         //         if (ChunkIsInBounds(targetPosition))
//         //         {
//         //             var targetChunk = _chunks[targetPosition.x, targetPosition.y];
//         //             chunkNeighbors.Add(side, targetChunk);
//         //         }
//         //     }
//         //
//         //     return chunkNeighbors;
//         // }
//         //
//         // [Button]
//         // private void ResetChunkRenderers()
//         // {
//         //     if (Application.isPlaying) return;
//         //     if (_amountOfChunks.x <= 0 || _amountOfChunks.y <= 0) return;
//         //     if (_chunkDimensions.x <= 0 || _chunkDimensions.y <= 0) return;
//         //
//         //     var worldChunkDimensions = (Vector2)_chunkDimensions / _pixelsPerUnit;
//         //
//         //     if (_chunkRenderers != null)
//         //     {
//         //         foreach (var chunkRenderer in _chunkRenderers)
//         //         {
//         //             if (chunkRenderer != null) DestroyImmediate(chunkRenderer.gameObject);
//         //         }
//         //     }
//         //
//         //     _chunkRenderers = new Renderer[_amountOfChunks.x,_amountOfChunks.y];
//         //     
//         //     for (var chunkColumn = 0; chunkColumn < _amountOfChunks.x; chunkColumn++)
//         //     {
//         //         for (var chunkRow = 0; chunkRow < _amountOfChunks.y; chunkRow++)
//         //         {
//         //             var renderer = Instantiate(_chunkRendererPrefab);
//         //
//         //             var texture = new Texture2D(_chunkDimensions.x, _chunkDimensions.y)
//         //             {
//         //                 wrapMode = TextureWrapMode.Clamp, 
//         //                 filterMode = FilterMode.Point
//         //             };
//         //             var colors = new Color[texture.width * texture.height];
//         //             texture.SetPixels(colors);
//         //             texture.Apply();
//         //
//         //             var material = new Material(_shader)
//         //             {
//         //                 mainTexture = texture
//         //             };
//         //             
//         //             renderer.name = $"Chunk {chunkColumn}, {chunkRow}";
//         //             renderer.transform.localScale = new Vector3(worldChunkDimensions.x, worldChunkDimensions.y, 1);
//         //             renderer.transform.position = new Vector3(
//         //                 worldChunkDimensions.x * (chunkColumn + 0.5f),
//         //                 worldChunkDimensions.y * (chunkRow + 0.5f),
//         //                 0);
//         //             renderer.transform.SetParent(transform, false);
//         //             renderer.sharedMaterial = material;
//         //
//         //             _chunkRenderers[chunkColumn, chunkRow] = renderer;
//         //         }
//         //     }
//         // }
//         //
//         // private void OnDrawGizmos()
//         // {
//         //     foreach (var chunkRenderer in _chunkRenderers)
//         //     {
//         //         Gizmos.DrawWireCube(chunkRenderer.transform.position, chunkRenderer.transform.lossyScale);
//         //     }
//         // }
//     }
// }