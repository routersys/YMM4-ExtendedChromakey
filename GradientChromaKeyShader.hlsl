#define PI 3.14159265359f
#define EPSILON 1e-10f
#define MIN_VALID_ALPHA 0.0001f

cbuffer Constants : register(b0)
{
    float2 screenSize;
    float tolerance;
    float luminanceMix;
    float edgeSoftness;
    float clipBlack;
    float clipWhite;
    float edgeBlur;

    float gradientStrength;
    float gradientAngle;
    float spillSuppression;
    float edgeBalance;

    float despot;
    float erode;
    float edgeDesaturation;
    float keyCleanup;

    float4 baseColor;

    float3 endColor;
    float _padding0;

    float4 replaceColor;

    int colorSpace;
    int mainKeyColor;
    int isInverted;
    int isCompleteKey;
    float hueRange;
    float saturationThreshold;
    int qualityPreset;
    float luminanceRange;

    float edgeDetection;
    float denoise;
    float feathering;
    float replaceIntensity;

    float preserveLuminance;
    int debugMode;
    float _padding1;
    float _padding2;

    float4 exceptionColor1;

    float3 exceptionColor2;
    float exceptionTolerance;

    float exceptionGradientStrength;
    float exceptionGradientAngle;
    float _padding3;
    float _padding4;

    float residualColorCorrection;
    float _padding5;
    float _padding6;
    float _padding7;

    float3 targetResidualColor;
    float correctionTolerance;

    float3 correctedColor;
    float transparencyQuality;

    float alphaBlendAdjustment;
    float _padding8;
    float _padding9;
    float _padding10;
};

Texture2D<float4> InputTexture : register(t0);
SamplerState InputSampler : register(s0);

float safe_divide(float a, float b)
{
    return abs(b) > EPSILON ? (a / b) : 0.0f;
}

float3 safe_normalize(float3 v)
{
    float len = length(v);
    return len > EPSILON ? (v / len) : float3(0.0f, 0.0f, 0.0f);
}

float safe_pow(float base, float exp)
{
    return pow(max(abs(base), EPSILON), exp);
}

float safe_sqrt(float x)
{
    return sqrt(max(x, 0.0f));
}

float safe_atan2(float y, float x)
{
    if (abs(x) < EPSILON && abs(y) < EPSILON)
        return 0.0f;
    return atan2(y, x);
}

float3 RGBToHSV(float3 c)
{
    c = saturate(c);
    float maxComp = max(max(c.r, c.g), c.b);
    float minComp = min(min(c.r, c.g), c.b);
    float delta = maxComp - minComp;
    
    float3 hsv = float3(0.0f, 0.0f, maxComp);
    if (maxComp > EPSILON)
    {
        hsv.y = safe_divide(delta, maxComp);
        if (delta > EPSILON)
        {
            if (abs(maxComp - c.r) < EPSILON)
                hsv.x = safe_divide((c.g - c.b), delta);
            else if (abs(maxComp - c.g) < EPSILON)
                hsv.x = 2.0f + safe_divide((c.b - c.r), delta);
            else
                hsv.x = 4.0f + safe_divide((c.r - c.g), delta);
            hsv.x = frac(hsv.x / 6.0f + 1.0f);
        }
    }
    
    return hsv;
}

float3 HSVToRGB(float3 hsv)
{
    hsv.x = frac(hsv.x);
    hsv.y = saturate(hsv.y);
    hsv.z = saturate(hsv.z);
    float c = hsv.z * hsv.y;
    float h = hsv.x * 6.0f;
    float x = c * (1.0f - abs(fmod(h, 2.0f) - 1.0f));
    float m = hsv.z - c;
    float3 rgb = float3(0.0f, 0.0f, 0.0f);
    if (h < 1.0f)
        rgb = float3(c, x, 0.0f);
    else if (h < 2.0f)
        rgb = float3(x, c, 0.0f);
    else if (h < 3.0f)
        rgb = float3(0.0f, c, x);
    else if (h < 4.0f)
        rgb = float3(0.0f, x, c);
    else if (h < 5.0f)
        rgb = float3(x, 0.0f, c);
    else
        rgb = float3(c, 0.0f, x);
    
    return saturate(rgb + m);
}

