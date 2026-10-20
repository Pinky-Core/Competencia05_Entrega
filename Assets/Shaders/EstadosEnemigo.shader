Shader "Competencia/EstadosEnemigo"
{
    Properties
    {
        _ColorEstado("Color del estado", Color) = (0.15, 0.8, 0.32, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "Estados"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorEstado;
            CBUFFER_END

            struct Atributos
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpoladores
            {
                float4 posicion : SV_POSITION;
                half iluminacion : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Interpoladores Vert(Atributos entrada)
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                Interpoladores salida;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(salida);
                salida.posicion = TransformObjectToHClip(entrada.posicion.xyz);
                float3 normal = TransformObjectToWorldNormal(entrada.normal);
                salida.iluminacion = 0.4 + 0.6 * saturate(dot(normal, normalize(float3(-0.4, 1, -0.5))));
                return salida;
            }

            half4 Frag(Interpoladores entrada) : SV_Target
            {
                return half4(_ColorEstado.rgb * entrada.iluminacion, 1);
            }
            ENDHLSL
        }
    }
}
