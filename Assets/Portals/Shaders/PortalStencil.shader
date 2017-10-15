Shader "Portals/PortalStencil"
{
	SubShader
	{
		// Writes 0x01 in the stencil buffer for the portal opening.
		Pass
		{
			Stencil {
				Ref 1
				Pass Replace
			}

			ZWrite Off
			ColorMask 0
			Cull Off

			Offset -3, 0

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex.z *= 0.5;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return float4(i.vertex.z, i.vertex.z, i.vertex.z, 1);
				//return fixed4(1, 1, 1, 1);
			}
			ENDCG
		}
	}
}
