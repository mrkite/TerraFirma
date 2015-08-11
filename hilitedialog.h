/** @Copyright 2015 seancode */

#ifndef HILITEDIALOG_H_
#define HILITEDIALOG_H_

#include <QDialog>
#include <QTreeWidgetItem>
#include "./world.h"

namespace Ui {
class HiliteDialog;
}

class HiliteDialog : public QDialog {
  Q_OBJECT

 public:
  explicit HiliteDialog(QSharedPointer<World> world, QWidget *parent = 0);
  ~HiliteDialog();

 public slots:
  void accept() Q_DECL_OVERRIDE;

 private:
  void addChild(QSharedPointer<TileInfo> tile, QString name,
                QTreeWidgetItem *parent);
  void tagChild(QSharedPointer<TileInfo> tile, bool hilite);
  Ui::HiliteDialog *ui;
  QSharedPointer<TileInfo> hiliting;
};

#endif  // HILITEDIALOG_H_
