#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Abstraction for random number generation to allow seamless swapping between Unity RNG and seeded test RNG.
    /// </summary>
    public interface IRandomProvider
    {
        int Range(int min, int max);
        float Value();
    }

    /// <summary>
    /// Default RNG provider wrapping UnityEngine.Random.
    /// </summary>
    public class UnityRandomProvider : IRandomProvider
    {
        public int Range(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public float Value()
        {
            return UnityEngine.Random.value;
        }
    }

    /// <summary>
    /// Deterministic RNG provider for unit tests and verification.
    /// </summary>
    public class SeededRandomProvider : IRandomProvider
    {
        private readonly System.Random random;

        public SeededRandomProvider(int seed)
        {
            random = new System.Random(seed);
        }

        public int Range(int min, int max)
        {
            return random.Next(min, max);
        }

        public float Value()
        {
            return (float)random.NextDouble();
        }
    }

    /// <summary>
    /// Generates randomized slot reel outcomes independently of visual animations.
    /// Supports weighted probability distribution and deterministic test seeds.
    /// </summary>
    public class RandomOutcomeGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Symbol database used for symbol pool and weights.")]
        [SerializeField] private SymbolDatabase symbolDatabase;

        [Tooltip("Use weighted probabilities instead of uniform distribution.")]
        [SerializeField] private bool useWeightedProbabilities = true;

        private IRandomProvider randomProvider = new UnityRandomProvider();

        public SymbolDatabase Database
        {
            get { return symbolDatabase; }
            set { symbolDatabase = value; }
        }

        public bool UseWeightedProbabilities
        {
            get { return useWeightedProbabilities; }
            set { useWeightedProbabilities = value; }
        }

        public void SetRandomProvider(IRandomProvider provider)
        {
            randomProvider = provider ?? new UnityRandomProvider();
        }

        /// <summary>
        /// Generates an array of 3 independent predetermined symbols (one per reel).
        /// </summary>
        public SymbolData[] GenerateOutcome(SymbolDatabase db = null)
        {
            SymbolDatabase activeDb = db != null ? db : symbolDatabase;
            if (activeDb == null || activeDb.Symbols == null || activeDb.Symbols.Length == 0)
            {
                throw new InvalidOperationException("Cannot generate outcome: SymbolDatabase is missing or empty.");
            }

            SymbolData[] outcome = new SymbolData[3];
            for (int i = 0; i < 3; i++)
            {
                outcome[i] = GenerateSingleSymbol(activeDb);
            }
            return outcome;
        }

        /// <summary>
        /// Generates a single randomized symbol based on database weights or uniform distribution.
        /// </summary>
        public SymbolData GenerateSingleSymbol(SymbolDatabase activeDb)
        {
            if (activeDb == null || activeDb.Symbols == null || activeDb.Symbols.Length == 0) return null;

            if (!useWeightedProbabilities)
            {
                int randomIdx = randomProvider.Range(0, activeDb.Symbols.Length);
                return activeDb.Symbols[randomIdx];
            }

            // Calculate total weight
            int totalWeight = 0;
            for (int i = 0; i < activeDb.Symbols.Length; i++)
            {
                if (activeDb.Symbols[i] != null)
                {
                    totalWeight += Mathf.Max(1, activeDb.Symbols[i].Weight);
                }
            }

            // Roll within total weight
            int roll = randomProvider.Range(0, totalWeight);
            int accumulated = 0;

            for (int i = 0; i < activeDb.Symbols.Length; i++)
            {
                if (activeDb.Symbols[i] != null)
                {
                    accumulated += Mathf.Max(1, activeDb.Symbols[i].Weight);
                    if (roll < accumulated)
                    {
                        return activeDb.Symbols[i];
                    }
                }
            }

            return activeDb.Symbols[activeDb.Symbols.Length - 1];
        }
    }
}
