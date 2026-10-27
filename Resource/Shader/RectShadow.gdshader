shader_type canvas_item;

// ===== 像素 =====
uniform float pixel_size : hint_range(1.0, 512.0) = 64.0;

// ===== 矩形 =====
uniform vec2 rect_size = vec2(0.6, 0.4);
// 圆角程度：0 = 纯直角，1 = 标准圆角
uniform float roundness : hint_range(0.0, 1.0) = 0.0;

// ===== 颜色 =====
uniform vec4 inner_color : source_color = vec4(1.0, 1.0, 0.9, 1.0);
uniform vec4 radiate_color : source_color = vec4(1.0, 0.5, 0.1, 1.0);
uniform vec4 outer_color : source_color = vec4(0.2, 0.0, 0.4, 1.0);

// ===== 辐射 =====
uniform float radiate_range : hint_range(0.0, 1.0) = 0.25;
uniform float outer_alpha : hint_range(0.0, 1.0) = 0.0;
uniform float intensity : hint_range(0.0, 4.0) = 1.0;

// ===== 动态扩散 =====
uniform float wave_speed : hint_range(0.0, 2.0) = 0.35;
uniform float wave_count : hint_range(1.0, 8.0) = 2.0;
uniform float wave_width : hint_range(0.01, 0.5) = 0.08;
uniform float wave_strength : hint_range(0.0, 4.0) = 1.0;
uniform float rect_pulse : hint_range(0.0, 0.3) = 0.0;
uniform vec4 wave_color : source_color = vec4(1.0, 0.9, 0.4, 1.0);

vec2 quantize_uv(vec2 uv, float size) {
    return floor(uv * size) / size + 0.5 / size;
}

// 圆角矩形有符号距离，roundness 在直角(0)与圆角(1)间插值
// <0 内部，>0 外部
float rect_sdf(vec2 p, vec2 half_size, float r) {
    vec2 d = abs(p) - half_size;

    // 纯直角：Chebyshev 距离
    float sharp = max(d.x, d.y);

    // 标准圆角矩形：Euclidean 距离
    float smooth_d = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);

    // 插值
    return mix(sharp, smooth_d, r);
}

void fragment() {
    vec4 self_mod = COLOR;

    vec2 puv = quantize_uv(UV, pixel_size);
    vec2 p = puv - vec2(0.5);

    float phase = TIME * wave_speed;

    float pulse = 1.0 + rect_pulse * sin(phase * 6.28318);
    vec2 half_size = rect_size * 0.5 * pulse;

    float d = rect_sdf(p, half_size, roundness);

    // 硬边内部判定（直角/圆角均硬边，像素风）
    float inside = step(d, 0.0);

    // 外部辐射因子
    float t = clamp(d / max(radiate_range, 1e-4), 0.0, 1.0);

    vec4 radiate_to_outer = mix(radiate_color, outer_color, t);
    radiate_to_outer.a = mix(radiate_color.a, outer_alpha, t);

    // ===== 动态扩散波 =====
    float wave_acc = 0.0;
    for (float i = 0.0; i < 8.0; i++) {
        if (i >= wave_count) break;
        float offset = i / wave_count;
        float wave_pos = fract(phase + offset) * radiate_range;
        float dist_to_wave = abs(d - wave_pos);
        float band = 1.0 - smoothstep(0.0, wave_width, dist_to_wave);
        float falloff = 1.0 - t;
        wave_acc += band * falloff;
    }
    wave_acc = clamp(wave_acc, 0.0, 1.0) * wave_strength;

    vec4 wave_layer = wave_color;
    wave_layer.a *= wave_acc;

    vec4 col = mix(radiate_to_outer, wave_layer, wave_acc);
    col = mix(col, inner_color, inside);
    col.a = mix(col.a, inner_color.a, inside);

    COLOR = col * self_mod * intensity;
}