shader_type canvas_item;

uniform vec4 flash_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float flash_hold : hint_range(0.0, 10.0) = 0.3;   // 闪烁色持续时间
uniform float base_hold  : hint_range(0.0, 10.0) = 0.5;   // 原色持续时间
uniform float fade_time  : hint_range(0.0, 10.0) = 0.15;  // 过渡时间

varying vec4 base_color;

void vertex() {
    // COLOR 在 vertex 阶段已包含 SelfModulate（以及父级 Modulate）
    base_color = COLOR;
}

void fragment() {
    vec4 tex = texture(TEXTURE, UV);

    float cycle = base_hold + fade_time + flash_hold + fade_time;
    float t = mod(TIME, cycle);

    float blend; // 0 = 原色，1 = 闪烁色
    if (t < base_hold) {
        blend = 0.0;
    } else if (t < base_hold + fade_time) {
        blend = (t - base_hold) / max(fade_time, 0.0001);
    } else if (t < base_hold + fade_time + flash_hold) {
        blend = 1.0;
    } else {
        blend = 1.0 - (t - base_hold - fade_time - flash_hold) / max(fade_time, 0.0001);
    }

    // 原色纹理（继承 SelfModulate）
    vec3 base_rgb = tex.rgb * base_color.rgb;
    // 闪烁色也乘上 base_color 的 alpha，保持与节点调制一致
    vec3 flash_rgb = flash_color.rgb;

    vec3 final_rgb = mix(base_rgb, flash_rgb, blend);

    COLOR = vec4(final_rgb, tex.a * base_color.a);
}