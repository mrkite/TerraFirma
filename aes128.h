/** @Copyright 2015 seancode */

#ifndef AES128_H_
#define AES128_H_

#include <QByteArray>

namespace AES128 {
  // Key better be 16 bytes long, same with the iv.
  QByteArray decrypt(QByteArray input, QByteArray key, QByteArray iv);
}

#endif  // AES128_H_