float get_luma(float3 c)
{
    return dot(saturate(c), float3(0.2126f, 0.7152f, 0.0722f));
}

float3 RGBToYUV(float3 c)
{
    c = saturate(c);
    float3x3 yuvMatrix = float3x3(
         0.299f, 0.587f, 0.114f,
        -0.147f, -0.289f, 0.436f,
         0.615f, -0.515f, -0.100f
    );
    return mul(yuvMatrix, c);
}

float3 sRGBToLinear(float3 c)
{
    c = saturate(c);
    return pow(c + EPSILON, 2.2f);
}

float3 RGBToXYZ(float3 c)
{
    c = sRGBToLinear(c);
    float3x3 xyzMatrix = float3x3(
        0.4124f, 0.3576f, 0.1805f,
        0.2126f, 0.7152f, 0.0722f,
        0.0193f, 0.1192f, 0.9505f
    );
    return mul(xyzMatrix, c);
}

float labf(float t)
{
    float delta = 6.0f / 29.0f;
    if (t > delta * delta * delta)
        return pow(t, 1.0f / 3.0f);
    else
        return t / (3.0f * delta * delta) + 4.0f / 29.0f;
}

float3 XYZToLab(float3 xyz)
{
    float3 white = float3(0.95047f, 1.00000f, 1.08883f);
    xyz = xyz / white;
    float fx = labf(xyz.x);
    float fy = labf(xyz.y);
    float fz = labf(xyz.z);
    
    float L = 116.0f * fy - 16.0f;
    float a = 500.0f * (fx - fy);
    float b = 200.0f * (fy - fz);
    
    return float3(L, a, b);
}

float3 LabToLCH(float3 lab)
{
    float L = lab.x;
    float a = lab.y;
    float b = lab.z;
    float C = safe_sqrt(a * a + b * b);
    float H = safe_atan2(b, a) * (180.0f / PI);
    H = fmod(H + 360.0f, 360.0f);
    return float3(L, C, H);
}

float hue_difference(float h1, float h2)
{
    float diff = abs(h1 - h2);
    return min(diff, 360.0f - diff);
}

float get_color_distance(float3 c1, float3 c2, int space)
{
    c1 = saturate(c1);
    c2 = saturate(c2);
    switch (space)
    {
        case 0:
            return distance(c1, c2);
        case 1:
        {
                float3 hsv1 = RGBToHSV(c1);
                float3 hsv2 = RGBToHSV(c2);
            
                float sat1 = hsv1.y;
                float sat2 = hsv2.y;
                float threshold = saturationThreshold;
                if (sat1 < threshold && sat2 < threshold)
                {
                    float luma_diff = abs(hsv1.z - hsv2.z);
                    return luma_diff / max(luminanceRange, EPSILON);
                }
            
                if (sat1 < threshold || sat2 < threshold)
                {
                    float luma_diff = abs(hsv1.z - hsv2.z);
                    float sat_diff = abs(sat1 - sat2);
                    return safe_sqrt(luma_diff * luma_diff + sat_diff * sat_diff * 0.5f);
                }
            
                float hue_diff = hue_difference(hsv1.x * 360.0f, hsv2.x * 360.0f) / 360.0f;
                float max_hue_range = hueRange;
            
                if (hue_diff > max_hue_range)
                    return 1.0f;
                float sat_diff = abs(hsv1.y - hsv2.y);
                float val_diff = abs(hsv1.z - hsv2.z);
                return safe_sqrt(hue_diff * hue_diff * 4.0f + sat_diff * sat_diff + val_diff * val_diff) / safe_sqrt(6.0f);
            }
        
        case 2:
        {
                float3 lab1 = XYZToLab(RGBToXYZ(c1));
                float3 lab2 = XYZToLab(RGBToXYZ(c2));
                return distance(lab1, lab2) / 100.0f;
            }
        
        case 3:
        {
                float3 yuv1 = RGBToYUV(c1);
                float3 yuv2 = RGBToYUV(c2);
            
                float y_diff = abs(yuv1.x - yuv2.x);
                float max_luma_range = luminanceRange;
                if (y_diff > max_luma_range)
                    return 1.0f;
                return distance(yuv1, yuv2);
            }
        
        case 4:
        {
                float3 xyz1 = RGBToXYZ(c1);
                float3 xyz2 = RGBToXYZ(c2);
                return distance(xyz1, xyz2);
            }
        
        case 5:
        {
                float3 lch1 = LabToLCH(XYZToLab(RGBToXYZ(c1)));
                float3 lch2 = LabToLCH(XYZToLab(RGBToXYZ(c2)));
            
                float deltaL = lch1.x - lch2.x;
                float deltaC = lch1.y - lch2.y;
                float deltaH = hue_difference(lch1.z, lch2.z);
            
                float max_hue_range = hueRange * 360.0f;
                if (deltaH > max_hue_range)
                    return 1.0f;
                return safe_sqrt(deltaL * deltaL + deltaC * deltaC + (deltaH * 0.5f) * (deltaH * 0.5f)) / 100.0f;
            }
    }
    
    return distance(c1, c2);
}

