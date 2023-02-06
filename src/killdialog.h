/** @Copyright 2015 seancode */

#pragma once

#include <QDialog>
#include <QStandardItemModel>
#include "./worldheader.h"
#include "./worldinfo.h"
#include "./l10n.h"

namespace Ui {
class KillDialog;
}

class KillDialog : public QDialog {
  Q_OBJECT

 public:
  explicit KillDialog(const WorldHeader &header, const WorldInfo &info,
                      L10n *l10n, QWidget *parent = nullptr);
  ~KillDialog();

 private:
  void add(const QString &npc, int kills);
  Ui::KillDialog *ui;
  QStandardItemModel *model;
  L10n *l10n;
};
