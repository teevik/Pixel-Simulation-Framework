using BettererBootlegStuff;
using UnityEngine;

[RequireComponent(typeof(NewPixelSimulation))]
public class test5 : MonoBehaviour
{
    private NewPixelSimulation _pixelSimulation;

    private void Awake()
    {
        _pixelSimulation = GetComponent<NewPixelSimulation>();
    }

    private void Update()
    {
        // var mousePosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // var gridPosition = _pixelSimulation.GridPositionAtWorldPosition(mousePosition);
        //
        // if (!_pixelSimulation.FindTilePosition(gridPosition, out var chunk, out var tilePosition)) return;
        //     
        // if (Input.GetMouseButton(0))
        // {
        //     if (chunk.TileExistsAt(tilePosition)) return;
        //
        //     // chunk.AddTile(new StaticTile(Color.gray), tilePosition);
        // }
        // else if (Input.GetMouseButton(1))
        // {
        //     // if (chunk.TileExistsAt(tilePosition)) return;
        //     // chunk.AddTile(new ParticleTile(Vector2.zero), tilePosition);
        //     
        //     // if (chunk.TileExistsAt(tilePosition)) return;
        //     // chunk.AddTile(new WaterTile(), tilePosition);
        //     
        //     for (int i = -5; i < 5; i++)
        //     {
        //         for (int j = -5; j < 5; j++)
        //         {
        //             var offset = new Vector2Int(i, j);
        //             var offsetTilePosition = tilePosition + offset;
        //             if (chunk.TileExistsAt(offsetTilePosition)) continue;
        //     
        //             chunk.AddTile(new ParticleTile(Random.insideUnitCircle * 20f), offsetTilePosition);
        //         }
        //     }
        // }
    }
}