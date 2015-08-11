#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 vUV;

uniform mat4 matrix;

out vec2 uv;

void main() {
  gl_Position = matrix * vec4(position, 1);
  uv = vUV;
}
