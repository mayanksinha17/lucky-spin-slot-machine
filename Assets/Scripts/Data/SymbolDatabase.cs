#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuckySpin.SlotMachine.Data
{
    /// <summary>
    /// ScriptableObject database containing all configured symbols in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolDatabase", menuName = "LuckySpin/Symbol Database", order = 0)]
    public class SymbolDatabase : ScriptableObject
    {
        [Tooltip("List of all available symbols in the machine.")]
        [SerializeField] private SymbolData[] symbols;

        public SymbolData[] Symbols { get { return symbols; } }

        public SymbolData GetSymbol(SymbolType type)
        {
            if (symbols == null) return null;
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i] != null && symbols[i].Type == type)
                {
                    return symbols[i];
                }
            }
            return null;
        }

        public SymbolData GetRandomSymbol()
        {
            if (symbols == null || symbols.Length == 0) return null;
            int idx = UnityEngine.Random.Range(0, symbols.Length);
            return symbols[idx];
        }

        public void Initialize(SymbolData[] symbolList)
        {
            symbols = symbolList;
        }
    }
}

