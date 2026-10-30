#pragma warning disable 0649, 0169, 0414
using UnityEngine;
using UnityEngine.UI;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Reels
{
    /// <summary>
    /// Displays a single symbol visual instance inside a reel slot with sizing, alignment, and win highlight support.
    /// </summary>
    public class SymbolItemView : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image highlightBorder;
        [SerializeField] private Image backgroundPlate;

        [Header("Runtime State")]
        [SerializeField] private SymbolData currentData;

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }

        public SymbolData CurrentData { get { return currentData; } }

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Binds and displays the given symbol asset, maintaining aspect ratio and sizing.
        /// </summary>
        public void SetSymbol(SymbolData data)
        {
            currentData = data;
            if (iconImage != null)
            {
                if (data != null && data.Icon != null)
                {
                    iconImage.sprite = data.Icon;
                    iconImage.enabled = true;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
            }
        }

        /// <summary>
        /// Updates the vertical anchor position of this symbol within its reel container.
        /// </summary>
        public void SetVerticalPosition(float yPosition)
        {
            if (RectTransform != null)
            {
                Vector2 pos = RectTransform.anchoredPosition;
                pos.y = yPosition;
                RectTransform.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Sets the explicit width and height of the symbol icon.
        /// </summary>
        public void SetSize(float width, float height)
        {
            if (RectTransform != null)
            {
                RectTransform.sizeDelta = new Vector2(width, height);
            }
            if (iconImage != null && iconImage.rectTransform != null)
            {
                iconImage.rectTransform.sizeDelta = new Vector2(width * 0.85f, height * 0.85f);
            }
        }

        /// <summary>
        /// Activates or deactivates the golden winning line highlight border.
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            if (highlightBorder != null)
            {
                highlightBorder.enabled = highlighted;
                if (highlighted)
                {
                    highlightBorder.color = new Color(1f, 0.85f, 0.2f, 0.9f); // Radiant gold
                }
            }
        }

        /// <summary>
        /// Clears current symbol display.
        /// </summary>
        public void Clear()
        {
            currentData = null;
            if (iconImage != null) iconImage.enabled = false;
            SetHighlight(false);
        }
    }
}
