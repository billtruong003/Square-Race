Shader "Custom/VortexPortal2D"
{
    Properties
    {
        [Header(Texture)]
        _MainTex ("Noise Texture", 2D) = "white" {}
        [HDR] _Color ("Tint Color", Color) = (0.5, 0.2, 1.0, 1.0)
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 1.0, 1.0, 1.0)

        [Header(Vortex Settings)]
        _SpinSpeed ("Spin Speed", Float) = 1.0
        _SuckSpeed ("Suction Speed", Float) = 0.5
        _Twist ("Twist Amount", Float) = 1.0
        
        [Header(Shape)]
        _Radius ("Radius", Range(0, 0.5)) = 0.5
        _Softness ("Edge Softness", Range(0.01, 0.2)) = 0.05
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
        }

        Blend SrcAlpha One   // Dùng chế độ cộng màu (Additive) để portal sáng rực
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half4 _CoreColor;
            float _SpinSpeed;
            float _SuckSpeed;
            float _Twist;
            float _Radius;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // 1. Chuyển UV về tâm (0,0)
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV);

                // 2. Cắt hình tròn (Masking)
                // Nếu bán kính = 0 thì ẩn hết
                float alphaMask = smoothstep(_Radius, _Radius - _Softness, dist);
                
                // Cắt bỏ pixel ngoài vòng tròn để tiết kiệm fill rate (quan trọng)
                clip(alphaMask - 0.001);

                // 3. TOẠ ĐỘ CỰC (POLAR COORDINATES) - "Phép thuật" nằm ở đây
                float angle = atan2(centeredUV.y, centeredUV.x); // Góc (-PI đến PI)
                
                // Chuẩn hóa góc về 0-1
                float normalizedAngle = angle / (2.0 * 3.14159265);

                // Tạo hiệu ứng xoáy: Góc bị bẻ cong theo khoảng cách
                normalizedAngle += dist * _Twist;

                // 4. Tạo UV mới để sample Texture
                float2 polarUV;
                // X: Xoay vòng quanh tâm
                polarUV.x = normalizedAngle + (_Time.y * _SpinSpeed);
                // Y: Hút vào tâm (Texture trôi từ 1 về 0)
                polarUV.y = dist - (_Time.y * _SuckSpeed);

                // Scale UV texture để lặp lại nhiều hơn (tùy chọn)
                polarUV *= float2(1.0, 1.0); 

                // 5. Sample Texture với UV cực
                half4 noise = tex2D(_MainTex, polarUV);

                // 6. Pha màu
                // Càng vào tâm càng sáng (Core)
                float coreIntensity = smoothstep(0.5, 0.0, dist); 
                
                half4 finalColor = lerp(_Color, _CoreColor, coreIntensity * coreIntensity);
                
                // Nhân với noise texture
                finalColor *= noise;
                
                // Làm tâm sáng rực lên che đi chỗ texture bị túm lại ở giữa
                finalColor += _CoreColor * smoothstep(0.1, 0.0, dist) * 2.0;

                finalColor.a *= alphaMask * i.color.a;

                return finalColor;
            }
            ENDCG
        }
    }
}