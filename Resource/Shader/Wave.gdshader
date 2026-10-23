shader_type canvas_item;

// ===== 颜色与透明度 =====
uniform vec4 base_color : source_color = vec4(0.08, 0.10, 0.20, 1.0);   // 底色
uniform vec4 wave_color_a : source_color = vec4(0.20, 0.55, 0.85, 1.0); // 波浪色 A
uniform vec4 wave_color_b : source_color = vec4(0.55, 0.20, 0.75, 1.0); // 波浪色 B
uniform float global_alpha : hint_range(0.0, 1.0) = 1.0;                 // 整体透明度

// ===== 像素化 =====
uniform float pixel_size : hint_range(1.0, 2048.0) = 8.0;  // 像素块大小（越大越像素化）

// ===== 波浪参数 =====
uniform float wave_scale : hint_range(0.5, 20.0) = 6.0;   // 波浪频率
uniform float wave_speed : hint_range(0.0, 5.0) = 1.2;    // 翻涌速度
uniform float wave_strength : hint_range(0.0, 2.0) = 0.6; // 波浪扰动强度
uniform float chaos : hint_range(0.0, 2.0) = 0.7;         // 混乱程度

// ===== 圆形镂空 =====
uniform vec2 hole_center = vec2(0.5, 0.5);                // 洞中心（UV 坐标 0~1）
uniform float hole_radius : hint_range(0.0, 1.0) = 0.2;   // 洞半径（UV 单位）
uniform float hole_softness : hint_range(0.0, 0.5) = 0.02;// 洞边缘柔和度

// 简单的伪随机噪声
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

// 值噪声（平滑插值）
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

void fragment() {
    // 1. 像素化：把 UV 量化到像素网格
    vec2 pixel_uv = floor(UV * pixel_size) / pixel_size;

    // 2. 多层噪声叠加，制造混乱翻涌感
    float t = TIME * wave_speed;
    vec2 p = pixel_uv * wave_scale;

    float n1 = noise(p + vec2(t, t * 0.7));
    float n2 = noise(p * 2.0 - vec2(t * 0.5, t * 1.3));
    float n3 = noise(p * 4.0 + vec2(t * 1.7, -t * 0.9));

    float wave = (n1 + n2 * chaos + n3 * chaos * 0.5) / (1.0 + chaos * 1.5);
    wave = wave * wave_strength;

    // 3. 用波浪值在两种波浪色之间插值，再与底色混合
    vec3 wave_col = mix(wave_color_a.rgb, wave_color_b.rgb, clamp(wave, 0.0, 1.0));
    vec3 final_rgb = mix(base_color.rgb, wave_col, clamp(wave * 1.5, 0.0, 1.0));

    // 4. 基础透明度
    float alpha = base_color.a * global_alpha;

    // 5. 圆形镂空：根据到洞中心的距离计算遮罩
    float dist = distance(pixel_uv, hole_center);
    float hole_mask = smoothstep(hole_radius, hole_radius + hole_softness, dist);
    // hole_mask = 0 表示在洞内（透明），1 表示在洞外（保留）
    alpha *= hole_mask;

    COLOR = vec4(final_rgb, alpha);
}