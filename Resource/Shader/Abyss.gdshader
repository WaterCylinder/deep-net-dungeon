shader_type canvas_item;

// 紫色调色板（深紫到亮紫）
uniform vec4 color_deep : source_color = vec4(0.18, 0.02, 0.35, 1.0);
uniform vec4 color_mid : source_color = vec4(0.45, 0.08, 0.65, 1.0);
uniform vec4 color_bright : source_color = vec4(0.85, 0.35, 0.95, 1.0);

// 像素化方格数量
uniform float blocks_x : hint_range(2.0, 200.0) = 64.0;
uniform float blocks_y : hint_range(2.0, 200.0) = 64.0;

// 螺旋参数
uniform float spiral_twist : hint_range(0.0, 20.0) = 6.0;
uniform float spiral_speed = 0.8;

// 深渊（中心暗化）参数
uniform float abyss_radius : hint_range(0.0, 1.0) = 0.45;
uniform float abyss_darkness : hint_range(0.0, 1.0) = 0.95;
uniform vec4 abyss_color : source_color = vec4(0.02, 0.0, 0.05, 1.0);

// 色带参数：把连续渐变量化成 N 条色带
uniform float color_bands : hint_range(2.0, 16.0) = 6.0;

void fragment() {
    // ===== 1. 像素化：每个方格取一个统一采样点 =====
    vec2 cell_index = floor(UV * vec2(blocks_x, blocks_y));
    vec2 cell_uv = (cell_index + 0.5) / vec2(blocks_x, blocks_y);

    // ===== 2. 极坐标（无波动，直接用 cell_uv） =====
    vec2 centered = cell_uv - vec2(0.5);
    float radius = length(centered);
    float angle = atan(centered.y, centered.x);

    // ===== 3. 螺旋扭曲 =====
    float spiral_angle = angle + radius * spiral_twist + TIME * spiral_speed;
    vec2 spiral_uv = vec2(cos(spiral_angle), sin(spiral_angle)) * radius + vec2(0.5);

    // ===== 4. 混合因子 =====
    float mix_factor = (spiral_uv.x + spiral_uv.y * 0.7) * 0.5 + 0.25;
    mix_factor += (cell_uv.x - 0.5) * 0.2 + (cell_uv.y - 0.5) * 0.1;
    mix_factor = clamp(mix_factor, 0.0, 1.0);

    // ===== 5. 色带量化：把连续 mix_factor 切成 N 条带 =====
    // 每条带内部取同一个代表值，相邻格子落入同一条带时颜色就完全相同
    float band = floor(mix_factor * color_bands);
    float band_t = (band + 0.5) / color_bands; // 取带中心，避免端点溢出

    // ===== 6. 紫色渐变（用带中心值采样） =====
    vec3 final_color;
    if (band_t < 0.5) {
        float t = band_t * 2.0;
        final_color = mix(color_deep.rgb, color_mid.rgb, t);
    } else {
        float t = (band_t - 0.5) * 2.0;
        final_color = mix(color_mid.rgb, color_bright.rgb, t);
    }

    // ===== 7. 深渊：越靠近中心越黑 =====
    float abyss = 1.0 - smoothstep(0.0, abyss_radius, radius);
    abyss = pow(abyss, 1.5) * abyss_darkness;
    final_color = mix(final_color, abyss_color.rgb, abyss);

    // 每个方格输出单一颜色
    COLOR = vec4(final_color, 1.0);
}