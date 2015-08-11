/** @Copyright 2015 seancode */

#ifndef RENDER_H_
#define RENDER_H_

#include <QOpenGLBuffer>
#include <QOpenGLVertexArrayObject>
#include <QOpenGLFunctions>
#include "./gltextures.h"

class Render {
 public:
  Render();

  void init(QOpenGLFunctions *gl);
  void destroy();

  void setTexturePath(QString path);
  bool texturesValid() const;

  QSharedPointer<QOpenGLTexture> get(int type);

  void add(int type, int x, int y, int texw, int texh, qint16 u, qint16 v,
           GLfloat z, quint8 paint, bool hilite,
           bool fliph = false, bool flipv = false);
  void addLiquid(int type, int x, int y, int texw, int texh, qint16 v,
                 GLfloat z, GLfloat alpha);
  void addSlope(int type, int slope, int x, int y, int texw, int texh,
                qint16 u, qint16 v, GLfloat z, quint8 paint, bool hilite);
  void addFog(int x, int y, GLfloat z = 0.0f);

  void apply();
  void applyLiquid();
  void applyFog();

  void drawBG(int type, int cropw, int croph, GLfloat x, GLfloat y,
              int w, int h);
  void drawFlat(int x, int y, int x2, int y2);

 private:
  QHash<int, QVector<GLfloat>> buffers;
  QVector<GLfloat> buffer;
  QHash<int, QVector<unsigned int>> indices;
  QVector<unsigned int> index;
  GLTextures textures;

  QOpenGLVertexArrayObject vao;
  QOpenGLBuffer vbo, ibo;
  QOpenGLFunctions *gl;
};

#endif  // RENDER_H_
