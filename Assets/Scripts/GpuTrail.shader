Shader "Hidden/GpuTrail_V6"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent-100" "IgnoreProjector" = "True"
        }
        LOD 100
        Blend One One
        ZWrite On
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"
            struct PointData
            {
                float3 position;
            };
            StructuredBuffer < PointData> _PointBuffer;
            StructuredBuffer < float> _WidthCurveBuffer;
            float3 _CurrentPos;
            int _HeadIndex;
            int _MaxPoints;
            float4 _Color;
            float _BaseWidth;
            float _ObjectScale;
            int _UseWorldSpaceWidth;
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            float3 GetPos(int logicalIndex)
            {
                if (logicalIndex == 0) return _CurrentPos;
                    int safeIndex = (_HeadIndex - logicalIndex + _MaxPoints) % _MaxPoints;
                return _PointBuffer[safeIndex].position;
            }
            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                int segmentID = id / 6;
                int vertID = id % 6;
                int cornerMap[6] =
                {
                    0, 1, 2, 2, 1, 3
                };
                int corner = cornerMap[vertID];
                bool isNext = (corner == 2 || corner == 3);
                int index = segmentID + (isNext ? 1 : 0);
                if (index >= _MaxPoints) index = _MaxPoints - 1;
                    float3 pMain = GetPos(index);
                float3 pOther = GetPos(isNext ? index - 1 : index + 1);
                float t = (float)index / (float)(_MaxPoints - 1);
                uint curveIdx = (uint)(t * 127);
                float w = _WidthCurveBuffer[curveIdx] * _BaseWidth * _ObjectScale * 0.5;
                float3 dir = normalize(pOther - pMain);
                float3 camPos = _WorldSpaceCameraPos;
                float3 camToMain = normalize(camPos - pMain);
                float3 perp = cross(dir, camToMain);
                if (length(perp) < 0.001)
                {
                        // Fallback if parallel: Use world up or arbitrary perp
                    float3 up = float3(0, 1, 0);
                    perp = cross(dir, up);
                    if (length(perp) < 0.001) perp = cross(dir, float3(1, 0, 0));
                    }
                perp = normalize(perp) * w;
                bool isRightSide = (corner == 1 || corner == 3);
                float sideSign = isRightSide ? 1.0 : -1.0;
                float3 offset = perp * sideSign;
                float3 worldPos = pMain + offset;
                if (!_UseWorldSpaceWidth)
                {
                        // Fallback to approximate screen-space (old behavior, but improved)
                    float depth = UnityWorldToViewPos(worldPos).z;
                    offset *= -depth; // Approximate pixel size adjustment (negative depth in view)
                }
                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = float2(t, isRightSide ? 0 : 1);
                float alpha = pow(1.0 - t, 1.5);    // Smoother fade (adjust exponent for "xịn" look)
                o.color = _Color;
                o.color.a *= alpha;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
