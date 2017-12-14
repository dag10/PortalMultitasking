Shader "Portals/PortalStencil"
{
	SubShader
	{
		// Writes 0x00 in the stencil buffer for the portal opening inside of the virtual scene, since we
		// don't want to render the backside of a portal when looking through it.
		Pass
		{
			Stencil {
				Ref 0
				Pass Replace
			}

			ZWrite Off
			ZTest Off
			ColorMask 0

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
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(1, 0.8, 0.8, 1);
			}
			ENDCG
		}
	}
}
