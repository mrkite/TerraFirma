/** @Copyright 2015 seancode */

#pragma once

#include <QOpenGLWidget>
#include <QOpenGLFunctions>
#include <QOpenGLBuffer>
#include <QOpenGLVertexArrayObject>
#include <QMatrix4x4>
#include "./render.h"
#include "./world.h"
#include "./chestview.h"
#include "./signview.h"
#include "./l10n.h"

QT_FORWARD_DECLARE_CLASS(QOpenGLShaderProgram)

class GLMap : public QOpenGLWidget, public QOpenGLFunctions {
  Q_OBJECT

 public:
  explicit GLMap(QWidget *parent = nullptr);
  ~GLMap();

  QSize minimumSizeHint() const override;
  QSize sizeHint() const override;

  void setTexturePath(QString path);
  void setWorld(const QSharedPointer<World> &world);
  void setL10n(L10n *l10n);

  void load(QString filename);

 signals:
  void status(QString text);
  void texturesUsed(bool use);
  void hilighting(bool use);
  void texturesAvailable(bool avail);
  void loaded(bool loaded);
  void error(QString text);

 public slots:
  void fogOfWar(bool use);
  void useTextures(bool use);
  void showHouses(bool show);
  void showWires(bool show);
  void resetValues(bool loaded);
  void jumpToSpawn();
  void jumpToDungeon();
  void jumpToLocation(QPointF);
  void startHilighting();
  void stopHilighting();
  void refresh();

 protected:
  void initializeGL() override;
  void paintGL() override;
  void resizeGL(int w, int h) override;
  void mousePressEvent(QMouseEvent *event) override;
  void mouseMoveEvent(QMouseEvent *event) override;
  void mouseReleaseEvent(QMouseEvent *event) override;
  void wheelEvent(QWheelEvent *event) override;
  void keyPressEvent(QKeyEvent *event) override;

 private:
  void calcBounds();
  void drawBackground();
  void drawWalls();
  void drawTiles();
  void drawNPCs();
  void drawWires();
  void drawLiquids();
  void drawFog();

  void drawFlat();

  int getTreeVariant(int offset);
  int findTreeStyle(int x, int y);
  int findBranchStyle(int x, int y);
  int findPillarStyle(int offset);

  QSharedPointer<World>world;
  double centerX, centerY, scale, zoom;
  bool useTexture, hilite, fogOfWarEnabled;
  bool houses, wires;
  bool fullReset;
  QPoint lastMouse;
  bool dragging;

  ChestView *chestView;
  SignView *signView;
  L10n *l10n;

  Render render;
  QOpenGLShaderProgram *program, *waterProgram, *fogProgram, *flatProgram;
  QMatrix4x4 projection;
  double aspect = 0.0;
  int width = 0, height = 0;
  int startX = 0, startY = 0, endX = 0, endY = 0;
  int flatW = 0, flatH = 0;
  QOpenGLTexture *flat;
  quint8 *flatData;
};
