using UnityEngine;
using UnityEngine.UI;

namespace BetterBootlegStuff
{
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public class StatsDisplay : MonoBehaviour
    {
        [SerializeField] private PixelSimulation pixelSimulation;
        
        private Text _text;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }
        
        void Update() {
            var stats = pixelSimulation.stats;
            _text.text = $"Total static pixels: {stats.staticPixels}\nTotal live pixels: {stats.updatePixels}";
        }
    }
}