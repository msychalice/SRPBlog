Shader "SRP/BaseDirLit"
{
	Properties
	{
		_Color("Tint", Color) = (0.5,0.5,0.5)
		_DiffuseFactor("Diffuse Factor", Range(0,1)) = 1
		_SpecularFactor("Specular Factor", Range(0,1)) = 1
		_SpecularPower("Specular Power",Float) = 100
	}


	HLSLINCLUDE

	#include "UnityCG.cginc"

	uniform float4 _LightDir;
	uniform float4 _LightColor;
	uniform float4 _CameraPos;
	uniform float4 _Color;
	uniform float _DiffuseFactor;
	uniform float _SpecularFactor;
	uniform float _SpecularPower;

	struct a2v
	{
		float4 position : POSITION;
		float4 normal : NORMAL;
		float4 color : COLOR;
	};

	struct v2f
	{
		float4 position : SV_POSITION;
		fixed4 color : COLOR0;
		float4 normalWorld : TEXCOORD1;
		float4 worldPos : TEXCOORD2;
	};

	v2f vert(a2v v)
	{
		v2f o;
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		o.position = UnityObjectToClipPos(v.position);
		o.normalWorld = float4(normalize(mul(normalize(v.normal.xyz), (float3x3)unity_WorldToObject)), v.normal.w);
		o.color = v.color;
		o.worldPos = mul(unity_ObjectToWorld, v.position);
		return o;
	}

	half4 frag(v2f v) : SV_Target
	{
		half4 fragColor = half4(0.0,0.0,0.0,1.0);
		//Lambert Lighting.
		fragColor += _DiffuseFactor * max(0.0,dot(_LightDir.xyz,normalize(v.normalWorld.xyz))) * _LightColor * float4(_Color.xyz,1.0);
		//Blinn-phong Lighting.
		float3 CameraDirection = normalize(_CameraPos.xyz - v.worldPos.xyz);
		float3 halfwayDir = normalize(_LightDir.xyz + CameraDirection);
		fragColor += _SpecularFactor * pow(max(0.0,dot(normalize(v.normalWorld.xyz), halfwayDir)), _SpecularPower) * _LightColor;
		return fragColor;
	}

	ENDHLSL

	SubShader
	{
		Tags{ "Queue" = "Geometry" }
		LOD 100
		Pass
		{
			Tags{ "LightMode" = "BaseLit" }

			HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag

			ENDHLSL
		}
	}
	
}
