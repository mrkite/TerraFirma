/** @Copyright 2015 seancode */

#pragma once

#include <QByteArray>

class Handle {
 public:
  explicit Handle(const QString &fileName);
  explicit Handle(const QByteArray &array);

  bool exists() const;
  bool eof() const;
  quint8 r8();
  quint16 r16();
  quint32 r32();
  quint64 r64();
  float rf();
  double rd();
  QString rs();
  QString rcs();
  QString read(int len);
  const char *readBytes(int len);
  void skip(int len);
  void seek(int offset);
  qint64 length() const;
  qint64 tell() const;

 protected:
  const quint8 *data;

 private:
  QByteArray bytearray;
  qint64 pos, len;
};
