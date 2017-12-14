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
#if defined(SHADER_API_D3D9) | defined(SHADER_API_D3D11)
				o.vertex.z = 0;   // Render quad at back of clip space to clear the depth buffer.
#else
				o.vertex.z = 1;   // Render quad at back of clip space to clear the depth buffer.
#endif
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

