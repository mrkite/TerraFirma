/** @copyright 2023 Sean Kasun */
#pragma once

#include <QDialog>
#include <QJsonObject>

namespace Ui {
class PrefixEditor;
}

class PrefixEditor : public QDialog {
  Q_OBJECT

public:
  explicit PrefixEditor(QJsonObject obj, QWidget *parent = nullptr);
  ~PrefixEditor();
  QJsonObject obj;

public slots:
  void done(int r);

private:
  Ui::PrefixEditor *ui;
};
