// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "PortalFriendly/Nature/Terrain/Diffuse" {
    Properties {
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
        // used in fallback on old cards & base map
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
    }

    CGINCLUDE
        #pragma surface surf Lambert vertex:SplatmapVert finalcolor:SplatmapFinalColor finalprepass:SplatmapFinalPrepass finalgbuffer:SplatmapFinalGBuffer noinstancing
        #pragma multi_compile_fog
        #include "TerrainSplatmapCommon.cginc"

		fixed4x4 _InvPortal;
		fixed _EyePortalDistances[2];

		// Extended from TerrainSplatmapCommon.cginc
		//struct Input {
		//	float2 uv_Splat0 : TEXCOORD0;
		//	float2 uv_Splat1 : TEXCOORD1;
		//	float2 uv_Splat2 : TEXCOORD2;
		//	float2 uv_Splat3 : TEXCOORD3;
		//	float2 tc_Control : TEXCOORD4;  // Not prefixing '_Contorl' with 'uv' allows a tighter packing of interpolators, which is necessary to support directional lightmap.
		//	UNITY_FOG_COORDS(5)
		//};

        void surf(Input IN, inout SurfaceOutput o)
        {
			//// If rendering through a portal, discard fragments between the back of the portal and the camera.
			//if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
			//	//fixed4 portalPos = mul(_InvPortal, fixed4(IN.worldPos, 1));
			//	//clip(-portalPos.z);
			//}

            half4 splat_control;
            half weight;
            fixed4 mixedDiffuse;
            SplatmapMix(IN, splat_control, weight, mixedDiffuse, o.Normal);
            o.Albedo = mixedDiffuse.rgb;
            o.Alpha = weight;
        }
    ENDCG

    Category {
        Tags {
            "Queue" = "Geometry-99"
            "RenderType" = "Opaque"
        }
        // TODO: Seems like "#pragma target 3.0 _TERRAIN_NORMAL_MAP" can't fallback correctly on less capable devices?
        // Use two sub-shaders to simulate different features for different targets and still fallback correctly.
        SubShader { // for sm3.0+ targets
		// Only render this material where the stencil buffer has 0x01.
		Stencil {
			Ref 1
			Comp Equal
			Pass Keep
		}
            CGPROGRAM
                #pragma target 3.0
                #pragma multi_compile __ _TERRAIN_NORMAL_MAP
            ENDCG
        }
        SubShader { // for sm2.0 targets
		// Only render this material where the stencil buffer has 0x01.
		Stencil {
			Ref 1
			Comp Equal
			Pass Keep
		}
            CGPROGRAM
            ENDCG
        }
    }

    Dependency "AddPassShader" = "PortalFriendly/TerrainEngine/Splatmap/Diffuse-AddPass"
    Dependency "BaseMapShader" = "Diffuse"
    Dependency "Details0"      = "Hidden/TerrainEngine/Details/Vertexlit"
    Dependency "Details1"      = "Hidden/TerrainEngine/Details/WavingDoublePass"
    Dependency "Details2"      = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
    Dependency "Tree0"         = "Hidden/TerrainEngine/BillboardTree"

    Fallback "Diffuse"
}
