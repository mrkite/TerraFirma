/**
 * @Copyright 2015 seancode
 *
 * Our main map view.. handles input and figures out how to draw the map.
 */


#include "./glmap.h"
#include <QThreadPool>
#include <QOpenGLShader>
#include <QMouseEvent>
#include <QSettings>
#include <QSurfaceFormat>
#include "./uvrules.h"

/*
 * Drawing order:
 * z = 0 - background tiles
 * z = 1 - walls
 * z = 2 - wall outlines
 * z = 3 - tiles
 * z = 4 - special tiles (armor, tree leaves, etc)
 * z = 5 - NPCs, flags
 * z = 6 - liquids
 * z = 7 - wires and actuators
 * z = 8 - fog of war
 */


GLMap::GLMap(QWidget *parent) : QOpenGLWidget(parent) {
  centerX = centerY = 0.0;
  scale = 1.0;
  zoom = 32.0;
  flatW = flatH = 0;
  flat = NULL;
  flatData = NULL;
  useTexture = false;
  hilite = false;
  fogOfWarEnabled = false;
  houses = false;
  wires = false;
  fullReset = false;
  dragging = false;
  program = waterProgram = fogProgram = flatProgram = 0;
  chestView = NULL;
  signView = NULL;
}

GLMap::~GLMap() {
  makeCurrent();
  render.destroy();
  delete program;
  delete waterProgram;
  delete fogProgram;
  delete flatProgram;
  if (flatData != NULL)
    delete [] flatData;
  if (flat != NULL)
    flat->destroy();
  doneCurrent();
  if (chestView != NULL)
    delete chestView;
  if (signView != NULL)
    delete signView;
}

QSize GLMap::minimumSizeHint() const {
  return QSize(50, 50);
}

QSize GLMap::sizeHint() const {
  return QSize(200, 200);
}

void GLMap::setTexturePath(QString path) {
  render.setTexturePath(path);
  emit texturesAvailable(render.texturesValid());
}

void GLMap::setWorld(QSharedPointer<World> world) {
  this->world = world;
  connect(world.data(), SIGNAL(loaded(bool)),
          this, SLOT(setEnabled(bool)));
  connect(world.data(), SIGNAL(loaded(bool)),
          this, SLOT(resetValues(bool)));
  connect(world.data(), SIGNAL(loaded(bool)),
          this, SIGNAL(loaded(bool)));
  emit loaded(false);
  emit texturesAvailable(render.texturesValid());
  emit texturesUsed(false);
  emit hilighting(false);
}

void GLMap::load(QString filename) {
  world->setFilename(filename);

  fullReset = true;
  QThreadPool::globalInstance()->start(world.data());
}

void GLMap::refresh() {
  QThreadPool::globalInstance()->start(world.data());
}

void GLMap::resetValues(bool val) {
  if (!fullReset || !val)
    return;
  fullReset = false;

  jumpToSpawn();
}

void GLMap::fogOfWar(bool val) {
  fogOfWarEnabled = val;
  QSettings info;
  info.setValue("fogOfWar", fogOfWarEnabled);
  update();
}

void GLMap::useTextures(bool val) {
  QSettings info;
  info.setValue("textures", val);
  useTexture = val && render.texturesValid();
  emit texturesUsed(useTexture);
  update();
}

void GLMap::showHouses(bool val) {
  houses = val;
  QSettings info;
  info.setValue("houses", houses);
  update();
}

void GLMap::showWires(bool val) {
  wires = val;
  QSettings info;
  info.setValue("wires", wires);
  update();
}

void GLMap::jumpToSpawn() {
  centerX = world->header["spawnX"]->toDouble();
  centerY = world->header["spawnY"]->toDouble();
  calcBounds();
  update();
}

void GLMap::jumpToDungeon() {
  centerX = world->header["dungeonX"]->toDouble();
  centerY = world->header["dungeonY"]->toDouble();
  calcBounds();
  update();
}

void GLMap::jumpToLocation(QPointF loc) {
  centerX = loc.x();
  centerY = loc.y();
  calcBounds();
  update();
}

void GLMap::startHilighting() {
  hilite = true;
  update();
  emit hilighting(true);
}

void GLMap::stopHilighting() {
  hilite = false;
  update();
  emit hilighting(false);
}

void GLMap::initializeGL() {
  initializeOpenGLFunctions();

  glEnable(GL_DEPTH_TEST);
  glEnable(GL_BLEND);
  glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

  QOpenGLShader *vshader = new QOpenGLShader(QOpenGLShader::Vertex, this);
  vshader->compileSourceFile(":/res/shaders/tiles_vs.glsl");
  QOpenGLShader *fshader = new QOpenGLShader(QOpenGLShader::Fragment, this);
  fshader->compileSourceFile(":/res/shaders/tiles_fs.glsl");
  program = new QOpenGLShaderProgram;
  program->addShader(vshader);
  program->addShader(fshader);
  program->link();
  program->bind();

  render.init(this);

  vshader = new QOpenGLShader(QOpenGLShader::Vertex, this);
  vshader->compileSourceFile(":/res/shaders/water_vs.glsl");
  fshader = new QOpenGLShader(QOpenGLShader::Fragment, this);
  fshader->compileSourceFile(":/res/shaders/water_fs.glsl");
  waterProgram = new QOpenGLShaderProgram;
  waterProgram->addShader(vshader);
  waterProgram->addShader(fshader);
  waterProgram->link();

  vshader = new QOpenGLShader(QOpenGLShader::Vertex, this);
  vshader->compileSourceFile(":/res/shaders/fog_vs.glsl");
  fshader = new QOpenGLShader(QOpenGLShader::Fragment, this);
  fshader->compileSourceFile(":/res/shaders/fog_fs.glsl");
  fogProgram = new QOpenGLShaderProgram;
  fogProgram->addShader(vshader);
  fogProgram->addShader(fshader);
  fogProgram->link();

  vshader = new QOpenGLShader(QOpenGLShader::Vertex, this);
  vshader->compileSourceFile(":/res/shaders/flat_vs.glsl");
  fshader = new QOpenGLShader(QOpenGLShader::Fragment, this);
  fshader->compileSourceFile(":/res/shaders/flat_fs.glsl");
  flatProgram = new QOpenGLShaderProgram;
  flatProgram->addShader(vshader);
  flatProgram->addShader(fshader);
  flatProgram->link();

  flat = new QOpenGLTexture(QOpenGLTexture::Target2D);
}

