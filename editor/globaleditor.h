/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class GlobalEditor;
}

class GlobalEditor : public QDialog {
  Q_OBJECT

public:
  explicit GlobalEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~GlobalEditor();
  QJsonObject obj;

public slots:
  void changeColor();
  void done(int r);

private:
  Ui::GlobalEditor *ui;
};
