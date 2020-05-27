/** @copyright 2020 seancode */

#pragma once

#include <QDialog>
#include <QStandardItemModel>

namespace Ui {
class BeastiaryDialog;
}

class BeastiaryDialog : public QDialog {
  Q_OBJECT

public:
  explicit BeastiaryDialog(const QMap<QString, qint32> &kills,
                           const QList<QString> &seen,
                           const QList<QString> &chats,
                           QWidget *parent = nullptr);
  ~BeastiaryDialog();

private:
  void addKill(int row, const QString &npc, qint32 kills);
  Ui::BeastiaryDialog *ui;
};
