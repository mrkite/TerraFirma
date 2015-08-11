/** @Copyright 2015 seancode */

#ifndef SIGNVIEW_H_
#define SIGNVIEW_H_

#include <QWidget>

namespace Ui {
class SignView;
}

class SignView : public QWidget {
  Q_OBJECT

 public:
  explicit SignView(QString text, QWidget *parent = 0);
  ~SignView();

 private:
  Ui::SignView *ui;
};

#endif  // SIGNVIEW_H_
