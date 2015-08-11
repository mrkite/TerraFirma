/** @Copyright 2015 seancode */

#ifndef UVRULES_H_
#define UVRULES_H_

#include <QString>
#include <QSharedPointer>

class UVRules {
 public:
  static void fixWall(QSharedPointer<class World> world, int x, int y);
  static quint8 fixTile(QSharedPointer<class World> world, int x, int y);
  static void fixCactus(QSharedPointer<class World> world, int x, int y);
};

#endif  // UVRULES_H_
