using System.Collections.Generic;
using System.Linq;
using BootlegStuff;
using Extensions;
using UnityEngine;
using System.Linq;

public class test3 : MonoBehaviour
{
    [SerializeField] private BootlegPixelSurface _pixelSurface;

    private void Update()
    {
        var mousePosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var pixelPosition = _pixelSurface.PixelPosAtWorldPos(mousePosition);
        if (!_pixelSurface.IsInBounds(pixelPosition)) return;
        
        if (Input.GetMouseButton(0))
        {
            _pixelSurface.SetStaticPixel(pixelPosition, Color.green);
        }
        else if (Input.GetMouseButton(1))
        {
            _pixelSurface.AddLivePixel(new WaterLivePixel(pixelPosition, 5));
        }
    }
}

public class WaterLivePixel : LivePixel
{
    private float _liquid;
    private float _newLiquid;
    
    private float _maxLiquid = 1.0f;
    private float _minLiquid = 0.005f;

    // Extra liquid a cell can store than the cell above it
    private float _maxCompression = 0.25f;

    // Lowest and highest amount of liquids allowed to flow per iteration
    private float _minFlow = 0.005f;
    private float _maxFlow = 4f;

    // Adjusts flow speed (0.0f - 1.0f)
    private float _flowSpeed = 2f;

    private float lastUpdateFrame;
    
    public WaterLivePixel(Vector2Int position, float liquid) : base(position)
    {
        _liquid = liquid;
        _newLiquid = liquid;
        color = Color.blue;
    }

    private bool ClearFromStaticPixelAt(BootlegPixelSurface pixelSurface, Vector2Int position) {
        Color c = pixelSurface.GetStaticPixel(position);
        return c.a == 0;
    }

    private bool WaterCellAt(BootlegPixelSurface pixelSurface, Vector2Int position, out WaterLivePixel waterLivePixel)
    {
        var pixels = pixelSurface.GetLivePixels<WaterLivePixel>(position);

        if (pixels.Count > 0)
        {
            waterLivePixel = pixels[0];
            return true;
        };

        waterLivePixel = null;
        return false;
    }

    private WaterLivePixel GetOrCreateWaterCellAt(BootlegPixelSurface pixelSurface, Vector2Int position)
    {
        var pixels = pixelSurface.GetLivePixels<WaterLivePixel>(position);

        if (pixels.Count > 0)
        {
            return pixels[0];
        };

        var pixel = new WaterLivePixel(position, 0f);
        pixelSurface.AddLivePixel(pixel);

        return pixel;
    }
    
    float CalculateVerticalFlowValue(float remainingLiquid, WaterLivePixel destination)
    {
        float sum = remainingLiquid + destination._liquid;
        float value = 0;

        if (sum <= _maxLiquid) {
            value = _maxLiquid;
        } else if (sum < 2 * _maxLiquid + _maxCompression) {
            value = (_maxLiquid * _maxLiquid + sum * _maxCompression) / (_maxLiquid + _maxCompression);
        } else {
            value = (sum + _maxCompression) / 2f;
        }

        return value;
    }


