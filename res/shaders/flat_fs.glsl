#version 330 core

in vec2 uv;

uniform sampler2D texture;
uniform bool hiliting;

out vec4 color;

void main() {
  vec4 c = texture2D(texture, uv);
  if (hiliting && c.rgb != vec3(1.0, 0.8, 1.0))
    c.rgb *= vec3(0.3, 0.3, 0.3);
  color = c;
}
