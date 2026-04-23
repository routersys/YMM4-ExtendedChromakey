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
static const float EPSILON = 1e-10;
static const float MIN_VALID_ALPHA = 0.0001;
static const float3 LUM_COEFF = float3(0.2126, 0.7152, 0.0722);

float safe_divide(float a, float b)
{
    return abs(b) > EPSILON ? (a / b) : 0.0;
}

float safe_sqrt(float x)
{
    return sqrt(max(x, 0.0));
}

float safe_atan2(float y, float x)
{
    if (abs(x) < EPSILON && abs(y) < EPSILON)
        return 0.0;
    return atan2(y, x);
}

float hue_difference(float h1, float h2)
{
    float diff = abs(h1 - h2);
    return min(diff, 360.0 - diff);
}

float3 RGBtoHSV(float3 c)
{
    c = saturate(c);
    float maxComp = max(max(c.r, c.g), c.b);
    float minComp = min(min(c.r, c.g), c.b);
    float delta = maxComp - minComp;
    float3 hsv = float3(0.0, 0.0, maxComp);
    if (maxComp > EPSILON)
    {
        hsv.y = safe_divide(delta, maxComp);
        if (delta > EPSILON)
        {
            if (abs(maxComp - c.r) < EPSILON)
                hsv.x = safe_divide(c.g - c.b, delta);
            else if (abs(maxComp - c.g) < EPSILON)
                hsv.x = 2.0 + safe_divide(c.b - c.r, delta);
            else
                hsv.x = 4.0 + safe_divide(c.r - c.g, delta);
            hsv.x = frac(hsv.x / 6.0 + 1.0);
        }
    }
    return hsv;
}

float3 sRGBToLinear(float3 c)
{
    c = saturate(c);
    return float3(
        (c.r > 0.04045) ? pow(max((c.r + 0.055) / 1.055, EPSILON), 2.4) : c.r / 12.92,
        (c.g > 0.04045) ? pow(max((c.g + 0.055) / 1.055, EPSILON), 2.4) : c.g / 12.92,
        (c.b > 0.04045) ? pow(max((c.b + 0.055) / 1.055, EPSILON), 2.4) : c.b / 12.92);
}

float3 RGBtoXYZ(float3 rgb)
{
    float3 lin = sRGBToLinear(rgb);
    return float3(
        lin.r * 0.4124564 + lin.g * 0.3575761 + lin.b * 0.1804375,
        lin.r * 0.2126729 + lin.g * 0.7151522 + lin.b * 0.0721750,
        lin.r * 0.0193339 + lin.g * 0.1191920 + lin.b * 0.9503041);
}

float labf(float t)
{
    float delta = 6.0 / 29.0;
    return (t > delta * delta * delta) ? pow(max(t, EPSILON), 1.0 / 3.0) : t / (3.0 * delta * delta) + 4.0 / 29.0;
}

float3 RGBtoLab(float3 rgb)
{
    float3 xyz = RGBtoXYZ(rgb);
    xyz /= float3(0.95047, 1.0, 1.08883);
    return float3(
        116.0 * labf(xyz.y) - 16.0,
        500.0 * (labf(xyz.x) - labf(xyz.y)),
        200.0 * (labf(xyz.y) - labf(xyz.z)));
}

float3 RGBtoYUV(float3 rgb)
{
    rgb = saturate(rgb);
    return float3(
        0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b,
        -0.14713 * rgb.r - 0.28886 * rgb.g + 0.436 * rgb.b,
        0.615 * rgb.r - 0.51499 * rgb.g - 0.10001 * rgb.b);
}

float3 RGBtoLCH(float3 rgb)
{
    float3 lab = RGBtoLab(rgb);
    float C = safe_sqrt(lab.y * lab.y + lab.z * lab.z);
    float H = fmod(safe_atan2(lab.z, lab.y) * (180.0 / PI) + 360.0, 360.0);
    return float3(lab.x, C, H);
}

