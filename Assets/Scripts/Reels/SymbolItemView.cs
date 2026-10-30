#pragma warning disable 0649, 0169, 0414
using UnityEngine;
using UnityEngine.UI;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Reels
{
    /// <summary>
    /// Displays a single symbol sprite inside a reel slot with optional glow / highlight effects.
    /// </summary>
    public class SymbolItemView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image highlightBorder;

        private SymbolData currentData;

        public SymbolData CurrentData { get { return currentData; } }

        public void SetSymbol(SymbolData data)
        {
            currentData = data;
            if (iconImage != null && data != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }
        }

        public void SetHighlight(bool highlighted)
        {
            if (highlightBorder != null)
            {
                highlightBorder.enabled = highlighted;
            }
        }
    }
}
