Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

cbuffer Constants : register(b0)
{
    float2 ScreenSize;
    float Tolerance;
    float LuminanceMix;

    float EdgeSoftness;
    float ClipBlack;
    float ClipWhite;
    float EdgeBlur;

    float GradientStrength;
    float GradientAngle;
    float SpillSuppression;
    float EdgeBalance;

    float Despot;
    float Erode;
    float EdgeDesaturation;
    float KeyCleanup;

    float4 BaseColor;

    float3 EndColor;
    float _padding0;

    float4 ReplaceColor;

    int ColorSpace;
    int MainKeyColor;
    int IsInverted;
    int IsCompleteKey;

    float HueRange;
    float SaturationThreshold;
    int QualityPreset;
    float LuminanceRange;

    float EdgeDetection;
    float Denoise;
    float Feathering;
    float ReplaceIntensity;

    float PreserveLuminance;
    int DebugMode;
    float _padding1;
    float _padding2;

    float4 ExceptionColor1;

    float3 ExceptionColor2;
    float ExceptionTolerance;

    float ExceptionGradientStrength;
    float ExceptionGradientAngle;
    float _padding3;
    float _padding4;

    float ResidualColorCorrection;
    float CorrectionTolerance;
    float TransparencyQuality;
    float AlphaBlendAdjustment;

    float3 TargetResidualColor;
    float _padding5;

    float3 CorrectedColor;
    float TranslucentDespill;

    float ForegroundBrightness;
    float ForegroundContrast;
    float ForegroundSaturation;
    float _padding7;
};

static const float PI = 3.14159265358979323846;
static const float3 LUM_COEFF = float3(0.2126, 0.7152, 0.0722);