void GLMap::paintGL() {
  glClearColor(0.8f, 0.8f, 0.8f, 1.0f);
  glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

  if (!isEnabled())
    return;

  try {
    if (this->useTexture && zoom >= 8.0) {
      if (fogOfWarEnabled) {
        fogProgram->bind();
        fogProgram->setUniformValue("matrix", projection);
        fogProgram->setAttributeBuffer(0, GL_FLOAT, 0, 3, 3 * sizeof(GLfloat));
        fogProgram->enableAttributeArray(0);

        drawFog();

        fogProgram->disableAttributeArray(0);
      }


      program->bind();
      program->setUniformValue("texture", 0);
      program->setUniformValue("matrix", projection);
      program->setUniformValue("hiliting", hilite);

      const int stride = 7 * sizeof(GLfloat);

      program->setAttributeBuffer(0, GL_FLOAT, 0, 3, stride);
      program->setAttributeBuffer(1, GL_FLOAT, 3 * sizeof(GLfloat),
                                  2, stride);
      program->setAttributeBuffer(2, GL_FLOAT, 5 * sizeof(GLfloat),
                                  1, stride);
      program->setAttributeBuffer(3, GL_FLOAT, 6 * sizeof(GLfloat),
                                  1, stride);
      program->enableAttributeArray(0);
      program->enableAttributeArray(1);
      program->enableAttributeArray(2);
      program->enableAttributeArray(3);

      // reverse order so gl does less work
      if (wires)
        drawWires();
      drawNPCs();
      drawTiles();
      drawWalls();
      drawBackground();


      program->disableAttributeArray(3);
      program->disableAttributeArray(2);
      program->disableAttributeArray(1);
      program->disableAttributeArray(0);

      waterProgram->bind();
      waterProgram->setUniformValue("texture", 0);
      waterProgram->setUniformValue("matrix", projection);
      waterProgram->setUniformValue("hiliting", hilite);

      const int waterStride = 6 * sizeof(GLfloat);

      waterProgram->setAttributeBuffer(0, GL_FLOAT, 0, 3, waterStride);
      waterProgram->setAttributeBuffer(1, GL_FLOAT, 3 * sizeof(GLfloat),
                                       2, waterStride);
      waterProgram->setAttributeBuffer(2, GL_FLOAT, 5 * sizeof(GLfloat),
                                       1, waterStride);
      waterProgram->enableAttributeArray(0);
      waterProgram->enableAttributeArray(1);
      waterProgram->enableAttributeArray(2);

      drawLiquids();

      waterProgram->disableAttributeArray(2);
      waterProgram->disableAttributeArray(1);
      waterProgram->disableAttributeArray(0);
    } else {
      flatProgram->bind();
      waterProgram->setUniformValue("texture", 0);
      flatProgram->setUniformValue("matrix", projection);
      flatProgram->setUniformValue("hiliting", hilite);

      const int stride = 5 * sizeof(GLfloat);
      flatProgram->setAttributeBuffer(0, GL_FLOAT, 0, 3, stride);
      flatProgram->setAttributeBuffer(1, GL_FLOAT, 3 * sizeof(GLfloat),
                                       2, stride);
      flatProgram->enableAttributeArray(0);
      flatProgram->enableAttributeArray(1);

      int newW = endX - startX;
      int newH = endY - startY;

      if (newW != flatW || newH != flatH) {
        if (flatData == NULL || newW > flatW || newH > flatH) {
          if (flatData != NULL)
            delete [] flatData;
          flatData = new quint8[newW * newH * 4];
        }
        flatW = newW;
        flatH = newH;
        flat->destroy();
        flat->create();
        flat->setSize(flatW, flatH);
        flat->setFormat(QOpenGLTexture::RGBA8_UNorm);
        flat->setMagnificationFilter(QOpenGLTexture::Nearest);
        flat->setMinificationFilter(QOpenGLTexture::Nearest);
        flat->setWrapMode(QOpenGLTexture::ClampToBorder);
        flat->setAutoMipMapGenerationEnabled(false);
        flat->allocateStorage();
      }

      drawFlat();

      flatProgram->disableAttributeArray(1);
      flatProgram->disableAttributeArray(0);
    }
  } catch (TextureException e) {
    setEnabled(false);  // prevent future errors...
    emit error(e.reason);
  }
}

void GLMap::resizeGL(int w, int h) {
  width = w;
  height = h;
  scale = qMin(w, h) / zoom;  // how to adjust scale to target 16 pixel tiles
  aspect = static_cast<double>(w) / h;
  glViewport(0, 0, w, h);

  calcBounds();
}

void GLMap::mousePressEvent(QMouseEvent *event) {
  if (!isEnabled()) return;
  lastMouse = event->pos();
  dragging = true;
}

void GLMap::mouseMoveEvent(QMouseEvent *event) {
  if (!isEnabled()) return;
  if (!dragging) {
    QMatrix4x4 m = projection.inverted();
    QVector3D mouse = m.map(QVector3D(
        static_cast<float>(event->x()) / (width / 2.0) - 1.0f,
        static_cast<float>(height - event->y()) / (height / 2.0) - 1.0f,
        0.0f));
    int x = mouse.x();
    int y = mouse.y();
    if (x >= 0 && y >= 0 && x < world->tilesWide && y < world->tilesHigh) {
      auto tile = &world->tiles[y * world->tilesWide + x];
      if (fogOfWarEnabled && !tile->seen()) {
        emit status(QString("%1,%2 - Murky Blackness").arg(x).arg(y));
      } else if (tile->active()) {
        auto info = world->info[tile];
        emit status(QString("%1,%2 - %3 (%4)").arg(x).arg(y)
                    .arg(info->name).arg(tile->color));
      } else if (tile->wall > 0) {
        auto info = world->info.walls[tile->wall];
        emit status(QString("%1,%2 - %3").arg(x).arg(y)
                    .arg(info->name));
      } else {
        emit status(QString("%1,%2").arg(x).arg(y));
      }
    }
    return;
  }

  centerX += (lastMouse.x() - event->x()) / (zoom / 2.0);
  centerY += (lastMouse.y() - event->y()) / (zoom / 2.0);
  centerX = qMax(0.0, qMin(centerX, world->tilesWide - 1.0));
  centerY = qMax(0.0, qMin(centerY, world->tilesHigh - 1.0));
  lastMouse = event->pos();
  calcBounds();
  update();
}

