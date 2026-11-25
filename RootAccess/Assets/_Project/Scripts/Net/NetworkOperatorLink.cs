using System;
using RootAccess.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace RootAccess.Net
{
    /// <summary>
    /// LAN transport of the Operator link over Netcode for GameObjects custom named messages (no NetworkObjects, no
    /// scene sync): the host runs the level and sends snapshots / the level map to every client; clients send commands
    /// to the host. Payloads are <see cref="OperatorWire"/> byte arrays.
    /// </summary>
    public sealed class NetworkOperatorLink : IOperatorLink
    {
        public const string SnapshotMessage = "ra.operator.snapshot";
        public const string LevelMapMessage = "ra.operator.map";
        public const string CommandMessage = "ra.operator.command";

        private readonly NetworkManager _manager;
        private readonly OperatorSnapshot _received = new();
        private OperatorLevelMap _lastMap;

        public NetworkOperatorLink(NetworkManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            CustomMessagingManager messaging = _manager.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshot);
            messaging.RegisterNamedMessageHandler(LevelMapMessage, OnLevelMap);
            messaging.RegisterNamedMessageHandler(CommandMessage, OnCommand);
            if (_manager.IsServer) _manager.OnClientConnectedCallback += OnClientConnected;
        }

        /// <summary>Host: at least one Operator connected. Client: connected to the host.</summary>
        public bool IsConnected => _manager != null && _manager.IsListening &&
                                   (_manager.IsServer ? _manager.ConnectedClientsIds.Count > (_manager.IsHost ? 1 : 0) : _manager.IsConnectedClient);

        public event Action<OperatorSnapshot> SnapshotReceived;
        public event Action<OperatorLevelMap> LevelMapReceived;
        public event Action<OperatorCommand> CommandReceived;

        public void SendSnapshot(OperatorSnapshot snapshot)
        {
            if (_manager.IsServer && IsConnected) SendToClients(SnapshotMessage, OperatorWire.EncodeSnapshot(snapshot), NetworkDelivery.UnreliableSequenced);
        }

        public void SendLevelMap(OperatorLevelMap map)
        {
            _lastMap = map;
            if (_manager.IsServer && IsConnected) SendToClients(LevelMapMessage, OperatorWire.EncodeLevelMap(map), NetworkDelivery.ReliableFragmentedSequenced);
        }

        public void SendCommand(OperatorCommand command)
        {
            if (_manager.IsServer || !_manager.IsConnectedClient) return;
            Send(NetworkManager.ServerClientId, CommandMessage, OperatorWire.EncodeCommand(command), NetworkDelivery.ReliableSequenced);
        }

        public void Dispose()
        {
            if (_manager != null)
            {
                if (_manager.CustomMessagingManager != null)
                {
                    _manager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotMessage);
                    _manager.CustomMessagingManager.UnregisterNamedMessageHandler(LevelMapMessage);
                    _manager.CustomMessagingManager.UnregisterNamedMessageHandler(CommandMessage);
                }

                _manager.OnClientConnectedCallback -= OnClientConnected;
            }

            SnapshotReceived = null;
            LevelMapReceived = null;
            CommandReceived = null;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId || _lastMap == null) return;
            Send(clientId, LevelMapMessage, OperatorWire.EncodeLevelMap(_lastMap), NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void SendToClients(string message, byte[] payload, NetworkDelivery delivery)
        {
            foreach (ulong client in _manager.ConnectedClientsIds)
                if (client != NetworkManager.ServerClientId) Send(client, message, payload, delivery);
        }

        private void Send(ulong client, string message, byte[] payload, NetworkDelivery delivery)
        {
            // Unreliable messages must fit one packet; anything bigger goes fragmented (large level maps).
            if (delivery == NetworkDelivery.UnreliableSequenced && payload.Length > 1000) delivery = NetworkDelivery.ReliableFragmentedSequenced;
            using var writer = new FastBufferWriter(payload.Length + 16, Allocator.Temp, payload.Length + 64);
            writer.WriteValueSafe(payload);
            _manager.CustomMessagingManager.SendNamedMessage(message, client, writer, delivery);
        }

        private static byte[] Read(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte[] payload);
            return payload;
        }

        private void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            try
            {
                OperatorWire.DecodeSnapshot(Read(reader), _received);
                SnapshotReceived?.Invoke(_received);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NetworkOperatorLink] Dropped a malformed snapshot: {exception.Message}");
            }
        }

        private void OnLevelMap(ulong sender, FastBufferReader reader)
        {
            try
            {
                LevelMapReceived?.Invoke(OperatorWire.DecodeLevelMap(Read(reader)));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NetworkOperatorLink] Dropped a malformed level map: {exception.Message}");
            }
        }

        private void OnCommand(ulong sender, FastBufferReader reader)
        {
            if (!_manager.IsServer) return;
            try
            {
                CommandReceived?.Invoke(OperatorWire.DecodeCommand(Read(reader)));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NetworkOperatorLink] Dropped a malformed command from client {sender}: {exception.Message}");
            }
        }
    }
}