float4 safe_sample(Texture2D<float4> tex, SamplerState samp, float2 uv)
{
    uv = saturate(uv);
    return tex.Sample(samp, uv);
}

float3 calculate_key_color(float2 uv, float3 base_color, float3 end_color, float gradient_strength, float gradient_angle)
{
    if (gradient_strength < EPSILON)
        return base_color;
    
    float2 gradient_dir = float2(cos(gradient_angle), sin(gradient_angle));
    float gradient_pos = dot((uv - 0.5f), gradient_dir) + 0.5f;
    gradient_pos = saturate(gradient_pos);
    
    float interpolation_factor = saturate(gradient_pos * gradient_strength);
    return lerp(base_color, end_color, interpolation_factor);
}

float calculate_base_color_distance(float3 pixel_color, float3 key_color, float luminance_mix_value)
{
    float base_distance = get_color_distance(pixel_color, key_color, colorSpace);
    if (luminance_mix_value > EPSILON)
    {
        float pixel_luma = get_luma(pixel_color);
        float key_luma = get_luma(key_color);
        float luma_distance = abs(pixel_luma - key_luma);
        
        base_distance = lerp(base_distance, luma_distance, luminance_mix_value);
    }
    
    return base_distance;
}

float calculate_preset_color_distance(float3 pixel_color, int main_key_color, float luminance_mix_value)
{
    float key_primary, other_max;
    switch (main_key_color)
    {
        case 1:
            key_primary = pixel_color.g;
            other_max = max(pixel_color.r, pixel_color.b);
            break;
        case 2:
            key_primary = pixel_color.b;
            other_max = max(pixel_color.r, pixel_color.g);
            break;
        case 3:
            key_primary = pixel_color.r;
            other_max = max(pixel_color.g, pixel_color.b);
            break;
        default:
            return 1.0f;
    }
    
    float matte = saturate(key_primary - other_max);
    if (luminance_mix_value > EPSILON)
    {
        float luma = get_luma(pixel_color);
        float luma_factor = lerp(1.0f, 1.0f - luma, saturate(luminance_mix_value));
        matte *= luma_factor;
    }
    
    return 1.0f - matte;
}