void GLMap::mouseReleaseEvent(QMouseEvent *event) {
  if (event->button() == Qt::RightButton) {
    QMatrix4x4 m = projection.inverted();
    QVector3D mouse = m.map(QVector3D(
        static_cast<float>(event->x()) / (width / 2.0) - 1.0f,
        static_cast<float>(height - event->y()) / (height / 2.0) - 1.0f,
        0.0f));
    int x = mouse.x();
    int y = mouse.y();
    for (auto const &chest : world->chests) {
      if ((chest.x == x || chest.x + 1 == x) &&
          (chest.y == y || chest.y + 1 == y)) {
        QList<QString> items;
        for (auto const &item : chest.items) {
          if (item.stack > 0) {
            if (item.prefix == "")
              items.append(QString("%1 %2").arg(item.stack).arg(item.name));
            else
              items.append(QString("%1 %2 %3").arg(item.stack)
                           .arg(item.prefix).arg(item.name));
          }
        }

        QString name = chest.name;
        if (name.isEmpty())
          name = world->info[&world->tiles[y * world->tilesWide + x]]->name;

        if (chestView != NULL)
          delete chestView;
        chestView = new ChestView(name, items, this);
        chestView->move(QCursor::pos());
        chestView->show();
      }
    }
    for (auto const &sign : world->signs) {
      if ((sign.x == x || sign.x + 1 == x) &&
          (sign.y == y || sign.y + 1 == y)) {
        if (signView != NULL)
          delete signView;
        signView = new SignView(sign.text, this);
        signView->move(QCursor::pos());
        signView->show();
      }
    }
  }
  dragging = false;
}

void GLMap::wheelEvent(QWheelEvent *event) {
  zoom += event->delta() / 90.0;
  if (zoom > 32.0) zoom = 32.0;
  else if (zoom < 2.0) zoom = 2.0;

  scale = qMin(width, height) / floor(zoom);
  calcBounds();
  update();
}

void GLMap::keyPressEvent(QKeyEvent *event) {
  double speed = 10.0;
  if (event->modifiers() & Qt::ShiftModifier)
    speed *= 2.0;
  if (event->modifiers() & Qt::ControlModifier)
    speed *= 10.0;

  switch (event->key()) {
    case Qt::Key_Up:
    case Qt::Key_W:
      centerY -= speed;
      if (centerY < 0) centerY = 0;
      break;
    case Qt::Key_Down:
    case Qt::Key_S:
      centerY += speed;
      if (centerY > world->tilesHigh) centerY = world->tilesHigh;
      break;
    case Qt::Key_Left:
    case Qt::Key_A:
      centerX -= speed;
      if (centerX < 0) centerX = 0;
      break;
    case Qt::Key_Right:
    case Qt::Key_D:
      centerX += speed;
      if (centerX > world->tilesWide) centerX = world->tilesWide;
      break;
    case Qt::Key_PageUp:
    case Qt::Key_E:
      zoom++;
      if (zoom > 32) zoom = 32.0;
      scale = qMin(width, height) / floor(zoom);
      break;
    case Qt::Key_PageDown:
    case Qt::Key_Q:
      zoom--;
      if (zoom < 2.0) zoom = 2.0;
      scale = qMin(width, height) / floor(zoom);
      break;
  }
  calcBounds();
  update();
}

// call when mouse move or scale is changed
void GLMap::calcBounds() {
  projection.setToIdentity();
  projection.ortho(-scale * aspect, scale * aspect, scale, -scale, 1.0, 16.0f);
  projection.translate(-centerX, -centerY, -15.0f);

  QMatrix4x4 m = projection.inverted();
  QVector3D pt = m.map(QVector3D(-1.0, 1.0, 0.0));  // topright corner
  startX = qMax(static_cast<int>(pt.x()) - 2, 0);
  startY = qMax(static_cast<int>(pt.y()) - 2, 0);
  pt = m.map(QVector3D(1.0, -1.0, 0.0));  // bottomleft
  endX = qMin(static_cast<int>(pt.x()) + 2, world->tilesWide);
  endY = qMin(static_cast<int>(pt.y()) + 2, world->tilesHigh);
}

static int backStyles[] = {
  66, 67, 68, 69, 128, 125, 185,
  70, 71, 68, 72, 128, 125, 185,
  73, 74, 75, 76, 134, 125, 185,
  77, 78, 79, 82, 134, 125, 185,
  83, 84, 85, 86, 137, 125, 185,
  83, 87, 88, 89, 137, 125, 185,
  121, 122, 123, 124, 140, 125, 185,
  153, 147, 148, 149, 150, 125, 185,
  146, 154, 155, 156, 157, 125, 185
};

void GLMap::drawBackground() {
  int groundLevel = world->header["groundLevel"]->toInt();
  int rockLevel = world->header["rockLevel"]->toInt();
  int hellLevel = ((world->tilesHigh - 330) - groundLevel) / 6;
  hellLevel = hellLevel * 6 + groundLevel - 5;
  int hellBottom = ((world->tilesHigh - 200) - hellLevel) / 6;
  hellBottom = hellBottom * 6 + hellLevel - 5;

  int hellStyle = world->header["hellBackStyle"]->toInt();


  render.drawBG(GLTextures::Background | 0, 0, 0, 0, 0,
                world->tilesWide, groundLevel);

  int lastX = 0;
  int style;
  for (int i = 0; i <= 3; i++) {
    style = world->header["caveBackStyle"]->at(i)->toInt() * 7;
    int nextX = i == 3 ? world->tilesWide :
        world->header["caveBackX"]->at(i)->toInt();
    render.drawBG(GLTextures::Background | backStyles[style], 128, 16,
                  lastX, groundLevel - 1, nextX - lastX, 1);
    render.drawBG(GLTextures::Background | backStyles[style + 1], 128, 96,
                  lastX, groundLevel, nextX - lastX, rockLevel - groundLevel);
    render.drawBG(GLTextures::Background | backStyles[style + 2], 128, 16,
                  lastX, rockLevel, nextX - lastX, 1);
    render.drawBG(GLTextures::Background | backStyles[style + 3], 128, 96,
                  lastX, rockLevel + 1,
                  nextX - lastX, hellLevel - (rockLevel + 1));
    render.drawBG(GLTextures::Background | (backStyles[style + 4] + hellStyle),
                  128, 16, lastX, hellLevel, nextX - lastX, 1);
    render.drawBG(GLTextures::Background | (backStyles[style + 5] + hellStyle),
                  128, 96, lastX, hellLevel + 1,
                  nextX - lastX, hellBottom - (hellLevel + 1));
    render.drawBG(GLTextures::Background | (backStyles[style + 6] + hellStyle),
                  128, 16, lastX, hellBottom, nextX - lastX, 1);
    lastX = nextX;
  }

  render.drawBG(GLTextures::Underworld | 4, 0, 0,
                0, hellBottom, world->tilesWide, world->tilesHigh - hellBottom);
}


