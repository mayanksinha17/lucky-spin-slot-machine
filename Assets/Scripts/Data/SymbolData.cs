using UnityEngine;

namespace LuckySpin.SlotMachine.Data
{
    /// <summary>
    /// Data asset defining a slot machine symbol's visual presentation, odds, and payout multiplier.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSymbolData", menuName = "LuckySpin/Symbol Data", order = 1)]
    public class SymbolData : ScriptableObject
    {
        [Header("Symbol Identity")]
        [Tooltip("Unique enum type for this symbol.")]
        [SerializeField] private SymbolType symbolType;

        [Tooltip("Display name used in UI and paytable.")]
        [SerializeField] private string displayName = "Symbol";

        [Header("Visual Asset")]
        [Tooltip("The high-resolution sprite shown on the reel.")]
        [SerializeField] private Sprite iconSprite;

        [Header("Economy & Odds")]
        [Tooltip("Multiplier applied to the current bet when 3 match on the payline.")]
        [SerializeField] private int payoutMultiplier = 2;

        [Tooltip("Relative probability weight when randomly generating strip outcomes.")]
        [Range(1, 100)]
        [SerializeField] private int weight = 20;

        public SymbolType Type { get { return symbolType; } }
        public string DisplayName { get { return displayName; } }
        public Sprite Icon { get { return iconSprite; } }
        public int PayoutMultiplier { get { return payoutMultiplier; } }
        public int Weight { get { return weight; } }

        /// <summary>
        /// Convenience constructor for runtime and unit test initialization.
        /// </summary>
        public void Initialize(SymbolType type, string name, Sprite sprite, int multiplier, int relativeWeight)
        {
            symbolType = type;
            displayName = name;
            iconSprite = sprite;
            payoutMultiplier = multiplier;
            weight = relativeWeight;
        }
    }
}
