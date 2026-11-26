using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RootAccess.Playthrough
{
    /// <summary>
    /// A virtual keyboard registered with the Input System: the bot "presses" keys on it and the game reads them through
    /// its normal input actions, exactly like a real keyboard. Taps last one frame (enough for "pressed this frame").
    /// </summary>
    internal sealed class BotKeyboard
    {
        private readonly Keyboard _device;
        private readonly HashSet<Key> _held = new();
        private readonly HashSet<Key> _taps = new();
        private readonly List<Key> _buffer = new();

        public BotKeyboard() => _device = InputSystem.AddDevice<Keyboard>("PlaythroughKeyboard");

        /// <summary>Keys kept down until <see cref="Release"/> or <see cref="ReleaseAll"/>.</summary>
        public void Hold(Key key) => _held.Add(key);

        public void Release(Key key) => _held.Remove(key);

        public bool IsHeld(Key key) => _held.Contains(key);

        /// <summary>Down for the next frame only.</summary>
        public void Tap(Key key) => _taps.Add(key);

        public void ReleaseAll()
        {
            _held.Clear();
            _taps.Clear();
        }

        /// <summary>Sends this frame's key state; call once per frame after deciding the inputs.</summary>
        public void Flush()
        {
            _buffer.Clear();
            _buffer.AddRange(_held);
            foreach (Key tap in _taps)
                if (!_held.Contains(tap)) _buffer.Add(tap);
            _taps.Clear();
            InputSystem.QueueStateEvent(_device, new KeyboardState(_buffer.ToArray()));
        }

        public void Dispose()
        {
            if (_device != null && _device.added) InputSystem.RemoveDevice(_device);
        }
    }
}
