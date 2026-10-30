#pragma warning disable 0649, 0169, 0414
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LuckySpin.SlotMachine.Gameplay;

namespace LuckySpin.SlotMachine.UI
{
    /// <summary>
    /// Controls the recent spin history section, displaying a list of recent outcomes and payouts.
    /// </summary>
    public class SpinHistoryUI : MonoBehaviour
    {
        [Header("Container & Prefab")]
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private GameObject historyItemPrefab;
        [SerializeField] private GameObject emptyHistoryPlaceholder;

        [Header("Controls")]
        [SerializeField] private Button toggleHistoryButton;
        [SerializeField] private Button closeHistoryButton;
        [SerializeField] private GameObject historyPanel;

        private readonly List<GameObject> activeItems = new List<GameObject>();

        private void Awake()
        {
            if (toggleHistoryButton != null)
            {
                toggleHistoryButton.onClick.AddListener(TogglePanel);
            }
            if (closeHistoryButton != null)
            {
                closeHistoryButton.onClick.AddListener(ClosePanel);
            }
        }

        public void TogglePanel()
        {
            if (historyPanel != null)
            {
                historyPanel.SetActive(!historyPanel.activeSelf);
            }
        }

        public void OpenPanel()
        {
            if (historyPanel != null)
            {
                historyPanel.SetActive(true);
            }
        }

        public void ClosePanel()
        {
            if (historyPanel != null)
            {
                historyPanel.SetActive(false);
            }
        }

        public void RefreshHistory(List<SpinRecord> records)
        {
            // Clear existing rows
            for (int i = 0; i < activeItems.Count; i++)
            {
                if (activeItems[i] != null)
                {
                    Destroy(activeItems[i]);
                }
            }
            activeItems.Clear();

            if (records == null || records.Count == 0)
            {
                if (emptyHistoryPlaceholder != null)
                {
                    emptyHistoryPlaceholder.SetActive(true);
                }
                return;
            }

            if (emptyHistoryPlaceholder != null)
            {
                emptyHistoryPlaceholder.SetActive(false);
            }

            if (historyItemPrefab == null || listContainer == null) return;

            for (int i = 0; i < records.Count; i++)
            {
                GameObject obj = Instantiate(historyItemPrefab, listContainer);
                SpinHistoryItemUI itemUI = obj.GetComponent<SpinHistoryItemUI>();
                if (itemUI != null)
                {
                    itemUI.Bind(records[i]);
                }
                activeItems.Add(obj);
            }
        }

        public void AddRecord(SpinRecord record)
        {
            if (emptyHistoryPlaceholder != null)
            {
                emptyHistoryPlaceholder.SetActive(false);
            }

            if (historyItemPrefab == null || listContainer == null) return;

            GameObject obj = Instantiate(historyItemPrefab, listContainer);
            obj.transform.SetAsFirstSibling();
            SpinHistoryItemUI itemUI = obj.GetComponent<SpinHistoryItemUI>();
            if (itemUI != null)
            {
                itemUI.Bind(record);
            }
            activeItems.Insert(0, obj);

            // Limit display count to 10
            if (activeItems.Count > 10)
            {
                GameObject oldest = activeItems[activeItems.Count - 1];
                activeItems.RemoveAt(activeItems.Count - 1);
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
        }
    }
}
