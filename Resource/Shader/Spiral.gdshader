shader_type canvas_item;

uniform float spin_speed : hint_range(-5.0, 5.0) = 1.0;
uniform float edge_transparency : hint_range(0.0, 1.0) = 0.8;
uniform vec4 center_color : source_color = vec4(1.0, 0.9, 0.6, 1.0);
uniform int arm_count : hint_range(1, 12) = 2;
uniform float pixel_size : hint_range(1.0, 64.0) = 8.0;

void fragment() {
    // 保存原始 COLOR（顶点色 × modulate × self_modulate）
    vec4 self_mod = COLOR;

    // 像素化 UV
    vec2 pixel_uv = floor(UV * pixel_size) / pixel_size;
    vec2 centered = (pixel_uv - 0.5) * 2.0;

    float dist = length(centered);
    float angle = atan(centered.y, centered.x);

    // 漩涡扭曲
    float spiral = angle + dist * 4.0 - TIME * spin_speed;
    float stripe = sin(spiral * float(arm_count)) * 0.5 + 0.5;

    float radial = 1.0 - clamp(dist, 0.0, 1.0);

    vec3 col = mix(center_color.rgb * 0.35, center_color.rgb, radial * stripe);

    // 边缘透明
    float edge_fade = smoothstep(1.0, 0.0, dist);
    float alpha = mix(1.0, edge_fade, edge_transparency);
    alpha *= step(dist, 1.0);

    // 颜色与透明度都乘以 self_modulate（COLOR 已包含 self_modulate）
    COLOR = vec4(col, alpha) * self_mod;
}