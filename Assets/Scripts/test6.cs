using BetterererBootlegStuff;
using UnityEngine;

[RequireComponent(typeof(NewestPixelSimulation))]
public class test6 : MonoBehaviour
{
    private NewestPixelSimulation _pixelSimulation;

    private void Awake()
    {
        _pixelSimulation = GetComponent<NewestPixelSimulation>();
    }

    private void Update()
    {
        var mousePosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var gridPosition = _pixelSimulation.GridPositionAtWorldPosition(mousePosition);
        
        if (Input.GetMouseButton(0))
        {
            _pixelSimulation.SetTileTo(gridPosition, new StaticTile(Color.gray));
        }
        else if (Input.GetMouseButton(1))
        {
            _pixelSimulation.SetTileTo(gridPosition, new EmptyTile(10));
        }
    }
}