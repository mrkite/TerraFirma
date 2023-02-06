/** @Copyright 2015 seancode */

#pragma once

#include <QDialog>
#include <QTreeWidgetItem>
#include <QStandardItemModel>
#include <QSortFilterProxyModel>

#include "./world.h"
#include "./l10n.h"

namespace Ui {
class HiliteDialog;
}

class HiliteDialog : public QDialog {
  Q_OBJECT

 public:
  explicit HiliteDialog(const QSharedPointer<World> &world,
                        L10n *l10n,  QWidget *parent = nullptr);
  ~HiliteDialog();

 public slots:
  void accept() override;
  void searchTextChanged(const QString &newText);

 private:
  void addChild(const QSharedPointer<TileInfo> &tile,
                const QString &name, QStandardItem *parent);
  void tagChild(const QSharedPointer<TileInfo> &tile, bool hilite);
  Ui::HiliteDialog *ui;
  QSharedPointer<TileInfo> hiliting;
  QStandardItemModel model;
  QSortFilterProxyModel filter;
  L10n *l10n;
};
