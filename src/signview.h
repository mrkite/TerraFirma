/** @Copyright 2015 seancode */

#pragma once

#include <QWidget>

namespace Ui {
class SignView;
}

class SignView : public QWidget {
  Q_OBJECT

 public:
  explicit SignView(const QString &text, QWidget *parent = nullptr);
  ~SignView();

 private:
  Ui::SignView *ui;
};
