using System.Text;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Systems;
using TMPro;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>
    /// Tab / Select toggles a read-only overview of inventory, contracts and skills. Skeleton for the full
    /// grid inventory, quest log and skill tree screens (ROADMAP P2/P4).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InventoryView : MonoBehaviour
    {
        private const string Empty = "-";

        [SerializeField] private InputReader input;
        [SerializeField] private TMP_Text inventoryText;
        [SerializeField] private QuestLogView questLog;
        [SerializeField] private SkillTreeView skillTree;

        private readonly StringBuilder _builder = new(256);
        private CanvasGroup _group;
        private bool _open;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            SetOpen(false);
        }

        private void OnEnable()
        {
            if (input != null) input.InventoryEvent += Toggle;
            EventBus<InventoryChanged>.Subscribe(OnInventoryChanged);
            EventBus<GameStateChanged>.Subscribe(OnGameState);
        }

        private void OnDisable()
        {
            if (input != null) input.InventoryEvent -= Toggle;
            EventBus<InventoryChanged>.Unsubscribe(OnInventoryChanged);
            EventBus<GameStateChanged>.Unsubscribe(OnGameState);
        }

        public void Toggle() => SetOpen(!_open);

        private void SetOpen(bool open)
        {
            _open = open;
            _group.alpha = open ? 1f : 0f;
            _group.blocksRaycasts = false;
            if (!open) return;
            Render();
            if (questLog != null) questLog.Render();
            if (skillTree != null) skillTree.Render();
        }

        private void Render()
        {
            if (inventoryText == null || !ServiceLocator.TryGet(out SessionService session)) return;
            _builder.Clear();
            foreach (InventorySlot slot in session.Session.Inventory.Slots)
            {
                if (slot.IsEmpty) continue;
                _builder.Append("• ").Append(slot.Item.DisplayName).Append("  ×").Append(slot.Quantity).Append('\n');
            }

            inventoryText.text = _builder.Length > 0 ? _builder.ToString() : Empty;
        }

        private void OnInventoryChanged(InventoryChanged _)
        {
            if (_open) Render();
        }

        private void OnGameState(GameStateChanged evt)
        {
            if (evt.Current != GameState.Playing && _open) SetOpen(false);
        }
    }
}
