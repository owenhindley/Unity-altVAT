Shader "altVAT/altVAT_UnlitShader"
{
    Properties
    {
        _PositionsTex ("Positions", 3D) = "white" {}
        
        _NormalisedFrame ("Normalised Frame", float) = 0
        
        _Color ("Color", Color) = (0,0,0,0)
        _BoundsMinPos ("Min Bounds Pos", Vector) = (0,0,0,0)
        _BoundsMaxPos ("Max Bounds Pos", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {               
                float4 vertex : SV_POSITION;                
            };

            float _NormalisedFrame;            
            
            sampler3D _PositionsTex;
            

            sampler3D _NormalsTex;
            

            float4 _BoundsMinPos;
            float4 _BoundsMaxPos;


            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                
                float4 diff = tex3Dlod(_PositionsTex, float4(v.uv2, _NormalisedFrame, 0));
                diff.x = lerp(_BoundsMinPos.x, _BoundsMaxPos.x, diff.x);
                diff.y = lerp(_BoundsMinPos.y, _BoundsMaxPos.y, diff.y);
                diff.z = lerp(_BoundsMinPos.z, _BoundsMaxPos.z, diff.z);

                o.vertex += diff;

                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
