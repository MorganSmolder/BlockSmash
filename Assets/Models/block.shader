Shader "Unlit/block"
{
    Properties
    {
       _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };


            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv =  v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float3 light_dir = float3(.25, .25, -.5);
                float dif = saturate(dot(light_dir, normalize(i.normal)));

                if(dif < .5)
                {
                    dif = 0.2f;
                }
                dif = lerp(.2, 1, dif);

                const float3 light_spec = float3(0, 1, 0);
                float spec = saturate(dot(light_spec, normalize(i.normal)));

                return saturate(dif * _Color * 2 + spec);
            }
            ENDCG
        }
    }
}
