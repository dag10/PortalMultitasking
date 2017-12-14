Shader "Special/HomeWallRings"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_NumRings ("Rings", int) = 3
		_RingThickness ("Ring Thickness (object space)", float) = 0.1
		_WaveHeight ("Wave Height (object space)", float) = 0.8
		_Alpha ("Alpha", float) = 0.6
		_Speed ("Speed", float) = 1.0
		_WaveY ("Wave Y (object space)", float) = 0.5
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		Cull Front
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

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
				float4 localPos : POSITION1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4x4 _InvPortal;
			fixed _EyePortalDistances[2];
			int _NumRings;
			float _RingThickness;
			float _WaveHeight;
			float _Alpha;
			float _Speed;
			float _WaveY;
				
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.localPos = v.vertex;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			float wave(v2f i, float speed, float waveheight, float thickness, float y, float smoothness) {
				float wave = sin(i.uv.x * 3.14159 * 2 + (speed * _Time[1])) * 0.5 * waveheight + y;
				float wavedist = abs(wave - i.uv.y);
				float alpha = (thickness - wavedist) * smoothness;
				return saturate(alpha);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// If rendering through a portal, discard fragments between the back of the portal and the camera.
				if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
					fixed4 portalPos = mul(_InvPortal, fixed4(i.worldPos, 1));
					clip(-portalPos.z);
				}

				// Don't render top or bottom faces, just the sides.
				clip(-abs(i.localPos.y) + 0.999);

				float alpha = wave(i, _Speed, _WaveHeight, _RingThickness, _WaveY, 500);
				alpha += wave(i, _Speed * -1.4, _WaveHeight * 0.7, _RingThickness, _WaveY * 0.7, 500);
				alpha += wave(i, _Speed * 1.8, _WaveHeight * 0.4, _RingThickness, _WaveY * 0.3, 500);
				alpha += wave(i, _Speed * 1.2, 0.4, 0.3, 0.5, 1) * 0.9;
				alpha *= _Alpha;

				return float4(1.0, 1.0, 1.0, alpha);
			}
			ENDCG
		}
	}
}
