using System.Collections.Generic;
using RootAccess.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>
    /// Trigger volume owning a dedicated CinemachineCamera. When the player enters, that camera gets the highest
    /// priority and the CinemachineBrain blends to it (room-to-room transitions).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CameraRoom : MonoBehaviour
    {
        private const int ActivePriority = 20;
        private const int InactivePriority = 0;
        private static readonly List<CameraRoom> Rooms = new();

        [SerializeField] private CinemachineCamera roomCamera;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (roomCamera == null) Debug.LogError($"[CameraRoom] '{name}' has no camera assigned.", this);
        }

        private void OnEnable()
        {
            Rooms.Add(this);
            if (roomCamera != null) roomCamera.Priority = InactivePriority;
        }

        private void OnDisable() => Rooms.Remove(this);

        private void Start()
        {
            if (roomCamera != null && ServiceLocator.TryGet(out IPlayerTarget player)) roomCamera.Target.TrackingTarget = player.Transform;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<IPlayerTarget>() == null) return;
            Activate();
        }

        public void Activate()
        {
            foreach (CameraRoom room in Rooms)
                if (room.roomCamera != null) room.roomCamera.Priority = room == this ? ActivePriority : InactivePriority;
            if (roomCamera != null && roomCamera.Target.TrackingTarget == null && ServiceLocator.TryGet(out IPlayerTarget player))
                roomCamera.Target.TrackingTarget = player.Transform;
        }
    }
}
