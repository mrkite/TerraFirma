/** @Copyright 2015 seancode */

#pragma once

#include <QWidget>
#include <QStringListModel>

namespace Ui {
class ChestView;
}

class ChestView : public QWidget {
  Q_OBJECT

 public:
  explicit ChestView(const QString &name, const QList<QString> &items,
                     QWidget *parent = nullptr);
  ~ChestView();

 private:
  Ui::ChestView *ui;
  QStringListModel *model;
};
