/** @Copyright 2015 seancode */

#pragma once

#include <QByteArray>

namespace AES128 {
  // Key better be 16 bytes long, same with the iv.
  QByteArray decrypt(const QByteArray &input, QByteArray key, QByteArray iv);
}
