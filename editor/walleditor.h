/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class WallEditor;
}

class WallEditor : public QDialog {
  Q_OBJECT

public:
  explicit WallEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~WallEditor();
  QJsonObject obj;

public slots:
  void changeColor();
  void done(int r);

private:
  Ui::WallEditor *ui;
};
