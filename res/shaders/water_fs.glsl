#version 330 core

in vec2 uv;
in float alpha;

uniform sampler2D texture;
uniform bool hiliting;

out vec4 color;

void main() {
  vec4 c = texture2D(texture, uv);
  c.a = alpha;
  if (c.a < 0.1)
    discard;
  if (hiliting)
    c.rgb *= vec3(0.3, 0.3, 0.3);
  color = c;
}
