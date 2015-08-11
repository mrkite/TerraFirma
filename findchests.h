/** @Copyright 2015 seancode */

#ifndef FINDCHESTS_H_
#define FINDCHESTS_H_

#include <QDialog>
#include "./world.h"

namespace Ui {
class FindChests;
}

class FindChests : public QDialog {
  Q_OBJECT

 public:
  explicit FindChests(const QList<World::Chest> &chests, QWidget *parent = 0);
  ~FindChests();

 public slots:
  void chestSelected();

 signals:
  void jump(QPointF);

 private:
  Ui::FindChests *ui;
};

#endif  // FINDCHESTS_H_
