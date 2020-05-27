/**
 * @Copyright 2015 seancode
 *
 * Handles building of buffers and drawing them
 */

#include <QDebug>
#include "./render.h"

Render::Render() : ibo(QOpenGLBuffer::IndexBuffer) {
}

void Render::setTexturePath(const QString &path) {
  textures.setRoot(path);
}

bool Render::texturesValid() const {
  return textures.valid;
}

QSharedPointer<QOpenGLTexture> Render::get(int type) {
  return textures.get(type, 0, 0);
}

void Render::init(QOpenGLFunctions *gl) {
  vao.create();
  vao.bind();
  vbo.create();
  vbo.bind();
  ibo.create();
  ibo.bind();
  this->gl = gl;
}

void Render::destroy() {
  vbo.destroy();
  ibo.destroy();
  vao.destroy();
  textures.destroy();
}

void Render::add(int type, int px, int py, int texw, int texh,
                 qint16 texu, qint16 texv, GLfloat z, quint8 paint,
                 bool hilite, bool fliph, bool flipv) {
  auto tex = textures.get(type);

  if (texw == 0)
    texw = tex->width();
  if (texh == 0)
    texh = tex->height();

  int start = buffers[type].count() / 7;

  bool h, v;

  for (int i = 0; i < 4; i++) {
    buffers[type].append(((i & 1) ? px + texw : px) / 16.0f);
    buffers[type].append(((i & 2) ? py + texh : py) / 16.0f);
    buffers[type].append(z);
    h = i & 1;
    if (fliph) h = !h;
    buffers[type].append((h ? texu + texw - 0.5f : texu + 0.5f) /
                         tex->width());
    v = i & 2;
    if (flipv) v = !v;
    buffers[type].append((v ? texv + texh - 0.5f : texv + 0.5f) /
                         tex->height());
    buffers[type].append(paint);
    buffers[type].append(hilite ? 1 : 0);
  }
  indices[type].append(start);
  indices[type].append(start + 2);
  indices[type].append(start + 3);
  indices[type].append(start);
  indices[type].append(start + 3);
  indices[type].append(start + 1);
}

void Render::addSlope(int type, int slope, int x, int y, int texw, int texh,
                      qint16 u, qint16 v, GLfloat z, quint8 paint,
                      bool hilite) {
  auto tex = textures.get(type);

  int start = buffers[type].count() / 7;

  GLfloat px[3], py[3], texu[3], texv[3];

  switch (slope) {
  case 1:
    px[0] = px[1] = x / 16.0f;
    py[0] = y / 16.0f;
    texu[0] = texu[1] = (u + 0.5f) / tex->width();
    texv[0] = texv[2] = (v + 0.5f) / tex->height();
    py[1] = py[2] = (y + texh) / 16.0f;
    texv[1] = (v + texh - 0.5f) / tex->height();
    px[2] = (x + texw) / 16.0f;
    texu[2] = (u + texw - 0.5f) / tex->width();
    break;
  case 2:
    px[0] = x / 16.0f;
    py[0] = py[1] = (y + texh) / 16.0f;
    texu[0] = (u + 0.5f) / tex->width();
    texv[0] = texv[2] = (v + 0.5f) / tex->height();
    px[1] = px[2] = (x + texw) / 16.0f;
    texu[1] = texu[2] = (u + texw - 0.5f) / tex->width();
    texv[1] = (v + texh - 0.5f) / tex->height();
    py[2] = y / 16.0f;
    break;
  case 3:
    px[0] = px[1] = x / 16.0f;
    py[0] = py[2] = y / 16.0f;
    texu[0] = texu[1] = (u + 0.5f) / tex->width();
    texv[0] = (v + 0.5f) / tex->height();
    py[1] = (y + texh) / 16.0f;
    texv[1] = texv[2] = (v + texh - 0.5f) / tex->height();
    px[2] = (x + texw) / 16.0f;
    texu[2] = (u + texw - 0.5f) / tex->width();
    break;
  case 4:
    px[0] = x / 16.0f;
    py[0] = py[2] = y / 16.0f;
    texu[0] = (u + 0.5f) / tex->width();
    texv[0] = texv[1] = (v + texh - 0.5f) / tex->height();
    px[1] = px[2] = (x + texw) / 16.0f;
    py[1] = (y + texh) / 16.0f;
    texu[1] = texu[2] = (u + texw - 0.5f) / tex->width();
    texv[2] = (v + 0.5f) / tex->height();
    break;
  }

  for (int i = 0; i < 3; i++) {
    buffers[type].append(px[i]);
    buffers[type].append(py[i]);
    buffers[type].append(z);
    buffers[type].append(texu[i]);
    buffers[type].append(texv[i]);
    buffers[type].append(paint);
    buffers[type].append(hilite ? 1 : 0);
    indices[type].append(start + i);
  }
}