void GLMap::drawWalls() {
  int stride = world->tilesWide;
  int wall;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      auto tile = &world->tiles[offset];
      if (fogOfWarEnabled && !tile->seen()) continue;
      if (tile->wall > 0) {
        auto info = world->info.walls[tile->wall];
        if (tile->wallu < 0)
          UVRules::fixWall(world, x, y);

        int color = tile->wallColor;
        if (color == 30)
          color = 43;
        else if (color >= 28)
          color = 40 + color - 28;

        render.add(GLTextures::Wall | tile->wall, x * 16 - 8, y * 16 - 8,
                   32, 32, tile->wallu, tile->wallv, 1.0f,
                   color, false);
        int blend = info->blend;
        if (x > 0 && (wall = world->tiles[offset - 1].wall) > 0 &&
            world->info.walls[wall]->blend != blend)
          render.add(GLTextures::WallOutline, x * 16, y * 16,
                     2, 16, 0, 0, 1.5f, 0, false);
        if (x < world->tilesWide - 2 &&
            (wall = world->tiles[offset + 1].wall) > 0 &&
            world->info.walls[wall]->blend != blend)
          render.add(GLTextures::WallOutline, x * 16 + 14, y * 16,
                     2, 16, 14, 0, 1.5, 0, false);
        if (y > 0 && (wall = world->tiles[offset - stride].wall) > 0 &&
            world->info.walls[wall]->blend != blend)
          render.add(GLTextures::WallOutline, x * 16, y * 16,
                     16, 2, 0, 0, 1.5f, 0, false);
        if (y < world->tilesHigh - 2 &&
            (wall = world->tiles[offset + stride].wall) > 0 &&
            world->info.walls[wall]->blend != blend)
          render.add(GLTextures::WallOutline, x * 16, y * 16 + 14,
                     16, 2, 0, 14, 1.5f, 0, false);
      }
    }
  }
  render.apply();
}


static int trackUVs[] = {
  0, 0, 0,  1, 0, 0,  2, 1, 1,  3, 1, 1,  0, 2, 8,  1, 2, 4,
  0, 1, 0,  1, 1, 0,  0, 3, 4,  1, 3, 8,  4, 1, 9,  5, 1, 5,
  6, 1, 1,  7, 1, 1,  2, 0, 0,  3, 0, 0,  4, 0, 8,  5, 0, 4,
  6, 0, 0,  7, 0, 0,  0, 4, 0,  1, 4, 0,  0, 5, 0,  1, 5, 0,
  2, 2, 2,  3, 2, 2,  4, 2, 10, 5, 2, 6,  6, 2, 2,  7, 2, 2,
  2, 3, 0,  3, 3, 0,  4, 3, 4,  5, 3, 8,  6, 3, 4,  7, 3, 8
};

