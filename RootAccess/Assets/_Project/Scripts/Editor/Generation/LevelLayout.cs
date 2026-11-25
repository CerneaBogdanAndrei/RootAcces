using System.Collections.Generic;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Builds a level left to right from reusable segments (rooftop gaps, wall-jump chimney, guard patrol, camera gate,
    /// laser corridor, network-node hop, drone arena, elevator, data vault, turret hall, checkpoint, exit). A cursor
    /// (<see cref="X"/>, <see cref="Floor"/>) tracks where the next segment starts; every segment lays its own floor,
    /// so segments can be reordered freely. <see cref="Finish"/> closes the level and sizes the camera bounds and the
    /// A* grid to whatever was built.
    /// <para>Units: one tile = one world unit. The player stands at y = <see cref="Floor"/>; a tile at row r spans
    /// [r, r + 1]. Jump reach is about 3.4 tiles (6 with the double jump), so walls of 8+ tiles are impassable.</para>
    /// </summary>
    internal sealed class LevelLayout
    {
        /// <summary>Bottom of every building block / floor slab.</summary>
        private const int BaseY = -12;
        /// <summary>Pit fall triggers sit this far below the lowest floor.</summary>
        private const float PitTriggerY = -7f;
        private const int OutdoorHeadroom = 13;

        private readonly BuildContext _ctx;
        private readonly SceneBuilder.GameplayScene _scene;
        private readonly string _prefix;
        private readonly int _startX;
        private int _counter;
        private int _maxTop;
        private readonly List<(int x0, int x1, int above)> _shells = new();
        private readonly List<(string segment, float seconds)> _estimate = new();

        public LevelLayout(BuildContext ctx, SceneBuilder.GameplayScene scene, string idPrefix, int startX, int ceiling = 0)
        {
            _ctx = ctx;
            _scene = scene;
            _prefix = idPrefix;
            _startX = startX;
            X = startX;
            Ceiling = ceiling;
            Props = SceneBuilder.Group("Props", scene.Root);
            Enemies = SceneBuilder.Group("Enemies", scene.Root);
            Spawns = SceneBuilder.Group("SpawnPoints", scene.Root);
            Hazards = SceneBuilder.Group("Hazards", scene.Root);
            Lights = SceneBuilder.Group("Lights", scene.Root);
            Fill(_scene.Walls, startX - 3, startX - 1, BaseY, Ceiling > 0 ? Ceiling + 1 : OutdoorHeadroom + 4, _ctx.TileBrick);
        }

        public int X { get; private set; }
        public int Floor { get; private set; }

        /// <summary>Interior ceiling height above the floor in tiles; 0 means open sky (rooftops).</summary>
        public int Ceiling { get; }

        public bool Indoor => Ceiling > 0;
        public Transform Props { get; }
        public Transform Enemies { get; }
        public Transform Spawns { get; }
        public Transform Hazards { get; }
        public Transform Lights { get; }
        public BuildContext Ctx => _ctx;

        /// <summary>Segment-by-segment playtime estimate for a first run (see <see cref="Estimate"/>).</summary>
        public IReadOnlyList<(string segment, float seconds)> PlaytimeBreakdown => _estimate;

        /// <summary>
        /// Adds a segment's estimated first-run time: walking, waiting for patrols/lasers, one attempt per hacking minigame
        /// (~20–25 s each) and reading hints. Deaths and retries are added on top by <c>PlaytimeReport</c>.
        /// </summary>
        public void Estimate(string segment, float seconds) => _estimate.Add((segment, seconds));

        // ---------------------------------------------------------------- primitives

        public Vector3 P(float dx, float dy = 0f) => new(X + dx, Floor + dy, 0f);

        public string NextId(string kind) => $"{_prefix}_{kind}_{++_counter}";

        public GameObject Put(string prefab, Vector3 position, Transform parent = null, string name = null) =>
            SceneBuilder.Place(_ctx, prefab, position, parent ?? Props, name);

        public void Spawn(string id, float dx = 1.5f) => SceneBuilder.Spawn(_ctx, Spawns, id, P(dx));

        public void Hint(string text, float dx, float dy = 4.2f, float width = 14f) =>
            SceneBuilder.Label(Props, text, P(dx, dy), 3f, new Color(0.65f, 0.95f, 1f), width);

        /// <summary>Solid ground for columns [X + from, X + to) with its top at <see cref="Floor"/> + <paramref name="rise"/>.</summary>
        public void Block(int from, int to, int rise = 0)
        {
            int top = Floor + rise;
            Fill(_scene.Ground, X + from, X + to - 1, BaseY, top - 1, _ctx.TileMetal, _ctx.TileMetalTop);
            if (Indoor) Shell(from, to, top);
            _maxTop = Mathf.Max(_maxTop, top);
        }

        /// <summary>One-tile-thick platform whose walkable top is <see cref="Floor"/> + <paramref name="height"/>.</summary>
        public void Platform(int from, int to, int height) =>
            Fill(_scene.Ground, X + from, X + to - 1, Floor + height - 1, Floor + height - 1, _ctx.TileMetalTop);

        /// <summary>Wall tiles (separate tilemap/layer from the floor) for columns [from, to), rows [Floor+y0, Floor+y1).</summary>
        public void Wall(int from, int to, int y0, int y1, TileBase tile = null) =>
            Fill(_scene.Walls, X + from, X + to - 1, Floor + y0, Floor + y1 - 1, tile ?? _ctx.TileConcrete);

        /// <summary>Interior: backdrop wall and a two-tile ceiling over columns [from, to) for a floor at <paramref name="top"/>.</summary>
        private void Shell(int from, int to, int top)
        {
            Fill(_scene.Backdrop, X + from, X + to - 1, top, top + Ceiling - 1, _ctx.TileBrick);
            Fill(_scene.Walls, X + from, X + to - 1, top + Ceiling, top + Ceiling + 1, _ctx.TileConcrete);
            _shells.Add((X + from, X + to - 1, top + Ceiling + 2));
        }

        private static void Fill(Tilemap map, int x0, int x1, int y0, int y1, TileBase tile, TileBase top = null) =>
            SceneBuilder.Fill(map, x0, x1, y0, y1, tile, top);

        private void Advance(int width) => X += width;

        private Light2D Lamp(float dx, float dy, Color color, float intensity = 1.2f, float radius = 7f)
        {
            Light2D light = PrefabFactory.Light(Lights, "CeilingLamp", Light2D.LightType.Point, color, intensity, radius, P(dx, dy), true, 40f, 80f);
            light.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            return light;
        }

        private void Decor(int width)
        {
            if (Indoor)
            {
                Lamp(width * 0.5f, Ceiling - 0.6f, new Color(0.75f, 0.9f, 1f), 1f, Ceiling + 2f);
                return;
            }

            Put("StreetLamp", P(width - 1.5f));
        }

        private Hazard[] HazardsOf(IEnumerable<GameObject> objects)
        {
            var list = new List<Hazard>();
            foreach (GameObject go in objects) list.Add(go.GetComponent<Hazard>());
            return list.ToArray();
        }

        private static void SetCycle(GameObject laser, float on, float off, float phase) =>
            EditorAssetUtil.SetFields(laser.GetComponent<Hazard>(), so =>
            {
                so.Require("onSeconds").floatValue = on;
                so.Require("offSeconds").floatValue = off;
                so.Require("phaseOffset").floatValue = phase;
            });

        private GameObject HackTerminal(string prefab, float dx, string id, string name, params MonoBehaviour[] targets)
        {
            GameObject terminal = Put(prefab, P(dx), Props, name);
            SceneBuilder.ConfigureTerminal(terminal, id, targets);
            return terminal;
        }

        /// <summary>Pit between two blocks: falling in costs one health point and returns the player to <paramref name="returnDx"/>.</summary>
        private void Pit(int from, int to, float returnDx, int returnRise)
        {
            float width = to - from;
            GameObject zone = PrefabFactory.Go("FallZone", Hazards, Layers.Trigger, new Vector3(X + from + width * 0.5f, PitTriggerY));
            zone.AddComponent<BoxCollider2D>().size = new Vector2(width + 2f, 2f);
            Transform back = PrefabFactory.Go("Return", zone.transform).transform;
            back.position = P(returnDx, returnRise + 0.2f);
            EditorAssetUtil.SetField(zone.AddComponent<FallZone>(), "returnPoint", back);
        }

        // ---------------------------------------------------------------- segments

        /// <summary>Flat stretch with a lamp; optional tutorial hint.</summary>
        public LevelLayout Flat(int width, string hint = null)
        {
            Estimate("Culoar", 6f + width * 0.25f);
            Block(0, width);
            Decor(width);
            if (hint != null) Hint(hint, width * 0.5f);
            Advance(width);
            return this;
        }

        /// <summary>
        /// Rooftop run: <paramref name="gaps"/> pits of 3–4 tiles between blocks that step up and down by up to two
        /// tiles. Each block gets a credit chip; pits send the player back to the block before them.
        /// </summary>
        public LevelLayout Rooftops(int gaps, string hint = null)
        {
            Estimate($"Acoperișuri ({gaps} gropi)", 8f + gaps * 9f);
            int[] rises = { 0, 2, 1, 3, 1, 0 };
            int[] gapWidths = { 3, 4, 3, 4, 3 };
            int baseFloor = Floor;
            if (hint != null) Hint(hint, 3f);
            for (int i = 0; i <= gaps; i++)
            {
                int width = i == 0 ? 6 : 5;
                Floor = baseFloor + rises[i % rises.Length];
                Block(0, width);
                if (i > 0) Put("Pickup_Credits", P(width * 0.5f, 1f));
                if (i == gaps)
                {
                    Advance(width);
                    break;
                }

                if (i % 2 == 1) Put("RooftopAC", P(1.5f));
                Advance(width);
                int gap = gapWidths[i % gapWidths.Length];
                Pit(0, gap, -2.5f, 0);
                Advance(gap);
            }

            Floor = baseFloor;
            Block(0, 3);
            Advance(3);
            return this;
        }

        /// <summary>
        /// Wall-jump chimney: a tall wall blocks the way; the only route over it is the 3-tile shaft between it and a
        /// hanging wall you walk under, climbing by jumping from wall to wall.
        /// </summary>
        public LevelLayout WallJumpChimney(int height = 10, string hint = null)
        {
            Estimate("Horn de wall-jump", 30f);
            const int width = 14;
            Block(0, width);
            int hangTop = Indoor ? Ceiling : height + 3;
            Wall(4, 5, 2, hangTop, _ctx.TileBrick);
            Wall(8, 10, 0, height, _ctx.TileBrick);
            Put("Pickup_Credits", P(9f, height + 0.5f));
            if (hint != null) Hint(hint, 2.5f, 5f, 8f);
            Advance(width);
            return this;
        }

        /// <summary>
        /// Patrol room: one guard walking between two points, a shadow to hide in, crates for cover and an optional
        /// catwalk above the route. Snipers stand on the catwalk instead and shoot down.
        /// </summary>
        public GameObject GuardPatrol(string guardPrefab = "Guard", int width = 18, bool catwalk = true, bool guardOnCatwalk = false)
        {
            Estimate($"Patrulă ({guardPrefab})", guardPrefab == "Guard_Sniper" ? 50f : 40f);
            Block(0, width);
            int walkHeight = 5;
            if (catwalk) Platform(3, width - 3, walkHeight);
            Put("RooftopAC", P(1.8f));
            Put("Crate", P(width - 4.5f, 0.5f));
            Put("HideZone", P(width * 0.35f));
            float y = guardOnCatwalk ? walkHeight : 0f;
            Transform a = PrefabFactory.Go("Patrol_A", Enemies, Layers.Default, P(guardOnCatwalk ? 4.5f : 3.5f, y)).transform;
            Transform b = PrefabFactory.Go("Patrol_B", Enemies, Layers.Default, P(width - (guardOnCatwalk ? 4.5f : 3.5f), y)).transform;
            GameObject guard = Put(guardPrefab, P(width * 0.6f, y), Enemies);
            EditorAssetUtil.SetFields(guard.GetComponent<Guard>(), so => so.SetArray("patrolPoints", new[] { a, b }));
            Decor(width);
            Advance(width);
            return guard;
        }

        /// <summary>
        /// Security gate: a sweeping camera watches a hackable door set into a full-height wall. The camera terminal sits
        /// out of the camera's sight at the entrance; the door terminal is under it.
        /// </summary>
        public (Terminal cameraTerminal, Terminal doorTerminal, Door door, SecurityCamera camera) CameraGate(string cameraId, string doorId,
            string cameraTerminalName = null, string doorTerminalName = null, int doorDifficulty = 1)
        {
            Estimate("Poartă cu cameră (2 spargeri)", 65f + doorDifficulty * 5f);
            const int width = 16;
            Block(0, width);
            int wallTop = Indoor ? Ceiling : OutdoorHeadroom;
            Wall(12, 13, 3, wallTop, _ctx.TileConcrete);
            GameObject camera = Put("SecurityCamera", P(11.4f, Indoor ? Ceiling - 1.2f : 7.5f), Enemies);
            EditorAssetUtil.SetFields(camera.GetComponent<SecurityCamera>(), so => so.Require("centerAngle").floatValue = -125f);
            GameObject door = Put("Door", P(12.5f));
            GameObject cameraTerminal = HackTerminal("Terminal_Camera", 2f, cameraId, cameraTerminalName ?? "Terminal_Camera_Gate", camera.GetComponent<SecurityCamera>());
            GameObject doorTerminal = HackTerminal("Terminal_Door", 9.5f, doorId, doorTerminalName ?? "Terminal_Door_Gate", door.GetComponent<Door>());
            EditorAssetUtil.SetFields(doorTerminal.GetComponent<Terminal>(), so => so.Require("difficulty").intValue = doorDifficulty);
            Put("Crate", P(6f, 0.5f));
            Put("NeonSign", P(7f, Indoor ? Ceiling - 2.5f : 6f));
            Advance(width);
            return (cameraTerminal.GetComponent<Terminal>(), doorTerminal.GetComponent<Terminal>(), door.GetComponent<Door>(), camera.GetComponent<SecurityCamera>());
        }

        /// <summary>
        /// Low tunnel crossed by cycling lasers (too low to jump over them). Time the gaps or hack the control terminal
        /// at the entrance to switch every beam off.
        /// </summary>
        public Terminal LaserCorridor(int lasers, string terminalId, float cycleOffset = 0.55f)
        {
            Estimate($"Coridor cu {lasers} lasere", 25f + lasers * 6f);
            int width = 5 + lasers * 3 + 2;
            Block(0, width);
            Wall(3, width - 1, 4, Indoor ? Ceiling : 8, _ctx.TileConcrete);
            var beams = new List<GameObject>();
            for (int i = 0; i < lasers; i++)
            {
                GameObject laser = Put("Hazard_Laser", P(5.5f + i * 3f), Hazards, $"Laser_{i + 1}");
                SetCycle(laser, 1.6f, 1.3f, i * cycleOffset);
                beams.Add(laser);
            }

            if (lasers >= 2) beams.Add(Put("Hazard_ElectricFloor", P(7f, 0.1f), Hazards, "ElectricFloor"));
            GameObject terminal = HackTerminal("Terminal", 1.5f, terminalId, "Terminal_Lasers", HazardsOf(beams));
            EditorAssetUtil.SetFields(terminal.GetComponent<Terminal>(), so =>
            {
                so.Require("minigame").enumValueIndex = (int)HackingMinigameType.PortScan;
                so.Require("actionOnSuccess").enumValueIndex = (int)HackAction.Disable;
                so.Require("rewardCredits").intValue = 20;
            });
            Lamp(width * 0.5f, 3.4f, new Color(1f, 0.3f, 0.6f), 0.8f, 5f);
            Advance(width);
            return terminal.GetComponent<Terminal>();
        }

        /// <summary>
        /// A building too tall to climb blocks the street; a network node pair carries the player onto its roof (where
        /// <paramref name="rooftopPrefab"/>, e.g. the data shard, waits) and the far side drops back to street level.
        /// </summary>
        public (NetworkNode ground, NetworkNode roof) NodeHop(string spawnId, int height = 10, string rooftopPrefab = null,
            bool requiresHack = false, string hackTerminalId = null)
        {
            Estimate("Salt prin noduri", 25f);
            Block(0, 8);
            if (spawnId != null) Spawn(spawnId, 1f);
            GameObject ground = Put("NetworkNode", P(4f), Props, "NetworkNode_A");
            GameObject nodeTerminal = requiresHack && hackTerminalId != null ? HackTerminal("Terminal", 6.5f, hackTerminalId, "Terminal_Node") : null;
            Advance(8);
            Floor += height;
            Block(0, 14);
            GameObject roof = Put("NetworkNode", P(2f), Props, "NetworkNode_B");
            if (rooftopPrefab != null) Put(rooftopPrefab, P(6.5f));
            Put("NeonSign", P(7f, 4f));
            Put("RooftopAC", P(10f));
            Advance(14);
            Floor -= height;
            SceneBuilder.LinkNodes(ground, roof, requiresHack);
            if (nodeTerminal != null)
                SceneBuilder.ConfigureTerminal(nodeTerminal, hackTerminalId, ground.GetComponent<NetworkNode>(), roof.GetComponent<NetworkNode>());

            Block(0, 5);
            Advance(5);
            return (ground.GetComponent<NetworkNode>(), roof.GetComponent<NetworkNode>());
        }

        /// <summary>Hall with pillars for the A* drones to path around, plus crates to break line of sight.</summary>
        public List<GameObject> DroneArena(int drones, string spawnId = null, string reward = null)
        {
            Estimate($"Arenă cu {drones} drone", 30f + drones * 10f);
            const int width = 24;
            Block(0, width);
            if (spawnId != null) Spawn(spawnId, 1f);
            int top = Indoor ? Ceiling : 11;
            Wall(6, 7, 3, Mathf.Min(top, 8));
            Wall(14, 15, 0, 4);
            Wall(19, 20, 4, Mathf.Min(top, 9));
            Put("Crate", P(10f, 0.5f));
            Put("Crate", P(17f, 0.5f));
            var list = new List<GameObject>();
            for (int i = 0; i < drones; i++)
                list.Add(Put("Drone", P(9f + i * 7f, 6f + (i % 2)), Enemies, $"Drone_{i + 1}"));
            if (reward != null) Put(reward, P(21.5f));
            Decor(width);
            Advance(width);
            return list;
        }

        /// <summary>
        /// Locked elevator up to the next storey: hack the terminal, ride the platform, step off onto the higher floor.
        /// Indoors the rise equals the ceiling height, so the upper storey sits right on top of the lower one.
        /// </summary>
        public Elevator ElevatorUp(string terminalId, int rise, string spawnId = null)
        {
            Estimate("Lift (spargere + urcare)", 50f);
            Block(0, 6);
            if (spawnId != null) Spawn(spawnId, 1f);
            Advance(6);
            int lower = Floor;
            // The shaft floor is one tile lower, so the parked platform sits flush with the corridor floor.
            Fill(_scene.Ground, X, X + 3, BaseY, lower - 2, _ctx.TileMetal, _ctx.TileMetalTop);
            var elevator = Put("Elevator", new Vector3(X + 2f, lower - 0.25f), Props).GetComponent<Elevator>();
            Transform a = PrefabFactory.Go("Elevator_A", Props, Layers.Default, new Vector3(X + 2f, lower - 0.25f)).transform;
            Transform b = PrefabFactory.Go("Elevator_B", Props, Layers.Default, new Vector3(X + 2f, lower + rise - 0.25f)).transform;
            EditorAssetUtil.SetFields(elevator, so =>
            {
                so.Require("pointA").objectReferenceValue = a;
                so.Require("pointB").objectReferenceValue = b;
                so.Require("locked").boolValue = true;
                so.Require("autoReturnSeconds").floatValue = 4f;
            });
            if (Indoor)
            {
                Fill(_scene.Backdrop, X, X + 3, lower, lower + rise + Ceiling - 1, _ctx.TileBrick);
                Fill(_scene.Walls, X, X + 3, lower + rise + Ceiling, lower + rise + Ceiling + 1, _ctx.TileConcrete);
            }

            Put("NeonSign", P(-1.5f, Indoor ? Ceiling - 2.5f : 5f));

            HackTerminal("Terminal", -3.5f, terminalId, "Terminal_Elevator", elevator);
            Advance(4);
            Floor = lower + rise;
            Block(0, 4);
            Advance(4);
            return elevator;
        }

        /// <summary>Vault room: the data terminal (Wire Connect) pays out a data shard; a neon-lit, guarded dead end.</summary>
        public Terminal DataVault(string terminalId, string terminalName = "Terminal_Vault", int difficulty = 2)
        {
            Estimate("Seif de date", 35f + difficulty * 5f);
            const int width = 12;
            Block(0, width);
            Wall(0, 1, 0, 1, _ctx.TileConcrete);
            GameObject terminal = HackTerminal("Terminal_Data", 6f, terminalId, terminalName);
            EditorAssetUtil.SetFields(terminal.GetComponent<Terminal>(), so => so.Require("difficulty").intValue = difficulty);
            Put("NeonSign", P(3f, Indoor ? Ceiling - 2.5f : 5f));
            Put("NeonSign", P(9f, Indoor ? Ceiling - 2.5f : 5f));
            PrefabFactory.Light(Lights, "VaultGlow", Light2D.LightType.Point, new Color(1f, 0.35f, 0.85f), 1.1f, 6f, P(6f, 2f));
            Advance(width);
            return terminal.GetComponent<Terminal>();
        }

        /// <summary>
        /// Turret hall: an EMP pickup at the entrance, one turret on the floor and one on a ledge, crates for cover. Turrets
        /// see 9 m: leave a corridor (≥ 8 tiles) before the next checkpoint so nobody respawns under fire.
        /// </summary>
        public LevelLayout TurretHall(int turrets = 2)
        {
            Estimate($"Sală cu {turrets} turele", 30f + turrets * 6f);
            const int width = 22;
            Block(0, width);
            Put("Pickup_Emp", P(2f));
            Platform(13, 18, 4);
            Put("Crate", P(7f, 0.5f));
            Put("Crate", P(7f, 1.5f));
            Put("Crate", P(11f, 0.5f));
            Put("Turret", P(19.5f), Enemies);
            if (turrets > 1) Put("Turret", P(15.5f, 4f), Enemies);
            Decor(width);
            Advance(width);
            return this;
        }

        /// <summary>
        /// Flooded basin: the floor drops <paramref name="depth"/> tiles and the hole is full of water (springy surface,
        /// buoyancy, crates that float). With <paramref name="dive"/> a wall hangs from the ceiling into the water, so the
        /// only way on is to swim under it.
        /// </summary>
        public WaterBody2D WaterPool(int width, int depth, bool dive = false, int crates = 2)
        {
            Estimate(dive ? "Bazin cu scufundare" : "Bazin", dive ? 35f : 22f);
            Fill(_scene.Ground, X, X + width - 1, BaseY, Floor - depth - 1, _ctx.TileMetal, _ctx.TileMetalTop);
            if (Indoor) Shell(0, width, Floor);
            WaterBody2D water = Water(X + width * 0.5f, Floor - depth, width, depth - 0.5f);
            for (int i = 0; i < crates; i++) Put("Crate", P(2f + i * (width - 4f) / Mathf.Max(1, crates - 1), 0.5f));
            if (dive) Wall(width / 2 - 1, width / 2 + 1, -2, Indoor ? Ceiling : 8, _ctx.TileConcrete);
            Advance(width);
            Block(0, 2);
            Advance(2);
            return water;
        }

        /// <summary>
        /// Sluice puzzle: a dry shaft whose far ledge is one tile higher than a double jump. Hacking the sluice terminal
        /// floods the shaft until the surface sits just under the ledge; swim up and jump out. The floor stays raised
        /// afterwards. A step inside the shaft lets the player climb back out before flooding it.
        /// </summary>
        public FloodControl FloodShaft(string terminalId, string spawnId = null, int shaftDepth = 6, int ledge = 6)
        {
            Estimate("Puț cu stavilă", 60f);
            const int width = 10;
            Block(0, 6);
            if (spawnId != null) Spawn(spawnId, 1f);
            Advance(6);
            Fill(_scene.Ground, X, X + width - 1, BaseY, Floor - shaftDepth - 1, _ctx.TileMetal, _ctx.TileMetalTop);
            if (Indoor) Shell(0, width, Floor + ledge);
            if (Indoor) Fill(_scene.Backdrop, X, X + width - 1, Floor - shaftDepth, Floor + ledge - 1, _ctx.TileBrick);
            Platform(1, 3, -3);
            WaterBody2D water = Water(X + width * 0.5f, Floor - shaftDepth, width, 1f);
            var control = PrefabFactory.Go("FloodControl", Hazards, Layers.Default, P(width * 0.5f, 0f)).AddComponent<FloodControl>();
            EditorAssetUtil.SetFields(control, so =>
            {
                so.Require("water").objectReferenceValue = water;
                so.Require("floodedDepth").floatValue = shaftDepth + ledge - 0.5f;
            });
            HackTerminal("Terminal", -2.5f, terminalId, "Terminal_Sluice", control);
            Put("SewerPipe", P(width * 0.5f, ledge + 1.5f));
            Advance(width);
            Floor += ledge;
            Block(0, 6);
            Put("Pickup_Credits", P(2f, 0f));
            Advance(6);
            return control;
        }

        /// <summary>
        /// Chain of server towers over a pit: each tower top has an arrival and a departure node, linked to the next
        /// tower, so the way across is node → node → node; the last tower drops back down to the floor.
        /// </summary>
        public List<NetworkNode> NodeChain(int towers, string spawnId = null)
        {
            Estimate($"Lanț de {towers} turnuri cu noduri", 12f + towers * 14f);
            int[] heights = { 6, 9, 7, 10 };
            var nodes = new List<NetworkNode>();
            Block(0, 6);
            if (spawnId != null) Spawn(spawnId, 1f);
            GameObject departure = Put("NetworkNode", P(4f), Props, "NetworkNode_Chain_0");
            Advance(6);
            int floor = Floor;
            int highest = 0;
            for (int i = 0; i < towers; i++) highest = Mathf.Max(highest, heights[i % heights.Length]);
            int ceiling = floor + highest + Mathf.Max(6, Ceiling - 3);
            for (int i = 0; i < towers; i++)
            {
                Pit(0, 5, -2.5f, 0);
                if (Indoor) ShellTo(0, 5, BaseY, ceiling);
                Advance(5);
                Floor = floor + heights[i % heights.Length];
                Fill(_scene.Ground, X, X + 5, BaseY, Floor - 1, _ctx.TileMetal, _ctx.TileMetalTop);
                _maxTop = Mathf.Max(_maxTop, Floor);
                if (Indoor) ShellTo(0, 6, Floor, ceiling);
                GameObject arrival = Put("NetworkNode", P(1.2f), Props, $"NetworkNode_Chain_{i + 1}a");
                SceneBuilder.LinkNodes(departure, arrival, false);
                nodes.Add(departure.GetComponent<NetworkNode>());
                nodes.Add(arrival.GetComponent<NetworkNode>());
                if (i % 2 == 0) Put("ServerRack", P(3f));
                Put("Pickup_Credits", P(3f, 0f));
                if (i < towers - 1) departure = Put("NetworkNode", P(4.8f), Props, $"NetworkNode_Chain_{i + 1}b");
                Advance(6);
            }

            Floor = floor;
            Block(0, 4);
            Advance(4);
            return nodes;
        }

        /// <summary>Interior shell between explicit rows (used where neighbouring floors differ, e.g. the node towers).</summary>
        private void ShellTo(int from, int to, int bottom, int ceiling)
        {
            Fill(_scene.Backdrop, X + from, X + to - 1, bottom, ceiling - 1, _ctx.TileBrick);
            Fill(_scene.Walls, X + from, X + to - 1, ceiling, ceiling + 1, _ctx.TileConcrete);
            _shells.Add((X + from, X + to - 1, ceiling + 2));
            _maxTop = Mathf.Max(_maxTop, ceiling - Ceiling);
        }

        /// <summary>Decor along the back wall: server racks (Level03/05) or pipes (Level04), no colliders.</summary>
        public void Dress(string prefab, int from, int to, int spacing, float height = 0f)
        {
            for (int x = from; x < to; x += spacing) Put(prefab, P(x + 0.5f, height));
        }

        private WaterBody2D Water(float centerX, float bottom, float width, float depth)
        {
            GameObject go = Put("WaterBody", new Vector3(centerX, bottom + depth * 0.5f), Hazards);
            var water = go.GetComponent<WaterBody2D>();
            EditorAssetUtil.SetFields(water, so =>
            {
                so.Require("width").floatValue = width;
                so.Require("depth").floatValue = depth;
                so.Require("columns").intValue = Mathf.Clamp(Mathf.RoundToInt(width * 5f), 16, 256);
            });
            go.GetComponent<BoxCollider2D>().size = new Vector2(width, depth);
            go.GetComponent<BuoyancyEffector2D>().surfaceLevel = depth * 0.5f;
            return water;
        }

        public LevelLayout Checkpoint(string checkpointId, string spawnId = null)
        {
            Estimate("Checkpoint", 4f);
            Block(0, 5);
            GameObject checkpoint = Put("Checkpoint", P(2.5f), Props, $"Checkpoint_{checkpointId}");
            EditorAssetUtil.SetFields(checkpoint.GetComponent<Gameplay.Checkpoint>(), so => so.Require("checkpointId").stringValue = checkpointId);
            if (spawnId != null) Spawn(spawnId, 1f);
            Advance(5);
            return this;
        }

        /// <summary>Level exit: completes the level, unlocks the next one and closes the right edge.</summary>
        public LevelExit Exit(string exitId, string levelId, LevelDefinition unlocks, string targetScene = SceneNames.Hub)
        {
            Estimate("Ieșire", 5f);
            Block(0, 8);
            GameObject exit = Put("LevelExit", P(5f), Props, "LevelExit");
            EditorAssetUtil.SetFields(exit.GetComponent<LevelExit>(), so =>
            {
                so.Require("exitId").stringValue = exitId;
                so.Require("levelId").stringValue = levelId;
                so.Require("targetScene").stringValue = targetScene;
                so.Require("completesLevel").boolValue = true;
                so.Require("unlocksLevel").objectReferenceValue = BuildContext.Fresh(unlocks);
            });
            Put("NeonSign", P(5f, Indoor ? Ceiling - 2.5f : 5f));
            Advance(8);
            return exit.GetComponent<LevelExit>();
        }

        /// <summary>
        /// Seals the right edge and sizes the camera confiner and the A* grid to the built area. Returns the level's
        /// world rectangle.
        /// </summary>
        public Rect Finish()
        {
            int top = Indoor ? _maxTop + Ceiling + 2 : _maxTop + OutdoorHeadroom;
            Fill(_scene.Walls, X, X + 2, BaseY, top + 3, _ctx.TileBrick);
            foreach ((int x0, int x1, int above) in _shells)
                if (above <= top + 2) Fill(_scene.Walls, x0, x1, above, top + 2, _ctx.TileConcrete);
            // Outdoors the pits drop out of the bottom of the screen; indoors only the floor slab shows below the player.
            float bottom = Indoor ? -2f : -3f;
            var rect = new Rect(_startX - 1f, bottom, X - _startX + 2f, top - bottom);
            _scene.CameraBounds.points = new[]
            {
                new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax)
            };

            GameObject pathfinding = PrefabFactory.Go("Pathfinding", _scene.Root, Layers.Default, rect.center);
            EditorAssetUtil.SetFields(pathfinding.AddComponent<PathfindingGrid>(), so =>
            {
                so.Require("size").vector2Value = rect.size + new Vector2(2f, 0f);
                so.Require("cellSize").floatValue = 0.5f;
            });
            pathfinding.AddComponent<PathRequestQueue>();
            return rect;
        }
    }
}