float CalculateColorDistance(float3 srcColor, float3 keyColor, int colorSpaceType,
    float hueRangeParam, float satThresholdParam, float lumRangeParam, float lumMixParam)
{
    srcColor = saturate(srcColor);
    keyColor = saturate(keyColor);

    if (colorSpaceType == 0)
    {
        float chromaDist = length(srcColor - keyColor);
        float lumDist = abs(dot(srcColor, LUM_COEFF) - dot(keyColor, LUM_COEFF));
        float dist = lerp(chromaDist, lumDist, lumMixParam);
        if (lumRangeParam > 0.0)
        {
            float lumDiff = abs(dot(srcColor, LUM_COEFF) - dot(keyColor, LUM_COEFF));
            if (lumDiff > lumRangeParam)
                dist += lumDiff - lumRangeParam;
        }
        return dist;
    }
    if (colorSpaceType == 1)
    {
        float3 srcHSV = RGBtoHSV(srcColor);
        float3 keyHSV = RGBtoHSV(keyColor);
        if (srcHSV.y < satThresholdParam && keyHSV.y < satThresholdParam)
            return safe_divide(abs(srcHSV.z - keyHSV.z), max(lumRangeParam, EPSILON));
        if (srcHSV.y < satThresholdParam || keyHSV.y < satThresholdParam)
        {
            float lD = abs(srcHSV.z - keyHSV.z);
            float sD = abs(srcHSV.y - keyHSV.y);
            return safe_sqrt(lD * lD + sD * sD * 0.5);
        }
        float hueDiff = hue_difference(srcHSV.x * 360.0, keyHSV.x * 360.0) / 360.0;
        if (hueRangeParam > 0.0 && hueDiff > hueRangeParam)
            return 1.0;
        float satD = abs(srcHSV.y - keyHSV.y);
        float valD = abs(srcHSV.z - keyHSV.z);
        return safe_sqrt(hueDiff * hueDiff * 4.0 + satD * satD + valD * valD) / safe_sqrt(6.0);
    }
    if (colorSpaceType == 2)
    {
        float3 diff = RGBtoLab(srcColor) - RGBtoLab(keyColor);
        return safe_sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z) / 100.0;
    }
    if (colorSpaceType == 3)
    {
        float3 srcYUV = RGBtoYUV(srcColor);
        float3 keyYUV = RGBtoYUV(keyColor);
        if (lumRangeParam > 0.0 && abs(srcYUV.x - keyYUV.x) > lumRangeParam)
            return 1.0;
        return length(srcYUV - keyYUV);
    }
    if (colorSpaceType == 4)
        return length(RGBtoXYZ(srcColor) - RGBtoXYZ(keyColor));
    if (colorSpaceType == 5)
    {
        float3 srcLCH = RGBtoLCH(srcColor);
        float3 keyLCH = RGBtoLCH(keyColor);
        float dL = srcLCH.x - keyLCH.x;
        float dC = srcLCH.y - keyLCH.y;
        float dH = hue_difference(srcLCH.z, keyLCH.z);
        if (hueRangeParam > 0.0 && dH > hueRangeParam * 360.0)
            return 1.0;
        return safe_sqrt(dL * dL + dC * dC + (dH * 0.5) * (dH * 0.5)) / 100.0;
    }
    {
        float3 srcLab = RGBtoLab(srcColor);
        float3 keyLab = RGBtoLab(keyColor);
        float C1 = safe_sqrt(srcLab.y * srcLab.y + srcLab.z * srcLab.z);
        float C2 = safe_sqrt(keyLab.y * keyLab.y + keyLab.z * keyLab.z);
        float Cab = (C1 + C2) / 2.0;
        float Cab7 = pow(max(Cab, EPSILON), 7.0);
        float G = 0.5 * (1.0 - safe_sqrt(Cab7 / (Cab7 + pow(25.0, 7.0))));
        float a1p = srcLab.y * (1.0 + G);
        float a2p = keyLab.y * (1.0 + G);
        float C1p = safe_sqrt(a1p * a1p + srcLab.z * srcLab.z);
        float C2p = safe_sqrt(a2p * a2p + keyLab.z * keyLab.z);
        float h1p = safe_atan2(srcLab.z, a1p);
        float h2p = safe_atan2(keyLab.z, a2p);
        if (h1p < 0.0)
            h1p += 2.0 * PI;
        if (h2p < 0.0)
            h2p += 2.0 * PI;
        float h1d = h1p * (180.0 / PI);
        float h2d = h2p * (180.0 / PI);
        float dLp = keyLab.x - srcLab.x;
        float dCp = C2p - C1p;
        float dhp = h2p - h1p;
        if (dhp > PI)
            dhp -= 2.0 * PI;
        if (dhp < -PI)
            dhp += 2.0 * PI;
        float dHp = 2.0 * safe_sqrt(C1p * C2p) * sin(dhp / 2.0);
        float Lbp = (srcLab.x + keyLab.x) / 2.0;
        float Cbp = (C1p + C2p) / 2.0;
        float hDiff = abs(h1d - h2d);
        float hSum = h1d + h2d;
        float hbpd;
        if (C1p * C2p < EPSILON)
            hbpd = 0.0;
        else if (hDiff <= 180.0)
            hbpd = hSum / 2.0;
        else
            hbpd = (hSum < 360.0) ? (hSum + 360.0) / 2.0 : (hSum - 360.0) / 2.0;
        float hbp = hbpd * (PI / 180.0);
        float T = 1.0 - 0.17 * cos(hbp - PI / 6.0) + 0.24 * cos(2.0 * hbp)
                + 0.32 * cos(3.0 * hbp + PI / 30.0) - 0.20 * cos(4.0 * hbp - 63.0 * PI / 180.0);
        float Lbpm50sq = (Lbp - 50.0) * (Lbp - 50.0);
        float SL = 1.0 + 0.015 * Lbpm50sq / safe_sqrt(20.0 + Lbpm50sq);
        float SC = 1.0 + 0.045 * Cbp;
        float SH = 1.0 + 0.015 * Cbp * T;
        float Cbp7 = pow(max(Cbp, EPSILON), 7.0);
        float dTheta = 30.0 * exp(-((hbpd - 275.0) / 25.0) * ((hbpd - 275.0) / 25.0));
        float RT = -2.0 * safe_sqrt(Cbp7 / (Cbp7 + pow(25.0, 7.0))) * sin(dTheta * (PI / 180.0));
        return safe_sqrt(
            pow(safe_divide(dLp, SL), 2.0) +
            pow(safe_divide(dCp, SC), 2.0) +
            pow(safe_divide(dHp, SH), 2.0) +
            RT * safe_divide(dCp, SC) * safe_divide(dHp, SH)) / 100.0;
    }
}

