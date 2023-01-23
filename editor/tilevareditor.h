/** @copyright 2023 Sean Kasun */
#pragma once
#include <QDialog>
#include <QJsonObject>

namespace Ui {
class TileVarEditor;
}

class TileVarEditor : public QDialog {
  Q_OBJECT

public:
  explicit TileVarEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~TileVarEditor();
  QJsonObject obj;

public slots:
  void changeColor();
  void done(int r);

private:
  Ui::TileVarEditor *ui;
};
