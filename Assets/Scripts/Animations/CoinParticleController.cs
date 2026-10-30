#pragma warning disable 0649, 0169, 0414
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LuckySpin.SlotMachine.Animations
{
    /// <summary>
    /// Spawns an animated fountain of golden coin particles in canvas UI space upon winning.
    /// Modular Bonus: Supports both standard win showers and massive 60-coin Jackpot burst fountains.
    /// </summary>
    public class CoinParticleController : MonoBehaviour
    {
        [Header("Coin Particle Settings")]
        [Tooltip("Parent transform to spawn coins into.")]
        [SerializeField] private RectTransform spawnParent;

        [Tooltip("Coin sprite to render.")]
        [SerializeField] private Sprite coinSprite;

        [Tooltip("Number of coins spawned per win fountain.")]
        [SerializeField] private int coinCount = 25;

        [Tooltip("Duration in seconds for coin shower.")]
        [SerializeField] private float duration = 1.6f;

        private class ActiveCoin
        {
            public RectTransform Rect;
            public Image Img;
            public Vector2 Velocity;
            public float AngularSpeed;
            public float Lifetime;
            public float MaxLifetime;
        }

        private readonly List<ActiveCoin> activeCoins = new List<ActiveCoin>();
        private bool isSpawning = false;

        public bool IsActive { get { return activeCoins.Count > 0 || isSpawning; } }
        public int ActiveCoinCount { get { return activeCoins.Count; } }

        private void Awake()
        {
            if (spawnParent == null)
            {
                spawnParent = GetComponent<RectTransform>();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = activeCoins.Count - 1; i >= 0; i--)
            {
                ActiveCoin c = activeCoins[i];
                c.Lifetime += dt;

                if (c.Lifetime >= c.MaxLifetime)
                {
                    if (c.Rect != null) Destroy(c.Rect.gameObject);
                    activeCoins.RemoveAt(i);
                    continue;
                }

                // Apply gravity
                c.Velocity.y -= 1400f * dt;
                c.Rect.anchoredPosition += c.Velocity * dt;

                // Spin rotation
                c.Rect.localEulerAngles = new Vector3(0f, 0f, c.Rect.localEulerAngles.z + c.AngularSpeed * dt);

                // Fade alpha in last 30% of lifetime
                float progress = c.Lifetime / c.MaxLifetime;
                if (progress > 0.7f && c.Img != null)
                {
                    float alpha = 1f - ((progress - 0.7f) / 0.3f);
                    Color col = c.Img.color;
                    col.a = Mathf.Clamp01(alpha);
                    c.Img.color = col;
                }
            }
        }

        /// <summary>
        /// Triggers the celebratory coin shower at the specified origin point.
        /// </summary>
        public void PlayCoinFountain(Vector2 origin)
        {
            StartCoroutine(SpawnFountainRoutine(origin, coinCount, 0.03f, 550f, 950f, 320f));
        }

        /// <summary>
        /// Triggers a mega jackpot coin fountain with increased particle count and velocity arc.
        /// </summary>
        public void PlayJackpotFountain(Vector2 origin)
        {
            StartCoroutine(SpawnFountainRoutine(origin, 60, 0.02f, 750f, 1300f, 500f));
        }

        private IEnumerator SpawnFountainRoutine(Vector2 origin, int count, float interval, float minVy, float maxVy, float maxVx)
        {
            isSpawning = true;
            int spawned = 0;

            while (spawned < count)
            {
                SpawnSingleCoin(origin, minVy, maxVy, maxVx);
                spawned++;
                yield return new WaitForSeconds(interval);
            }
            isSpawning = false;
        }

        private void SpawnSingleCoin(Vector2 origin, float minVy = 550f, float maxVy = 950f, float maxVx = 320f)
        {
            if (spawnParent == null) return;

            GameObject coinObj = new GameObject("CoinParticle");
            coinObj.transform.SetParent(spawnParent, false);

            RectTransform rt = coinObj.AddComponent<RectTransform>();
            rt.anchoredPosition = origin;
            rt.sizeDelta = new Vector2(36f, 36f);

            Image img = coinObj.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold

            if (coinSprite != null)
            {
                img.sprite = coinSprite;
            }

            ActiveCoin coin = new ActiveCoin();
            coin.Rect = rt;
            coin.Img = img;
            // Upward fan velocity
            float vx = UnityEngine.Random.Range(-maxVx, maxVx);
            float vy = UnityEngine.Random.Range(minVy, maxVy);
            coin.Velocity = new Vector2(vx, vy);
            coin.AngularSpeed = UnityEngine.Random.Range(-360f, 360f);
            coin.Lifetime = 0f;
            coin.MaxLifetime = UnityEngine.Random.Range(1.2f, 2.0f);

            activeCoins.Add(coin);
        }

        public void Clear()
        {
            StopAllCoroutines();
            isSpawning = false;
            for (int i = 0; i < activeCoins.Count; i++)
            {
                if (activeCoins[i].Rect != null)
                {
                    Destroy(activeCoins[i].Rect.gameObject);
                }
            }
            activeCoins.Clear();
        }
    }
}