void GLMap::drawTiles() {
  int stride = world->tilesWide;

  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      auto tile = &world->tiles[offset];
      if (fogOfWarEnabled && !tile->seen()) continue;
      auto info = world->info[tile];

      if (tile->active()) {
        if (tile->u < 0)
          UVRules::fixTile(world, x, y);

        bool fliph = info->flip && (x & 1);
        bool flipv = false;
        if (tile->type == 184) {  // moss
          if (tile->v < 108)
            fliph = x & 1;
          else
            flipv = y & 1;
        } else if (tile->type == 185 && tile->v == 0) {
          fliph = x & 1;
        }

        int toppad = info->toppad;
        if (tile->type == 4 && y > 0 &&
            world->info[world->tiles[offset - stride].type]->solid) {
          toppad = 2;
          if (y < world->tilesHigh - 1 &&
              ((x > 0 &&
                world->info[world->tiles[offset + stride - 1].type]->solid) ||
               (x < world->tilesWide - 1 &&
                world->info[world->tiles[offset + stride + 1].type]->solid)))
            toppad = 4;
        }

        if (tile->type >= 373 && tile->type <= 375)  // don't draw water drops
          continue;

        // calculate color
        int color = tile->color;
        if (color >= 28)
          color = 40 + color - 28;
        else if (color > 0 && color < 13 && (info->grass || tile->type == 5))
          color += 27;

        // special types are drawn in front of others

        // item frames
        if (tile->type == 395) {
          // TODO(mrkite): draw item in frame
        }

        // clothes on mannequins
        if ((tile->type == 128 || tile->type == 269) && tile->u >= 100) {
          int armor = tile->u / 100;
          int piece = tile->v / 18;
          fliph = (tile->u % 100) < 36;
          switch (piece) {
            case 0:
              render.add(GLTextures::ArmorHead | armor,
                         x * 16 - 4, y * 16 - 12, 40, 36, 0, 0, 4.0f,
                         color, info->isHilighting, fliph);
              break;
            case 1:
              if (tile->type == 269)
                render.add(GLTextures::ArmorFemale | armor,
                           x * 16 - 4, y * 16 - 28, 40, 54, 0, 0, 4.0f,
                           color, info->isHilighting, fliph);
              else
                render.add(GLTextures::ArmorBody | armor,
                           x * 16 - 4, y * 16 - 28, 40, 54, 0, 0, 4.0f,
                           color, info->isHilighting, fliph);
              break;
            case 2:
              if (armor == 83 && tile->type == 128) armor = 117;
              if (armor == 84 && tile->type == 128) armor = 120;
              render.add(GLTextures::ArmorLegs | armor,
                         x * 16 - 4, y * 16 - 44, 40, 54, 0, 0, 4.0f,
                         color, info->isHilighting, fliph);
              break;
          }
        }

        // tree branches
        if (tile->type == 5 && tile->u >= 22 && tile->v >= 198) {
          int variant;
          switch (tile->v) {
            case 220: variant = 1; break;
            case 242: variant = 2; break;
            default: variant = 0; break;
          }

          if (tile->u == 22) {  // tree top
            int texw = 80;
            int texh = 80;
            int padx = 32;
            int style = findTreeStyle(x, y);
            switch (style) {
              case 2: case 11: case 13:  // jungle trees
                texw = 114;
                texh = 96;
                padx = 48;
                break;
              case 3:  // hallowed trees
                texh = 140;
                if (x % 3 == 1)
                  variant += 3;
                else if (x % 3 == 2)
                  variant += 6;
                break;
            }

            render.add(GLTextures::TreeTops | style,
                       x * 16 - padx, (y + 1) * 16 - texh, texw, texh,
                       variant * (texw + 2), 0, 4.0f,
                       color, info->isHilighting);
          } else if (tile->u == 44) {  // left branch
            int style = findBranchStyle(x + 1, y);
            if (style == 3) {  // hallowed trees
              if (x % 3 == 1)
                variant += 3;
              else if (x % 3 == 2)
                variant += 6;
            }
            render.add(GLTextures::TreeBranches | style,
                       x * 16 - 24, y * 16 - 12, 40, 40,
                       0, variant * 42, 4.0f,
                       color, info->isHilighting);
          } else if (tile->u == 66) {  // right branch
            int style = findBranchStyle(x - 1, y);
            if (style == 3) {  // hallowed trees
              if (x % 3 == 1)
                variant += 3;
              else if (x % 3 == 2)
                variant += 6;
            }
            render.add(GLTextures::TreeBranches | style,
                       x * 16, y * 16 - 12, 40, 40,
                       42, variant * 42, 4.0f, color, info->isHilighting);
          }
        }

        // pillars
        if (tile->type == 323 && tile->u >= 88 && tile->u <= 132) {
          int variant;
          switch (tile->u) {
            case 110: variant = 1; break;
            case 132: variant = 2; break;
            default: variant = 0; break;
          }
          int style = findPillarStyle(offset);
          render.add(GLTextures::TreeTops | 15,
                     x * 16 - 32 + tile->v, y * 16 - 80 + 16, 80, 80,
                     variant * 82, style * 82, 4.0f, color, info->isHilighting);
        }

        int texw = info->width - 2;
        int texh = info->height - 2;

        // lunar crafting station
        if (tile->type == 412 && tile->u == 0 && tile->v == 0) {
          render.add(GLTextures::Tile | tile->type,
                     x * 16, y * 16 + info->toppad, texw, texh,
                     tile->u, tile->v, 4.0f, color, info->isHilighting);
          continue;
        }

        // lihzhard altar
        if (tile->type == 237 && tile->u == 0 && tile->v == 0) {
          render.add(GLTextures::Tile | tile->type,
                     x * 16, y * 16 + info->toppad, texw, texh,
                     tile->u, tile->v, 4.0f, color, info->isHilighting);
          continue;
        }

        // weapon rack
        if (tile->type == 334 && tile->u >= 5000 && tile->v == 18) {
          // TODO(mrkite): draw weapon on rack
        }

        // mushroom tree tops
        if (tile->type == 72 && tile->u >= 36) {
          int variant = 0;
          switch (tile->v) {
            case 18: variant = 1; break;
            case 36: variant = 2; break;
            default: variant = 0; break;
          }
          render.add(GLTextures::Shroom, x * 16 - 22, y * 16 - 26, 60, 42,
                     variant * 62, 0, 4.0f, color, info->isHilighting);
        }


        // minecart tracks
        if (tile->type == 314) {
          int u = tile->u;
          render.add(GLTextures::Tile | tile->type,
                     x * 16, y * 16 + info->toppad, texw, texh,
                     trackUVs[u * 3] * 18, trackUVs[u * 3 + 1] * 18, 3.0f,
                     color, info->isHilighting);
          if (u >= 0 && u < 36) {
            int mask = trackUVs[u * 3 + 2];
            if (mask & 8)
              render.add(GLTextures::Tile | tile->type,
                         x * 16, (y + 1) * 16 + info->toppad,
                         texw, texh, 0, 108, 3.0f, color, info->isHilighting);
            if (mask & 4)
              render.add(GLTextures::Tile | tile->type,
                         x * 16, (y + 1) * 16 + info->toppad,
                         texw, texh, 18, 108, 3.0f, color, info->isHilighting);
            if (mask & 2)
              render.add(GLTextures::Tile | tile->type,
                         x * 16, (y - 1) * 16 + info->toppad,
                         texw, texh, 18, 126, 3.0f, color, info->isHilighting);
            if (mask & 1)
              render.add(GLTextures::Tile | tile->type,
                         x * 16, (y - 1) * 16 + info->toppad,
                         texw, texh,  0, 126, 3.0f, color, info->isHilighting);
          }
        } else if (tile->type == 171) {  // xmas tree
          if (tile->u >= 10) {
            int topper = tile->v & 7;
            int garland = (tile->v >> 3) & 7;
            int ornaments = (tile->v >> 6) & 0xf;
            int lights = (tile->v >> 10) & 0xf;

            render.add(GLTextures::Xmas | 0, x * 16, y * 16 + toppad,
                       64, 128, 0, 0, 3.0f, color, info->isHilighting);
            if (topper > 0)
              render.add(GLTextures::Xmas | 3, x * 16, y * 16 + toppad,
                         64, 128, 66 * (topper - 1), 0, 4.0f,
                         color, info->isHilighting);
            if (garland > 0)
              render.add(GLTextures::Xmas | 1, x * 16, y * 16 + toppad,
                         64, 128, 66 * (garland - 1), 0, 4.0f,
                         color, info->isHilighting);
            if (ornaments > 0)
              render.add(GLTextures::Xmas | 2, x * 16, y * 16 + toppad,
                         64, 128, 66 * (ornaments - 1), 0, 4.0f,
                         color, info->isHilighting);
            if (lights > 0)
              render.add(GLTextures::Xmas | 4, x * 16, y * 16 + toppad,
                         64, 128, 66 * (lights - 1), 0, 4.0f,
                         color, info->isHilighting);
          }
        } else if (tile->slope > 0) {  // sloped tile
          if (tile->type == 19) {  // stairs
            render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                       texw, texh, tile->u, tile->v, 3.0f,
                       color, info->isHilighting);
            auto br = &world->tiles[offset + stride + 1];
            auto bl = &world->tiles[offset + stride - 1];
            if (tile->slope == 1 && br->active() &&
                br->slope != 2 && !br->half() && br->type != 386 &&
                br->type != 387 && br->type !=54) {
              int u = 198;
              if (br->type == 19 && br->slope == 0)
                u = 324;
              render.add(GLTextures::Tile | tile->type,
                         x * 16, (y + 1) * 16 + toppad,
                         16, 16, u, tile->v, 3.0f,
                         color, info->isHilighting);
            } else if (tile->slope == 2 && bl->active() &&
                       bl->slope != 1 && !bl->half() && bl->type != 386 &&
                       bl->type != 387 && bl->type !=54) {
              int u = 162;
              if (bl->type == 19 && bl->slope == 0)
                u = 306;
              render.add(GLTextures::Tile | tile->type,
                         x * 16, (y + 1) * 16 + toppad,
                         16, 16, u, tile->v, 3.0f,
                         color, info->isHilighting);
            }
          } else {
            render.addSlope(GLTextures::Tile | tile->type, tile->slope,
                            x * 16, y * 16 + toppad, texw, texh,
                            tile->u, tile->v, 3.0f,
                            color, info->isHilighting);
          }
        } else if (tile->type == 80) {  // cactus
          int cactus = -1;
          int coff = offset;
          if (tile->u == 36) coff--;
          if (tile->u == 54) coff++;
          if (tile->u == 108) {
            if (tile->v == 18)
              coff--;
            else
              coff++;
          }
          auto ctile = &world->tiles[coff];
          while (!ctile->active() || ctile->type == 80 ||
                 !world->info[ctile->type]->solid) {
            coff += stride;
            ctile = &world->tiles[coff];
          }
          if (ctile->active()) {
            switch (ctile->type) {
              case 112:
                cactus = 1;
                break;
              case 116:
                cactus = 2;
                break;
              case 234:
                cactus = 3;
                break;
            }
          }
          if (cactus == -1)
            render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                       texw, texh, tile->u, tile->v, 4.0f,
                       color, info->isHilighting);
          else
            render.add(GLTextures::Cactus | cactus, x * 16, y * 16 + toppad,
                       texw, texh, tile->u, tile->v, 4.0f,
                       color, info->isHilighting);
        } else if (tile->type == 272 && !tile->half() && x > 0 &&
                   !world->tiles[offset - 1].half() &&
                   x < world->tilesWide - 1 &&
                   !world->tiles[offset + 1].half()) {
          // iron plating
          int variant = (x & 1) + (y & 1) + (x % 3) + (y % 3);
          while (variant > 1) variant -= 2;
          render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                     texw, texh, tile->u, tile->v + variant * 90, 3.0f,
                     color, info->isHilighting);
        } else if (tile->type != 19 && tile->type != 380 &&
                   info->solid && !tile->half() &&
                   ((x > 0 && world->tiles[offset - 1].half()) ||
                    ((x < world->tilesWide - 1 &&
                      world->tiles[offset + 1].half())))) {
          // adjacent to half block
          if (world->tiles[offset - 1].half() &&
              world->tiles[offset + 1].half()) {
            // both sides are half
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad + 8, texw, 8,
                       tile->u, tile->v + 8, 3.0f,
                       color, info->isHilighting);
            if (world->tiles[offset - stride].slope < 3 &&
                world->tiles[offset - stride].type == tile->type)
              render.add(GLTextures::Tile | tile->type,
                         x * 16, y * 16 + toppad, 16, 8, 90, 0, 3.0f,
                         color, info->isHilighting);
            else
              render.add(GLTextures::Tile | tile->type,
                         x * 16, y * 16 + toppad, 16, 8, 126, 0, 3.0f,
                         color, info->isHilighting);
          } else if (world->tiles[offset - 1].half()) {
            // just left side
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad + 8, texw, 8,
                       tile->u, tile->v + 8, 3.0f,
                       color, info->isHilighting);
            render.add(GLTextures::Tile | tile->type,
                       x * 16 + 4, y * 16 + toppad, texw - 4, texh,
                       tile->u + 4, tile->v, 3.0f,
                       color, info->isHilighting);
            render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                       4, 8, 126, 0, 3.0f,
                       color, info->isHilighting);
          } else {
            // just right side
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad + 8, texw, 8,
                       tile->u, tile->v + 8, 3.0f,
                       color, info->isHilighting);
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad, texw - 4, 8,
                       tile->u, tile->v, 3.0f,
                       color, info->isHilighting);
            render.add(GLTextures::Tile | tile->type,
                       x * 16 + 12, y * 16 + toppad, 4, 8, 138, 0, 3.0f,
                       color, info->isHilighting);
          }
        } else if (tile->type == 128 || tile->type == 269) {
          // mannequin
          render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                     texw, texh, tile->u % 100, tile->v, 3.0f,
                     color, info->isHilighting);
        } else if (tile->type == 334) {
          // weapon rack
          render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                     texw, texh, ((tile->u / 5000) - 1) * 18, tile->v, 3.0f,
                     color, info->isHilighting);
        } else if (tile->type == 5) {
          // tree
          int toff = offset;
          if (tile->u == 66 && tile->v <= 45) toff++;
          if (tile->u == 88 && tile->v >= 66 && tile->v <= 110) toff--;
          if (tile->u == 22 && tile->v >= 132) toff--;
          if (tile->u == 44 && tile->v >= 132) toff++;
          while (world->tiles[toff].active() &&
                 world->tiles[toff].type == 5) toff += stride;
          int variant = getTreeVariant(toff);
          if (variant == -1)
            render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                       texw, texh, tile->u, tile->v, 3.0f,
                       color, info->isHilighting);
          else
            render.add(GLTextures::Wood | variant, x * 16, y * 16 + toppad,
                       texw, texh, tile->u, tile->v, 3.0f,
                       color, info->isHilighting);
        } else if (tile->type == 323) {
          int poff = offset;
          while (world->tiles[poff].active() &&
                 world->tiles[poff].type == 323) poff += stride;
          int variant = findPillarStyle(poff);
          render.add(GLTextures::Tile | tile->type, x * 16, y * 16 + toppad,
                     texw, texh, tile->u, 22 * variant, 3.0f,
                     color, info->isHilighting);
        } else if (tile->half() && y < world->tilesHigh - 1 &&
                   (!world->tiles[offset + stride].active() ||
                    !world->info[world->tiles[offset + stride].type]->solid ||
                    world->tiles[offset + stride].half())) {
          if (tile->type == 19) {
            // just draw platform lower
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad + 8, texw, texh,
                       tile->u, tile->v, 3.0f,
                       color, info->isHilighting);
          } else {
            // floating half block
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad + 8, texw, texh - 12,
                       tile->u, tile->v, 3.0f,
                       color, info->isHilighting);
            render.add(GLTextures::Tile | tile->type,
                       x * 16, y * 16 + toppad + 12, texw, 4, 144, 66, 3.0f,
                       color, info->isHilighting);
          }
        } else {
          // normal tile..
          render.add(GLTextures::Tile | tile->type,
                     x * 16 - (texw - 16) / 2,
                     y * 16 + info->toppad + (tile->half() ? 8 : 0),
                     texw, texh - (tile->half() ? 8 : 0),
                     tile->u, tile->v, 3.0f,
                     color, info->isHilighting, fliph, flipv);
        }
      }
    }
  }
  render.apply();
}

