Shader "Custom/VFX/DirectionalSlashWipe"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [HDR] [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Directional Sweep)]
        _SweepDirection("Sweep Direction (1 = Right to Left, -1 = Left to Right)", Float) = 1.0
        _ArcCenter("Arc Center UV", Vector) = (0.529, 0.372, 0, 0)
        _StartAngleDeg("Start Angle (Deg)", Range(60, 175)) = 148.0
        _EndAngleDeg("End Angle (Deg)", Range(60, 175)) = 132.0

        [Header(Progressive Timing)]
        _RevealDuration("Head Reveal Duration (0..1 Lifetime)", Range(0.05, 0.95)) = 0.45
        _TailStart("Tail Dissolve Start (0..1 Lifetime)", Range(0.0, 0.95)) = 0.20
        _HeadSoftness("Head Edge Softness", Range(0.01, 0.35)) = 0.09
        _TailSoftness("Tail Edge Softness", Range(0.02, 0.45)) = 0.18

        [Header(Leading Blade Glow)]
        [HDR] _LeadingGlowColor("Leading Edge Glow Color", Color) = (1.4, 1.9, 2.2, 1.0)
        _LeadingGlowIntensity("Leading Edge Glow Intensity", Range(0.0, 4.0)) = 1.15
        _LeadingGlowWidth("Leading Edge Glow Width", Range(0.01, 0.4)) = 0.14

        [Header(Debug)]
        _ManualProgress("Manual Progress (-1 = Use Particle Age)", Range(-1.0, 1.0)) = -1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _SweepDirection;
                float4 _ArcCenter;
                float _StartAngleDeg;
                float _EndAngleDeg;
                float _RevealDuration;
                float _TailStart;
                float _HeadSoftness;
                float _TailSoftness;
                half4 _LeadingGlowColor;
                half _LeadingGlowIntensity;
                float _LeadingGlowWidth;
                float _ManualProgress;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
                // Custom Vertex Streams on ParticleSystemRenderer:
                // TEXCOORD0.xy = UV, TEXCOORD0.z = AgePercent (0..1)
                float4 uvAndAge   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
                float age         : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uvAndAge.xy, _BaseMap);
                output.age = input.uvAndAge.z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                float2 d = input.uv - _ArcCenter.xy;
                float radius = length(d);

                // Clean center pinch & bottom seam gap
                float radialClean = smoothstep(0.03, 0.11, radius);
                float angleRad = atan2(d.x, d.y);
                float angleDeg = degrees(angleRad);
                float gapClean = smoothstep(174.0, 156.0, abs(angleDeg));

                // Map angular position along the swing direction to [0..1]
                // _SweepDirection >= 0: Right (+angle) -> Front (0) -> Left (-angle)
                // _SweepDirection <  0: Left (-angle)  -> Front (0) -> Right (+angle)
                float dirSign = (_SweepDirection >= 0.0) ? 1.0 : -1.0;
                float signedAngle = angleDeg * dirSign;
                float totalSpan = max(1.0, _StartAngleDeg + _EndAngleDeg);
                float arcT = (_StartAngleDeg - signedAngle) / totalSpan;

                float age = (_ManualProgress >= 0.0) ? saturate(_ManualProgress) : saturate(input.age);

                // 1. Progressive Head Reveal (0 -> 1 along slash direction)
                float revealNorm = saturate(age / max(0.01, _RevealDuration));
                float revealEased = 1.0 - pow(1.0 - revealNorm, 1.65);
                float headPos = lerp(-_HeadSoftness, 1.0 + _HeadSoftness, revealEased);
                float headMask = smoothstep(headPos + _HeadSoftness, headPos - _HeadSoftness, arcT);

                // 2. Progressive Tail Follow-Through Dissolve (0 -> 1 along slash direction)
                float tailNorm = saturate((age - _TailStart) / max(0.01, 1.0 - _TailStart));
                float tailEased = pow(tailNorm, 1.25);
                float tailPos = lerp(-_TailSoftness, 1.0 + _TailSoftness, tailEased);
                float tailMask = smoothstep(tailPos - _TailSoftness, tailPos + _TailSoftness, arcT);

                float wipeMask = headMask * tailMask * radialClean * gapClean;

                // 3. Leading Blade Tip Highlight
                float distFromHead = headPos - arcT;
                float leadingBand = smoothstep(_LeadingGlowWidth, 0.0, distFromHead)
                                  * smoothstep(-0.02, 0.02, distFromHead)
                                  * (1.0 - revealNorm * 0.55);

                half3 rgb = tex.rgb * _BaseColor.rgb * input.color.rgb;
                rgb += _LeadingGlowColor.rgb * (leadingBand * tex.a * _LeadingGlowIntensity);

                half finalFade = wipeMask * _BaseColor.a * input.color.a;
                rgb *= finalFade;
                half alpha = saturate(tex.a * finalFade);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}