float calculate_distance_at_uv(float2 sample_uv)
{
    float4 sample_color = safe_sample(InputTexture, InputSampler, sample_uv);
    if (sample_color.a < MIN_VALID_ALPHA)
        return 1.0f;

    float3 sample_pixel = sample_color.rgb / max(sample_color.a, MIN_VALID_ALPHA);
    sample_pixel = saturate(sample_pixel);
    
    if (mainKeyColor == 0)
    {
        float3 effective_key_color = calculate_key_color(sample_uv, baseColor.rgb, endColor.rgb, gradientStrength, gradientAngle);
        return calculate_base_color_distance(sample_pixel, effective_key_color, luminanceMix);
    }
    else
    {
        return calculate_preset_color_distance(sample_pixel, mainKeyColor, luminanceMix);
    }
}

float apply_edge_blur_filter(float2 center_uv, float blur_radius)
{
    if (blur_radius < EPSILON)
        return calculate_distance_at_uv(center_uv);
        
    float2 texel_size = 1.0f / max(screenSize, float2(1.0f, 1.0f));
    float total_distance = 0.0f;
    float total_weight = 0.0f;
    
    if (qualityPreset == 2)
    {
        const int kernel_size = 49;
        float2 offsets[kernel_size] =
        {
            float2(-3, -3), float2(-2, -3), float2(-1, -3), float2(0, -3), float2(1, -3), float2(2, -3), float2(3, -3),
            float2(-3, -2), float2(-2, -2), float2(-1, -2), float2(0, -2), float2(1, -2), float2(2, -2), float2(3, -2),
            float2(-3, -1), float2(-2, -1), float2(-1, -1), float2(0, -1), float2(1, -1), float2(2, -1), float2(3, -1),
            float2(-3, 0), float2(-2, 0), float2(-1, 0), float2(0, 0), float2(1, 0), float2(2, 0), float2(3, 0),
            float2(-3, 1), float2(-2, 1), float2(-1, 1), float2(0, 1), float2(1, 1), float2(2, 1), float2(3, 1),
            float2(-3, 2), float2(-2, 2), float2(-1, 2), float2(0, 2), float2(1, 2), float2(2, 2), float2(3, 2),
            float2(-3, 3), float2(-2, 3), float2(-1, 3), float2(0, 3), float2(1, 3), float2(2, 3), float2(3, 3)
        };
        [unroll(49)]
        for (int i = 0; i < kernel_size; i++)
        {
            float2 offset = texel_size * offsets[i] * blur_radius;
            float weight = exp(-length(offset) * 2.0f);
            total_distance += calculate_distance_at_uv(center_uv + offset) * weight;
            total_weight += weight;
        }
    }
    else if (qualityPreset == 1)
    {
        const int kernel_size = 25;
        float2 offsets[kernel_size] =
        {
            float2(-2, -2), float2(-1, -2), float2(0, -2), float2(1, -2), float2(2, -2),
            float2(-2, -1), float2(-1, -1), float2(0, -1), float2(1, -1), float2(2, -1),
            float2(-2, 0), float2(-1, 0), float2(0, 0), float2(1, 0), float2(2, 0),
            float2(-2, 1), float2(-1, 1), float2(0, 1), float2(1, 1), float2(2, 1),
            float2(-2, 2), float2(-1, 2), float2(0, 2), float2(1, 2), float2(2, 2)
        };
        [unroll(25)]
        for (int i = 0; i < kernel_size; i++)
        {
            float2 offset = texel_size * offsets[i] * blur_radius;
            float weight = exp(-length(offset) * 2.0f);
            total_distance += calculate_distance_at_uv(center_uv + offset) * weight;
            total_weight += weight;
        }
    }
    else
    {
        const int kernel_size = 9;
        float2 offsets[kernel_size] =
        {
            float2(-1, -1), float2(0, -1), float2(1, -1),
            float2(-1, 0), float2(0, 0), float2(1, 0),
            float2(-1, 1), float2(0, 1), float2(1, 1)
        };
        [unroll(9)]
        for (int i = 0; i < kernel_size; i++)
        {
            float2 offset = texel_size * offsets[i] * blur_radius;
            float weight = exp(-length(offset) * 2.0f);
            total_distance += calculate_distance_at_uv(center_uv + offset) * weight;
            total_weight += weight;
        }
    }
    
    return safe_divide(total_distance, total_weight);
}

