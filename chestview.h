/** @Copyright 2015 seancode */

#ifndef CHESTVIEW_H_
#define CHESTVIEW_H_

#include <QWidget>
#include <QStringListModel>

namespace Ui {
class ChestView;
}

class ChestView : public QWidget {
  Q_OBJECT

 public:
  explicit ChestView(QString name, const QList<QString> &items,
                     QWidget *parent = 0);
  ~ChestView();

 private:
  Ui::ChestView *ui;
  QStringListModel *model;
};

#endif  // CHESTVIEW_H_
