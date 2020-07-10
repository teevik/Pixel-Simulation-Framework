using UnityEngine;

namespace Extensions
{
    public static class Texture2DExtensions
    {
        public static Color GetPixel(this Texture2D texture, Vector2Int position)
        {
            return texture.GetPixel(position.x, position.y);
        }
        
        public static void SetPixel(this Texture2D texture, Vector2Int position, Color color)
        {
            texture.SetPixel(position.x, position.y, color);
        }
    }
}