void Render::addLiquid(int type, int px, int py, int texw, int texh,
                       qint16 texv, GLfloat z, GLfloat alpha) {
  auto tex = textures.get(type);

  int start = buffers[type].count() / 6;

  for (int i = 0; i < 4; i++) {
    buffers[type].append(((i & 1) ? px + texw : px) / 16.0f);
    buffers[type].append(((i & 2) ? py + texh : py) / 16.0f);
    buffers[type].append(z);
    buffers[type].append(((i & 1) ? texw - 0.5f : 0.5f) / tex->width());
    buffers[type].append(((i & 2) ? 15.5f : texv + 0.5f) / tex->height());
    buffers[type].append(alpha);
  }
  indices[type].append(start);
  indices[type].append(start + 2);
  indices[type].append(start + 3);
  indices[type].append(start);
  indices[type].append(start + 3);
  indices[type].append(start + 1);
}

void Render::addFog(int px, int py, GLfloat z) {
  int start = buffer.count() / 3;

  for (int i = 0; i < 4; i++) {
    buffer.append(((i & 1) ? px + 16 : px) / 16.0f);
    buffer.append(((i & 2) ? py + 16 : py) / 16.0f);
    buffer.append(z);
  }
  index.append(start);
  index.append(start + 2);
  index.append(start + 3);
  index.append(start);
  index.append(start + 3);
  index.append(start + 1);
}


void Render::drawBG(int type, int cropw, int croph, GLfloat sx, GLfloat sy,
                    int w, int h) {
  auto tex = textures.get(type, cropw, croph);

  for (int i = 0; i < 4; i++) {
    buffer.append((i & 1) ? sx + w : sx);
    buffer.append((i & 2) ? sy + h : sy);
    buffer.append(h == 1 ? 0.5f : 0.0f);  // shims on top
    buffer.append(((i & 1) ? w * 16 - 0.5f : 0.5f) / tex->width());
    buffer.append(((i & 2) ? h * 16 - 0.5f : 0.5f) / tex->height());
    buffer.append(0);
    buffer.append(0);
  }
  index.append(0);
  index.append(2);
  index.append(3);
  index.append(0);
  index.append(3);
  index.append(1);

  tex->bind();
  vbo.bind();
  vbo.allocate(buffer.constData(), buffer.count() * sizeof(GLfloat));
  ibo.bind();
  ibo.allocate(index.constData(), index.count() * sizeof(unsigned int));
  gl->glDrawElements(GL_TRIANGLES, index.count(), GL_UNSIGNED_INT, nullptr);
  buffer.clear();
  index.clear();
}

void Render::drawFlat(int x, int y, int x2, int y2) {
  for (int i = 0; i < 4; i++) {
    buffer.append((i & 1) ? x2 : x);
    buffer.append((i & 2) ? y2 : y);
    buffer.append(0);
    buffer.append((i & 1) ? 1.0 : 0);
    buffer.append((i & 2) ? 1.0 : 0);
  }
  index.append(0);
  index.append(2);
  index.append(3);
  index.append(0);
  index.append(3);
  index.append(1);

  vbo.bind();
  vbo.allocate(buffer.constData(), buffer.count() * sizeof(GLfloat));
  ibo.bind();
  ibo.allocate(index.constData(), index.count() * sizeof(unsigned int));
  gl->glDrawElements(GL_TRIANGLES, index.count(), GL_UNSIGNED_INT, nullptr);
  buffer.clear();
  index.clear();
}



void Render::apply() {
  QHashIterator<int, QVector<GLfloat>> i(buffers);
  while (i.hasNext()) {
    i.next();
    textures.get(i.key())->bind();
    vbo.bind();
    vbo.allocate(i.value().constData(), i.value().count() * sizeof(GLfloat));
    auto j = indices[i.key()];
    ibo.bind();
    ibo.allocate(j.constData(), j.count() * sizeof(unsigned int));
    gl->glDrawElements(GL_TRIANGLES, j.count(), GL_UNSIGNED_INT, nullptr);
  }
  buffers.clear();
  indices.clear();
}

void Render::applyLiquid() {
  QHashIterator<int, QVector<GLfloat>> i(buffers);
  while (i.hasNext()) {
    i.next();
    textures.get(i.key())->bind();
    vbo.bind();
    vbo.allocate(i.value().constData(), i.value().count() * sizeof(GLfloat));
    auto j = indices[i.key()];
    ibo.bind();
    ibo.allocate(j.constData(), j.count() * sizeof(unsigned int));
    gl->glDrawElements(GL_TRIANGLES, j.count(), GL_UNSIGNED_INT, nullptr);
  }
  buffers.clear();
  indices.clear();
}

void Render::applyFog() {
  vbo.bind();
  vbo.allocate(buffer.constData(), buffer.count() * sizeof(GLfloat));
  ibo.bind();
  ibo.allocate(index.constData(), index.count() * sizeof(unsigned int));
  gl->glDrawElements(GL_TRIANGLES, index.count(), GL_UNSIGNED_INT, nullptr);
  buffer.clear();
  index.clear();
}