float apply_morphology_operation(float2 center_uv, float operation_size, bool is_erosion)
{
    if (abs(operation_size) < EPSILON)
        return calculate_distance_at_uv(center_uv);
        
    float2 texel_size = 1.0f / max(screenSize, float2(1.0f, 1.0f));
    float radius = abs(operation_size);
    
    float extreme_value = is_erosion ? 1.0f : 0.0f;
    int sample_count = (qualityPreset == 0) ? 4 : 8;
    float2 morph_offsets[8] =
    {
        float2(0, -1), float2(1, 0), float2(0, 1), float2(-1, 0),
        float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, 1)
    };
    [unroll(8)]
    for (int i = 0; i < sample_count; i++)
    {
        float2 offset = texel_size * morph_offsets[i] * radius;
        float sample_distance = calculate_distance_at_uv(center_uv + offset);
        
        if (is_erosion)
            extreme_value = min(extreme_value, sample_distance);
        else
            extreme_value = max(extreme_value, sample_distance);
    }
    
    return extreme_value;
}

float apply_edge_detection_filter(float2 center_uv, float strength)
{
    if (strength < EPSILON)
        return 0.0f;
        
    float2 texel_size = 1.0f / max(screenSize, float2(1.0f, 1.0f));
    
    float distances[9];
    float2 sobel_offsets[9] =
    {
        float2(-1, -1), float2(0, -1), float2(1, -1),
        float2(-1, 0), float2(0, 0), float2(1, 0),
        float2(-1, 1), float2(0, 1), float2(1, 1)
    };
    [unroll(9)]
    for (int i = 0; i < 9; i++)
    {
        distances[i] = calculate_distance_at_uv(center_uv + texel_size * sobel_offsets[i]);
    }
    
    float sobel_x = (distances[2] + 2.0f * distances[5] + distances[8]) - (distances[0] + 2.0f * distances[3] + distances[6]);
    float sobel_y = (distances[6] + 2.0f * distances[7] + distances[8]) - (distances[0] + 2.0f * distances[1] + distances[2]);
    float edge_magnitude = safe_sqrt(sobel_x * sobel_x + sobel_y * sobel_y) * 0.125f;
    
    return edge_magnitude * strength;
}

float apply_despot_filter(float2 center_uv, float despot_size, float center_distance)
{
    if (despot_size < EPSILON)
        return center_distance;
        
    float2 texel_size = 1.0f / max(screenSize, float2(1.0f, 1.0f));
    
    float neighbor_sum = 0.0f;
    int neighbor_count = 0;
    int sample_count = (qualityPreset == 0) ? 4 : 8;
    float2 neighbor_offsets[8] =
    {
        float2(0, -1), float2(1, 0), float2(0, 1), float2(-1, 0),
        float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, 1)
    };
    [unroll(8)]
    for (int i = 0; i < sample_count; i++)
    {
        float2 offset = texel_size * neighbor_offsets[i] * despot_size;
        neighbor_sum += calculate_distance_at_uv(center_uv + offset);
        neighbor_count++;
    }
    
    if (neighbor_count == 0)
        return center_distance;
        
    float neighbor_avg = neighbor_sum / float(neighbor_count);
    float threshold = 0.5f;
    float blend_strength = 0.8f;
    
    if (center_distance < threshold && neighbor_avg > threshold)
        return lerp(center_distance, neighbor_avg, blend_strength);
    if (center_distance > threshold && neighbor_avg < threshold)
        return lerp(center_distance, neighbor_avg, blend_strength);
        
    return center_distance;
}

