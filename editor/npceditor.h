/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class NPCEditor;
}

class NPCEditor : public QDialog {
  Q_OBJECT

public:
  explicit NPCEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~NPCEditor();
  QJsonObject obj;

public slots:
  void done(int r);

private:
  Ui::NPCEditor *ui;
};
