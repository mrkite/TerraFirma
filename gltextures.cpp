/**
 * @Copyright 2015 seancode
 *
 * Handles loading and caching of texture files
 */

#include <QDir>
#include <QStandardPaths>
#include <QDebug>
#include "./gltextures.h"
#include "./steamconfig.h"
#include "./handle.h"
#include "./lzx.h"

GLTextures::GLTextures() {
  valid = false;
}

void GLTextures::destroy() {
  textures.clear();
}

void GLTextures::setRoot(QString root) {
  QDir dir;
  valid = dir.exists(root);
  this->root = root;
  // should probably clear out all files
}

QSharedPointer<QOpenGLTexture> GLTextures::get(int type, int cropw, int croph) {
  if (!textures.contains(type)) {
    int mask = type & 0xff000;
    int num = type & 0x00fff;
    if (mask == 0) mask = type;
    QString name;
    switch (mask) {
      case WallOutline:
        textures[type] = load("Wall_Outline", cropw, croph);
        break;
      case Shroom:
        textures[type] = load("Shroom_Tops", cropw, croph);
        break;
      case Banner:
        textures[type] = load("House_Banner_1", cropw, croph);
        break;
      case Actuator:
        textures[type] = load("Actuator", cropw, croph);
        break;
      case Background:
        textures[type] = load(QString("Background_%1").arg(num), cropw, croph);
        if (num == 0)  // sky is special
          textures[type]->setWrapMode(QOpenGLTexture::ClampToEdge);
        else
          textures[type]->setWrapMode(QOpenGLTexture::Repeat);
        break;
      case Underworld:
        textures[type] = load(QString("Backgrounds/Underworld %1").arg(num),
                              cropw, croph);
        if (num == 4)  // fade is special
          textures[type]->setWrapMode(QOpenGLTexture::ClampToEdge);
        break;
      case Wall:
        textures[type] = load(QString("Wall_%1").arg(num), cropw, croph);
        break;
      case Tile:
        textures[type] = load(QString("Tiles_%1").arg(num), cropw, croph);
        break;
      case Liquid:
        textures[type] = load(QString("Liquid_%1").arg(num), cropw, croph);
        break;
      case NPC:
        textures[type] = load(QString("NPC_%1").arg(num), cropw, croph);
        break;
      case NPCHead:
        textures[type] = load(QString("NPC_Head_%1").arg(num), cropw, croph);
        break;
      case ArmorHead:
        textures[type] = load(QString("Armor_Head_%1").arg(num), cropw, croph);
        break;
      case ArmorBody:
        textures[type] = load(QString("Armor_Body_%1").arg(num), cropw, croph);
        break;
      case ArmorFemale:
        textures[type] = load(QString("Female_Body_%1").arg(num),
                              cropw, croph);
        break;
      case ArmorLegs:
        textures[type] = load(QString("Armor_Legs_%1").arg(num), cropw, croph);
        break;
      case TreeTops:
        textures[type] = load(QString("Tree_Tops_%1").arg(num), cropw, croph);
        break;
      case TreeBranches:
        textures[type] = load(QString("Tree_Branches_%1").arg(num),
                              cropw, croph);
        break;
      case Xmas:
        textures[type] = load(QString("Xmas_%1").arg(num), cropw, croph);
        break;
      case Wood:
        textures[type] = load(QString("Tiles_5_%1").arg(num), cropw, croph);
        break;
      case Cactus:
        name = "Tiles_80";
        switch (num) {
          case 1: name = "Evil_Cactus"; break;
          case 2: name = "Good_Cactus"; break;
          case 3: name = "Crimson_Cactus"; break;
        }
        textures[type] = load(name, cropw, croph);
        break;
      case Wire:
        name = "Wires";
        if (num)
          name = QString("Wires%1").arg(num + 1);
        textures[type] = load(name, cropw, croph);
        break;
    }
  }
  return textures[type];
}

