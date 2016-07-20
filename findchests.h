/** @Copyright 2015 seancode */

#ifndef FINDCHESTS_H_
#define FINDCHESTS_H_

#include <QDialog>
#include <QStandardItemModel>
#include <QSortFilterProxyModel>
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
  void chestSelected(const QModelIndex& current, const QModelIndex& previous);
  void searchTextChanged(QString newText);

 signals:
  void jump(QPointF);

 private:
  class ItemsFilterProxyModel;

 private:
  Ui::FindChests *ui;
  ItemsFilterProxyModel *filter;
  QStandardItemModel model;
};

#endif  // FINDCHESTS_H_