void GLMap::drawNPCs() {
  int stride = world->tilesWide;

  for (auto const &npc : world->npcs) {
    if (npc.sprite != 0 &&
        (npc.x + 32) / 16 >= startX && npc.x / 16 < endX &&
        (npc.y + 56) / 16 >= startY && npc.y / 16 < endY) {
      int offset = static_cast<int>(npc.y / 16) * stride +
          static_cast<int>(npc.x / 16);
      if (!fogOfWarEnabled || world->tiles[offset].seen()) {
        render.add(GLTextures::NPC | npc.sprite, npc.x, npc.y - 16,
                   0, 56, 0, 0, 5.5f, 0, false);
      }
    }
    if (houses && npc.head != 0) {
      int hx = npc.homeX;
      int hy = npc.homeY - 1;
      int offset = hy * stride + hx;
      while (!world->tiles[offset].active() ||
             !world->info[world->tiles[offset].type]->solid) {
        hy--;
        offset -= stride;
        if (hy < 10) break;
      }
      hy++;
      offset += stride;
      if (hx >= startX && hx < endX && hy >= startY && hy < endY) {
        if (!fogOfWarEnabled || world->tiles[offset].seen()) {
          int dy = 18;
          if (world->tiles[offset - stride].type == 19)  // platform
            dy -= 8;
          auto tex = render.get(GLTextures::Banner);  // house banner
          render.add(GLTextures::Banner,
                     hx * 16 - tex->width() / 2,
                     hy * 16 + dy - tex->height() / 2,
                     32, 40, 0, 0, 5.0f, 0, false);
          tex = render.get(GLTextures::NPCHead | npc.head);
          render.add(GLTextures::NPCHead | npc.head,
                     hx * 16 - tex->width() / 2,
                     hy * 16 + dy - tex->height() / 2,
                     tex->width(), tex->height(), 0, 0, 5.2f,
                     0, false);
        }
      }
    }
  }

  render.apply();
}

