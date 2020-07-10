using UnityEngine;
using UnityEngine.UI;

namespace BetterBootlegStuff2
{
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public class StatsDisplay : MonoBehaviour
    {
        [SerializeField] private BetterBootlegStuff2.PixelSimulation pixelSimulation;
        
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