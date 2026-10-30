#pragma warning disable 0649, 0169, 0414
using UnityEngine;

namespace LuckySpin.SlotMachine.Audio
{
    /// <summary>
    /// Manages playback of sound effects, reel movement audio, and win fanfare.
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

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip);
        }

        public void StartSpinLoop()
        {
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

        public void PlayWin()
        {
            PlaySFX(winChimeClip);
        }

        public void PlayCoinPayout()
        {
            PlaySFX(coinPayoutClip);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
