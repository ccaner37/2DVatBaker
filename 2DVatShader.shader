Shader "URP/2DVatShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}

        _AnimationTimeOffset("AnimationTimeOffset", float) = 0.0
        _VatPositionTex ("VatPositionTex", 2D) = "white" {}
        _VatAnimFps("VatAnimFps", float) = 5.0
        _VatAnimLength("VatAnimLength", float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Off // Disable backface culling to render both sides
            Blend SrcAlpha OneMinusSrcAlpha // Use alpha blending for transparency

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Vat.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 objectSpaceViewDir : TEXCOORD1;
            };

            sampler2D _MainTex;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _AnimationTimeOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert (Attributes IN, uint vId : SV_VertexID)
            {
                float animTime = CalcVatAnimationTime(_Time.y + UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationTimeOffset));
                float3 pos = GetVatPosition(vId, animTime);

                Varyings OUT;
                //OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;

                // Get the world-space view direction
                //float3 worldPos = TransformObjectToWorld(IN.positionOS);
                float3 worldPos = TransformObjectToWorld(pos);
                float3 viewDir = normalize(GetWorldSpaceViewDir(worldPos));

                // Transform the view direction into object space
                OUT.objectSpaceViewDir = mul((float3x3)unity_WorldToObject, viewDir);

                return OUT;
            }

            half4 frag (Varyings IN, float faceDirection : VFACE) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.uv);
                return half4(color.rgb, color.a);
            }
            ENDHLSL
        }
    }
}
