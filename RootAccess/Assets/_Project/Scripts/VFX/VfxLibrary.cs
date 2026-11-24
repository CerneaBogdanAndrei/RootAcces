using System;
using System.Collections.Generic;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.VFX
{
    [Serializable]
    public sealed class VfxEntry
    {
        [SerializeField] private VfxId id;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(1)] private int prewarm = 2;

        public VfxEntry(VfxId vfxId, GameObject vfxPrefab, int prewarmCount = 2)
        {
            id = vfxId;
            prefab = vfxPrefab;
            prewarm = Mathf.Max(1, prewarmCount);
        }

        public VfxId Id => id;
        public GameObject Prefab => prefab;
        public int Prewarm => prewarm;
    }

    [CreateAssetMenu(menuName = "Root Access/VFX Library", fileName = "VfxLibrary")]
    public sealed class VfxLibrary : ScriptableObject
    {
        [SerializeField] private VfxEntry[] entries = new VfxEntry[0];

        private Dictionary<VfxId, VfxEntry> _lookup;

        public IReadOnlyList<VfxEntry> Entries => entries;

        public bool TryGet(VfxId id, out VfxEntry entry)
        {
            if (_lookup == null || _lookup.Count != entries.Length)
            {
                _lookup = new Dictionary<VfxId, VfxEntry>();
                foreach (VfxEntry e in entries)
                    if (e != null && e.Prefab != null) _lookup[e.Id] = e;
            }

            return _lookup.TryGetValue(id, out entry);
        }

        public void SetEntries(VfxEntry[] values)
        {
            entries = values ?? new VfxEntry[0];
            _lookup = null;
        }
    }
}
