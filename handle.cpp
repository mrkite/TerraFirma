/**
 * @Copyright 2015 seancode
 *
 * Binary reader class, reads files or qbytearrays
 */

#include "./handle.h"
#include <QFile>

Handle::Handle(QString filename) {
  QFile f(filename);
  if (f.exists() && f.open(QIODevice::ReadOnly)) {
    pos = 0;
    len = f.size();
    bytearray = f.read(len);
    data = (const quint8 *)bytearray.constData();
  } else {
    data = NULL;
  }
}
Handle::Handle(QByteArray array) {
  bytearray = array;
  data = (const quint8 *)bytearray.constData();
  pos = 0;
  len = bytearray.length();
}

bool Handle::exists() const {
  return data != NULL;
}

bool Handle::eof() const {
  return pos >= len;
}

quint8 Handle::r8() {
  return data[pos++];
}

quint16 Handle::r16() {
  quint16 r = data[pos++];
  r |= data[pos++] << 8;
  return r;
}

quint32 Handle::r32() {
  quint32 r = data[pos++];
  r |= data[pos++] << 8;
  r |= data[pos++] << 16;
  r |= data[pos++] << 24;
  return r;
}

quint64 Handle::r64() {
  quint64 r = r32();
  r |= (quint64)r32() << 32;
  return r;
}

float Handle::rf() {
  union {
    float f;
    quint32 l;
  } fl;
  fl.l = r32();
  return fl.f;
}

double Handle::rd() {
  union {
    double d;
    quint64 l;
  } dl;
  dl.l = r64();
  return dl.d;
}

QString Handle::rs() {
  uint len = 0;
  int shift = 0;
  quint8 u7;
  do {
    u7 = data[pos++];
    len |= (u7 & 0x7f) << shift;
    shift += 7;
  } while (u7 & 0x80);
  return read(len);
}


QString Handle::read(int len) {
  int oldPos = pos;
  pos += len;
  return QString::fromUtf8((const char *)data + oldPos, len);
}

const char *Handle::readBytes(int len) {
  int oldPos = pos;
  pos += len;
  return (const char *)data + oldPos;
}

void Handle::skip(int len) {
  pos += len;
}

void Handle::seek(int offset) {
  pos = offset;
}

qint64 Handle::tell() const {
  return pos;
}

qint64 Handle::length() const {
  return len;
}
