#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace LuckySpin.SlotMachine.Animations
{
    /// <summary>
    /// Lightweight tween and animation utilities for smooth UI scaling, button bounce, and rollup counters.
    /// </summary>
    public static class UIAnimationHelper
    {
        /// <summary>
        /// Smooth elastic punch effect on a RectTransform (e.g. for button press feedback).
        /// </summary>
        public static IEnumerator PunchScaleRoutine(RectTransform target, Vector3 punchScale, float duration)
        {
            if (target == null) yield break;

            Vector3 initialScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Damped sine wave decay
                float decay = Mathf.Sin(t * Mathf.PI) * (1f - t);
                target.localScale = initialScale + punchScale * decay;
                yield return null;
            }

            target.localScale = initialScale;
        }

        /// <summary>
        /// Pop-in animation with overshoot (0.0 -> 1.1 -> 1.0) for banners and modals.
        /// </summary>
        public static IEnumerator PopInRoutine(RectTransform target, float duration, Action onComplete = null)
        {
            if (target == null) yield break;

            target.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack formula: overshoot then settle
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
            if (onComplete != null) onComplete();
        }

        /// <summary>
        /// Smoothly rolls up an integer text counter from startValue to endValue.
        /// </summary>
        public static IEnumerator RollupTextRoutine(TextMeshProUGUI textComponent, int startValue, int endValue, float duration, string prefix = "+", string suffix = " G")
        {
            if (textComponent == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutQuad
                float easedT = 1f - (1f - t) * (1f - t);
                int current = (int)Mathf.Lerp((float)startValue, (float)endValue, easedT);
                textComponent.text = string.Format("{0}{1:N0}{2}", prefix, current, suffix);
                yield return null;
            }

            textComponent.text = string.Format("{0}{1:N0}{2}", prefix, endValue, suffix);
        }

        /// <summary>
        /// Evaluates EaseOutBack curve for a normalized time t in [0, 1].
        /// </summary>
        public static float EaseOutBack(float t, float overshoot = 1.4f)
        {
            t = Mathf.Clamp01(t);
            float c3 = overshoot + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Evaluates EaseInQuad curve for a normalized time t in [0, 1].
        /// </summary>
        public static float EaseInQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }
    }
}
