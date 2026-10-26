shader_type canvas_item;

// ========== 基础控制 ==========
uniform float spin_speed : hint_range(-5.0, 5.0) = 1.5;
uniform int arm_count : hint_range(1, 12) = 3;
uniform float pixel_size : hint_range(1.0, 64.0) = 8.0;

// ========== 能量开关 ==========
uniform float energy : hint_range(0.0, 1.0) = 1.0;
uniform float open_progress : hint_range(0.0, 1.0) = 1.0;

// ========== 颜色 ==========
uniform vec4 core_color : source_color = vec4(0.15, 0.55, 1.0, 1.0);
uniform vec4 mid_color  : source_color = vec4(0.35, 0.85, 1.0, 1.0);
uniform vec4 rim_color  : source_color = vec4(0.85, 0.95, 1.0, 1.0);
uniform vec4 void_color : source_color = vec4(0.02, 0.02, 0.08, 1.0);

// ========== 边缘与光晕 ==========
uniform float edge_transparency : hint_range(0.0, 1.0) = 0.85;
uniform float rim_width : hint_range(0.0, 0.5) = 0.15;
uniform float pulse_speed : hint_range(0.0, 5.0) = 2.0;
uniform float pulse_strength : hint_range(0.0, 1.0) = 0.25;

// ========== 扭曲 ==========
uniform float twist_strength : hint_range(0.0, 12.0) = 5.0;

// ========== 色块化（降低渐变感）==========
uniform float color_levels  : hint_range(2.0, 32.0) = 6.0;  // 最终颜色档数
uniform float stripe_levels : hint_range(2.0, 32.0) = 4.0;  // 条纹档数
uniform float radial_levels : hint_range(2.0, 32.0) = 8.0;  // 径向分层（0=关闭，见下方说明）

void fragment() {
    vec4 self_mod = COLOR;

    // ----- 像素化 UV -----
    vec2 pixel_uv = (pixel_size > 1.0) ? floor(UV * pixel_size) / pixel_size : UV;
    vec2 centered = (pixel_uv - 0.5) * 2.0;
    float dist = length(centered);
    float angle = atan(centered.y, centered.x);

    // ----- 开门进度：可见遮罩 -----
    float visible_radius = mix(0.0, 1.0, open_progress);
    float visible_mask = step(dist, visible_radius);

    // ----- 径向分层（可选，让同心环更硬）-----
    // 想关闭径向分层，把 radial_levels 设很大（如 32）即可近乎无效
    float dist_q = floor(dist * radial_levels + 0.5) / radial_levels;
    // 用 dist_q 参与径向项，angle 仍用真实 dist 保证漩涡不歪
    // （若你希望完全关掉径向量化，把下一行改成 float dist_use = dist;）
    float dist_use = dist_q;

    // ----- 主漩涡扭曲 -----
    float spiral = angle + dist * twist_strength - TIME * spin_speed;
    float stripe = sin(spiral * float(arm_count)) * 0.5 + 0.5;
    // ★ 条纹量化：色带变硬
    stripe = floor(stripe * stripe_levels + 0.5) / stripe_levels;

    // ----- 反向内层漩涡 -----
    float inner_spiral = angle - dist * (twist_strength * 0.7) + TIME * spin_speed * 0.6;
    float inner_stripe = sin(inner_spiral * float(arm_count + 1)) * 0.5 + 0.5;
    inner_stripe = floor(inner_stripe * stripe_levels + 0.5) / stripe_levels;

    // ----- 径向能量流 -----
    float flow = fract(dist_use * 3.0 + TIME * 1.5 + stripe * 0.4);

    // ----- 分层颜色混合 -----
    vec3 col = mix(core_color.rgb, mid_color.rgb, stripe);
    col = mix(col, inner_stripe * mid_color.rgb + (1.0 - inner_stripe) * core_color.rgb * 0.6, 0.35);
    col = mix(col, rim_color.rgb, smoothstep(0.75, 1.0, flow) * 0.4);

    // ----- 中心黑洞 -----
    float void_mask = smoothstep(0.18, 0.0, dist);
    col = mix(col, void_color.rgb, void_mask * 0.85);

    // ----- 外缘亮带 -----
    float rim = smoothstep(1.0 - rim_width, 1.0, dist)
              * (1.0 - smoothstep(1.0, 1.0 + rim_width * 0.1, dist));
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);
    float rim_glow = rim * (1.0 + pulse * pulse_strength);
    col = mix(col, rim_color.rgb, clamp(rim_glow, 0.0, 1.0));

    // ----- 边缘淡出 -----
    float edge_fade = smoothstep(1.0, 1.0 - rim_width * 2.0, dist);
    float alpha = mix(1.0, edge_fade, edge_transparency);
    alpha = max(alpha, void_mask * 0.9);

    // ----- 应用开门遮罩 -----
    alpha *= visible_mask;

    // ★ 最终颜色量化：相近颜色归为同一档
    col = floor(col * color_levels + 0.5) / color_levels;

    // ----- 能量亮度 -----
    col *= mix(0.6, 1.0, energy);

    COLOR = vec4(col, alpha) * self_mod;
}