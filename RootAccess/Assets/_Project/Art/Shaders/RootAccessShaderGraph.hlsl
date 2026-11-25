// Custom Function entry points for the Shader Graph assets (Art/Shaders/Graphs). Shader Graph appends the precision
// suffix (_float / _half) to the function name, so both variants are provided. The wind math itself lives in
// RootAccessCommon.hlsl and is shared with the hand-written HLSL shaders.
#ifndef ROOTACCESS_SHADERGRAPH_INCLUDED
#define ROOTACCESS_SHADERGRAPH_INCLUDED

#include "Assets/_Project/Art/Shaders/RootAccessCommon.hlsl"

// Sways a foliage vertex with the global wind and bends it away from the player. Weight grows with UV height so roots stay planted.
void RA_FoliagePosition_float(float3 PositionOS, float2 UV, float WindWeight, float BendRadius, float BendStrength, out float3 Out)
{
    float3 worldPos = TransformObjectToWorld(PositionOS);
    float weight = UV.y * UV.y * WindWeight;
    worldPos.x += RA_FoliageOffset(worldPos, weight, BendRadius, BendStrength);
    Out = TransformWorldToObject(worldPos);
}

void RA_FoliagePosition_half(half3 PositionOS, half2 UV, half WindWeight, half BendRadius, half BendStrength, out half3 Out)
{
    float3 result;
    RA_FoliagePosition_float(PositionOS, UV, WindWeight, BendRadius, BendStrength, result);
    Out = (half3)result;
}

#endif
