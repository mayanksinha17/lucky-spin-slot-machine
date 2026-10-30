#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Gameplay;
using LuckySpin.SlotMachine.UI;
using LuckySpin.SlotMachine.Audio;

namespace LuckySpin.SlotMachine.Core
{
    /// <summary>
    /// Master coordinator for game lifecycle, player balance, betting state, and subsystem communication.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economy Configuration")]
        [Tooltip("Initial player credits.")]
        [SerializeField] private int startingBalance = 1000;

        [Tooltip("Current bet placed per spin.")]
        [SerializeField] private int currentBet = 10;

        [Tooltip("Minimum allowable bet.")]
        [SerializeField] private int minBet = 10;

        [Tooltip("Maximum allowable bet.")]
        [SerializeField] private int maxBet = 100;

        [Tooltip("Step increment for bet adjustments.")]
        [SerializeField] private int betStep = 10;

        [Header("Subsystem Controllers")]
        [SerializeField] private SlotMachineController slotMachine;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;

        [Header("Runtime State")]
        [SerializeField] private GameState currentState = GameState.Idle;
        [SerializeField] private int balance;

        public GameState CurrentState { get { return currentState; } }
        public int Balance { get { return balance; } }
        public int CurrentBet { get { return currentBet; } }
        public int MinBet { get { return minBet; } }
        public int MaxBet { get { return maxBet; } }

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnBalanceChanged;
        public event Action<int> OnBetChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            balance = startingBalance;
        }

        private void Start()
        {
            InitializeSubsystems();
            SetState(GameState.Idle);
            UpdateUI();
        }

        private void Update()
        {
            // WebGL & Desktop Keyboard shortcut support: Spacebar or Enter to Spin
            if (currentState == GameState.Idle && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                RequestSpin();
            }
        }

        private void InitializeSubsystems()
        {
            if (uiManager != null)
            {
                uiManager.OnSpinClicked += RequestSpin;
                uiManager.OnIncreaseBetClicked += IncreaseBet;
                uiManager.OnDecreaseBetClicked += DecreaseBet;
                uiManager.OnMaxBetClicked += SetMaxBet;
            }
        }

        public void RequestSpin()
        {
            if (currentState != GameState.Idle) return;

            if (balance < currentBet)
            {
                SetState(GameState.InsufficientFunds);
                if (uiManager != null)
                {
                    uiManager.DisplayMessage("Insufficient Credits! Please lower your bet.");
                }
                return;
            }

            // Deduct bet and begin spin cycle
            balance -= currentBet;
            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(balance);
            }
            if (uiManager != null)
            {
                uiManager.UpdateBalance(balance);
            }

            SetState(GameState.Spinning);
            if (uiManager != null)
            {
                uiManager.SetSpinInteractable(false);
            }
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
                audioManager.StartSpinLoop();
            }

            // Note: Full RNG and sequential stop triggers will be connected in Phase 3 & 4
        }

        public void IncreaseBet()
        {
            if (currentState != GameState.Idle) return;
            if (currentBet + betStep <= maxBet)
            {
                currentBet += betStep;
                if (audioManager != null) audioManager.PlayButtonClick();
                if (OnBetChanged != null) OnBetChanged(currentBet);
                if (uiManager != null) uiManager.UpdateBet(currentBet);
            }
        }

        public void DecreaseBet()
        {
            if (currentState != GameState.Idle) return;
            if (currentBet - betStep >= minBet)
            {
                currentBet -= betStep;
                if (audioManager != null) audioManager.PlayButtonClick();
                if (OnBetChanged != null) OnBetChanged(currentBet);
                if (uiManager != null) uiManager.UpdateBet(currentBet);
            }
        }

        public void SetMaxBet()
        {
            if (currentState != GameState.Idle) return;
            currentBet = maxBet;
            if (audioManager != null) audioManager.PlayButtonClick();
            if (OnBetChanged != null) OnBetChanged(currentBet);
            if (uiManager != null) uiManager.UpdateBet(currentBet);
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            if (OnStateChanged != null)
            {
                OnStateChanged(currentState);
            }
        }

        private void UpdateUI()
        {
            if (uiManager != null)
            {
                uiManager.UpdateBalance(balance);
                uiManager.UpdateBet(currentBet);
                uiManager.SetSpinInteractable(currentState == GameState.Idle);
            }
        }
    }
}
