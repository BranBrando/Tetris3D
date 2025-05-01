Shader "Custom/OutlineShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 5.0)) = 1.0 // Increased range for screen-space
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // UV coordinates for screen-space calculations
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate derivatives (how much UV changes per pixel)
                float2 dx = ddx(i.uv);
                float2 dy = ddy(i.uv);

                // Calculate edge detection (simplified, can be improved)
                float edge = max(abs(dx.x), abs(dy.y));

                // Calculate outline factor based on edge detection and width
                float outline = smoothstep(0.0, _OutlineWidth * 0.01, edge); // Adjust multiplier for desired width

                // Blend main color and outline color
                fixed4 color = lerp(_MainColor, _OutlineColor, outline);

                return color;
            }
            ENDCG
        }
    }
}
