shader_type canvas_item;

// 发光强度
uniform float glow_strength : hint_range(0.0, 5.0) = 1.5;
// 亮度阈值：超过此亮度的像素才会发光
uniform float threshold : hint_range(0.0, 1.0) = 0.6;
// 辉光扩散半径（像素）
uniform float glow_radius : hint_range(0.0, 32.0) = 8.0;
// 发光颜色染色
uniform vec4 glow_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);
// 采样次数（越大越平滑，性能越低）
uniform int samples : hint_range(4, 32) = 12;
// 是否让原图变暗以突出辉光
uniform float base_darken : hint_range(0.0, 1.0) = 0.0;

// 计算像素亮度
float luminance(vec3 c) {
    return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

// 判断像素是否发光，并返回发光颜色
// 修改函数签名，接收纹理参数
vec4 get_glow(sampler2D tex, vec2 uv) {
    vec4 col = texture(tex, uv);
    if (col.a <= 0.0) return vec4(0.0);
    float lum = luminance(col.rgb);
    float glow_amount = smoothstep(threshold, 1.0, lum);
    return vec4(col.rgb * glow_amount * col.a, glow_amount * col.a);
}

void fragment() {
    vec2 uv = UV;
    vec4 base = texture(TEXTURE, uv);

    // 累加周围发光像素
    vec3 glow = vec3(0.0);
    float total_weight = 0.0;
    float radius = glow_radius * TEXTURE_PIXEL_SIZE.x;

    // 环形多方向采样，模拟高斯模糊
    for (int i = 0; i < samples; i++) {
        float angle = (float(i) / float(samples)) * 6.2831853;
        // 两层半径，让辉光更自然
        for (int r = 1; r <= 2; r++) {
            float dist = radius * (float(r) / 2.0);
            vec2 offset = vec2(cos(angle), sin(angle)) * dist;
            vec4 s = get_glow(TEXTURE, uv + offset);
            // 距离越远权重越低
            float w = 1.0 - (float(r) / 3.0);
            glow += s.rgb * w;
            total_weight += w;
        }
    }
    if (total_weight > 0.0) {
        glow /= total_weight;
    }

    // 合成：原图 + 辉光
    vec3 final_rgb = base.rgb * (1.0 - base_darken) + glow * glow_strength * glow_tint.rgb;
    // 保留原图 alpha，发光也参考 alpha
    float final_alpha = max(base.a, luminance(glow) * glow_strength * glow_tint.a);
    final_alpha = clamp(final_alpha, 0.0, 1.0);

    COLOR = vec4(final_rgb, base.a * final_alpha > 0.0 ? max(base.a, final_alpha) : 0.0);
}