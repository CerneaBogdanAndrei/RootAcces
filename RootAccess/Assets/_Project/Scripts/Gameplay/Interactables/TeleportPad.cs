using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Always-on teleporter used to chain the rubric showcase rooms (and debug shortcuts).</summary>
    public sealed class TeleportPad : Teleporter
    {
        [SerializeField] private TeleportPad destination;

        public override string Prompt => Strings.Prompts.TeleportPad;
        protected override Teleporter Destination => destination;

        public void SetDestination(TeleportPad pad) => destination = pad;
    }
}