float CalculatePresetDistance(float3 color, int preset, float lumMixParam)
{
    float key_primary, other_max;
    if (preset == 1)
    {
        key_primary = color.g;
        other_max = max(color.r, color.b);
    }
    else if (preset == 2)
    {
        key_primary = color.b;
        other_max = max(color.r, color.g);
    }
    else
    {
        key_primary = color.r;
        other_max = max(color.g, color.b);
    }
    float matte = saturate(key_primary - other_max);
    if (lumMixParam > EPSILON)
        matte *= lerp(1.0, 1.0 - dot(saturate(color), LUM_COEFF), saturate(lumMixParam));
    return 1.0 - matte;
}

float3 CalcGradientKeyColor(float2 uv, float3 baseCol, float3 endCol, float strength, float angle)
{
    if (strength < EPSILON)
        return baseCol;
    float rad = angle * PI / 180.0;
    float gradPos = saturate(dot(uv - 0.5, float2(cos(rad), sin(rad))) + 0.5);
    return lerp(baseCol, endCol, saturate(gradPos * strength));
}

float ComputeDistanceFromColor(float3 color, float2 uv)
{
    if (MainKeyColor != 0)
        return CalculatePresetDistance(color, MainKeyColor, LuminanceMix);
    float3 keyColor = CalcGradientKeyColor(uv, BaseColor.rgb, EndColor, GradientStrength, GradientAngle);
    return CalculateColorDistance(color, keyColor, ColorSpace, HueRange, SaturationThreshold, LuminanceRange, LuminanceMix);
}