    public override void Update(BootlegPixelSurface pixelSurface)
    {
        // var otherWaterPixelsInSamePlace = pixelSurface.GetLivePixels<WaterLivePixel>(roundedPosition);
        // if (otherWaterPixelsInSamePlace.Count > 1)
        // {
        //     position += Vector2.up;
        // }
        
        if (lastUpdateFrame != Time.frameCount)
        {
            lastUpdateFrame = Time.frameCount;
            _liquid = _newLiquid;
        }
        
        if (_liquid <= _minLiquid)
        {
            DieClear();
            return;
        }

        if (!pixelSurface.IsInBounds(roundedPosition))
        {
            DieClear();
        }
        
        float startValue = _liquid;
        float remainingValue = _liquid;
        var flow = 0f;

        if (ClearFromStaticPixelAt(pixelSurface, roundedPosition + Vector2Int.down))
        {
            var bottom = GetOrCreateWaterCellAt(pixelSurface, roundedPosition + Vector2Int.down);
            
            flow = CalculateVerticalFlowValue(_liquid, bottom) - bottom._liquid;
            
            if (bottom._liquid > 0 && flow > _minFlow) flow *= _flowSpeed; 
            
            flow = Mathf.Max (flow, 0);
            if (flow > Mathf.Min(_maxFlow, _liquid)) flow = Mathf.Min(_maxFlow, _liquid);

            if (flow != 0) {
                remainingValue -= flow;
                _newLiquid -= flow;
                bottom._newLiquid += flow;
            }
        }
        
        if (remainingValue < _minLiquid) {
            _newLiquid -= remainingValue;
            return;
        }
        
        if (ClearFromStaticPixelAt(pixelSurface, roundedPosition + Vector2Int.left)) {
            var left = GetOrCreateWaterCellAt(pixelSurface, roundedPosition + Vector2Int.left);

            // Calculate flow rate
            flow = (remainingValue - left._liquid) / 4f;
            if (flow > _minFlow) flow *= _flowSpeed;

            // constrain flow
            flow = Mathf.Max (flow, 0);
            if (flow > Mathf.Min(_maxFlow, remainingValue)) flow = Mathf.Min(_maxFlow, remainingValue);

            // Adjust temp values
            if (flow != 0) {
                remainingValue -= flow;
                _newLiquid -= flow;
                left._newLiquid += flow;
            } 
        }

        // Check to ensure we still have liquid in this cell
        if (remainingValue < _minLiquid) {
            _newLiquid -= remainingValue;
            return;
        }

        // Flow to right cell
        if (ClearFromStaticPixelAt(pixelSurface, roundedPosition + Vector2Int.right)) {
            var right = GetOrCreateWaterCellAt(pixelSurface, roundedPosition + Vector2Int.right);

            // calc flow rate
            flow = (remainingValue - right._liquid) / 3f;										
            if (flow > _minFlow) flow *= _flowSpeed; 

            // constrain flow
            flow = Mathf.Max (flow, 0);
            if (flow > Mathf.Min(_maxFlow, remainingValue)) flow = Mathf.Min(_maxFlow, remainingValue);

            // Adjust temp values
            if (flow != 0) {
                remainingValue -= flow;
                _newLiquid -= flow;
                right._newLiquid += flow;
            } 
        }

        // Check to ensure we still have liquid in this cell
        if (remainingValue < _minLiquid) {
            _newLiquid -= remainingValue;
            return;
        }
				
        // Flow to Top cell
        if (ClearFromStaticPixelAt(pixelSurface, roundedPosition + Vector2Int.up)) {
            var up = GetOrCreateWaterCellAt(pixelSurface, roundedPosition + Vector2Int.up);

            flow = remainingValue - CalculateVerticalFlowValue (remainingValue, up); 
            if (flow > _minFlow)
                flow *= _flowSpeed; 

            // constrain flow
            flow = Mathf.Max (flow, 0);
            if (flow > Mathf.Min(_maxFlow, remainingValue)) 
                flow = Mathf.Min(_maxFlow, remainingValue);

            // Adjust values
            if (flow != 0) {
                remainingValue -= flow;
                _newLiquid -= flow;
                up._newLiquid += flow;
            } 
        }

        // Check to ensure we still have liquid in this cell
        if (remainingValue < _minLiquid) {
            _newLiquid -= remainingValue;
            return;
        }
        
        



        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (i == 0 && j == 0) continue;
                
                var offset = new Vector2Int(i, j);
                
                if (ClearFromStaticPixelAt(pixelSurface, roundedPosition + offset))
                {
                    var neighbor = GetOrCreateWaterCellAt(pixelSurface, roundedPosition + offset);

                    



                    // if (WaterCellAt(, out var neighbor))
                    // {

                    // var deltaPressure = _pressure - neighbor._pressure;
                    // // var flow = 1 * deltaPressure;
                    // // flow = Mathf.Clamp(flow, _pressure / 6.0f, -neighbor._pressure / 6.0f);
                    //
                    // float flow;
                    //
                    // if ( j == -1 )
                    // {
                    //     if ( ( mass < maxMass ) ||
                    //          ( neighbor.mass < maxMass ) )
                    //     {
                    //         flow = mass - maxMass;
                    //     }
                    //     else
                    //     {
                    //         flow = mass - neighbor.mass - maxCompress;
                    //         flow *= 0.5f;
                    //     }
                    // }
                    // else if ( j == 1 )
                    // {
                    //     if ( ( mass < maxMass ) ||
                    //          ( neighbor.mass < maxMass ) )
                    //     {
                    //         flow = maxMass - neighbor.mass;
                    //     }
                    //     else
                    //     {
                    //         flow = mass - neighbor.mass + maxCompress;
                    //         flow *= 0.5f; 
                    //     }
                    // }
                    // else // neighbour is on same level
                    // {
                    //     flow = ( mass - neighbor.mass ) * 0.5f;
                    // }
                    //
                    // flow = Mathf.Clamp(flow, _pressure / 6.0f, -neighbor._pressure / 6.0f);
                    //
                    //
                    // _newPressure -= flow;
                    // neighbor._newPressure += flow;
                    // // }
                }
            }
        }
    }
}

public class ParticleLivePixel : LivePixel {
    private const float gravity = 50f;
	
    private Vector2 _velocity;

    public ParticleLivePixel(Vector2Int position, Vector2 velocity) : base(position)
    {
        color = Color.yellow;
        this._velocity = velocity;
    }
	
    bool ClearAt(BootlegPixelSurface surf, Vector2Int position) {
        Color c = surf.GetStaticPixel(position);
        return c.a == 0;
    }

    public override void Update(BootlegPixelSurface surf) {
        _velocity.y -= gravity * Time.deltaTime;
        _velocity *= 1f - 0.1f * Time.deltaTime;

        position += _velocity * Time.deltaTime;
		  
        if (!ClearAt(surf, roundedPosition)) {
            // We've hit something.  See if it's clear to the sides.
            bool clearLeft = ClearAt(surf, new Vector2Int(roundedPosition.x-1, roundedPosition.y));
            bool clearRight = ClearAt(surf, new Vector2Int(roundedPosition.x+1, roundedPosition.y));
            if (clearLeft && clearRight) {
                if (Random.Range(0f,1f) > 0.5f) position += Vector2.left;
                else position += Vector2.right;
            } else if (clearLeft) {
                position += Vector2.left;
            } else if (clearRight) {
                position += Vector2.right;
            } else if (_velocity.y < 0) {
                // Falling down, and couldn't find a clear spot.  Move back up 1 space, and die.
                position += Vector2.up;
                Die();
            }
            _velocity = Vector2.zero;
        }
    }
}