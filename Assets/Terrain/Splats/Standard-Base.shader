// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "PortalFriendly/TerrainEngine/Splatmap/Standard-Base" {
    Properties {
        _MainTex ("Base (RGB) Smoothness (A)", 2D) = "white" {}
        _MetallicTex ("Metallic (R)", 2D) = "white" {}

        // used in fallback on old cards
        _Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-100"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        // needs more than 8 texcoords
        #pragma exclude_renderers gles
        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex;
        sampler2D _MetallicTex;

		fixed4x4 _InvPortal;
		fixed _EyePortalDistances[2];

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o) {
			// If rendering through a portal, discard fragments between the back of the portal and the camera.
			//if (_InvPortal[3][3] != 0 && _EyePortalDistances[unity_StereoEyeIndex] < 0) {
			//	fixed4 portalPos = mul(_InvPortal, fixed4(IN.worldPos, 1));
			//	clip(-portalPos.z);
			//}

            half4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Alpha = 1;
            o.Smoothness = c.a;
            o.Metallic = tex2D (_MetallicTex, IN.uv_MainTex).r;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
