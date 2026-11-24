#ifndef ROOTACCESS_COMMON_INCLUDED
#define ROOTACCESS_COMMON_INCLUDED

// Globals published by RootAccess.VFX.FoliageWind (Shader.SetGlobalVector).
// _RA_Wind:            x = strength, y = frequency, z = gustiness, w = script time
// _RA_PlayerPosition:  x, y = player world position, z = |horizontal speed|, w = 1 when valid
float4 _RA_Wind;
float4 _RA_PlayerPosition;

float RA_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float RA_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = RA_Hash21(i);
    float b = RA_Hash21(i + float2(1.0, 0.0));
    float c = RA_Hash21(i + float2(0.0, 1.0));
    float d = RA_Hash21(i + float2(1.0, 1.0));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float RA_Fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 3; i++)
    {
        value += RA_ValueNoise(p) * amplitude;
        p *= 2.03;
        amplitude *= 0.5;
    }
    return value;
}

// Horizontal displacement for foliage: layered sine wind + push away from the player.
float RA_FoliageOffset(float3 worldPos, float weight, float bendRadius, float bendStrength)
{
    float strength = _RA_Wind.x;
    float frequency = max(_RA_Wind.y, 0.01);
    float t = _Time.y;
    float sway = sin(t * frequency + worldPos.x * 0.7) * 0.6 + sin(t * frequency * 2.3 + worldPos.x * 1.9) * 0.25;
    float offset = sway * strength * 0.18 * weight;

    float2 toPlayer = worldPos.xy - _RA_PlayerPosition.xy;
    float horizontal = abs(toPlayer.x);
    float vertical = abs(toPlayer.y);
    float influence = saturate(1.0 - horizontal / max(bendRadius, 0.01)) * step(vertical, 1.6) * _RA_PlayerPosition.w;
    offset += sign(toPlayer.x) * influence * bendStrength * weight;
    return offset;
}

#endif