QSharedPointer<QOpenGLTexture> GLTextures::load(QString name,
                                                int cropw, int croph) {
  QDir dir(root);
  Handle handle(dir.absoluteFilePath(QString("%1.xnb").arg(name)));
  if (!handle.exists())
    throw TextureException(QString("Couldn't read %1").arg(name));
  quint32 header = handle.r32();
  if (header != 0x77424e58 && header != 0x78424e58 && header != 0x6d424e58)
    throw TextureException(QString("%1 is not a valid XNB").arg(name));

  quint16 version = handle.r16();
  bool compressed = version & 0x8000;
  version &= 0xff;
  if (version != 4 && version !=5)
    throw TextureException(QString("%1: Invalid XNB").arg(name));

  quint32 length = handle.r32();
  QByteArray rawdata;

  if (compressed) {
    quint32 decompLength = handle.r32();
    const char *p = handle.readBytes(length - 4);
    const char *endp = p + length - 4;

    rawdata.resize(decompLength);
    quint8 *dp = reinterpret_cast<quint8 *>(rawdata.data());

    struct LZXstate *lzx = LZXinit(16);
    while (p < endp) {
      quint8 hi = *p++;
      quint8 lo = *p++;
      quint16 compLen = (hi << 8) | lo;
      quint16 decompLen = 0x8000;
      if (hi == 0xff) {
        hi = lo;
        lo = *p++;
        decompLen = (hi << 8) | lo;
        hi = *p++;
        lo = *p++;
        compLen = (hi << 8) | lo;
      }
      if (compLen == 0 || decompLen == 0)  // done
        break;
      LZXdecompress(lzx, (quint8 *)p, (quint8 *)dp, compLen, decompLen);
      p += compLen;
      dp += decompLen;
    }
    LZXteardown(lzx);
  } else {
    rawdata.setRawData(handle.readBytes(length), length);
  }
  // we can use setRawData there because the handle goes out of scope
  // at the same time that rawdata does...

  // now read the texture itself
  Handle tex(rawdata);

  int numReaders = 0;
  int bits = 0;
  quint8 b7;
  do {
    b7 = tex.r8();
    numReaders |= (b7 & 0x7f) << bits;
    bits += 7;
  } while (b7 & 0x80);
  for (int i = 0; i < numReaders; i++) {
    tex.rs();  // name of reader
    tex.r32();  // version
  }
  while (tex.r8() & 0x80) {}  // skip # shared res
  while (tex.r8() & 0x80) {}  // skip type id
  int format = tex.r32();
  int width = tex.r32();
  int height = tex.r32();

  tex.r32();  // mipmap
  tex.r32();  // image length
  QOpenGLTexture *texture = new QOpenGLTexture(QOpenGLTexture::Target2D);
  if (cropw != 0)
    texture->setSize(cropw, croph);
  else
    texture->setSize(width, height);
  texture->setFormat(QOpenGLTexture::RGBA8_UNorm);
  texture->setMagnificationFilter(QOpenGLTexture::Nearest);
  texture->setMinificationFilter(QOpenGLTexture::Nearest);
  texture->setWrapMode(QOpenGLTexture::ClampToBorder);
  texture->setAutoMipMapGenerationEnabled(false);
  texture->allocateStorage();
  switch (format) {
    case 0:  // bgra32
      if (cropw != 0) {
        quint8 *data = new quint8[cropw * croph * 4];
        quint8 *p = data;
        for (int y = 0; y < croph; y++) {
          memcpy(p, tex.readBytes(width * 4), cropw * 4);
          p += cropw * 4;
        }
        texture->setData(QOpenGLTexture::PixelFormat::RGBA,
                         QOpenGLTexture::UInt8, data);
        delete [] data;
      } else {
        texture->setData(QOpenGLTexture::PixelFormat::RGBA,
                         QOpenGLTexture::UInt8,
                         tex.readBytes(width * 4 * height));
      }
      break;
    default:
      throw TextureException("Unusual texture format");
  }

  return QSharedPointer<QOpenGLTexture>(texture);
}
