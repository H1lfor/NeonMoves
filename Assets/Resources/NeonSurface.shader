Shader "Neonmoves/Surface" {
 Properties { _BaseColor("Color", Color)=(1,1,1,1) _Emission("Glow", Color)=(0,0,0,1) }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor; float4 _Emission;
   CBUFFER_END
   Varyings vert(Attributes i) { Varyings o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(i.normalOS); return o; }
   half4 frag(Varyings i):SV_Target {
    float3 n=normalize(i.normalWS), v=normalize(GetWorldSpaceViewDir(i.positionWS));
    float diffuse=saturate(dot(n,normalize(float3(-.5,.8,-.8))));
    float rim=pow(1-saturate(dot(n,v)),3);
    float rimWeight=saturate(max(_BaseColor.r,max(_BaseColor.g,_BaseColor.b))*4);
    float3 color=_BaseColor.rgb*(.29+diffuse*.8)+rim*float3(.035,.075,.11)*rimWeight+_Emission.rgb;
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
