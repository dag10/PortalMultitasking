Shader "Portals/ClipStencil"
{
	SubShader
	{
		// Writes 0x01 in the entire stencil buffer on the correct side of a line
		// that represents the camera's near clip plane's intersection with the
		// portal plane.
		Pass
		{
			Stencil {
				Ref 1
				Comp Always
				Pass Replace
			}

			ZTest Off
			ZWrite Off
			ColorMask 0
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			uniform int _TopLeftCornerClipped;
			uniform fixed4 _IntersectionPoint;
			uniform fixed4 _IntersectionTangent;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 clipSpacePos : TEXCOORD0;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.vertex.xy *= 2; // Quad mesh vertices only extend to +- 0.5, so we double it to fill the clip space.
#if defined(SHADER_API_D3D9) | defined(SHADER_API_D3D11)
				o.vertex.z = 0;   // Render quad at back of clip space.
#else
				o.vertex.z = 1;   // Render quad at back of clip space.
#endif
				o.clipSpacePos = o.vertex.xy;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 pos = i.clipSpacePos;
#if defined(SHADER_API_D3D9) | defined(SHADER_API_D3D11)
				pos.y *= -1;
#endif
				float2 linePoint = _IntersectionPoint.xy;
				float2 lineTangent = _IntersectionTangent.xy;

				// Make the fragment's position be relative to the
				// known position on the intersection line.
				pos -= linePoint;

				// Calculate which side of the intersection line we're on.
				float3 C = cross(float3(lineTangent, 0), float3(pos, 0));

				clip(C.z);

				return fixed4(0.5, C.z > 0 ? 1 : 0, 0.3, 1);
			}
			ENDCG
		}
	}
}
