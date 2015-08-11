#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 vertUV;
layout(location = 2) in float vAlpha;

uniform mat4 matrix;

out vec2 uv;
out float alpha;

void main() {
  gl_Position = matrix * vec4(position, 1);
  uv = vertUV;
  alpha = vAlpha;
}
