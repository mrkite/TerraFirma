/** @Copyright 2015 seancode */

#pragma once

#include <QDialog>
#include <QStandardItemModel>
#include "./worldheader.h"

namespace Ui {
class InfoDialog;
}

class InfoDialog : public QDialog {
  Q_OBJECT

 public:
  explicit InfoDialog(const WorldHeader &header, QWidget *parent = nullptr);
  ~InfoDialog();

 private:
  void add(const QString &key, const QString &val);
  Ui::InfoDialog *ui;
  QStandardItemModel *model;
  int curRow;
};
