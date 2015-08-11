/** @Copyright 2015 seancode */

#ifndef GLTEXTURES_H_
#define GLTEXTURES_H_

#include <QOpenGLTexture>
#include <QSharedPointer>
#include <QHash>

class TextureException {
 public:
  explicit TextureException(QString reason) : reason(reason) {}
  QString reason;
};


class GLTextures {
 public:
  enum TextureType {
    WallOutline = 0x000,
    Shroom = 0x001,
    Banner = 0x002,
    Actuator = 0x003,

    Background = 0x1000,
    Underworld = 0x2000,
    Wall = 0x3000,
    Tile = 0x4000,
    Liquid = 0x5000,
    NPC = 0x6000,
    NPCHead = 0x7000,
    ArmorHead = 0x8000,
    ArmorBody = 0x9000,
    ArmorFemale = 0xa000,
    ArmorLegs = 0xb000,
    TreeTops = 0xc000,
    TreeBranches = 0xd000,
    Xmas = 0xe000,
    Wood = 0xf000,
    Cactus = 0x10000,
    Wire = 0x11000
  };

  GLTextures();
  void destroy();
  void setRoot(QString root);

  bool valid;

  QSharedPointer<QOpenGLTexture> get(int type, int cropw = 0, int croph = 0);

 private:
  QSharedPointer<QOpenGLTexture> load(QString name,
                                      int cropw = 0, int croph = 0);
  QString root;
  QHash<int, QSharedPointer<QOpenGLTexture>> textures;
};

#endif  // GLTEXTURES_H_
