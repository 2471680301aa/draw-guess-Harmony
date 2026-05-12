Shader "Custom/DebugRainbowLine"
{
    Properties { }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float4 wpos : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = abs(sin(i.wpos.y * 10));
                return fixed4(t, 1-t, 0.2, 1);
            }
            ENDCG
        }
    }
}
