using RootAccess.Core;
using TMPro;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>
    /// Fixed text baked into a prefab or scene (button captions, titles, level hints, cutscene captions) with its
    /// Romanian and English versions. It follows <see cref="Loc"/> while the label still shows one of the two; once a
    /// view writes something else into the label (a counter, a quest name) the view owns it and this component stays out.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField, TextArea] private string romanian;
        [SerializeField, TextArea] private string english;

        public string Romanian => romanian;
        public string English => english;

        public void Configure(TMP_Text target, string romanianText, string englishText)
        {
            label = target;
            romanian = romanianText;
            english = englishText;
        }

        private void Awake()
        {
            if (label == null) TryGetComponent(out label);
        }

        private void OnEnable()
        {
            EventBus<LanguageChanged>.Subscribe(OnLanguageChanged);
            Apply();
        }

        private void OnDisable() => EventBus<LanguageChanged>.Unsubscribe(OnLanguageChanged);

        private void OnLanguageChanged(LanguageChanged _) => Apply();

        private void Apply()
        {
            if (label == null) return;
            string shown = label.text;
            if (shown != romanian && shown != english) return;
            string wanted = Loc.T(romanian, english);
            if (shown != wanted) label.text = wanted;
        }
    }
}