float3 RGBtoHSV(float3 c)
{
    float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 RGBtoLab(float3 rgb)
{
    float3 xyz;
    float3 lin = float3(
        (rgb.r > 0.04045) ? pow((rgb.r + 0.055) / 1.055, 2.4) : rgb.r / 12.92,
        (rgb.g > 0.04045) ? pow((rgb.g + 0.055) / 1.055, 2.4) : rgb.g / 12.92,
        (rgb.b > 0.04045) ? pow((rgb.b + 0.055) / 1.055, 2.4) : rgb.b / 12.92);
    xyz.x = lin.r * 0.4124564 + lin.g * 0.3575761 + lin.b * 0.1804375;
    xyz.y = lin.r * 0.2126729 + lin.g * 0.7151522 + lin.b * 0.0721750;
    xyz.z = lin.r * 0.0193339 + lin.g * 0.1191920 + lin.b * 0.9503041;
    xyz /= float3(0.95047, 1.0, 1.08883);
    float3 f = float3(
        (xyz.x > 0.008856) ? pow(xyz.x, 1.0/3.0) : (7.787 * xyz.x + 16.0/116.0),
        (xyz.y > 0.008856) ? pow(xyz.y, 1.0/3.0) : (7.787 * xyz.y + 16.0/116.0),
        (xyz.z > 0.008856) ? pow(xyz.z, 1.0/3.0) : (7.787 * xyz.z + 16.0/116.0));
    return float3(116.0 * f.y - 16.0, 500.0 * (f.x - f.y), 200.0 * (f.y - f.z));
}

float3 RGBtoYUV(float3 rgb)
{
    return float3(
        0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b,
        -0.14713 * rgb.r - 0.28886 * rgb.g + 0.436 * rgb.b,
        0.615 * rgb.r - 0.51499 * rgb.g - 0.10001 * rgb.b);
}

float3 RGBtoXYZ(float3 rgb)
{
    float3 lin = float3(
        (rgb.r > 0.04045) ? pow((rgb.r + 0.055) / 1.055, 2.4) : rgb.r / 12.92,
        (rgb.g > 0.04045) ? pow((rgb.g + 0.055) / 1.055, 2.4) : rgb.g / 12.92,
        (rgb.b > 0.04045) ? pow((rgb.b + 0.055) / 1.055, 2.4) : rgb.b / 12.92);
    return float3(
        lin.r * 0.4124564 + lin.g * 0.3575761 + lin.b * 0.1804375,
        lin.r * 0.2126729 + lin.g * 0.7151522 + lin.b * 0.0721750,
        lin.r * 0.0193339 + lin.g * 0.1191920 + lin.b * 0.9503041);
}

float3 RGBtoLCH(float3 rgb)
{
    float3 lab = RGBtoLab(rgb);
    float C = sqrt(lab.y * lab.y + lab.z * lab.z);
    float H = atan2(lab.z, lab.y);
    return float3(lab.x, C, H);
}

float CalculateColorDistance(float3 srcColor, float3 keyColor, int colorSpaceType,
    float hueRangeParam, float satThresholdParam, float lumRangeParam, float lumMixParam)
{
    float dist = 0;

    if (colorSpaceType == 0)
    {
        float chromaDist = length(srcColor - keyColor);
        float lumDist = abs(dot(srcColor, LUM_COEFF) - dot(keyColor, LUM_COEFF));
        dist = lerp(chromaDist, lumDist, lumMixParam);
        if (lumRangeParam > 0)
        {
            float lumDiff = abs(dot(srcColor, LUM_COEFF) - dot(keyColor, LUM_COEFF));
            if (lumDiff > lumRangeParam) dist += lumDiff - lumRangeParam;
        }
    }
    else if (colorSpaceType == 1)
    {
        float3 srcHSV = RGBtoHSV(srcColor);
        float3 keyHSV = RGBtoHSV(keyColor);
        float hueDiff = abs(srcHSV.x - keyHSV.x);
        if (hueDiff > 0.5) hueDiff = 1.0 - hueDiff;
        float satDiff = abs(srcHSV.y - keyHSV.y);
        float valDiff = abs(srcHSV.z - keyHSV.z);
        dist = hueDiff * 2.0 + satDiff * 0.5 + valDiff * lumMixParam;
        if (hueRangeParam > 0 && hueDiff * 360.0 > hueRangeParam) dist += (hueDiff * 360.0 - hueRangeParam) / 360.0;
        if (satThresholdParam > 0 && srcHSV.y < satThresholdParam) dist *= 0.3;
    }
    else if (colorSpaceType == 2)
    {
        float3 srcLab = RGBtoLab(srcColor);
        float3 keyLab = RGBtoLab(keyColor);
        float3 diff = srcLab - keyLab;
        dist = sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z) / 100.0;
    }
    else if (colorSpaceType == 3)
    {
        float3 srcYUV = RGBtoYUV(srcColor);
        float3 keyYUV = RGBtoYUV(keyColor);
        float chromaDist = length(srcYUV.yz - keyYUV.yz);
        float lumDist = abs(srcYUV.x - keyYUV.x);
        dist = lerp(chromaDist, lumDist, lumMixParam);
        if (lumRangeParam > 0 && lumDist > lumRangeParam) dist += lumDist - lumRangeParam;
    }
    else if (colorSpaceType == 4)
    {
        float3 srcXYZ = RGBtoXYZ(srcColor);
        float3 keyXYZ = RGBtoXYZ(keyColor);
        dist = length(srcXYZ - keyXYZ);
    }
    else if (colorSpaceType == 5)
    {
        float3 srcLCH = RGBtoLCH(srcColor);
        float3 keyLCH = RGBtoLCH(keyColor);
        float dL = srcLCH.x - keyLCH.x;
        float dC = srcLCH.y - keyLCH.y;
        float dH = srcLCH.z - keyLCH.z;
        if (dH > PI) dH -= 2.0 * PI;
        if (dH < -PI) dH += 2.0 * PI;
        float dHChroma = 2.0 * sqrt(srcLCH.y * keyLCH.y) * sin(dH / 2.0);
        dist = sqrt(dL * dL + dC * dC + dHChroma * dHChroma) / 100.0;
        if (hueRangeParam > 0 && abs(dH) * 180.0 / PI > hueRangeParam) dist += (abs(dH) * 180.0 / PI - hueRangeParam) / 360.0;
    }
    else
    {
        float3 srcLab = RGBtoLab(srcColor);
        float3 keyLab = RGBtoLab(keyColor);
        float C1 = sqrt(srcLab.y * srcLab.y + srcLab.z * srcLab.z);
        float C2 = sqrt(keyLab.y * keyLab.y + keyLab.z * keyLab.z);
        float Cab = (C1 + C2) / 2.0;
        float Cab7 = pow(Cab, 7.0);
        float G = 0.5 * (1.0 - sqrt(Cab7 / (Cab7 + pow(25.0, 7.0))));
        float a1p = srcLab.y * (1.0 + G);
        float a2p = keyLab.y * (1.0 + G);
        float C1p = sqrt(a1p * a1p + srcLab.z * srcLab.z);
        float C2p = sqrt(a2p * a2p + keyLab.z * keyLab.z);
        float h1p = atan2(srcLab.z, a1p);
        float h2p = atan2(keyLab.z, a2p);
        if (h1p < 0) h1p += 2.0 * PI;
        if (h2p < 0) h2p += 2.0 * PI;
        float dLp = keyLab.x - srcLab.x;
        float dCp = C2p - C1p;
        float dhp = h2p - h1p;
        if (dhp > PI) dhp -= 2.0 * PI;
        if (dhp < -PI) dhp += 2.0 * PI;
        float dHp = 2.0 * sqrt(C1p * C2p) * sin(dhp / 2.0);
        float Lbp = (srcLab.x + keyLab.x) / 2.0;
        float Cbp = (C1p + C2p) / 2.0;
        float hbp = (abs(h1p - h2p) <= PI) ? (h1p + h2p) / 2.0 : (h1p + h2p + 2.0 * PI) / 2.0;
        float T = 1.0 - 0.17 * cos(hbp - PI/6.0) + 0.24 * cos(2.0*hbp) + 0.32 * cos(3.0*hbp + PI/30.0) - 0.20 * cos(4.0*hbp - 63.0*PI/180.0);
        float Lbpm50sq = (Lbp - 50.0) * (Lbp - 50.0);
        float SL = 1.0 + 0.015 * Lbpm50sq / sqrt(20.0 + Lbpm50sq);
        float SC = 1.0 + 0.045 * Cbp;
        float SH = 1.0 + 0.015 * Cbp * T;
        float Cbp7 = pow(Cbp, 7.0);
        float RT = -2.0 * sqrt(Cbp7 / (Cbp7 + pow(25.0, 7.0))) * sin(PI/3.0 * exp(-((hbp - 275.0*PI/180.0)/(25.0*PI/180.0)) * ((hbp - 275.0*PI/180.0)/(25.0*PI/180.0))));
        dist = sqrt(pow(dLp/SL, 2.0) + pow(dCp/SC, 2.0) + pow(dHp/SH, 2.0) + RT * (dCp/SC) * (dHp/SH)) / 100.0;
    }

    return dist;
}

