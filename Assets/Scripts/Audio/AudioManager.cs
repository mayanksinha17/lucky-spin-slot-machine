#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;

namespace LuckySpin.SlotMachine.Audio
{
    /// <summary>
    /// Master Audio Manager.
    /// Manages playback of sound effects, looping mechanical reel hums,
    /// progressive staggered reel stops, win chimes, streak chords, and jackpot brass fanfares.
    /// Provides volume control and audio mute capability.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource reelLoopSource;

        [Header("Sound Clips")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip winChimeClip;
        [SerializeField] private AudioClip coinPayoutClip;
        [SerializeField] private AudioClip jackpotFanfareClip;
        [SerializeField] private AudioClip streakFanfareClip;

        [Header("Audio Settings")]
        [SerializeField] private bool isMuted = false;
        [SerializeField] private float masterVolume = 1.0f;

        public bool IsMuted { get { return isMuted; } }
        public float MasterVolume { get { return masterVolume; } }

        public event Action<bool> OnMuteToggled;

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip);
        }

        public void StartSpinLoop()
        {
            if (isMuted) return;
            if (reelLoopSource != null && spinLoopClip != null)
            {
                reelLoopSource.clip = spinLoopClip;
                reelLoopSource.loop = true;
                reelLoopSource.Play();
            }
        }

        public void StopSpinLoop()
        {
            if (reelLoopSource != null)
            {
                reelLoopSource.Stop();
            }
        }

        public void PlayReelStop()
        {
            PlaySFX(reelStopClip);
        }

        public void PlayReelStopAtIndex(int reelIndex)
        {
            if (isMuted) return;
            if (sfxSource != null && reelStopClip != null)
            {
                // Slight pitch progression (1.0 -> 1.06 -> 1.12) creates tension and satisfaction
                sfxSource.pitch = 1.0f + (reelIndex * 0.06f);
                sfxSource.PlayOneShot(reelStopClip);
                sfxSource.pitch = 1.0f;
            }
        }

        public void PlayWin()
        {
            PlaySFX(winChimeClip);
        }

        public void PlayCoinPayout()
        {
            PlaySFX(coinPayoutClip);
        }

        public void PlayJackpotFanfare()
        {
            PlaySFX(jackpotFanfareClip != null ? jackpotFanfareClip : winChimeClip);
        }

        public void PlayStreakFanfare()
        {
            PlaySFX(streakFanfareClip != null ? streakFanfareClip : winChimeClip);
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;
            if (musicSource != null) musicSource.mute = isMuted;
            if (sfxSource != null) sfxSource.mute = isMuted;
            if (reelLoopSource != null) reelLoopSource.mute = isMuted;

            if (OnMuteToggled != null)
            {
                OnMuteToggled(isMuted);
            }
        }

        public void ToggleMute()
        {
            SetMuted(!isMuted);
        }

        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (musicSource != null) musicSource.volume = masterVolume;
            if (sfxSource != null) sfxSource.volume = masterVolume;
            if (reelLoopSource != null) reelLoopSource.volume = masterVolume;
        }

        private void PlaySFX(AudioClip clip)
        {
            if (isMuted) return;
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }
    }
}
