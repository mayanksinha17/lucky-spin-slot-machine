#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LuckySpin.SlotMachine.Animations;
using LuckySpin.SlotMachine.Audio;

namespace LuckySpin.SlotMachine.Bonus
{
    /// <summary>
    /// Coordinates arcade jackpot celebrations:
    /// marquee glow flashing, cabinet camera pulse, celebratory modal pop-in,
    /// audio fanfare, and massive golden coin/star fountains.
    /// </summary>
    public class JackpotCelebrationController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject jackpotModalRoot;
        [SerializeField] private RectTransform jackpotModalRect;
        [SerializeField] private TextMeshProUGUI jackpotAmountText;
        [SerializeField] private Graphic marqueeLightGraphic;
        [SerializeField] private Button dismissButton;

        [Header("FX References")]
        [SerializeField] private CoinParticleController particleController;
        [SerializeField] private AudioManager audioManager;

        [Header("Celebration Parameters")]
        [SerializeField] private int jackpotThreshold = 1000;
        [SerializeField] private float marqueeFlashDuration = 3.5f;

        private Coroutine celebrationRoutine = null;
        private bool isCelebrationActive = false;

        public bool IsCelebrationActive { get { return isCelebrationActive; } }
        public int JackpotThreshold { get { return jackpotThreshold; } set { jackpotThreshold = value; } }

        public event Action<int> OnJackpotTriggered;
        public event Action OnJackpotDismissed;

        public void Initialize(AudioManager am = null, CoinParticleController pc = null)
        {
            if (am != null) audioManager = am;
            if (pc != null) particleController = pc;
        }

        private void Awake()
        {
            if (dismissButton != null)
            {
                dismissButton.onClick.AddListener(Dismiss);
            }
            if (jackpotModalRoot != null)
            {
                jackpotModalRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Checks whether a spin result constitutes a jackpot and triggers the sequence.
        /// </summary>
        public bool CheckAndTriggerJackpot(int payout, bool isDiamondMatch)
        {
            if (isDiamondMatch || payout >= jackpotThreshold)
            {
                TriggerJackpot(payout);
                return true;
            }
            return false;
        }

        public void TriggerJackpot(int amount)
        {
            isCelebrationActive = true;

            if (OnJackpotTriggered != null)
            {
                OnJackpotTriggered(amount);
            }

            if (audioManager != null)
            {
                audioManager.PlayJackpotFanfare();
            }

            if (particleController != null)
            {
                particleController.PlayCoinFountain(Vector2.zero);
            }

            if (celebrationRoutine != null) StopCoroutine(celebrationRoutine);
            celebrationRoutine = StartCoroutine(CelebrationSequence(amount));
        }

        private IEnumerator CelebrationSequence(int amount)
        {
            // 1. Show modal with PopIn
            if (jackpotModalRoot != null)
            {
                jackpotModalRoot.SetActive(true);
                if (jackpotModalRect != null)
                {
                    yield return UIAnimationHelper.PopInRoutine(jackpotModalRect, 0.5f);
                }
            }

            // 2. Rollup count
            if (jackpotAmountText != null)
            {
                yield return UIAnimationHelper.RollupTextRoutine(jackpotAmountText, 0, amount, 1.2f, "JACKPOT: +", " G");
            }

            // 3. Pulsate marquee light colors
            float elapsed = 0f;
            Color cGold = new Color(1f, 0.85f, 0.1f, 1f);
            Color cPurple = new Color(0.8f, 0.2f, 1f, 1f);

            while (elapsed < marqueeFlashDuration && isCelebrationActive)
            {
                elapsed += Time.deltaTime;
                if (marqueeLightGraphic != null)
                {
                    float lerp = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;
                    marqueeLightGraphic.color = Color.Lerp(cGold, cPurple, lerp);
                }
                yield return null;
            }
        }

        public void Dismiss()
        {
            isCelebrationActive = false;
            if (celebrationRoutine != null)
            {
                StopCoroutine(celebrationRoutine);
                celebrationRoutine = null;
            }

            if (jackpotModalRoot != null)
            {
                jackpotModalRoot.SetActive(false);
            }

            if (OnJackpotDismissed != null)
            {
                OnJackpotDismissed();
            }
        }
    }
}

