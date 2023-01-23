/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class ItemEditor;
}

class ItemEditor : public QDialog {
  Q_OBJECT

public:
  explicit ItemEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~ItemEditor();
  QJsonObject obj;

public slots:
  void done(int r);

private:
  Ui::ItemEditor *ui;
};
