Shader "altVAT/altVAT_2D_SimpleDirectionalLitShader"
{
    Properties
    {
        _PositionsTex ("Positions", 2D) = "white" {}
        _NormalsTex ("Normals", 2D) = "white" {}
        _NormalisedFrame ("Normalised Frame", Range(0,1)) = 0
        
        
        _LightDirection ("Light Direction", Vector) = (0,0,0,0)
        _AmbientLightAmount("Ambient Light", float) = 0
        
        _BoundsMinPos ("Min Bounds Pos", Vector) = (0,0,0,0)
        _BoundsMaxPos ("Max Bounds Pos", Vector) = (0,0,0,0)
        _BoundsMinNorm ("Min Bounds Norm", Vector) = (0,0,0,0)
        _BoundsMaxNorm ("Max Bounds Norm", Vector) = (0,0,0,0)
        
        _Color( "Color", Color ) = ( 1.0, 1.0, 1.0, 1.0 )		
        
        [MaterialToggle] _Autoplay("Autoplay", float) = 0
        
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
                float4 normal : NORMAL;
                float4 color : COLOR;
            };

            float _NormalisedFrame;            
            float _Autoplay;
            
            sampler2D _PositionsTex;
            

            sampler2D _NormalsTex;
            

            float4 _BoundsMinPos;
            float4 _BoundsMaxPos;

            float4 _BoundsMinNorm;
            float4 _BoundsMaxNorm;

            uniform float4 _Color;
			
            uniform float4 _LightDirection;
            float _AmbientLightAmount;

            v2f vert (appdata v)
            {
                v2f o;

                if (_Autoplay > 0)
                {
                    _NormalisedFrame = _Time.x % 1.0f;
                }
                          
                
                float4 diff = tex2Dlod(_PositionsTex, float4(v.uv2.x, _NormalisedFrame,0,0));
                diff.x = lerp(_BoundsMinPos.x, _BoundsMaxPos.x, diff.x);
                diff.y = lerp(_BoundsMinPos.y, _BoundsMaxPos.y, diff.y);
                diff.z = lerp(_BoundsMinPos.z, _BoundsMaxPos.z, diff.z);

                v.vertex += diff;

                o.vertex = UnityObjectToClipPos(v.vertex);     

                diff = tex2Dlod(_NormalsTex, float4(v.uv2.x, _NormalisedFrame, 0,0));
                diff.x = lerp(_BoundsMinNorm.x, _BoundsMaxNorm.x, diff.x);
                diff.y = lerp(_BoundsMinNorm.y, _BoundsMaxNorm.y, diff.y);
                diff.z = lerp(_BoundsMinNorm.z, _BoundsMaxNorm.z, diff.z);

                float4 norm = -v.normal + diff;

                float illuminationAmt = clamp(dot(norm.xyz, _LightDirection.xyz), _AmbientLightAmount, _LightDirection.w);
                o.color = (illuminationAmt * _Color);

                // o.color = float4(v.uv2, 0, 1);

                o.normal = norm;
                
                // UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = i.color;
                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