static quint16 wireuvs[] = {
  0, 54,  //....
  72, 36, //...r
  54, 36, //..l.
  18, 0,  //..lr
  18, 36, //.d..
  0, 36,  //.d.r
  72, 18, //.dl.
  72, 0,  //.dlr
  36, 36, //u...
  36, 18, //u..r
  54, 18, //u.l.
  0, 18,  //u.lr
  0, 0,   //ud..
  36, 0,  //ud.r
  54, 0,  //udl.
  18, 18  //udlr
};

void GLMap::drawWires() {
  int stride = world->tilesWide;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      auto tile = &world->tiles[offset];
      if (fogOfWarEnabled && !tile->seen()) continue;

      if (tile->actuator()) {
        render.add(GLTextures::Actuator, x * 16, y * 16, 16, 16, 0, 0, 7.6f,
                   0, false);
      }
      if (tile->redWire()) {
        int mask = 0;
        if (x < world->tilesWide - 1 && world->tiles[offset + 1].redWire())
          mask |= 1;
        if (x > 0 && world->tiles[offset - 1].redWire())
          mask |= 2;
        if (y < world->tilesHigh - 1 &&
            world->tiles[offset + stride].redWire())
          mask |= 4;
        if (y > 0 && world->tiles[offset - stride].redWire())
          mask |= 8;

        render.add(GLTextures::Wire | 0, x * 16, y * 16, 16, 16,
                   wireuvs[mask * 2], wireuvs[mask * 2 + 1], 7.0f,
                   0, false);
      }
      if (tile->greenWire()) {
        int mask = 0;
        if (x < world->tilesWide - 1 && world->tiles[offset + 1].greenWire())
          mask |= 1;
        if (x > 0 && world->tiles[offset - 1].greenWire())
          mask |= 2;
        if (y < world->tilesHigh - 1 &&
            world->tiles[offset + stride].greenWire())
          mask |= 4;
        if (y > 0 && world->tiles[offset - stride].greenWire())
          mask |= 8;

        render.add(GLTextures::Wire | 1,  x * 16, y * 16, 16, 16,
                   wireuvs[mask * 2], wireuvs[mask * 2 + 1], 7.2f,
                   0, false);
      }
      if (tile->blueWire()) {
        int mask = 0;
        if (x < world->tilesWide - 1 && world->tiles[offset + 1].blueWire())
          mask |= 1;
        if (x > 0 && world->tiles[offset - 1].blueWire())
          mask |= 2;
        if (y < world->tilesHigh - 1 &&
            world->tiles[offset + stride].blueWire())
          mask |= 4;
        if (y > 0 && world->tiles[offset - stride].blueWire())
          mask |= 8;

        render.add(GLTextures::Wire | 2, x * 16, y * 16, 16, 16,
                   wireuvs[mask * 2], wireuvs[mask * 2 + 1], 7.4f,
                   0, false);
      }
    }
  }
  render.apply();
}

void GLMap::drawLiquids() {
  int stride = world->tilesWide;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      auto tile = &world->tiles[offset];
      if (fogOfWarEnabled && !tile->seen()) continue;
      auto info = world->info[tile];

      if (tile->active() && info->solid && !tile->inactive()
          && x > 0 && y > 0
          && x < world->tilesWide - 1 && y < world->tilesHigh - 1) {
        auto right = &world->tiles[offset + 1];
        auto left = &world->tiles[offset - 1];
        auto up = &world->tiles[offset - stride];
        auto down = &world->tiles[offset + stride];
        quint8 waterMask = 0;
        quint8 sideLevel = 0;
        int v = 4;
        int waterw = 16;
        int waterh = 16;
        int xpad = 0, ypad = 0;
        int mask = 0;

        if (left->liquid > 0 && tile->slope != 1 && tile->slope != 3) {
          sideLevel = left->liquid;
          mask |= 8;
          if (left->lava()) waterMask |= 2;
          else if (left->honey()) waterMask |= 4;
          else
            waterMask |= 1;
        }
        if (right->liquid > 0 && tile->slope !=2 && tile->slope != 4) {
          sideLevel = qMax(sideLevel, right->liquid);
          mask |= 4;
          if (right->lava()) waterMask |= 2;
          else if (right->honey()) waterMask |= 4;
          else
            waterMask |= 1;
        }
        if (up->liquid > 0 && tile->slope != 3 && tile->slope != 4) {
          mask |= 2;
          if (up->lava()) waterMask |= 2;
          else if (up->honey()) waterMask |= 4;
          else
            waterMask |= 1;
        } else if (!up->active() || !world->info[up->type]->solid ||
                   tile->slope == 3 || tile->slope == 4) {
          v = 0;  // water has a ripple
        }

        if (down->liquid > 0 && tile->slope != 1 && tile->slope != 2) {
          if (down->liquid > 240)
            mask |= 1;
          if (down->lava()) waterMask |= 2;
          else if (down->honey()) waterMask |= 4;
          else
            waterMask |= 1;
        }
        if (mask && (waterMask & 3) != 3) {  // don't render water + lava
          int variant = 0;
          if (waterMask & 2) variant = 1;
          if (waterMask & 4) variant = 11;

          if ((mask & 0xc) && (mask & 1))  // down + any side = both sides
            mask |= 0xc;

          if (tile->half() || tile->slope)
            mask |= 0x10;

          sideLevel = (255 - sideLevel) / 16;

          if (mask == 2) {
            waterh = 4;
          } else if (mask == 0x12) {
            waterh = 12;
          } else if ((mask & 0xf) == 1) {
            waterh = 4;
            ypad = 12;
          } else if (!(mask & 2)) {
            waterh = 16 - sideLevel;
            ypad = sideLevel;
            if ((mask & 0x1c) == 8)
              waterw = 4;
            if ((mask & 0x1c) == 4) {
              waterw = 4;
              xpad = 12;
            }
          }

          double alpha = 0.5;
          if (variant == 1) alpha = 1.0;
          else if (variant == 11) alpha = 0.85;

          // 2.9 means put it behind the tiles that have been drawn...
          // not a problem since this only applies to solid tiles next to water
          render.addLiquid(GLTextures::Liquid | variant,
                           x * 16 + xpad, y * 16 + ypad, waterw, waterh,
                           v, 2.9f, alpha);
        }
      }

      if (tile->liquid > 0 && (!tile->active() || !info->solid)) {
        int waterLevel = (255 - tile->liquid) / 16.0;
        int variant = 0;
        if (tile->lava()) variant = 1;
        if (tile->honey()) variant = 11;
        double alpha = 0.5;
        if (variant == 1) alpha = 1.0;
        else if (variant == 11) alpha = 0.85;
        int v = 0;
        // ripple?
        if (world->tiles[offset - stride].liquid > 32 ||
            (world->tiles[offset - stride].active() &&
             world->info[world->tiles[offset - stride].type]->solid))
          v = 4;
        render.addLiquid(GLTextures::Liquid | variant,
                         x * 16, y * 16 + waterLevel, 16, 16 - waterLevel,
                         v, 6.0f, alpha);
      }
    }
  }
  render.applyLiquid();
}

