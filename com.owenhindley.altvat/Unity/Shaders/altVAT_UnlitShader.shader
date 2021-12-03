Shader "altVAT/altVAT_UnlitShader"
{
    Properties
    {
        _PositionsTex ("Positions", 3D) = "white" {}
        _NormalsTex ("Normals", 3D) = "white" {}
        _FrameNum ("Frame Number", float) = 0
        _FrameCount ("Frame Count", float) = 0
        _Color ("Color", Color) = (0,0,0,0)
        _BoundsMin ("Min Bounds", Vector) = (0,0,0,0)
        _BoundsMax ("Max Bounds", Vector) = (0,0,0,0)
        
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

            float _FrameNum;
            float _FrameCount;
            
            sampler3D _PositionsTex;
            float4 _PositionsTex_ST;

            sampler3D _NormalsTex;
            float4 _NormalsTex_ST;

            float4 _BoundsMin;
            float4 _BoundsMax;

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                float4 diff = tex3Dlod(_PositionsTex, float4(v.uv2, _FrameNum / _FrameCount, 0));
                diff.x = lerp(_BoundsMin.x, _BoundsMax.x, diff.x);
                diff.y = lerp(_BoundsMin.y, _BoundsMax.y, diff.y);
                diff.z = lerp(_BoundsMin.z, _BoundsMax.z, diff.z);

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
