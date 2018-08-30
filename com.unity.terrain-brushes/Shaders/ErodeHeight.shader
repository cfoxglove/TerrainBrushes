    Shader "ErodeHeight" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_SHARPNESS  (_BrushParams[1])
            #define BRUSH_FEATURESIZE   (_BrushParams[2])
            #define BRUSH_ROTATION      (_BrushParams[3])

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            float3 RotateUVs(float2 sourceUV, float rotAngle)
            {
                float4 rotAxes;
                rotAxes.x = cos(rotAngle);
                rotAxes.y = sin(rotAngle);
                rotAxes.w = rotAxes.x;
                rotAxes.z = -rotAxes.y;

                float2 tempUV = sourceUV - float2(0.5, 0.5);
                float3 retVal;

                // We fix some flaws by setting zero-value to out of range UVs, so what we do here
                // is test if we're out of range and store the mask in the third component.
                retVal.xy = float2(dot(rotAxes.xy, tempUV), dot(rotAxes.zw, tempUV)) + float2(0.5, 0.5);
                tempUV = clamp(retVal.xy, float2(0.0, 0.0), float2(1.0, 1.0));
                retVal.z = ((tempUV.x == retVal.x) && (tempUV.y == retVal.y)) ? 1.0 : 0.0;
                return retVal;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            /*
            float ApplyBrush(float height, float brushStrength)
            {
                float targetHeight = BRUSH_TARGETHEIGHT;
                if (targetHeight > height)
                {
                    height += brushStrength;
                    height = height < targetHeight ? height : targetHeight;
                }
                else
                {
                    height -= brushStrength;
                    height = height > targetHeight ? height : targetHeight;
                }
                return height;
            }*/

        ENDCG


        Pass    // 11 Erode
        {
            Name "Erode Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment ErodeHeight

            float4 ErodeHeight(v2f i) : SV_Target
            {
                float3 sampleUV = RotateUVs(i.texcoord, BRUSH_ROTATION);
                float height = UnpackHeightmap(tex2D(_MainTex, i.texcoord));
                float brushStrength = sampleUV.z * BRUSH_STRENGTH * UnpackHeightmap(tex2D(_BrushTex, sampleUV.xy));

                float avg = 0.0F;
                float xoffset = _MainTex_TexelSize.x * BRUSH_FEATURESIZE;
                float yoffset = _MainTex_TexelSize.y * BRUSH_FEATURESIZE;

                float localMaxima = height;

                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2( xoffset,  0      ))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2(-xoffset,  0      ))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2( xoffset,  yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2(-xoffset,  yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2( xoffset, -yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2(-xoffset, -yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2( 0,        yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, i.texcoord + float2( 0,       -yoffset))));

                float sharpness = 0.8F;
                float erodeAmt = pow(clamp(0.01F * (localMaxima - height), 0.0f, 1.0f), sharpness);
                float h = height - erodeAmt;

                return PackHeightmap(lerp(height, h, brushStrength));
            }
            ENDCG
        }
    }
    Fallback Off
}
