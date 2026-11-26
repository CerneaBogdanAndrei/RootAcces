using RootAccess.Core;
using RootAccess.VFX;
using UnityEditor;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>Particle system prefabs (pooled one-shots and looping ambience) and the VFX library asset.</summary>
    public static class VfxFactory
    {
        private sealed class Spec
        {
            public VfxId Id;
            public string Name;
            public bool Loop;
            public float Duration = 0.5f;
            public Vector2 Lifetime = new(0.3f, 0.6f);
            public Vector2 Speed = new(2f, 5f);
            public Vector2 Size = new(0.08f, 0.16f);
            public Color ColorA = Color.white;
            public Color ColorB = Color.white;
            public float Gravity;
            public int Burst = 20;
            public float Rate;
            public ParticleSystemShapeType Shape = ParticleSystemShapeType.Circle;
            public float ShapeRadius = 0.1f;
            public float ShapeAngle = 25f;
            public Vector3 ShapeRotation;
            public Vector3 ShapeScale = Vector3.one;
            public bool Additive = true;
            public bool Stretch;
            public bool ShrinkOverLife = true;
            public int MaxParticles = 64;
        }

        public static void BuildAll(BuildContext ctx)
        {
            Spec[] specs =
            {
                new() { Id = VfxId.Sparks, Name = "VFX_Sparks", Burst = 24, Speed = new(3f, 8f), Lifetime = new(0.2f, 0.45f), Size = new(0.05f, 0.1f), ColorA = new Color(1f, 0.85f, 0.4f) * 3f, ColorB = new Color(1f, 0.3f, 0.1f), Gravity = 1.5f, Stretch = true },
                new() { Id = VfxId.EmpExplosion, Name = "VFX_EmpExplosion", Burst = 60, Speed = new(4f, 9f), Lifetime = new(0.3f, 0.7f), Size = new(0.15f, 0.35f), ColorA = new Color(0.4f, 1.6f, 3f), ColorB = new Color(0.6f, 0.2f, 1f), ShapeRadius = 0.3f, MaxParticles = 96 },
                new() { Id = VfxId.Rain, Name = "VFX_Rain", Loop = true, Duration = 5f, Rate = 160f, Burst = 0, Speed = new(14f, 18f), Lifetime = new(1.2f, 1.6f), Size = new(0.05f, 0.08f), ColorA = new Color(0.7f, 0.85f, 1f, 0.55f), ColorB = new Color(0.7f, 0.85f, 1f, 0.2f), Shape = ParticleSystemShapeType.Box, ShapeScale = new Vector3(40f, 0.5f, 1f), ShapeRotation = new Vector3(90f, 0f, 0f), Additive = false, Stretch = true, ShrinkOverLife = false, MaxParticles = 400 },
                new() { Id = VfxId.Steam, Name = "VFX_Steam", Loop = true, Duration = 4f, Rate = 14f, Burst = 0, Speed = new(0.6f, 1.4f), Lifetime = new(1.5f, 2.5f), Size = new(0.5f, 1.1f), ColorA = new Color(0.85f, 0.9f, 1f, 0.35f), ColorB = new Color(0.85f, 0.9f, 1f, 0f), Shape = ParticleSystemShapeType.Cone, ShapeAngle = 12f, ShapeRotation = new Vector3(-90f, 0f, 0f), Additive = false, ShrinkOverLife = false },
                new() { Id = VfxId.DataStream, Name = "VFX_DataStream", Burst = 30, Speed = new(1.5f, 3.5f), Lifetime = new(0.5f, 0.9f), Size = new(0.05f, 0.09f), ColorA = new Color(0.3f, 2.5f, 1.4f), ColorB = new Color(0.2f, 1f, 2f), Shape = ParticleSystemShapeType.Cone, ShapeAngle = 20f, ShapeRotation = new Vector3(-90f, 0f, 0f) },
                new() { Id = VfxId.LandingDust, Name = "VFX_LandingDust", Burst = 14, Speed = new(0.8f, 2f), Lifetime = new(0.25f, 0.5f), Size = new(0.15f, 0.3f), ColorA = new Color(0.75f, 0.72f, 0.8f, 0.6f), ColorB = new Color(0.6f, 0.6f, 0.7f, 0f), Shape = ParticleSystemShapeType.Cone, ShapeAngle = 80f, ShapeRotation = new Vector3(-90f, 0f, 0f), Additive = false },
                new() { Id = VfxId.Splash, Name = "VFX_Splash", Burst = 28, Speed = new(3f, 6f), Lifetime = new(0.4f, 0.8f), Size = new(0.07f, 0.14f), ColorA = new Color(0.6f, 0.9f, 1f, 0.9f), ColorB = new Color(0.4f, 0.7f, 1f, 0f), Gravity = 2f, Shape = ParticleSystemShapeType.Cone, ShapeAngle = 35f, ShapeRotation = new Vector3(-90f, 0f, 0f), Additive = false },
                new() { Id = VfxId.Death, Name = "VFX_Death", Burst = 50, Speed = new(2f, 6f), Lifetime = new(0.5f, 1f), Size = new(0.08f, 0.2f), ColorA = new Color(2.5f, 0.3f, 1.6f), ColorB = new Color(0.3f, 0.1f, 0.6f), Gravity = 0.6f, ShapeRadius = 0.4f, MaxParticles = 80 },
                new() { Id = VfxId.TeleportOut, Name = "VFX_TeleportOut", Burst = 40, Speed = new(0.5f, 2.5f), Lifetime = new(0.4f, 0.8f), Size = new(0.06f, 0.12f), ColorA = new Color(0.3f, 2.2f, 3f), ColorB = new Color(1f, 0.3f, 2f), Gravity = -1.5f, Shape = ParticleSystemShapeType.Box, ShapeScale = new Vector3(0.7f, 1.4f, 0.1f) },
                new() { Id = VfxId.TeleportIn, Name = "VFX_TeleportIn", Burst = 40, Speed = new(0.5f, 2.5f), Lifetime = new(0.4f, 0.8f), Size = new(0.06f, 0.12f), ColorA = new Color(1f, 0.3f, 2f), ColorB = new Color(0.3f, 2.2f, 3f), Gravity = 1f, Shape = ParticleSystemShapeType.Box, ShapeScale = new Vector3(0.7f, 1.4f, 0.1f) },
                new() { Id = VfxId.TeleportTrail, Name = "VFX_TeleportTrail", Loop = true, Duration = 2f, Rate = 90f, Burst = 0, Speed = new(0.1f, 0.6f), Lifetime = new(0.3f, 0.6f), Size = new(0.08f, 0.16f), ColorA = new Color(0.3f, 2.4f, 3.2f), ColorB = new Color(1.2f, 0.3f, 2f), ShapeRadius = 0.15f },
                new() { Id = VfxId.PickupGlow, Name = "VFX_PickupGlow", Burst = 18, Speed = new(1f, 3f), Lifetime = new(0.3f, 0.6f), Size = new(0.06f, 0.12f), ColorA = new Color(2.5f, 2f, 0.6f), ColorB = new Color(0.6f, 2f, 2.5f) },
                new() { Id = VfxId.Updraft, Name = "VFX_Updraft", Loop = true, Duration = 3f, Rate = 46f, Burst = 0, Speed = new(7f, 11f), Lifetime = new(0.7f, 1.1f), Size = new(0.04f, 0.07f), ColorA = new Color(0.75f, 0.95f, 1f, 0.5f), ColorB = new Color(0.75f, 0.95f, 1f, 0.15f), Shape = ParticleSystemShapeType.Box, ShapeScale = new Vector3(2.6f, 0.2f, 1f), ShapeRotation = new Vector3(-90f, 0f, 0f), Additive = false, Stretch = true, ShrinkOverLife = false, MaxParticles = 120 }
            };

            var entries = new VfxEntry[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                GameObject prefab = Build(specs[i], ctx);
                ctx.VfxPrefabs[specs[i].Id] = prefab;
                ctx.Prefabs[specs[i].Name] = prefab;
                entries[i] = new VfxEntry(specs[i].Id, prefab, specs[i].Loop ? 1 : 3);
            }

            ctx.Vfx = EditorAssetUtil.LoadOrCreateAsset<VfxLibrary>(EditorPaths.ScriptableObjects + "/VfxLibrary.asset");
            ctx.Vfx.SetEntries(entries);
            EditorUtility.SetDirty(ctx.Vfx);
            AssetDatabase.SaveAssets();
        }

        private static GameObject Build(Spec spec, BuildContext ctx)
        {
            var go = new GameObject(spec.Name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = spec.Duration;
            main.loop = spec.Loop;
            main.playOnAwake = spec.Loop;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.Lifetime.x, spec.Lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.Speed.x, spec.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.Size.x, spec.Size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(spec.ColorA, spec.ColorB);
            main.gravityModifier = spec.Gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = spec.MaxParticles;
            main.stopAction = spec.Loop ? ParticleSystemStopAction.None : ParticleSystemStopAction.Callback;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = spec.Rate;
            emission.SetBursts(spec.Burst > 0 ? new[] { new ParticleSystem.Burst(0f, (short)spec.Burst) } : new ParticleSystem.Burst[0]);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = spec.Shape;
            shape.radius = spec.ShapeRadius;
            shape.angle = spec.ShapeAngle;
            shape.rotation = spec.ShapeRotation;
            shape.scale = spec.ShapeScale;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            if (spec.ShrinkOverLife)
            {
                ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = spec.Additive ? ctx.ParticlesAdditive : ctx.ParticlesAlpha;
            renderer.renderMode = spec.Stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (spec.Stretch)
            {
                renderer.velocityScale = 0.05f;
                renderer.lengthScale = 2f;
            }

            renderer.sortingLayerName = SortingLayers.VFX;

            string path = $"{EditorPaths.PrefabsVfx}/{spec.Name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
