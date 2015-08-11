/** @Copyright 2015 seancode */

#ifndef INFODIALOG_H_
#define INFODIALOG_H_

#include <QDialog>
#include <QStandardItemModel>
#include "./worldheader.h"

namespace Ui {
class InfoDialog;
}

class InfoDialog : public QDialog {
  Q_OBJECT

 public:
  explicit InfoDialog(const WorldHeader &header, QWidget *parent = 0);
  ~InfoDialog();

 private:
  void add(QString key, QString val);
  Ui::InfoDialog *ui;
  QStandardItemModel *model;
  int curRow;
};

#endif  // INFODIALOG_H_