float3 GetKeyColorForPreset(int preset)
{
    if (preset == 1) return float3(0, 1, 0);
    if (preset == 2) return float3(0, 0, 1);
    if (preset == 3) return float3(1, 0, 0);
    return float3(0, 1, 0);
}

float3 SuppressSpill(float3 color, int keyType, float strength)
{
    if (strength <= 0 || keyType == 0) return color;

    float3 result = color;
    if (keyType == 1)
    {
        float spillAmount = max(0, color.g - max(color.r, color.b));
        result.g -= spillAmount * strength;
        result.r += spillAmount * strength * 0.5;
        result.b += spillAmount * strength * 0.5;
    }
    else if (keyType == 2)
    {
        float spillAmount = max(0, color.b - max(color.r, color.g));
        result.b -= spillAmount * strength;
        result.r += spillAmount * strength * 0.5;
        result.g += spillAmount * strength * 0.5;
    }
    else if (keyType == 3)
    {
        float spillAmount = max(0, color.r - max(color.g, color.b));
        result.r -= spillAmount * strength;
        result.g += spillAmount * strength * 0.5;
        result.b += spillAmount * strength * 0.5;
    }
    return saturate(result);
}

float3 SuppressTranslucentSpill(float3 color, int keyType, float alpha, float strength)
{
    if (strength <= 0 || keyType == 0 || alpha >= 1.0 || alpha <= 0.0) return color;

    float translucentFactor = 1.0 - alpha;
    float adjustedStrength = strength * translucentFactor;
    return SuppressSpill(color, keyType, adjustedStrength);
}

