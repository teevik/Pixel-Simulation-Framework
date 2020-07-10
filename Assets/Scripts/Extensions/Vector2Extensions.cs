using UnityEngine;

namespace Extensions
{
    public static class Vector2Extensions
    {
        public static Vector2Int RoundToVector2Int(this Vector2 vector)
        {
            var x = Mathf.RoundToInt(vector.x);
            var y = Mathf.RoundToInt(vector.y);

            return new Vector2Int(x, y);
        }
        
        public static Vector2Int FloorToVector2Int(this Vector2 vector)
        {
            var x = Mathf.FloorToInt(vector.x);
            var y = Mathf.FloorToInt(vector.y);

            return new Vector2Int(x, y);
        }
    }
}