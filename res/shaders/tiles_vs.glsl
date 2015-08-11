#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 vertUV;
layout(location = 2) in float vPaint;
layout(location = 3) in float vHilite;

uniform mat4 matrix;

out vec2 uv;
out float hilite;
out float paint;

void main() {
  gl_Position = matrix * vec4(position, 1);
  uv = vertUV;
  hilite = vHilite;
  paint = floor(vPaint);
}
