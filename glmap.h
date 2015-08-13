/** @Copyright 2015 seancode */

#ifndef GLMAP_H_
#define GLMAP_H_

#include <QOpenGLWidget>
#include <QOpenGLFunctions>
#include <QOpenGLBuffer>
#include <QOpenGLVertexArrayObject>
#include <QMatrix4x4>
#include <QThread>
#include "./render.h"
#include "./world.h"
#include "./chestview.h"
#include "./signview.h"

QT_FORWARD_DECLARE_CLASS(QOpenGLShaderProgram)
QT_FORWARD_DECLARE_CLASS(StatusThread)

class GLMap : public QOpenGLWidget, public QOpenGLFunctions {
  Q_OBJECT
  friend class StatusThread;

 public:
  explicit GLMap(QWidget *parent = 0);
  ~GLMap();

  QSize minimumSizeHint() const Q_DECL_OVERRIDE;
  QSize sizeHint() const Q_DECL_OVERRIDE;

  void setTexturePath(QString path);
  void setWorld(QSharedPointer<World> world);

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
  void initializeGL() Q_DECL_OVERRIDE;
  void paintGL() Q_DECL_OVERRIDE;
  void resizeGL(int w, int h) Q_DECL_OVERRIDE;
  void mousePressEvent(QMouseEvent *event) Q_DECL_OVERRIDE;
  void mouseMoveEvent(QMouseEvent *event) Q_DECL_OVERRIDE;
  void mouseReleaseEvent(QMouseEvent *event) Q_DECL_OVERRIDE;
  void wheelEvent(QWheelEvent *event) Q_DECL_OVERRIDE;
  void keyPressEvent(QKeyEvent *event) Q_DECL_OVERRIDE;

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
  StatusThread *runningStatusThread;
  StatusThread *queuedStatusThread;

  ChestView *chestView;
  SignView *signView;

  Render render;
  QOpenGLShaderProgram *program, *waterProgram, *fogProgram, *flatProgram;
  QMatrix4x4 projection;
  double aspect;
  int width, height;
  int startX, startY, endX, endY;
  int flatW, flatH;
  QOpenGLTexture *flat;
  quint8 *flatData;
  
private slots:
  void startQueuedStatusThread();
};



class StatusThread : public QThread {
  Q_OBJECT
  const GLMap *map;
  const QMouseEvent *event;
  void run();
public:
  StatusThread(GLMap *parent, const QMouseEvent *e)
    : QThread(parent), map(parent), event(e) {}
signals:
  void status(const QString &s);
};

#endif  // GLMAP_H_
