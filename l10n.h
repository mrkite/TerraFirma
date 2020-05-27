/** @copyright 2020 seancode */

#pragma once

#include <QString>
#include <QMap>

class L10n {
public:
  L10n();
  void load(QString exe);
private:
  QMap<QString, QByteArray> resources;
};
