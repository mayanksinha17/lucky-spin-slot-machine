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
        [SerializeField] private GameHistoryManager historyManager;

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
                uiManager.OnQuickBetSelected += SetBet;
            }

            if (historyManager != null && uiManager != null && uiManager.HistoryUI != null)
            {
                historyManager.OnSpinRecorded += uiManager.HistoryUI.AddRecord;
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
                    uiManager.DisplayMessage("Insufficient Credits! Lower bet or reset balance.");
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
                uiManager.HideResultBanner();
            }

            SetState(GameState.Spinning);
            if (uiManager != null)
            {
                uiManager.SetSpinInteractable(false);
                uiManager.UpdateStatus("SPINNING...", new Color(0.9f, 0.7f, 1f, 1f));
            }
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
                audioManager.StartSpinLoop();
            }
        }

        public void IncreaseBet()
        {
            if (currentState != GameState.Idle) return;
            if (currentBet + betStep <= maxBet)
            {
                currentBet += betStep;
                OnBetUpdated();
            }
        }

        public void DecreaseBet()
        {
            if (currentState != GameState.Idle) return;
            if (currentBet - betStep >= minBet)
            {
                currentBet -= betStep;
                OnBetUpdated();
            }
        }

        public void SetMaxBet()
        {
            if (currentState != GameState.Idle) return;
            currentBet = maxBet;
            OnBetUpdated();
        }

        public void SetBet(int amount)
        {
            if (currentState != GameState.Idle) return;
            currentBet = Mathf.Clamp(amount, minBet, maxBet);
            OnBetUpdated();
        }

        private void OnBetUpdated()
        {
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

            if (uiManager != null)
            {
                switch (currentState)
                {
                    case GameState.Idle:
                        uiManager.UpdateStatus("READY TO SPIN", new Color(0.4f, 0.9f, 0.5f, 1f));
                        break;
                    case GameState.Spinning:
                        uiManager.UpdateStatus("SPINNING...", new Color(0.85f, 0.65f, 1f, 1f));
                        break;
                    case GameState.Evaluating:
                        uiManager.UpdateStatus("EVALUATING...", new Color(1f, 0.85f, 0.3f, 1f));
                        break;
                    case GameState.Payout:
                        uiManager.UpdateStatus("PAYOUT!", new Color(1f, 0.9f, 0.2f, 1f));
                        break;
                    case GameState.InsufficientFunds:
                        uiManager.UpdateStatus("INSUFFICIENT FUNDS", new Color(1f, 0.35f, 0.35f, 1f));
                        break;
                }
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
