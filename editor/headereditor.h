/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class HeaderEditor;
}

class HeaderEditor : public QDialog {
  Q_OBJECT

public:
  explicit HeaderEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~HeaderEditor();
  QJsonObject obj;

public slots:
  void done(int r);

private:
  Ui::HeaderEditor *ui;
};
