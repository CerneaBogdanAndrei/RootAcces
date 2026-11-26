using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Puppet rasterizer for the characters. A pose is a tiny skeleton (hip, torso lean, two-bone IK legs, FK arms,
    /// head) that is drawn with shaded capsules, so every animation frame is generated from joint angles instead of
    /// hand-placed rectangles: run cycles come from a sine phase, the scarf and coat flutter, and the normal map is the
    /// real surface normal of each capsule. Canvases are 48×48 px (1.5 × 1.5 units), pivot at the feet.
    /// </summary>
    internal static class CharacterArt
    {
        public const int Size = 48;

        internal enum HeadKind { Hood, Helmet, RiotHelmet, Cap, Bare }

        internal enum Weapon { None, Baton, Rifle, Shield }

        internal struct Pose
        {
            /// <summary>Hip offset from the canvas centre (x) and hip height in px (y).</summary>
            public float HipX, HipY;
            /// <summary>Torso lean in degrees (positive = forward, towards +x).</summary>
            public float Lean;
            public float HeadTilt;
            /// <summary>Ankle targets: x relative to the hip, y absolute (1.8 = standing on the ground).</summary>
            public Vector2 FootF, FootB;
            /// <summary>Upper-arm angle from straight down (positive = forward) and elbow bend (positive = forearm forward).</summary>
            public float ArmF, ElbowF, ArmB, ElbowB;
            /// <summary>0 = scarf/coat hang down, 1 = they stream behind the character.</summary>
            public float Flutter;
            /// <summary>Whole-body rotation in degrees around the feet (positive = falls backwards).</summary>
            public float Roll;
            public bool VisorOff;
            /// <summary>Weapon raised in the front hand (attack frames) instead of holstered.</summary>
            public bool WeaponUp;
            public bool MuzzleFlash;
        }

        internal sealed class Style
        {
            public Ramp Jacket, Pants, Boots, Head, Gear, Accent;
            public Color32 Visor = Paint.Rgb(46, 242, 255);
            public Color32 Rim = Paint.Rgb(46, 242, 255);
            public Color32 Outline = Paint.Rgb(8, 6, 18);
            public HeadKind HeadKind = HeadKind.Hood;
            public Weapon Weapon = Weapon.None;
            public float Bulk = 1f;
            public bool Scarf, Backpack, CoatTails, ShoulderPads, Belt = true;
        }

        private const float Thigh = 9f, Shin = 8.6f, UpperArm = 6f, Forearm = 5.6f, Torso = 12f;
        public const float Ground = 1.8f;

        private sealed class Skeleton
        {
            public Vector2 Hip, Neck, Head, Up, Fwd;
            public Vector2 HipF, KneeF, AnkleF, ToeF, HipB, KneeB, AnkleB, ToeB;
            public Vector2 ShoulderF, ElbowF, HandF, ShoulderB, ElbowB, HandB;

            public void Rotate(Vector2 pivot, float degrees)
            {
                if (Mathf.Approximately(degrees, 0f)) return;
                Vector2 R(Vector2 p) => pivot + Rot(p - pivot, degrees);
                Hip = R(Hip); Neck = R(Neck); Head = R(Head);
                HipF = R(HipF); KneeF = R(KneeF); AnkleF = R(AnkleF); ToeF = R(ToeF);
                HipB = R(HipB); KneeB = R(KneeB); AnkleB = R(AnkleB); ToeB = R(ToeB);
                ShoulderF = R(ShoulderF); ElbowF = R(ElbowF); HandF = R(HandF);
                ShoulderB = R(ShoulderB); ElbowB = R(ElbowB); HandB = R(HandB);
                Up = Rot(Up, degrees);
                Fwd = Rot(Fwd, degrees);
            }

            public void Translate(Vector2 d)
            {
                Hip += d; Neck += d; Head += d;
                HipF += d; KneeF += d; AnkleF += d; ToeF += d;
                HipB += d; KneeB += d; AnkleB += d; ToeB += d;
                ShoulderF += d; ElbowF += d; HandF += d;
                ShoulderB += d; ElbowB += d; HandB += d;
            }
        }

        /// <summary>Rotates counter-clockwise by <paramref name="degrees"/>.</summary>
        private static Vector2 Rot(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>Two-bone IK: the knee always bends forward (+x before any roll).</summary>
        private static Vector2 Knee(Vector2 hip, Vector2 ankle, float a, float b)
        {
            Vector2 d = ankle - hip;
            float dist = Mathf.Clamp(d.magnitude, 0.5f, a + b - 0.05f);
            Vector2 u = d.normalized;
            float cos = Mathf.Clamp((a * a + dist * dist - b * b) / (2f * a * dist), -1f, 1f);
            float alpha = Mathf.Acos(cos);
            var perp = new Vector2(-u.y, u.x);
            if (perp.x < 0f) perp = -perp;
            return hip + u * (a * Mathf.Cos(alpha)) + perp * (a * Mathf.Sin(alpha));
        }

        private static Skeleton Solve(Pose p, Style s)
        {
            var k = new Skeleton();
            float cx = Size * 0.5f;
            k.Hip = new Vector2(cx + p.HipX, p.HipY);
            k.Up = Rot(Vector2.up, -p.Lean);
            k.Fwd = Rot(k.Up, -90f);
            k.Neck = k.Hip + k.Up * (Torso * Mathf.Lerp(1f, 1.05f, s.Bulk - 1f));
            k.Head = k.Neck + Rot(k.Up, -p.HeadTilt) * 5.4f;

            k.HipF = k.Hip + Vector2.right * 0.8f;
            k.HipB = k.Hip - Vector2.right * 0.8f;
            k.AnkleF = new Vector2(k.Hip.x + p.FootF.x, p.FootF.y);
            k.AnkleB = new Vector2(k.Hip.x + p.FootB.x, p.FootB.y);
            k.KneeF = Knee(k.HipF, k.AnkleF, Thigh, Shin);
            k.KneeB = Knee(k.HipB, k.AnkleB, Thigh, Shin);
            k.ToeF = k.AnkleF + Rot(Vector2.right, p.FootF.y > 3f ? -25f : 0f) * 3.2f;
            k.ToeB = k.AnkleB + Rot(Vector2.right, p.FootB.y > 3f ? -25f : 0f) * 3.2f;

            Vector2 down = -k.Up;
            Vector2 Dir(float degrees) => down * Mathf.Cos(degrees * Mathf.Deg2Rad) + k.Fwd * Mathf.Sin(degrees * Mathf.Deg2Rad);
            k.ShoulderF = k.Neck - k.Up * 1.8f + k.Fwd * 0.6f;
            k.ShoulderB = k.Neck - k.Up * 1.8f - k.Fwd * 1.2f;
            k.ElbowF = k.ShoulderF + Dir(p.ArmF) * UpperArm;
            k.HandF = k.ElbowF + Dir(p.ArmF + p.ElbowF) * Forearm;
            k.ElbowB = k.ShoulderB + Dir(p.ArmB) * UpperArm;
            k.HandB = k.ElbowB + Dir(p.ArmB + p.ElbowB) * Forearm;

            if (!Mathf.Approximately(p.Roll, 0f))
            {
                // Lying down: rotate about the hip, then rest the body on the ground, centred on the canvas.
                k.Rotate(k.Hip, p.Roll);
                Vector2[] points = { k.Hip, k.Neck, k.Head, k.KneeF, k.AnkleF, k.ToeF, k.KneeB, k.AnkleB, k.ToeB, k.HandF, k.HandB, k.ElbowF, k.ElbowB };
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue;
                foreach (Vector2 point in points)
                {
                    minX = Mathf.Min(minX, point.x);
                    maxX = Mathf.Max(maxX, point.x);
                    minY = Mathf.Min(minY, point.y);
                }

                k.Translate(new Vector2(cx - (minX + maxX) * 0.5f, 2.6f - minY));
            }

            return k;
        }

        /// <summary>Draws one frame.</summary>
        public static PixelCanvas Draw(Pose p, Style s)
        {
            var c = new PixelCanvas(Size, Size);
            Skeleton k = Solve(p, s);
            float bulk = s.Bulk;
            const float backShade = 0.7f;

            // Back limbs first, darker: they are further from the key light.
            Arm(c, k.ShoulderB, k.ElbowB, k.HandB, s, bulk, backShade);
            Leg(c, k.HipB, k.KneeB, k.AnkleB, k.ToeB, s, bulk, backShade);

            if (s.CoatTails)
            {
                Vector2 tail = k.Hip - k.Fwd * (2.5f + p.Flutter * 3.5f) - k.Up * (8.5f - p.Flutter * 3.5f);
                c.Capsule(k.Hip - k.Fwd * 1.5f + k.Up * 2f, tail, 3.4f * bulk, 2.2f * bulk, s.Jacket.Darker(0.78f));
            }

            if (s.Backpack)
            {
                Vector2 pack = k.Neck - k.Up * 5.5f - k.Fwd * 4.2f;
                c.Capsule(pack - k.Up * 2.2f, pack + k.Up * 2.2f, 2.2f, 2f, s.Gear.Darker(0.72f));
                Vector2 led = pack - k.Fwd * 1.2f;
                c.Set(Mathf.RoundToInt(led.x), Mathf.RoundToInt(led.y), s.Visor);
                Vector2 antenna = pack + k.Up * 2.6f - k.Fwd * 1f;
                Vector2 tip = antenna + k.Up * 5f - k.Fwd * 1.2f;
                c.Line(Mathf.RoundToInt(antenna.x), Mathf.RoundToInt(antenna.y), Mathf.RoundToInt(tip.x), Mathf.RoundToInt(tip.y), s.Gear.Dark);
                c.Set(Mathf.RoundToInt(tip.x), Mathf.RoundToInt(tip.y), s.Visor);
            }

            if (s.Weapon == Weapon.Rifle && !p.WeaponUp)
            {
                // Slung across the back.
                Vector2 a = k.Neck - k.Up * 2f - k.Fwd * 5f, b = k.Hip + k.Fwd * 3f - k.Up * 1f;
                c.Capsule(a, b, 1f, 1f, s.Gear.Darker(0.8f));
            }

            // Torso: tapered from the pelvis to the chest, a touch forward of the spine.
            c.Capsule(k.Hip + k.Up * 1.2f, k.Neck - k.Up * 1.6f + k.Fwd * 0.4f, 3.7f * bulk, 4.5f * bulk, s.Jacket);
            if (s.ShoulderPads) c.Dome(k.ShoulderF.x - k.Fwd.x * 0.6f, k.ShoulderF.y + 0.6f, 3.2f * bulk, 2.4f * bulk, s.Gear);
            // Jacket zip / chest rig line in the accent colour.
            Vector2 zipA = k.Hip + k.Up * 2f + k.Fwd * 3f * bulk, zipB = k.Neck - k.Up * 2.2f + k.Fwd * 3.6f * bulk;
            c.Line(Mathf.RoundToInt(zipA.x), Mathf.RoundToInt(zipA.y), Mathf.RoundToInt(zipB.x), Mathf.RoundToInt(zipB.y), s.Accent.Base);
            if (s.Belt)
            {
                Vector2 beltA = k.Hip + k.Up * 1.4f - k.Fwd * 3.6f * bulk, beltB = k.Hip + k.Up * 1.4f + k.Fwd * 3.6f * bulk;
                c.Line(Mathf.RoundToInt(beltA.x), Mathf.RoundToInt(beltA.y), Mathf.RoundToInt(beltB.x), Mathf.RoundToInt(beltB.y), s.Gear.Dark, 2);
                Vector2 buckle = k.Hip + k.Up * 1.4f + k.Fwd * 3f * bulk;
                c.Set(Mathf.RoundToInt(buckle.x), Mathf.RoundToInt(buckle.y), s.Accent.Light);
            }

            if (s.Weapon == Weapon.Baton && !p.WeaponUp)
            {
                Vector2 a = k.Hip + k.Up * 1f + k.Fwd * 2.5f, b = a - k.Up * 6f + k.Fwd * 1.2f;
                c.Capsule(a, b, 0.9f, 0.9f, s.Gear.Darker(0.7f));
                c.Set(Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.y), s.Accent.Light);
            }

            Leg(c, k.HipF, k.KneeF, k.AnkleF, k.ToeF, s, bulk, 1f);

            if (s.Scarf)
            {
                Vector2 wrap = k.Neck - k.Up * 0.4f;
                c.Capsule(wrap - k.Fwd * 2.6f, wrap + k.Fwd * 2.4f, 2.2f, 2.2f, s.Accent);
                // The tail hangs down the back at rest and streams out horizontally when the hacker moves fast.
                float wave = Mathf.Clamp01(p.Flutter);
                Vector2 root = wrap - k.Fwd * 2.2f;
                Vector2 t1 = root + Vector2.Lerp(-k.Fwd * 1.6f - k.Up * 3.8f, -k.Fwd * 4.6f - k.Up * 0.6f, wave);
                Vector2 t2 = t1 + Vector2.Lerp(-k.Fwd * 0.8f - k.Up * 3.6f, -k.Fwd * 4.6f + k.Up * (0.4f + Mathf.Sin(p.Flutter * 7f) * 0.9f), wave);
                c.Capsule(root, t1, 1.7f, 1.4f, s.Accent);
                c.Capsule(t1, t2, 1.4f, 0.9f, s.Accent.Darker(0.85f));
            }

            Head(c, k, p, s);
            Arm(c, k.ShoulderF, k.ElbowF, k.HandF, s, bulk, 1f);
            HeldWeapon(c, k, p, s);

            c.Rim(s.Rim, new Vector2Int(1, 0), 0.5f);
            c.Rim(s.Rim, new Vector2Int(0, 1), 0.22f);
            c.SelectiveOutline(s.Outline, 0.3f);
            return c;
        }

        private static void Leg(PixelCanvas c, Vector2 hip, Vector2 knee, Vector2 ankle, Vector2 toe, Style s, float bulk, float shade)
        {
            Ramp pants = shade < 1f ? s.Pants.Darker(shade) : s.Pants;
            Ramp boots = shade < 1f ? s.Boots.Darker(shade) : s.Boots;
            c.Capsule(hip, knee, 2.8f * bulk, 2.3f * bulk, pants);
            c.Capsule(knee, ankle, 2.3f * bulk, 1.9f * bulk, pants);
            c.Capsule(ankle + Vector2.up * 0.6f, toe, 2f * bulk, 1.5f * bulk, boots);
        }

        private static void Arm(PixelCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, Style s, float bulk, float shade)
        {
            Ramp sleeve = shade < 1f ? s.Jacket.Darker(shade) : s.Jacket;
            Ramp glove = shade < 1f ? s.Boots.Darker(shade) : s.Boots;
            c.Capsule(shoulder, elbow, 2.1f * bulk, 1.8f * bulk, sleeve);
            c.Capsule(elbow, hand, 1.8f * bulk, 1.5f * bulk, sleeve);
            c.Dome(hand.x, hand.y, 1.6f * bulk, 1.6f * bulk, glove);
        }

        private static void Head(PixelCanvas c, Skeleton k, Pose p, Style s)
        {
            Vector2 h = k.Head;
            Vector2 up = Rot(k.Up, -p.HeadTilt);
            Vector2 fwd = Rot(up, -90f);
            float r = s.HeadKind == HeadKind.RiotHelmet ? 5.6f : 5.1f;
            switch (s.HeadKind)
            {
                case HeadKind.Hood:
                    c.Capsule(h - fwd * 3.2f + up * 2.6f, h - fwd * 5.6f - up * 0.4f, 2f, 1.3f, s.Head);
                    c.Dome(h.x, h.y, r, r * 1.08f, s.Head);
                    // Face in the hood's shadow.
                    Vector2 face = h + fwd * 2.4f - up * 0.6f;
                    c.Ellipse(face.x, face.y, 2.6f, 3.1f, s.Head.Deep);
                    break;
                case HeadKind.Helmet:
                case HeadKind.RiotHelmet:
                    c.Dome(h.x, h.y, r, r, s.Head);
                    Vector2 brim = h + up * 1.2f;
                    c.Capsule(brim - fwd * 4.5f, brim + fwd * 4.8f, 1.1f, 1.1f, s.Head.Darker(0.8f));
                    if (s.HeadKind == HeadKind.RiotHelmet)
                    {
                        Vector2 guard = h + fwd * 2.5f - up * 2.2f;
                        c.Capsule(guard - fwd * 1f, guard + fwd * 2.3f, 1.6f, 1.4f, s.Gear);
                    }

                    break;
                case HeadKind.Cap:
                    c.Dome(h.x, h.y, r, r, s.Head);
                    Vector2 crown = h + up * 2f - fwd * 0.4f;
                    c.Capsule(crown - fwd * 3.6f, crown + fwd * 3.2f, 2.4f, 2.4f, s.Gear);
                    Vector2 peak = h + up * 1.6f + fwd * 4.4f;
                    c.Capsule(peak - fwd * 1.4f, peak + fwd * 2.2f, 0.9f, 0.7f, s.Gear.Darker(0.7f));
                    break;
                default:
                    c.Dome(h.x, h.y, r, r, s.Head);
                    // Hair: a dark cap over the back of the head and a short tail.
                    Vector2 hair = h + up * 1.6f - fwd * 1.2f;
                    c.Dome(hair.x, hair.y, r * 0.95f, r * 0.72f, s.Gear.Darker(0.55f));
                    c.Capsule(h - fwd * 4.4f + up * 1f, h - fwd * 6.4f - up * 3.4f, 1.6f, 1f, s.Gear.Darker(0.55f));
                    break;
            }

            if (p.VisorOff) return;
            Vector2 v0 = h + fwd * 1.2f + up * 0.2f, v1 = h + fwd * (r - 0.2f) + up * 0.2f;
            c.Line(Mathf.RoundToInt(v0.x), Mathf.RoundToInt(v0.y), Mathf.RoundToInt(v1.x), Mathf.RoundToInt(v1.y), s.Visor, 2);
            c.Set(Mathf.RoundToInt(v1.x), Mathf.RoundToInt(v1.y), Paint.Mix(s.Visor, Paint.Rgb(255, 255, 255), 0.7f));
            c.Glow(v1.x, v1.y + 0.5f, 3.2f, new Color(s.Visor.r / 255f, s.Visor.g / 255f, s.Visor.b / 255f, 0.35f));
        }

        private static void HeldWeapon(PixelCanvas c, Skeleton k, Pose p, Style s)
        {
            switch (s.Weapon)
            {
                case Weapon.Baton when p.WeaponUp:
                {
                    Vector2 dir = (k.HandF - k.ElbowF).normalized;
                    Vector2 tip = k.HandF + dir * 7f;
                    c.Capsule(k.HandF, tip, 1f, 0.9f, s.Gear.Darker(0.7f));
                    c.Glow(tip.x, tip.y, 3.5f, new Color(1f, 0.85f, 0.3f, 0.7f));
                    c.Set(Mathf.RoundToInt(tip.x), Mathf.RoundToInt(tip.y), Paint.Rgb(255, 240, 160));
                    break;
                }
                case Weapon.Rifle when p.WeaponUp:
                {
                    Vector2 stock = k.ShoulderF - k.Fwd * 2f - k.Up * 0.6f;
                    Vector2 muzzle = stock + k.Fwd * 17f;
                    c.Capsule(stock, muzzle, 1.4f, 0.8f, s.Gear.Darker(0.75f));
                    Vector2 scope = stock + k.Fwd * 6f + k.Up * 1.8f;
                    c.Capsule(scope - k.Fwd * 2f, scope + k.Fwd * 2f, 1f, 1f, s.Gear);
                    c.Set(Mathf.RoundToInt(muzzle.x), Mathf.RoundToInt(muzzle.y), Paint.Rgb(255, 60, 70));
                    if (p.MuzzleFlash) c.Glow(muzzle.x + 2f, muzzle.y, 4.5f, new Color(1f, 0.8f, 0.4f, 0.95f), 1.2f);
                    break;
                }
                case Weapon.Shield:
                {
                    Vector2 centre = k.HandF + k.Fwd * (p.WeaponUp ? 2.5f : 1.5f) + k.Up * 2f;
                    var shield = new List<Vector2>();
                    for (int i = 0; i < 4; i++)
                    {
                        float sx = i == 0 || i == 3 ? -1.6f : 1.6f, sy = i < 2 ? -9f : 7f;
                        shield.Add(centre + k.Fwd * sx + k.Up * sy);
                    }

                    c.Polygon(shield, s.Gear.Base);
                    Vector2 stripe = centre + k.Up * 1f;
                    c.Line(Mathf.RoundToInt(stripe.x), Mathf.RoundToInt(stripe.y - 5f), Mathf.RoundToInt(stripe.x), Mathf.RoundToInt(stripe.y + 4f), s.Accent.Base, 1);
                    c.Line(Mathf.RoundToInt(centre.x + k.Fwd.x * 1.6f), Mathf.RoundToInt(centre.y - 9f), Mathf.RoundToInt(centre.x + k.Fwd.x * 1.6f), Mathf.RoundToInt(centre.y + 7f), s.Gear.Light);
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- pose library

        public static Pose Stand(float bob = 0f) => new()
        {
            HipY = 17.4f + bob, FootF = new Vector2(1.8f, Ground), FootB = new Vector2(-1.8f, Ground),
            ArmF = 8f, ElbowF = 12f, ArmB = -8f, ElbowB = 10f, Lean = 2f
        };

        /// <summary>Run/walk cycle at <paramref name="phase"/> ∈ [0, 1): stride and lift in px, arm swing in degrees.</summary>
        public static Pose Stride(float phase, float stride, float lift, float armSwing, float lean, float bob, float elbow)
        {
            float a = phase * Mathf.PI * 2f;
            float sin = Mathf.Sin(a), cos = Mathf.Cos(a);
            return new Pose
            {
                HipY = 16.4f + (1f - Mathf.Abs(sin)) * bob,
                Lean = lean,
                FootF = new Vector2(stride * sin + 1f, Ground + Mathf.Max(0f, cos) * lift),
                FootB = new Vector2(-stride * sin - 1f, Ground + Mathf.Max(0f, -cos) * lift),
                ArmF = -armSwing * sin + 2f, ElbowF = elbow, ArmB = armSwing * sin, ElbowB = elbow,
                Flutter = 0.75f + 0.2f * Mathf.Sin(a * 2f), HeadTilt = -lean * 0.4f
            };
        }

        // ---------------------------------------------------------------- frame sets

        /// <summary>Every player frame by name (without the "player_" prefix).</summary>
        public static Dictionary<string, PixelCanvas> PlayerFrames()
        {
            Style s = PlayerStyle();
            var frames = new Dictionary<string, PixelCanvas>();
            for (int i = 0; i < 4; i++)
            {
                float t = i / 4f * Mathf.PI * 2f;
                Pose idle = Stand(-0.55f * (1f - Mathf.Cos(t)));
                idle.Flutter = 0.12f + 0.1f * Mathf.Sin(t);
                idle.ArmF += Mathf.Sin(t) * 2f;
                frames[$"idle_{i}"] = Draw(idle, s);
            }

            for (int i = 0; i < 8; i++)
                frames[$"run_{i}"] = Draw(Stride(i / 8f, 7f, 5f, 40f, 12f, 1.4f, 78f), s);

            frames["jump"] = Draw(new Pose
            {
                HipY = 19f, Lean = 6f, FootF = new Vector2(4f, 9f), FootB = new Vector2(-3f, 5.5f),
                ArmF = 150f, ElbowF = -10f, ArmB = -40f, ElbowB = 30f, Flutter = 0.1f
            }, s);
            for (int i = 0; i < 2; i++)
                frames[$"fall_{i}"] = Draw(new Pose
                {
                    HipY = 19f, Lean = 2f, FootF = new Vector2(3f, 4f), FootB = new Vector2(-4f, 2.6f + i),
                    ArmF = 118f + i * 16f, ElbowF = 22f, ArmB = -100f - i * 14f, ElbowB = 30f, Flutter = 1.1f + i * 0.2f, HeadTilt = 8f
                }, s);

            PixelCanvas dash = Draw(new Pose
            {
                HipY = 17f, Lean = 32f, FootF = new Vector2(7f, 8f), FootB = new Vector2(-8f, 4f),
                ArmF = 70f, ElbowF = 50f, ArmB = -60f, ElbowB = 20f, Flutter = 1.6f, HeadTilt = -18f
            }, s);
            for (int i = 0; i < 5; i++) dash.Rect(1 + i, 12 + i * 4, 9 - i * 2, 1, Paint.Alpha(s.Visor, 170 - i * 25));
            frames["dash"] = dash;

            frames["wallslide"] = Draw(new Pose
            {
                HipY = 18f, Lean = -6f, FootF = new Vector2(5f, 5f), FootB = new Vector2(2f, 2.2f),
                ArmF = 165f, ElbowF = -15f, ArmB = 25f, ElbowB = 40f, Flutter = 1.25f, HeadTilt = -10f
            }, s);

            for (int i = 0; i < 2; i++)
                frames[$"crouch_{i}"] = Draw(new Pose
                {
                    HipY = 10.5f - i * 0.4f, Lean = 26f, FootF = new Vector2(4.5f, Ground), FootB = new Vector2(-4f, Ground),
                    ArmF = 35f, ElbowF = 45f, ArmB = 10f, ElbowB = 60f, Flutter = 0.2f, HeadTilt = -18f
                }, s);

            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * Mathf.PI * 2f;
                frames[$"swim_{i}"] = Draw(new Pose
                {
                    HipX = -5f, HipY = 15f, Lean = 78f, FootF = new Vector2(-11f, 15f + Mathf.Sin(a) * 3f), FootB = new Vector2(-12f, 14f - Mathf.Sin(a) * 3f),
                    ArmF = 150f + Mathf.Sin(a) * 30f, ElbowF = 10f, ArmB = 60f - Mathf.Sin(a) * 30f, ElbowB = 20f, Flutter = 1.2f, HeadTilt = -60f
                }, s);
            }

            for (int i = 0; i < 4; i++)
            {
                Pose hack = Stand(i % 2 == 0 ? 0f : -0.3f);
                hack.ArmF = 72f + (i % 2) * 8f;
                hack.ElbowF = 38f;
                hack.ArmB = 58f + ((i + 1) % 2) * 8f;
                hack.ElbowB = 48f;
                hack.Lean = 6f;
                hack.HeadTilt = 12f;
                PixelCanvas canvas = Draw(hack, s);
                // Holographic keyboard + screen in front of the hands.
                canvas.Rect(32, 22, 10, 1, Paint.Alpha(s.Visor, 200)).Rect(33, 25, 9, 8, Paint.Alpha(s.Visor, 70)).Frame(33, 25, 9, 8, Paint.Alpha(s.Visor, 180));
                for (int line = 0; line < 3; line++) canvas.Rect(35, 27 + line * 2, 3 + (line + i) % 4, 1, Paint.Alpha(Paint.Rgb(255, 255, 255), 200));
                frames[$"hack_{i}"] = canvas;
            }

            frames["hurt"] = Draw(new Pose
            {
                HipY = 17f, Lean = -16f, FootF = new Vector2(4f, Ground), FootB = new Vector2(-3f, 3f),
                ArmF = -30f, ElbowF = 40f, ArmB = -60f, ElbowB = 30f, Flutter = 0.9f, HeadTilt = -20f
            }, s).Tint(new Color(1f, 0.62f, 0.62f));

            frames["death_0"] = Draw(new Pose
            {
                HipY = 9f, Lean = 30f, FootF = new Vector2(5f, Ground), FootB = new Vector2(-4f, 3f),
                ArmF = 10f, ElbowF = 20f, ArmB = 0f, ElbowB = 20f, Flutter = 0.1f, HeadTilt = 30f
            }, s);
            Pose lying = Stand();
            lying.Roll = 84f;
            lying.HipX = 8f;
            lying.HipY = 12f;
            lying.ArmF = 40f;
            lying.ArmB = -30f;
            frames["death_1"] = Draw(lying, s);
            lying.VisorOff = true;
            frames["death_2"] = Draw(lying, s);
            return frames;
        }

        /// <summary>Idle, walk and attack frames of one guard archetype (without the prefix).</summary>
        public static Dictionary<string, PixelCanvas> GuardFrames(Style s)
        {
            var frames = new Dictionary<string, PixelCanvas>();
            bool heavy = s.Weapon == Weapon.Shield, sniper = s.Weapon == Weapon.Rifle;
            for (int i = 0; i < 4; i++)
            {
                float t = i / 4f * Mathf.PI * 2f;
                Pose idle = Stand(-0.45f * (1f - Mathf.Cos(t)));
                if (heavy) { idle.ArmF = 55f; idle.ElbowF = 50f; }
                if (sniper) { idle.ArmF = 20f; idle.ElbowF = 60f; }
                idle.Flutter = 0.1f;
                frames[$"idle_{i}"] = Draw(idle, s);
            }

            for (int i = 0; i < 6; i++)
            {
                Pose walk = Stride(i / 6f, heavy ? 4f : 5f, 3f, heavy ? 10f : 22f, 4f, 0.8f, 18f);
                if (heavy) { walk.ArmF = 55f; walk.ElbowF = 50f; }
                walk.Flutter = 0.35f;
                frames[$"walk_{i}"] = Draw(walk, s);
            }

            for (int i = 0; i < 3; i++)
            {
                Pose attack = Stand();
                attack.WeaponUp = true;
                if (sniper)
                {
                    attack.ArmF = 84f; attack.ElbowF = 10f; attack.ArmB = 70f; attack.ElbowB = 30f; attack.Lean = 4f;
                    attack.MuzzleFlash = i == 1;
                    attack.FootF = new Vector2(4f, Ground);
                    attack.FootB = new Vector2(-4f, Ground);
                }
                else if (heavy)
                {
                    float[] lean = { -6f, 18f, 10f };
                    attack.Lean = lean[i];
                    attack.ArmF = 70f + i * 8f; attack.ElbowF = 30f;
                    attack.FootF = new Vector2(3f + i * 2f, Ground);
                    attack.FootB = new Vector2(-4f, Ground);
                }
                else
                {
                    float[] arm = { 200f, 105f, 60f };
                    float[] lean = { -5f, 16f, 8f };
                    attack.ArmF = arm[i]; attack.ElbowF = i == 0 ? 20f : 5f; attack.Lean = lean[i];
                    attack.FootF = new Vector2(i == 0 ? 2f : 5f, Ground);
                    attack.FootB = new Vector2(-4f, Ground);
                }

                frames[$"attack_{i}"] = Draw(attack, s);
            }

            return frames;
        }

        /// <summary>Hideout NPC: idle with crossed arms and a talking loop.</summary>
        public static Dictionary<string, PixelCanvas> FixerFrames()
        {
            Style s = FixerStyle();
            var frames = new Dictionary<string, PixelCanvas>();
            for (int i = 0; i < 4; i++)
            {
                float t = i / 4f * Mathf.PI * 2f;
                Pose idle = Stand(-0.4f * (1f - Mathf.Cos(t)));
                idle.ArmF = 60f; idle.ElbowF = 75f; idle.ArmB = 50f; idle.ElbowB = 80f;
                idle.Flutter = 0.05f;
                frames[$"idle_{i}"] = Draw(idle, s);
            }

            for (int i = 0; i < 4; i++)
            {
                Pose talk = Stand();
                talk.ArmF = 40f + (i % 2) * 25f; talk.ElbowF = 60f - (i % 2) * 20f; talk.ArmB = 50f; talk.ElbowB = 80f;
                talk.HeadTilt = i % 2 == 0 ? -4f : 4f;
                frames[$"talk_{i}"] = Draw(talk, s);
            }

            return frames;
        }

        public static Style PlayerStyle() => new()
        {
            Jacket = Ramp.From(Paint.Rgb(52, 70, 110)), Pants = Ramp.From(Paint.Rgb(46, 44, 64)), Boots = Ramp.From(Paint.Rgb(30, 30, 40)),
            Head = Ramp.From(Paint.Rgb(44, 58, 96)), Gear = Ramp.From(Paint.Rgb(92, 100, 118)), Accent = Ramp.From(Paint.Rgb(255, 46, 140)),
            Visor = Paint.Rgb(46, 242, 255), Rim = Paint.Rgb(90, 230, 255), HeadKind = HeadKind.Hood,
            Scarf = true, Backpack = true, CoatTails = true
        };

        public static Style GuardStyle() => new()
        {
            Jacket = Ramp.From(Paint.Rgb(78, 92, 112)), Pants = Ramp.From(Paint.Rgb(44, 50, 62)), Boots = Ramp.From(Paint.Rgb(26, 28, 34)),
            Head = Ramp.From(Paint.Rgb(66, 76, 92)), Gear = Ramp.From(Paint.Rgb(52, 58, 70)), Accent = Ramp.From(Paint.Rgb(255, 196, 60)),
            Visor = Paint.Rgb(255, 56, 72), Rim = Paint.Rgb(255, 170, 110), HeadKind = HeadKind.Helmet, Weapon = Weapon.Baton,
            ShoulderPads = true
        };

        public static Style HeavyStyle() => new()
        {
            Jacket = Ramp.From(Paint.Rgb(104, 58, 56)), Pants = Ramp.From(Paint.Rgb(48, 40, 46)), Boots = Ramp.From(Paint.Rgb(26, 24, 28)),
            Head = Ramp.From(Paint.Rgb(70, 64, 70)), Gear = Ramp.From(Paint.Rgb(60, 64, 76)), Accent = Ramp.From(Paint.Rgb(255, 196, 60)),
            Visor = Paint.Rgb(255, 120, 40), Rim = Paint.Rgb(255, 140, 90), HeadKind = HeadKind.RiotHelmet, Weapon = Weapon.Shield,
            ShoulderPads = true, Bulk = 1.22f
        };

        public static Style SniperStyle() => new()
        {
            Jacket = Ramp.From(Paint.Rgb(64, 78, 66)), Pants = Ramp.From(Paint.Rgb(40, 46, 44)), Boots = Ramp.From(Paint.Rgb(26, 28, 28)),
            Head = Ramp.From(Paint.Rgb(46, 50, 56)), Gear = Ramp.From(Paint.Rgb(70, 74, 80)), Accent = Ramp.From(Paint.Rgb(255, 70, 80)),
            Visor = Paint.Rgb(255, 40, 60), Rim = Paint.Rgb(200, 255, 190), HeadKind = HeadKind.Cap, Weapon = Weapon.Rifle,
            CoatTails = true
        };

        /// <summary>The fixer in the hideout: long coat, no helmet, amber visor glasses.</summary>
        public static Style FixerStyle() => new()
        {
            Jacket = Ramp.From(Paint.Rgb(120, 60, 110)), Pants = Ramp.From(Paint.Rgb(40, 36, 52)), Boots = Ramp.From(Paint.Rgb(30, 26, 32)),
            Head = Ramp.From(Paint.Rgb(150, 104, 84)), Gear = Ramp.From(Paint.Rgb(80, 84, 98)), Accent = Ramp.From(Paint.Rgb(255, 200, 70)),
            Visor = Paint.Rgb(255, 190, 60), Rim = Paint.Rgb(255, 120, 200), HeadKind = HeadKind.Bare, CoatTails = true
        };
    }
}