float apply_key_cleanup_filter(float2 center_uv, float cleanup_amount, float center_distance)
{
    if (abs(cleanup_amount) < EPSILON)
        return center_distance;
        
    float2 texel_size = 1.0f / max(screenSize, float2(1.0f, 1.0f));
    float cleanup_radius = abs(cleanup_amount) * 0.01f;
    int sample_count = (qualityPreset == 2) ? 8 : 4;
    float2 neighbor_offsets[8] =
    {
        float2(-cleanup_radius, 0.0f), float2(cleanup_radius, 0.0f),
        float2(0.0f, -cleanup_radius), float2(0.0f, cleanup_radius),
        float2(-cleanup_radius, -cleanup_radius), float2(cleanup_radius, -cleanup_radius),
        float2(-cleanup_radius, cleanup_radius), float2(cleanup_radius, cleanup_radius)
    };
    float min_distance = center_distance;
    float max_distance = center_distance;

    [unroll(8)]
    for (int i = 0; i < sample_count; i++)
    {
        float dist = calculate_distance_at_uv(center_uv + texel_size * neighbor_offsets[i]);
        min_distance = min(min_distance, dist);
        max_distance = max(max_distance, dist);
    }
    
    return (cleanup_amount < 0.0f) ? max_distance : min_distance;
}

float3 apply_spill_suppression_filter(float3 pixel_color, int main_key_color, float suppression_strength, float alpha_mask)
{
    if (suppression_strength < EPSILON || main_key_color == 0)
        return pixel_color;
        
    float3 result_color = pixel_color;
    float spill_amount = 0.0f;
    
    switch (main_key_color)
    {
        case 1:
        {
                float green_component = pixel_color.g;
                float other_max = max(pixel_color.r, pixel_color.b);
                spill_amount = saturate(green_component - other_max);
                if (spill_amount > EPSILON)
                {
                    float avg_other = (pixel_color.r + pixel_color.b) * 0.5f;
                    float target_green = lerp(green_component, avg_other, spill_amount * 0.8f);
                    result_color.g = lerp(green_component, target_green, suppression_strength);
                }
                break;
            }
        case 2:
        {
                float blue_component = pixel_color.b;
                float other_max = max(pixel_color.r, pixel_color.g);
                spill_amount = saturate(blue_component - other_max);
                if (spill_amount > EPSILON)
                {
                    float avg_other = (pixel_color.r + pixel_color.g) * 0.5f;
                    float target_blue = lerp(blue_component, avg_other, spill_amount * 0.8f);
                    result_color.b = lerp(blue_component, target_blue, suppression_strength);
                }
                break;
            }
        case 3:
        {
                float red_component = pixel_color.r;
                float other_max = max(pixel_color.g, pixel_color.b);
                spill_amount = saturate(red_component - other_max);
                if (spill_amount > EPSILON)
                {
                    float avg_other = (pixel_color.g + pixel_color.b) * 0.5f;
                    float target_red = lerp(red_component, avg_other, spill_amount * 0.8f);
                    result_color.r = lerp(red_component, target_red, suppression_strength);
                }
                break;
            }
    }
    
    float original_luma = get_luma(pixel_color);
    float new_luma = get_luma(result_color);
    if (new_luma > EPSILON && original_luma > EPSILON)
    {
        result_color *= safe_divide(original_luma, new_luma);
    }
    
    float edge_factor = saturate((1.0f - alpha_mask) * 2.0f);
    float spill_factor = suppression_strength * edge_factor * spill_amount;
    return lerp(pixel_color, result_color, saturate(spill_factor));
}

float3 apply_edge_desaturation_filter(float3 pixel_color, float desaturation_strength, float alpha_mask)
{
    if (desaturation_strength < EPSILON)
        return pixel_color;
        
    float edge_factor = saturate((1.0f - alpha_mask) * alpha_mask * 4.0f);
    if (edge_factor < EPSILON)
        return pixel_color;
        
    float luma = get_luma(pixel_color);
    float3 desaturated = float3(luma, luma, luma);
    
    float final_strength = desaturation_strength * edge_factor;
    return lerp(pixel_color, desaturated, final_strength);
}

