Shader "PortalFriendly/Unlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		// Only render this material where the stencil buffer has 0x01.
		Stencil {
			Ref 1
			Comp Equal
			Pass Keep
		}

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
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 worldPos : POSITIONT;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4x4 _InvPortal;
			fixed _EyePortalDistances[2];
				
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// If rendering through a portal, discard fragments between the back of the portal and the camera.
				if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
					fixed4 portalPos = mul(_InvPortal, fixed4(i.worldPos, 1));
					clip(-portalPos.z);
				}

				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
