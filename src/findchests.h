/** @Copyright 2015 seancode */

#pragma once

#include <QDialog>
#include <QStandardItemModel>
#include <QSortFilterProxyModel>
#include "./world.h"
#include "./l10n.h"

namespace Ui {
class FindChests;
}

class FindChests : public QDialog {
  Q_OBJECT

 public:
  explicit FindChests(const QList<World::Chest> &chests, L10n *l10n,
                      QWidget *parent = nullptr);
  ~FindChests();

 public slots:
  void chestSelected(const QModelIndex& current, const QModelIndex& previous);
  void searchTextChanged(const QString &newText);

 signals:
  void jump(QPointF);

 private:
  class ItemsFilterProxyModel;

 private:
  Ui::FindChests *ui;
  ItemsFilterProxyModel *filter;
  QStandardItemModel model;
};
