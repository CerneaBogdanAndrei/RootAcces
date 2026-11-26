using System.Collections.Generic;
using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Fan set in the floor that blows a column of air upwards (Level 6). Dynamic bodies inside the trigger have this
    /// step's gravity cancelled and are pushed towards <see cref="LiftSpeed"/>, so the player floats up a wall too tall
    /// to jump. A fan can start switched off (hack its terminal to start it) and can blow in pulses.
    /// <para>The column queries its overlaps itself every physics step instead of waiting for OnTriggerStay2D: a player
    /// standing still on the grill goes to sleep, and a sleeping body gets no trigger callbacks, so a gust starting under
    /// an idle player would otherwise never lift them.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Updraft : MonoBehaviour, IHackable
    {
        [SerializeField] private bool running = true;
        [SerializeField, Min(0f)] private float liftSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 30f;
        [SerializeField, Min(0f), Tooltip("0 = blows all the time. Otherwise a cycle of this many seconds...")]
        private float pulseSeconds;
        [SerializeField, Range(0.1f, 1f), Tooltip("...blowing for this fraction of it.")]
        private float pulseDuty = 0.65f;
        [SerializeField] private ParticleSystem stream;
        [SerializeField] private Transform rotor;
        [SerializeField] private Light2D glow;

        private readonly HashSet<Rigidbody2D> _pushedThisStep = new();
        private readonly List<Collider2D> _overlaps = new();
        private Collider2D _column;
        private ContactFilter2D _solidOnly;
        private bool _wasBlowing;

        public string HackLabel => Strings.HackTargets.Fan;
        public bool IsHacked => running;
        public bool IsRunning => running;
        public float LiftSpeed => liftSpeed;
        public bool IsBlowing => running && (pulseSeconds <= 0f || Mathf.Repeat(Time.time, pulseSeconds) < pulseSeconds * pulseDuty);

        private void Awake()
        {
            _column = GetComponent<Collider2D>();
            _column.isTrigger = true;
            _solidOnly = new ContactFilter2D { useTriggers = false };
            _wasBlowing = !IsBlowing;
            RefreshVisuals();
        }

        public void ApplyHack(HackAction action)
        {
            if (running) return;
            running = true;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.EmpBlast, transform.position));
            EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.FanStarted));
        }

        private void Update()
        {
            RefreshVisuals();
            // An edge-on propeller: squash the blade sprite back and forth so it reads as spinning.
            if (rotor != null && running) rotor.localScale = new Vector3(Mathf.Sin(Time.time * (IsBlowing ? 38f : 9f)), 1f, 1f);
        }

        private void FixedUpdate()
        {
            if (!IsBlowing) return;
            _pushedThisStep.Clear();
            _column.Overlap(_solidOnly, _overlaps);
            foreach (Collider2D other in _overlaps) Push(other.attachedRigidbody);
        }

        private void RefreshVisuals()
        {
            bool blowing = IsBlowing;
            if (blowing == _wasBlowing) return;
            _wasBlowing = blowing;
            if (stream != null)
            {
                if (blowing) stream.Play();
                else stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (glow != null) glow.intensity = blowing ? 1.1f : 0.25f;
        }

        private void Push(Rigidbody2D body)
        {
            // One push per body per physics step, however many colliders it has inside the column.
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic || !_pushedThisStep.Add(body)) return;

            body.WakeUp();
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = body.linearVelocity;
            velocity.y -= Physics2D.gravity.y * body.gravityScale * dt;
            velocity.y = Mathf.MoveTowards(velocity.y, liftSpeed, acceleration * dt);
            body.linearVelocity = velocity;
        }
    }
}