float SampleDistanceAtUV(float2 uv)
{
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
        return 1.0;
    float4 samp = InputTexture.SampleLevel(InputSampler, uv, 0);
    if (samp.a < MIN_VALID_ALPHA)
        return 1.0;
    float3 color = saturate(samp.rgb / max(samp.a, MIN_VALID_ALPHA));
    return ComputeDistanceFromColor(color, uv);
}

float DistanceToMask(float dist)
{
    return smoothstep(max(Tolerance - EdgeSoftness * Tolerance, 0.0), Tolerance, dist);
}

float SampleMaskAtUV(float2 uv)
{
    return DistanceToMask(SampleDistanceAtUV(uv));
}

float ApplyEdgeBlurFilter(float2 uv, float centerDist)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float totalDist = centerDist;
    float totalWeight = 1.0;
    int sampleCount = (QualityPreset == 2) ? 12 : (QualityPreset == 1) ? 8 : 4;
    for (int i = 0; i < 12; i++)
    {
        if (i >= sampleCount)
            break;
        float a = (float) i / (float) sampleCount * 2.0 * PI;
        float2 offset = float2(cos(a), sin(a)) * ts * EdgeBlur;
        float w = exp(-length(offset) * 2.0);
        totalDist += SampleDistanceAtUV(uv + offset) * w;
        totalWeight += w;
    }
    return safe_divide(totalDist, totalWeight);
}

float ApplyMorphologyOperation(float2 uv, float operationSize, bool isErosion)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float extreme = isErosion ? 1.0 : 0.0;
    int sampleCount = (QualityPreset == 0) ? 4 : 8;
    float2 offsets[8] =
    {
        float2(0, -1), float2(1, 0), float2(0, 1), float2(-1, 0),
        float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, 1)
    };
    for (int i = 0; i < 8; i++)
    {
        if (i >= sampleCount)
            break;
        float s = SampleDistanceAtUV(uv + ts * offsets[i] * operationSize);
        extreme = isErosion ? min(extreme, s) : max(extreme, s);
    }
    return extreme;
}

float ApplyDespotFilter(float2 uv, float despotSize, float centerDist)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float neighborSum = 0.0;
    int sampleCount = (QualityPreset == 0) ? 4 : 8;
    float2 offsets[8] =
    {
        float2(0, -1), float2(1, 0), float2(0, 1), float2(-1, 0),
        float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, 1)
    };
    for (int i = 0; i < 8; i++)
    {
        if (i >= sampleCount)
            break;
        neighborSum += SampleDistanceAtUV(uv + ts * offsets[i] * despotSize);
    }
    float neighborAvg = safe_divide(neighborSum, (float) sampleCount);
    if (centerDist < 0.5 && neighborAvg > 0.5)
        return lerp(centerDist, neighborAvg, 0.8);
    if (centerDist > 0.5 && neighborAvg < 0.5)
        return lerp(centerDist, neighborAvg, 0.8);
    return centerDist;
}

float ApplyKeyCleanup(float2 uv, float cleanupAmount, float centerDist)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float radius = abs(cleanupAmount) * 0.01;
    int sampleCount = (QualityPreset == 2) ? 8 : 4;
    float2 offsets[8] =
    {
        float2(-radius, 0.0), float2(radius, 0.0),
        float2(0.0, -radius), float2(0.0, radius),
        float2(-radius, -radius), float2(radius, -radius),
        float2(-radius, radius), float2(radius, radius)
    };
    float minD = centerDist;
    float maxD = centerDist;
    for (int i = 0; i < 8; i++)
    {
        if (i >= sampleCount)
            break;
        float d = SampleDistanceAtUV(uv + ts * offsets[i]);
        minD = min(minD, d);
        maxD = max(maxD, d);
    }
    return (cleanupAmount < 0.0) ? maxD : minD;
}