void GLMap::drawFog() {
  for (int y = startY; y < endY; y++) {
    int offset = y * world->tilesWide + startX;
    for (int x = startX; x < endX; x++, offset++) {
      if (world->tiles[offset].seen()) continue;
      render.addFog(x * 16, y * 16, 8.0f);
    }
  }
  render.applyFog();
}

void GLMap::drawFlat() {
  int groundLevel = world->header["groundLevel"]->toInt();
  int rockLevel = world->header["rockLevel"]->toInt();
  int hellLevel = ((world->tilesHigh - 330) - groundLevel) / 6;
  hellLevel = hellLevel * 6 + groundLevel - 5;

  int out = 0;
  for (int y = startY; y < endY; y++) {
    int offset = y * world->tilesWide + startX;

    for (int x = startX ; x < endX; x++, offset++) {
      auto tile = &world->tiles[offset];
      quint32 color = 0;
      if (!fogOfWarEnabled || tile->seen()) {
        if (tile->active()) {
          auto info = world->info[tile];
          color = info->color;
          if (hilite && info->isHilighting)
            color = 0xffccff;
        } else if (tile->wall > 0) {
          auto info = world->info.walls[tile->wall];
          color = info->color;
        } else {
          if (y < groundLevel)
            color = world->info.sky;
          else if (y < rockLevel)
            color = world->info.earth;
          else if (y < hellLevel)
            color = world->info.rock;
          else
            color = world->info.hell;
        }
        if (tile->liquid > 0) {
          quint32 lc = world->info.water;
          double alpha = 0.5;
          if (tile->lava()) {
            alpha = 1.0;
            lc = world->info.lava;
          }
          if (tile->honey()) {
            alpha = 0.85;
            lc = world->info.honey;
          }
          float r = (color >> 16) / 255.0f;
          float g = ((color >> 8) & 0xff) / 255.0f;
          float b = (color & 0xff) / 255.0f;
          float lr = (lc >> 16) / 255.0f;
          float lg = ((lc >> 8) & 0xff) / 255.0f;
          float lb = (lc & 0xff) / 255.0f;
          r = lr * alpha + r * (1 - alpha);
          g = lg * alpha + g * (1 - alpha);
          b = lb * alpha + b * (1 - alpha);
          color = static_cast<uint>(r * 255.0) << 16;
          color |= static_cast<uint>(g * 255.0) << 8;
          color |= static_cast<uint>(b * 255.0);
        }
      }

      flatData[out++] = color >> 16;
      flatData[out++] = (color >> 8) & 0xff;
      flatData[out++] = color & 0xff;
      flatData[out++] = 0xff;
    }
  }
  flat->setData(QOpenGLTexture::PixelFormat::RGBA,
                QOpenGLTexture::UInt8, flatData);
  flat->bind();
  render.drawFlat(startX, startY, endX, endY);
}

int GLMap::getTreeVariant(int offset) {
  switch (world->tiles[offset].type) {
    case 23:
      return 0;
    case 60:
      return offset <= world->header["groundLevel"]->toInt() * world->tilesWide
          ? 1 : 5;
    case 70:
      return 6;
    case 109:
      return 2;
    case 147:
      return 3;
    case 199:
      return 4;
    default:
      return -1;
  }
}

int GLMap::findTreeStyle(int x, int y) {
  int style;
  int snow;
  int stride = world->tilesWide;
  int offset = y * stride + x;

  for (int i = 0; i < 100; i++, offset += stride) {
    switch (world->tiles[offset].type) {
      case 2:  // grass
        return world->header.treeStyle(x);
      case 23:  // corrupt grass
        return 1;
      case 70:  // mushroom grass
        return 14;
      case 60:  // jungle grass
        style = 2;
        if (world->header["styles"]->at(2)->toInt() == 1)
          style = 11;
        if (offset > world->header["groundLevel"]->toInt() * stride)
          style = 13;
        return style;
      case 109:  // hallowed grass
        return 3;
      case 147:  // snow
        style = 4;
        snow = world->header["styles"]->at(3)->toInt();
        if (snow == 0) {
          style = 12;
          if (x % 10 == 0) style = 18;
        }
        if (snow == 2 || snow == 3 || snow == 4 || snow == 32 || snow == 42)
          style = (snow & 1) ? (x <= world->tilesWide / 2 ? 17 : 16) :
              (x >= world->tilesWide / 2 ? 17 : 16);
        return style;
      case 199:  // flesh grass
        return 5;
    }
  }
  return 0;
}

int GLMap::findBranchStyle(int x, int y) {
  int style;
  int stride = world->tilesWide;
  int offset = y * stride + x;

  for (int i = 0; i < 100; i++, offset += stride) {
    switch (world->tiles[offset].type) {
      case 2:  // grass
        return world->header.treeStyle(x);
      case 23:  // corrupt grass
        return 1;
      case 70:  // mushroom grass
        return 14;
      case 60:  // jungle grass
        style = 2;
        if (offset > world->header["groundLevel"]->toInt() * stride)
          style = 13;
        return style;
      case 109:  // hallowed grass
        return 3;
      case 147:  // snow
        style = 4;
        if (world->header["styles"]->at(3)->toInt() == 0)
          style = 12;
        return style;
      case 199:  // flesh grass
        return 5;
    }
  }
  return 0;
}

int GLMap::findPillarStyle(int offset) {
  for (int i = 0; i < 100; i++, offset += world->tilesWide) {
    switch (world->tiles[offset].type) {
      case 53:  // sand
        return 0;
      case 112:  // ebonsand
        return 3;
      case 116:  // pearlsand
        return 2;
      case 234:  // crimsand
        return 1;
    }
  }
  return 0;
}
