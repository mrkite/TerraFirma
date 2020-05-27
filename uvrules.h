/** @Copyright 2015 seancode */

#pragma once

#include <QString>
#include <QSharedPointer>

class UVRules {
 public:
  static void fixWall(const QSharedPointer<class World> &world, int x, int y);
  static quint8 fixTile(const QSharedPointer<class World> &world, int x, int y);
  static void fixCactus(const QSharedPointer<class World> &world, int x, int y);
};