float CalculateGradientKey(float2 uv, float3 baseCol, float3 endCol, float3 srcColor,
    float strength, float angle, int colorSpaceType,
    float hueRangeParam, float satThresholdParam, float lumRangeParam, float lumMixParam, float tolerance)
{
    if (strength <= 0) return 0;

    float rad = angle * PI / 180.0;
    float gradPos = dot(uv - 0.5, float2(cos(rad), sin(rad))) + 0.5;
    gradPos = saturate(gradPos);

    float3 gradColor = lerp(baseCol, endCol, gradPos);
    float gradDist = CalculateColorDistance(srcColor, gradColor, colorSpaceType,
        hueRangeParam, satThresholdParam, lumRangeParam, lumMixParam);

    float gradMask = 1.0 - smoothstep(tolerance * 0.5, tolerance, gradDist);
    return gradMask * strength;
}

float4 main(float4 pos : SV_POSITION, float4 posScene : SCENE_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
{
    float4 src = InputTexture.Sample(InputSampler, uv);

    if (src.a <= 0.001) return float4(0, 0, 0, 0);

    float3 srcRGB = src.rgb / max(src.a, 0.001);

    float3 keyColor;
    int actualColorSpace;
    if (MainKeyColor == 0)
    {
        keyColor = BaseColor.rgb;
        actualColorSpace = ColorSpace;
    }
    else
    {
        keyColor = GetKeyColorForPreset(MainKeyColor);
        actualColorSpace = 2;
    }

    float dist = CalculateColorDistance(srcRGB, keyColor, actualColorSpace,
        HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);

    float mask = smoothstep(Tolerance - EdgeSoftness * Tolerance, Tolerance, dist);

    float gradContrib = CalculateGradientKey(uv, keyColor, EndColor, srcRGB,
        GradientStrength, GradientAngle, actualColorSpace,
        HueRange, SaturationThreshold, LuminanceRange, LuminanceMix, Tolerance);

    mask = min(mask, 1.0 - gradContrib);

    float exMask = 0;
    if (ExceptionColor1.a > 0 && ExceptionTolerance > 0)
    {
        float exDist = CalculateColorDistance(srcRGB, ExceptionColor1.rgb, actualColorSpace,
            0, 0, 0, LuminanceMix);
        exMask = 1.0 - smoothstep(ExceptionTolerance * 0.8, ExceptionTolerance, exDist);

        float exGrad = CalculateGradientKey(uv, ExceptionColor1.rgb, ExceptionColor2, srcRGB,
            ExceptionGradientStrength, ExceptionGradientAngle, actualColorSpace,
            0, 0, 0, LuminanceMix, ExceptionTolerance);
        exMask = max(exMask, exGrad);
    }
    mask = max(mask, exMask);

    float2 texelSize = 1.0 / ScreenSize;

    if (EdgeBlur > 0)
    {
        float blurRadius = EdgeBlur;
        int samples;
        if (QualityPreset == 0) samples = 4;
        else if (QualityPreset == 2) samples = 12;
        else samples = 8;

        float blurredMask = 0;
        float totalWeight = 0;
        for (int i = 0; i < samples; i++)
        {
            float angle2 = (float)i / (float)samples * 2.0 * PI;
            float2 offset = float2(cos(angle2), sin(angle2)) * texelSize * blurRadius;
            float4 neighborSrc = InputTexture.Sample(InputSampler, uv + offset);
            if (neighborSrc.a > 0.001)
            {
                float3 nRGB = neighborSrc.rgb / max(neighborSrc.a, 0.001);
                float nDist = CalculateColorDistance(nRGB, keyColor, actualColorSpace,
                    HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);
                float nMask = smoothstep(Tolerance - EdgeSoftness * Tolerance, Tolerance, nDist);
                blurredMask += nMask;
                totalWeight += 1.0;
            }
        }
        if (totalWeight > 0) mask = lerp(mask, blurredMask / totalWeight, 0.7);
    }

    if (Despot > 0)
    {
        float despotRadius = Despot;
        float minMask = mask;
        float maxMask = mask;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                float2 nUV = uv + float2(dx, dy) * texelSize * despotRadius;
                float4 nSrc = InputTexture.Sample(InputSampler, nUV);
                if (nSrc.a > 0.001)
                {
                    float3 nRGB = nSrc.rgb / max(nSrc.a, 0.001);
                    float nDist = CalculateColorDistance(nRGB, keyColor, actualColorSpace,
                        HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);
                    float nMask = smoothstep(Tolerance - EdgeSoftness * Tolerance, Tolerance, nDist);
                    minMask = min(minMask, nMask);
                    maxMask = max(maxMask, nMask);
                }
            }
        }
        if (maxMask - minMask > 0.5) mask = minMask;
    }

    if (Erode > 0)
    {
        float erodeMin = mask;
        for (int ex = -1; ex <= 1; ex++)
        {
            for (int ey = -1; ey <= 1; ey++)
            {
                if (ex == 0 && ey == 0) continue;
                float2 nUV = uv + float2(ex, ey) * texelSize * Erode;
                float4 nSrc = InputTexture.Sample(InputSampler, nUV);
                if (nSrc.a > 0.001)
                {
                    float3 nRGB = nSrc.rgb / max(nSrc.a, 0.001);
                    float nDist = CalculateColorDistance(nRGB, keyColor, actualColorSpace,
                        HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);
                    float nMask = smoothstep(Tolerance - EdgeSoftness * Tolerance, Tolerance, nDist);
                    erodeMin = min(erodeMin, nMask);
                }
            }
        }
        mask = erodeMin;
    }

    if (KeyCleanup > 0)
    {
        if (mask < KeyCleanup * 0.5) mask = 0;
        else if (mask > 1.0 - KeyCleanup * 0.5) mask = 1;
    }

    if (Feathering > 0)
    {
        float featherSum = 0;
        float featherWeight = 0;
        int fSamples = (QualityPreset == 2) ? 8 : 4;
        for (int fi = 0; fi < fSamples; fi++)
        {
            float fAngle = (float)fi / (float)fSamples * 2.0 * PI;
            float2 fOffset = float2(cos(fAngle), sin(fAngle)) * texelSize * Feathering;
            float4 fSrc = InputTexture.Sample(InputSampler, uv + fOffset);
            if (fSrc.a > 0.001)
            {
                float3 fRGB = fSrc.rgb / max(fSrc.a, 0.001);
                float fDist = CalculateColorDistance(fRGB, keyColor, actualColorSpace,
                    HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);
                float fMask = smoothstep(Tolerance - EdgeSoftness * Tolerance, Tolerance, fDist);
                featherSum += fMask;
                featherWeight += 1.0;
            }
        }
        if (featherWeight > 0) mask = lerp(mask, featherSum / featherWeight, 0.5);
    }

    if (EdgeBalance != 0)
    {
        if (EdgeBalance > 0) mask = pow(mask, 1.0 / (1.0 + EdgeBalance));
        else mask = pow(mask, 1.0 + abs(EdgeBalance));
    }

    if (Denoise > 0)
    {
        float noiseMask = 0;
        float noiseW = 0;
        for (int ni = -1; ni <= 1; ni++)
        {
            for (int nj = -1; nj <= 1; nj++)
            {
                float2 nUV = uv + float2(ni, nj) * texelSize;
                float4 nSrc = InputTexture.Sample(InputSampler, nUV);
                if (nSrc.a > 0.001)
                {
                    float3 nRGB = nSrc.rgb / max(nSrc.a, 0.001);
                    float nDist = CalculateColorDistance(nRGB, keyColor, actualColorSpace,
                        HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);
                    noiseMask += smoothstep(Tolerance - EdgeSoftness * Tolerance, Tolerance, nDist);
                    noiseW += 1.0;
                }
            }
        }
        if (noiseW > 0) mask = lerp(mask, noiseMask / noiseW, Denoise);
    }

    if (EdgeDetection > 0)
    {
        float edgeSum = 0;
        for (int ei = -1; ei <= 1; ei++)
        {
            for (int ej = -1; ej <= 1; ej++)
            {
                float w = (ei == 0 && ej == 0) ? -8.0 : 1.0;
                float2 eUV = uv + float2(ei, ej) * texelSize;
                float4 eSrc = InputTexture.Sample(InputSampler, eUV);
                float eLum = dot(eSrc.rgb, LUM_COEFF);
                edgeSum += eLum * w;
            }
        }
        float edgeStrength = saturate(abs(edgeSum));
        mask = lerp(mask, max(mask, edgeStrength), EdgeDetection);
    }

    mask = smoothstep(ClipBlack, ClipWhite, mask);

    if (IsInverted) mask = 1.0 - mask;

    if (DebugMode == 1) return float4(mask, mask, mask, 1.0);
    if (DebugMode == 2) return float4(dist, dist, dist, 1.0);

    float3 resultRGB = srcRGB;

    if (!IsCompleteKey)
    {
        resultRGB = SuppressSpill(resultRGB, MainKeyColor, SpillSuppression * (1.0 - mask));

        if (TranslucentDespill > 0 && mask > 0 && mask < 1.0)
            resultRGB = SuppressTranslucentSpill(resultRGB, MainKeyColor, mask, TranslucentDespill);

        if (EdgeDesaturation > 0 && mask < 1.0 && mask > 0)
        {
            float edgeFactor = 1.0 - mask;
            float lum = dot(resultRGB, LUM_COEFF);
            float3 gray = float3(lum, lum, lum);
            resultRGB = lerp(resultRGB, gray, EdgeDesaturation * edgeFactor);
        }

        if (ReplaceIntensity > 0)
        {
            float replaceFactor = (1.0 - mask) * ReplaceIntensity;
            float3 replaceRGB = ReplaceColor.rgb;
            if (PreserveLuminance > 0)
            {
                float srcLum = dot(resultRGB, LUM_COEFF);
                float repLum = dot(replaceRGB, LUM_COEFF);
                if (repLum > 0.001)
                    replaceRGB *= lerp(1.0, srcLum / repLum, PreserveLuminance);
            }
            resultRGB = lerp(resultRGB, replaceRGB, replaceFactor * ReplaceColor.a);
        }
    }

    if (DebugMode == 3) return float4(resultRGB * src.a, src.a);

    if (ResidualColorCorrection > 0)
    {
        float resDist = length(resultRGB - TargetResidualColor);
        float resFactor = 1.0 - smoothstep(CorrectionTolerance * 0.5, CorrectionTolerance, resDist);
        resultRGB = lerp(resultRGB, CorrectedColor, resFactor * ResidualColorCorrection);
    }

    if (DebugMode == 4) return float4(resultRGB * src.a, src.a);

    if (ForegroundBrightness != 0) resultRGB += ForegroundBrightness;
    if (ForegroundContrast != 0) resultRGB = ((resultRGB - 0.5) * (1.0 + ForegroundContrast)) + 0.5;
    if (ForegroundSaturation != 0)
    {
        float grayVal = dot(resultRGB, LUM_COEFF);
        resultRGB = lerp(float3(grayVal, grayVal, grayVal), resultRGB, 1.0 + ForegroundSaturation);
    }
    resultRGB = saturate(resultRGB);

    float finalAlpha = src.a * mask;

    if (TransparencyQuality > 0 && mask > 0 && mask < 1.0)
    {
        float alphaRefine = lerp(mask, smoothstep(0, 1, mask), TransparencyQuality);
        finalAlpha = src.a * alphaRefine;
    }

    if (AlphaBlendAdjustment != 0) finalAlpha = saturate(finalAlpha + AlphaBlendAdjustment * (1.0 - mask));

    return float4(resultRGB * finalAlpha, finalAlpha);
}