float ApplyEdgeDetectionFilter(float2 uv)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float d[9];
    float2 sobelOffsets[9] =
    {
        float2(-1, -1), float2(0, -1), float2(1, -1),
        float2(-1, 0), float2(0, 0), float2(1, 0),
        float2(-1, 1), float2(0, 1), float2(1, 1)
    };
    for (int i = 0; i < 9; i++)
        d[i] = SampleDistanceAtUV(uv + ts * sobelOffsets[i]);
    float sobelX = (d[2] + 2.0 * d[5] + d[8]) - (d[0] + 2.0 * d[3] + d[6]);
    float sobelY = (d[6] + 2.0 * d[7] + d[8]) - (d[0] + 2.0 * d[1] + d[2]);
    return safe_sqrt(sobelX * sobelX + sobelY * sobelY) * 0.125;
}

float ApplyDenoiseFilter(float2 uv, float centerMask)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float sum = 0.0;
    for (int ni = -1; ni <= 1; ni++)
        for (int nj = -1; nj <= 1; nj++)
            sum += SampleMaskAtUV(uv + float2(ni, nj) * ts);
    return lerp(centerMask, sum / 9.0, Denoise);
}

float ApplyFeatheringFilter(float2 uv, float centerMask)
{
    float2 ts = 1.0 / max(ScreenSize, float2(1.0, 1.0));
    float sum = 0.0;
    int fSamples = (QualityPreset == 2) ? 8 : 4;
    float2 offsets[8] =
    {
        float2(0, -1), float2(1, 0), float2(0, 1), float2(-1, 0),
        float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, 1)
    };
    for (int fi = 0; fi < 8; fi++)
    {
        if (fi >= fSamples)
            break;
        sum += SampleMaskAtUV(uv + offsets[fi] * ts * Feathering);
    }
    return lerp(centerMask, safe_divide(sum, (float) fSamples), 0.5);
}

float NoiseReduction(float mask, float threshold)
{
    float t = threshold * 0.5;
    if (mask < t)
        return 0.0;
    if (mask > 1.0 - t)
        return 1.0;
    return smoothstep(t, 1.0 - t, mask);
}

float ImproveTransparencyQuality(float mask, float origAlpha, float quality)
{
    if (origAlpha < MIN_VALID_ALPHA)
        return mask;
    float alphaDiff = abs(mask - origAlpha);
    float adjustment = lerp(0.0, alphaDiff * 0.5, quality);
    if (origAlpha < 0.5)
        mask = lerp(mask, origAlpha, adjustment);
    return saturate(mask);
}

float3 SuppressSpill(float3 color, int keyType, float strength, float mask)
{
    if (strength <= 0.0 || keyType == 0)
        return color;
    float spill = 0.0;
    float3 result = color;
    if (keyType == 1)
    {
        spill = saturate(color.g - max(color.r, color.b));
        if (spill > EPSILON)
        {
            float avg = (color.r + color.b) * 0.5;
            result.g = lerp(color.g, lerp(color.g, avg, spill * 0.8), strength);
        }
    }
    else if (keyType == 2)
    {
        spill = saturate(color.b - max(color.r, color.g));
        if (spill > EPSILON)
        {
            float avg = (color.r + color.g) * 0.5;
            result.b = lerp(color.b, lerp(color.b, avg, spill * 0.8), strength);
        }
    }
    else if (keyType == 3)
    {
        spill = saturate(color.r - max(color.g, color.b));
        if (spill > EPSILON)
        {
            float avg = (color.g + color.b) * 0.5;
            result.r = lerp(color.r, lerp(color.r, avg, spill * 0.8), strength);
        }
    }
    float origLuma = dot(color, LUM_COEFF);
    float newLuma = dot(result, LUM_COEFF);
    if (newLuma > EPSILON && origLuma > EPSILON)
        result *= safe_divide(origLuma, newLuma);
    float edgeFactor = saturate((1.0 - mask) * 2.0);
    return saturate(lerp(color, result, saturate(strength * edgeFactor * spill)));
}