float3 apply_residual_color_correction(float3 pixel_color, float correction_strength, float alpha_mask)
{
    if (correction_strength < EPSILON)
        return pixel_color;
        
    float correction_distance = get_color_distance(pixel_color, targetResidualColor, 0);
    
    if (correction_distance > correctionTolerance)
        return pixel_color;
        
    float correction_factor = saturate(1.0f - (correction_distance / max(correctionTolerance, EPSILON)));
    correction_factor *= correction_strength;
    
    float edge_influence = saturate((1.0f - alpha_mask) * 2.0f);
    correction_factor *= edge_influence;
    
    return lerp(pixel_color, correctedColor, correction_factor);
}

float apply_noise_reduction(float alpha_mask, float noise_threshold)
{
    if (noise_threshold < EPSILON)
        return alpha_mask;
        
    float threshold = noise_threshold * 0.5f;
    
    if (alpha_mask < threshold)
        return 0.0f;
    else if (alpha_mask > (1.0f - threshold))
        return 1.0f;
    else
        return smoothstep(threshold, 1.0f - threshold, alpha_mask);
}

float improve_transparency_quality(float alpha_mask, float4 source_color, float quality_factor)
{
    if (quality_factor < EPSILON)
        return alpha_mask;
        
    float original_alpha = source_color.a;
    if (original_alpha < MIN_VALID_ALPHA)
        return alpha_mask;
        
    float alpha_difference = abs(alpha_mask - original_alpha);
    float quality_adjustment = lerp(0.0f, alpha_difference * 0.5f, quality_factor);
    
    if (original_alpha < 0.5f)
    {
        alpha_mask = lerp(alpha_mask, original_alpha, quality_adjustment);
    }
    
    return saturate(alpha_mask);
}

