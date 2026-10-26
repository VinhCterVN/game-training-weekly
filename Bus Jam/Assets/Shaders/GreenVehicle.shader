Shader "BusJam/GreenVehicle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"
        }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
                fixed4 color:COLOR;
            };

            struct v2f
            {
                float4 pos:SV_POSITION;
                float2 uv:TEXCOORD0;
                fixed4 color:COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                // Shift only blue paint, preserving white arrows, windows, tires, and shading.
                float mask = saturate((c.b - max(c.r, c.g)) * 8);
                fixed3 green = fixed3(c.r, c.b, c.g * .72);
                c.rgb = lerp(c.rgb, green, mask);
                return c * i.color;
            }
            ENDCG
        }
    }
}