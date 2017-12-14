Shader "PortalFriendly/Standard" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_OffsetFactor ("Offset Factor", int) = 0
		_OffsetUnits ("Offset Units", int) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Offset [_OffsetFactor], [_OffsetUnits]

		// Only render this material where the stencil buffer has 0x01.
		Stencil {
			Ref 1
			Comp Equal
			Pass Keep
		}
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		fixed4x4 _InvPortal;
		fixed _EyePortalDistances[2];

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END


		void surf (Input IN, inout SurfaceOutputStandard o) {
			// If rendering through a portal, discard fragments between the back of the portal and the camera.
			if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
				fixed4 portalPos = mul(_InvPortal, fixed4(IN.worldPos, 1));
				clip(-portalPos.z + 0.001);
			}

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