float4 main(float4 pos : SV_POSITION, float4 posScene : SCENE_POSITION, float4 uv0 : TEXCOORD0) : SV_TARGET
{
    float4 source_color = safe_sample(InputTexture, InputSampler, uv0.xy);
    if (source_color.a < MIN_VALID_ALPHA)
    {
        if (debugMode > 0)
            return float4(0, 0, 0, 1);
        return source_color;
    }
    
    float3 pixel_color = source_color.rgb / max(source_color.a, MIN_VALID_ALPHA);
    pixel_color = saturate(pixel_color);
    
    if (mainKeyColor == 0 && baseColor.a < MIN_VALID_ALPHA)
    {
        if (debugMode > 0)
            return float4(0, 0, 0, 1);
        return source_color;
    }

    float color_distance = calculate_distance_at_uv(uv0.xy);
    if (debugMode == 2)
    {
        return float4(color_distance, color_distance, color_distance, 1.0f);
    }
    
    if (isCompleteKey == 0)
    {
        color_distance = saturate(color_distance + edgeBalance * 0.01f);
        
        if (edgeBlur > EPSILON)
        {
            color_distance = apply_edge_blur_filter(uv0.xy, edgeBlur * 0.1f);
        }
        
        if (despot > EPSILON)
        {
            color_distance = apply_despot_filter(uv0.xy, despot, color_distance);
        }
        
        if (abs(erode) > EPSILON)
        {
            bool is_erosion = erode > 0.0f;
            color_distance = apply_morphology_operation(uv0.xy, abs(erode) * 0.1f, is_erosion);
        }
        
        if (edgeDetection > EPSILON)
        {
            float edge_enhancement = apply_edge_detection_filter(uv0.xy, edgeDetection);
            color_distance = saturate(color_distance - edge_enhancement);
        }
        
        if (abs(keyCleanup) > EPSILON)
        {
            color_distance = apply_key_cleanup_filter(uv0.xy, keyCleanup, color_distance);
        }
    }
    
    float alpha_mask;
    if (isCompleteKey != 0)
    {
        alpha_mask = (color_distance < tolerance) ? 0.0f : 1.0f;
    }
    else
    {
        float effective_softness = max(edgeSoftness, EPSILON) + feathering * 0.1f;
        float tolerance_val = max(tolerance, EPSILON);
        alpha_mask = smoothstep(tolerance_val - effective_softness, tolerance_val + effective_softness, color_distance);
    }
    
    if (clipWhite < 0.999f || clipBlack > 0.001f)
    {
        float clip_black_val = saturate(clipBlack);
        float clip_white_val = saturate(clipWhite);
        alpha_mask = smoothstep(clip_black_val, max(clip_black_val + EPSILON, clip_white_val), alpha_mask);
    }
    
    if (denoise > EPSILON)
    {
        alpha_mask = apply_noise_reduction(alpha_mask, denoise);
    }
    
    if (transparencyQuality > EPSILON)
    {
        alpha_mask = improve_transparency_quality(alpha_mask, source_color, transparencyQuality);
    }
    
    if (isInverted != 0)
    {
        alpha_mask = 1.0f - alpha_mask;
    }
    
    if (exceptionColor1.a > MIN_VALID_ALPHA)
    {
        float3 effective_exception_color = calculate_key_color(uv0.xy, exceptionColor1.rgb, exceptionColor2.rgb, exceptionGradientStrength, exceptionGradientAngle);
        float exception_distance = get_color_distance(pixel_color, effective_exception_color, colorSpace);
        float protection_mask = 1.0f - smoothstep(0.0f, max(exceptionTolerance, EPSILON), exception_distance);
        alpha_mask = max(alpha_mask, protection_mask);
    }
    
    if (debugMode == 1)
    {
        return float4(alpha_mask, alpha_mask, alpha_mask, 1.0f);
    }
    
    float3 final_color = pixel_color;
    if (isCompleteKey == 0)
    {
        if (spillSuppression > EPSILON)
        {
            final_color = apply_spill_suppression_filter(final_color, mainKeyColor, spillSuppression, alpha_mask);
        }
        
        if (debugMode == 3)
        {
            return float4(final_color, 1.0f);
        }

        if (edgeDesaturation > EPSILON)
        {
            final_color = apply_edge_desaturation_filter(final_color, edgeDesaturation, alpha_mask);
        }
        
        if (residualColorCorrection > EPSILON)
        {
            final_color = apply_residual_color_correction(final_color, residualColorCorrection, alpha_mask);
        }
        
        if (debugMode == 4)
        {
            return float4(final_color, 1.0f);
        }
    }
    
    float3 output_color = final_color;
    float output_alpha = source_color.a * alpha_mask;
    
    if (alphaBlendAdjustment > EPSILON)
    {
        float blend_factor = alphaBlendAdjustment;
        float enhanced_alpha = saturate(alpha_mask + blend_factor * (1.0f - alpha_mask) * 0.5f);
        alpha_mask = lerp(alpha_mask, enhanced_alpha, blend_factor);
        output_alpha = source_color.a * alpha_mask;
    }
    
    if (replaceIntensity > EPSILON)
    {
        float3 replace_color_rgb = saturate(replaceColor.rgb);
        if (preserveLuminance > EPSILON && isCompleteKey == 0)
        {
            float original_luma = get_luma(pixel_color);
            float replace_luma = get_luma(replace_color_rgb);
            
            if (replace_luma > EPSILON)
            {
                float luma_ratio = safe_divide(original_luma, replace_luma);
                float preserve_factor = saturate(preserveLuminance);
                replace_color_rgb = lerp(replace_color_rgb, replace_color_rgb * luma_ratio, preserve_factor);
            }
        }
        
        float replace_factor = saturate(replaceIntensity);
        float inverse_alpha = 1.0f - alpha_mask;
        
        float3 blended_color = lerp(replace_color_rgb, final_color, alpha_mask);
        output_color = lerp(final_color, blended_color, replace_factor);
        float replace_alpha = saturate(replaceColor.a);
        float blended_alpha = lerp(replace_alpha, source_color.a, alpha_mask);
        output_alpha = lerp(source_color.a * alpha_mask, blended_alpha, replace_factor);
    }
    
    output_color = saturate(output_color);
    output_alpha = saturate(output_alpha);
    
    return float4(output_color * output_alpha, output_alpha);
}