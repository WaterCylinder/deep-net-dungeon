shader_type canvas_item;

uniform float flash_modifier : hint_range(0.0, 1.0) = 0.0;
uniform vec4 flash_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec3 col = mix(tex.rgb, flash_color.rgb, flash_modifier * tex.a);
    COLOR = vec4(col, tex.a);
}