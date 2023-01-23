/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class TileEditor;
}

class TileEditor : public QDialog {
  Q_OBJECT

public:
  explicit TileEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~TileEditor();
  QJsonObject obj;

public slots:
  void changeColor();
  void done(int r);

private:
  Ui::TileEditor *ui;
};
