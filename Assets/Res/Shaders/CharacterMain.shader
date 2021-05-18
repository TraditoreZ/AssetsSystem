// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld' 
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Shader/Character/CharacterMain"     
{
Properties
    {
   		_MainTex	("Main Texture", 2D) 		= "white"{}
      	_Color 		("Diffuse Color", Color) 	= (1,1,1,1) 
      	_SpecColor	("Specular Color", Color) 	= (1,1,1,1) 
      	_Alpha 		("Alpha", Range(0,1)) 		= 1
      	_PowStrength("PowStrength", Range(1,100)) = 1
      	lightDirection ("LightDirection", Vector) = (1.0,1.0,1.0,1.0)
    }
 
    SubShader
    {
    	Tags { "RenderType"="Transparent" }
    	LOD 200
        Pass
        {   
			
        	Tags {"LightMode" = "ForwardBase" }
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

        	#include "UnityCG.cginc"
      		#include "AutoLight.cginc"

      		sampler2D _MainTex;  
      		fixed4 _Color; 
      		fixed4 _SpecColor; 
      		fixed _Alpha;
      		fixed _PowStrength;
      		fixed4 lightDirection;

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 posWorld : TEXCOORD0;
      		   	fixed4 tex : TEXCOORD1;
      		   	fixed3 tangentWorld : TANGENT;  
      		   	fixed3 normalWorld : NORMAL;
      		   	fixed3 binormalWorld : TEXCOORD2;
      		   	LIGHTING_COORDS(3,4)
            };

            v2f vert(appdata_tan v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos (v.vertex);
      		   	o.tangentWorld = normalize(mul(unity_ObjectToWorld, fixed4(v.tangent.xyz, 0.0)).xyz);
      		   	o.normalWorld = normalize(mul(fixed4(v.normal, 0.0), unity_WorldToObject).xyz);
      		   	o.binormalWorld = normalize(cross(o.normalWorld, o.tangentWorld) * v.tangent.w);
      		   	o.posWorld = mul(unity_ObjectToWorld, v.vertex);
      		   	o.tex = v.texcoord;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            fixed4 frag(v2f i) : COLOR
            {

        	    fixed4 tex = tex2D(_MainTex,i.tex);
      		    fixed3 localCoords = fixed3(0,0,1);
      		    fixed3x3 local2WorldTranspose = fixed3x3(i.tangentWorld, i.binormalWorld, i.normalWorld);
      		    fixed3 normalDirection = normalize(mul(localCoords, local2WorldTranspose));
      		    fixed3 viewDirection = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
      		    //fixed3 lightDirection;
      		    fixed attenuation  = 	LIGHT_ATTENUATION(i);
      		    //lightDirection = normalize(_WorldSpaceLightPos0.xyz);

      		    half NdotL = dot(normalDirection, lightDirection.xyz);
      		    if (NdotL <= 0.0) 
      		    	NdotL = 0;
				else
					NdotL = 1;

      		    fixed3 diffuseReflection = attenuation * _Color.rgb * max(0.0, NdotL);
      		    fixed specstrength = pow(max(0.0, dot(reflect(normalize(-lightDirection), normalize(normalDirection)), normalize(viewDirection))), _PowStrength);
      		    fixed3 specularReflection =specstrength* attenuation  * _SpecColor.rgb;
      		    return fixed4((0.6 + diffuseReflection + specularReflection)*tex.rgb, _Alpha);

            }
            ENDCG
        }
    }
    Fallback"Diffuse"
}