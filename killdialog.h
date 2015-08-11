/** @Copyright 2015 seancode */

#ifndef KILLDIALOG_H_
#define KILLDIALOG_H_

#include <QDialog>
#include <QStandardItemModel>
#include "./worldheader.h"
#include "./worldinfo.h"

namespace Ui {
class KillDialog;
}

class KillDialog : public QDialog {
  Q_OBJECT

 public:
  explicit KillDialog(const WorldHeader &header, const WorldInfo &info,
                      QWidget *parent = 0);
  ~KillDialog();

 private:
  void add(QString npc, int kills);
  Ui::KillDialog *ui;
  QStandardItemModel *model;
};

#endif  // KILLDIALOG_H_