float3 SuppressTranslucentSpill(float3 color, int keyType, float alpha, float strength)
{
    if (strength <= 0.0 || keyType == 0 || alpha >= 1.0 || alpha <= 0.0)
        return color;
    float3 keyColor;
    if (keyType == 1)
        keyColor = float3(0, 1, 0);
    else if (keyType == 2)
        keyColor = float3(0, 0, 1);
    else
        keyColor = float3(1, 0, 0);
    float3 restored = (color - (1.0 - alpha) * keyColor) / max(alpha, EPSILON);
    return lerp(color, saturate(restored), strength);
}

float3 ApplyEdgeDesaturation(float3 color, float mask)
{
    float edgeFactor = saturate((1.0 - mask) * mask * 4.0);
    float luma = dot(color, LUM_COEFF);
    return lerp(color, float3(luma, luma, luma), EdgeDesaturation * edgeFactor);
}

float3 ApplyResidualColorCorrection(float3 color)
{
    float resDist = length(color - TargetResidualColor);
    float resFactor = 1.0 - smoothstep(CorrectionTolerance * 0.5, CorrectionTolerance, resDist);
    float origLuma = dot(color, LUM_COEFF);
    float corrLuma = dot(CorrectedColor, LUM_COEFF);
    float3 corrected = CorrectedColor;
    if (corrLuma > EPSILON)
        corrected *= safe_divide(origLuma, corrLuma);
    return lerp(color, saturate(corrected), resFactor * ResidualColorCorrection);
}

float3 ApplyForegroundCorrection(float3 color)
{
    if (abs(ForegroundBrightness) > EPSILON)
        color += ForegroundBrightness;
    if (abs(ForegroundContrast) > EPSILON)
        color = (color - 0.5) * (1.0 + ForegroundContrast) + 0.5;
    if (abs(ForegroundSaturation) > EPSILON)
    {
        float gray = dot(color, LUM_COEFF);
        color = lerp(float3(gray, gray, gray), color, 1.0 + ForegroundSaturation);
    }
    return saturate(color);
}

