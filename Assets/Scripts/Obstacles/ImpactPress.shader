Shader "Custom/ImpactPress"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" { }
_Color("Tint", Color) = (1, 1, 1, 1)
[HDR] _HeatColor("Heat Color", Color) = (3, 1, 0, 1)
        _HeatPower("Heat Power", Range(0.1, 5)) = 2.0
        _HeatLevel("Heat Level", Range(0, 1)) = 0.0
        _SquashAmount("Squash Amount", Range(0, 0.5)) = 0.0
        _FlashAmount("Flash Amount", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
#pragma fragment frag
#pragma target 2.0

# include "UnityCG.cginc"

            struct appdata_t
{
    float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

struct v2f
{
    float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 uvOriginal : TEXCOORD1;
            };

sampler2D _MainTex;
fixed4 _Color;
fixed4 _HeatColor;
float _HeatPower;
float _HeatLevel;
float _SquashAmount;
float _FlashAmount;

v2f vert(appdata_t IN)
{
    v2f OUT;

    float squashFactor = 1.0 - (_SquashAmount * (1.0 - IN.texcoord.y));
    float4 pos = IN.vertex;
    pos.y *= squashFactor;

    OUT.vertex = UnityObjectToClipPos(pos);
    OUT.texcoord = IN.texcoord;
    OUT.uvOriginal = IN.texcoord;
    OUT.color = IN.color * _Color;

    return OUT;
}

fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

float heatGradient = pow(1.0 - IN.uvOriginal.y, _HeatPower);
float heatMask = smoothstep(1.0 - _HeatLevel, 1.0, heatGradient);
fixed4 heatEmission = _HeatColor * heatMask * c.a;

c.rgb += heatEmission.rgb;
c.rgb = lerp(c.rgb, fixed3(1, 1, 1), _FlashAmount * c.a);
c.rgb *= c.a;

return c;
            }
            ENDCG
        }
    }
}