Shader "LuminaRush/VertexLit"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Ambient ("Ambient", Range(0, 1)) = 0.48
        _LightDir ("Light Dir", Vector) = (0.35, 0.9, 0.28, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Tint;
            float _Ambient;
            float4 _LightDir;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float ndotl = saturate(dot(normalize(normal), normalize(_LightDir.xyz)));
                float light = _Ambient + (1.0 - _Ambient) * ndotl;
                o.color = fixed4(v.color.rgb * _Tint.rgb * light, 1);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = i.color;
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