float4 main(float4 pos : SV_POSITION, float4 posScene : SCENE_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
{
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
        return float4(0, 0, 0, 0);

    float4 src = InputTexture.Sample(InputSampler, uv);

    if (src.a < MIN_VALID_ALPHA)
    {
        if (DebugMode > 0)
            return float4(0, 0, 0, 1);
        return src;
    }

    float3 srcRGB = saturate(src.rgb / max(src.a, MIN_VALID_ALPHA));

    if (MainKeyColor == 0 && BaseColor.a < MIN_VALID_ALPHA)
    {
        if (DebugMode > 0)
            return float4(0, 0, 0, 1);
        return src;
    }

    float dist = ComputeDistanceFromColor(srcRGB, uv);

    if (DebugMode == 2)
        return float4(dist, dist, dist, 1.0);

    if (!IsCompleteKey)
    {
        dist = saturate(dist + EdgeBalance * 0.01);

        if (EdgeBlur > EPSILON)
            dist = ApplyEdgeBlurFilter(uv, dist);

        if (Despot > EPSILON)
            dist = ApplyDespotFilter(uv, Despot, dist);

        if (abs(Erode) > EPSILON)
            dist = ApplyMorphologyOperation(uv, abs(Erode) * 0.1, Erode > 0.0);

        if (EdgeDetection > EPSILON)
            dist = saturate(dist - ApplyEdgeDetectionFilter(uv) * EdgeDetection);

        if (abs(KeyCleanup) > EPSILON)
            dist = ApplyKeyCleanup(uv, KeyCleanup / 50.0, dist);
    }

    float mask;
    if (IsCompleteKey)
        mask = (dist < Tolerance) ? 0.0 : 1.0;
    else
        mask = DistanceToMask(dist);

    mask = smoothstep(ClipBlack, max(ClipBlack + EPSILON, ClipWhite), mask);

    if (Denoise > EPSILON)
        mask = NoiseReduction(mask, Denoise);

    if (Feathering > EPSILON)
        mask = ApplyFeatheringFilter(uv, mask);

    if (EdgeBalance != 0.0 && !IsCompleteKey)
    {
        if (EdgeBalance > 0.0)
            mask = pow(max(mask, 0.0), 1.0 / (1.0 + EdgeBalance));
        else
            mask = pow(max(mask, 0.0), 1.0 + abs(EdgeBalance));
    }

    if (TransparencyQuality > EPSILON)
        mask = ImproveTransparencyQuality(mask, src.a, TransparencyQuality);

    if (IsInverted)
        mask = 1.0 - mask;

    if (ExceptionColor1.a > MIN_VALID_ALPHA && ExceptionTolerance > 0.0)
    {
        float3 effectiveExColor = CalcGradientKeyColor(uv, ExceptionColor1.rgb, ExceptionColor2, ExceptionGradientStrength, ExceptionGradientAngle);
        float exDist = CalculateColorDistance(srcRGB, effectiveExColor, ColorSpace, 0.0, 0.0, 0.0, LuminanceMix);
        float exMask = 1.0 - smoothstep(0.0, max(ExceptionTolerance, EPSILON), exDist);
        mask = max(mask, exMask);
    }

    if (DebugMode == 1)
        return float4(mask, mask, mask, 1.0);

    float3 resultRGB = srcRGB;

    if (!IsCompleteKey)
    {
        if (SpillSuppression > EPSILON)
            resultRGB = SuppressSpill(resultRGB, MainKeyColor, SpillSuppression * (1.0 - mask), mask);

        if (TranslucentDespill > EPSILON && mask > 0.0 && mask < 1.0)
            resultRGB = SuppressTranslucentSpill(resultRGB, MainKeyColor, mask, TranslucentDespill);

        if (DebugMode == 3)
            return float4(resultRGB * src.a, src.a);

        if (EdgeDesaturation > EPSILON && mask < 1.0 && mask > 0.0)
            resultRGB = ApplyEdgeDesaturation(resultRGB, mask);

        if (ReplaceIntensity > EPSILON)
        {
            float3 replaceRGB = ReplaceColor.rgb;
            if (PreserveLuminance > EPSILON)
            {
                float srcLum = dot(resultRGB, LUM_COEFF);
                float repLum = dot(replaceRGB, LUM_COEFF);
                if (repLum > EPSILON)
                    replaceRGB = lerp(replaceRGB, replaceRGB * safe_divide(srcLum, repLum), PreserveLuminance);
            }
            float3 blended = lerp(replaceRGB, resultRGB, mask);
            resultRGB = lerp(resultRGB, blended, ReplaceIntensity);
            float blendedAlpha = lerp(ReplaceColor.a, src.a, mask);
            float fa = lerp(src.a * mask, blendedAlpha, ReplaceIntensity);
            resultRGB = ApplyResidualColorCorrection(resultRGB);
            resultRGB = ApplyForegroundCorrection(resultRGB);
            if (DebugMode == 4)
                return float4(resultRGB * src.a, src.a);
            fa = saturate(fa);
            if (AlphaBlendAdjustment > EPSILON)
            {
                float enhanced = saturate(mask + AlphaBlendAdjustment * (1.0 - mask) * 0.5);
                mask = lerp(mask, enhanced, AlphaBlendAdjustment);
                fa = src.a * mask;
            }
            return float4(saturate(resultRGB) * fa, fa);
        }
    }

    if (ResidualColorCorrection > EPSILON)
        resultRGB = ApplyResidualColorCorrection(resultRGB);

    if (DebugMode == 4)
        return float4(resultRGB * src.a, src.a);

    resultRGB = ApplyForegroundCorrection(resultRGB);

    float finalAlpha = src.a * mask;

    if (TransparencyQuality > EPSILON && mask > 0.0 && mask < 1.0)
    {
        float alphaRefine = lerp(mask, smoothstep(0.0, 1.0, mask), TransparencyQuality);
        finalAlpha = src.a * alphaRefine;
    }

    if (AlphaBlendAdjustment > EPSILON)
    {
        float enhanced = saturate(mask + AlphaBlendAdjustment * (1.0 - mask) * 0.5);
        mask = lerp(mask, enhanced, AlphaBlendAdjustment);
        finalAlpha = src.a * mask;
    }

    return float4(saturate(resultRGB) * finalAlpha, finalAlpha);
}
