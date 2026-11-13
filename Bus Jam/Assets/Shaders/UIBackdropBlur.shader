  Shader "UI/BackdropBlur"
  {
      Properties
      {
          [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
          _Radius ("Blur Radius", Range(1, 100)) = 10
          _Tint ("Tint Color", Color) = (0, 0, 0, 0.4)
      }
      SubShader
      {
          Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
          GrabPass { "_GrabTex" }

          Pass
          {
              CGPROGRAM
              #pragma vertex vert
              #pragma fragment frag
              #include "UnityCG.cginc"

              sampler2D _GrabTex;
              float4 _GrabTex_TexelSize;
              float _Radius;
              fixed4 _Tint;

              struct appdata { float4 vertex : POSITION; };
              struct v2f { float4 pos : SV_POSITION; float4 grabPos : TEXCOORD0; };

              v2f vert(appdata v)
              {
                  v2f o;
                  o.pos = UnityObjectToClipPos(v.vertex);
                  o.grabPos = ComputeGrabScreenPos(o.pos);
                  return o;
              }

              fixed4 frag(v2f i) : SV_Target
              {
                  float2 uv = i.grabPos.xy / i.grabPos.w;
                  float2 step = _GrabTex_TexelSize.xy * _Radius;
                  fixed4 col = 0;
                  col += tex2D(_GrabTex, uv + float2(-step.x, -step.y)) * 0.25;
                  col += tex2D(_GrabTex, uv + float2( step.x, -step.y)) * 0.25;
                  col += tex2D(_GrabTex, uv + float2(-step.x,  step.y)) * 0.25;
                  col += tex2D(_GrabTex, uv + float2( step.x,  step.y)) * 0.25;
                  return lerp(col, _Tint, _Tint.a);
              }
              ENDCG
          }
      }
  }
