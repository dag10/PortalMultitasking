//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Used for the teleport markers
//
//=============================================================================
// UNITY_SHADER_NO_UPGRADE
Shader "PortalFriendly/Valve/VR/Highlight"
{
	Properties
	{
		_TintColor( "Tint Color", Color ) = ( 1, 1, 1, 1 )
		_SeeThru( "SeeThru", Range( 0.0, 1.0 ) ) = 0.25
		_Darken( "Darken", Range( 0.0, 1.0 ) ) = 0.0
		_MainTex( "MainTex", 2D ) = "white" {}
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
	CGINCLUDE
		
		// Pragmas --------------------------------------------------------------------------------------------------------------------------------------------------
		#pragma target 5.0
		#pragma only_renderers d3d11 vulkan
		#pragma exclude_renderers gles

		// Includes -------------------------------------------------------------------------------------------------------------------------------------------------
		#include "UnityCG.cginc"

		// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
		struct VertexInput
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			fixed4 color : COLOR;
		};
		
		struct VertexOutput
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			float3 worldPos : POSITIONT;
			fixed4 color : COLOR;
		};

		// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
		sampler2D _MainTex;
		float4 _MainTex_ST;
		float4 _TintColor;
		float _SeeThru;
		float _Darken;
		fixed4x4 _InvPortal;
		fixed _EyePortalDistances[2];
				
		// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
		VertexOutput MainVS( VertexInput i )
		{
			VertexOutput o;
#if UNITY_VERSION >= 540
			o.vertex = UnityObjectToClipPos(i.vertex);
#else
			o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
#endif
			o.worldPos = mul(unity_ObjectToWorld, i.vertex);
			o.uv = TRANSFORM_TEX( i.uv, _MainTex );
			o.color = i.color;
			
			return o;
		}
		
		// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
		float4 MainPS( VertexOutput i ) : SV_Target
		{
			// If rendering through a portal, discard fragments between the back of the portal and the camera.
			if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
				fixed4 portalPos = mul(_InvPortal, fixed4(i.worldPos, 1));
				clip(-portalPos.z);
			}

			float4 vTexel = tex2D( _MainTex, i.uv ).rgba;
			float4 vColor = vTexel.rgba * _TintColor.rgba * i.color.rgba;
			vColor.rgba = saturate( 2.0 * vColor.rgba );
			float flAlpha = vColor.a;

			vColor.rgb *= vColor.a;
			vColor.a = lerp( 0.0, _Darken, flAlpha );

			return vColor.rgba;
		}

		// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
		float4 SeeThruPS( VertexOutput i ) : SV_Target
		{
			// If rendering through a portal, discard fragments between the back of the portal and the camera.
			if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
				fixed4 portalPos = mul(_InvPortal, fixed4(i.worldPos, 1));
				clip(-portalPos.z);
			}

			float4 vTexel = tex2D( _MainTex, i.uv ).rgba;
			float4 vColor = vTexel.rgba * _TintColor.rgba * i.color.rgba * _SeeThru;
			vColor.rgba = saturate( 2.0 * vColor.rgba );
			float flAlpha = vColor.a;

			vColor.rgb *= vColor.a;
			vColor.a = lerp( 0.0, _Darken, flAlpha * _SeeThru );

			return vColor.rgba;
		}

	ENDCG

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		// Only render this material where the stencil buffer has 0x01.
		Stencil {
			Ref 1
			Comp Equal
			Pass Keep
		}

		// Behind Geometry ---------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend One OneMinusSrcAlpha
			Cull Off
			ZWrite Off
			ZTest Greater

			CGPROGRAM
				#pragma vertex MainVS
				#pragma fragment SeeThruPS
			ENDCG
		}

		Pass
		{
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend One OneMinusSrcAlpha
			Cull Off
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
				#pragma vertex MainVS
				#pragma fragment MainPS
			ENDCG
		}
	}
}
