Shader "Utopia/World/MaskGenerator"
{
	Properties
	{

	}
	SubShader
	{
		// Disable everything
		ZWrite Off
		ZTest Off
		Blend Off
		Cull Off
		ZClip False

		HLSLPROGRAM
		
		#pragma vertex Vertex
		#pragma fragment Fragment

		void Vertex()
		{
			
		}
		
		void Geometry()
		{

		}

		void Fragment()
		{

		}

		ENDHLSL
	}
}
