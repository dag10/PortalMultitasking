Shader "Portals/PortalStencil"
{
	SubShader
	{
		// Pass 0 zeros out the entire stencil buffer, but preserves the depth buffer.
		Pass
		{
			Stencil {
				Comp Always
				Pass Zero
			}

			ZTest Off
			ZWrite Off
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
				o.vertex.z = 1;   // Render quad at back of clip space to clear the depth buffer.
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0, 1, 1, 1);
			}
			ENDCG
		}

		// Pass 1 1's out the entire stencil buffer and clears the depth buffer.
		Pass
		{
			Stencil {
				Ref 1
				Comp Always
				Pass Replace
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
				o.vertex.x *= 2; // Quad mesh vertices only extend to +- 0.5, so we double it to fill the clip space.
				o.vertex.y = (o.vertex.y * 4) - 1.0; // On windows, the Y coordinates seem vertically shifted.
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0, 1, 1, 1);
			}
			ENDCG
		}

		// Pass 2 clears the depth buffer only where the stencil buffer has 0x01.
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
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0, 1, 1, 1);
			}
			ENDCG
		}

		// Pass 3 1's out the portal opening.
		Pass
		{
			Stencil {
				Ref 1
				Pass Replace
			}

			ColorMask 0
			ZWrite Off
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
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(1, 1, 0, 1);
			}
			ENDCG
		}
	}
}
