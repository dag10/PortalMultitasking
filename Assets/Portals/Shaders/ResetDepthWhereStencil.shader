Shader "Portals/ResetDepthWhereStencil"
{
	SubShader
	{
		// Clears the depth buffer only where the stencil buffer has 0x01.
		Pass
		{
			Stencil {
				Ref 1
				Comp Equal
			}

			ZTest Off
			ZWrite On
			ColorMask 0
			Cull Off

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
				o.vertex = v.vertex;
				o.vertex.xy *= 2; // Quad mesh vertices only extend to +- 0.5, so we double it to fill the clip space.
				o.vertex.z = 1;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0, 0, 1, 1);
			}
			ENDCG
		}
	}
}

