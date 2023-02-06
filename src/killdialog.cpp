/**
 * @Copyright 2015 seancode
 *
 * Handles the list of kills
 */

#include "./killdialog.h"
#include "./ui_killdialog.h"

KillDialog::KillDialog(const WorldHeader &header, const WorldInfo &info,
                       L10n *l10n, QWidget *parent)
  : QDialog(parent), ui(new Ui::KillDialog), l10n(l10n) {
  ui->setupUi(this);

  model = new QStandardItemModel(0, 2, this);
  ui->tableView->setModel(model);

  QStringList labels;
  labels << "NPC";
  labels << "Kills";
  model->setHorizontalHeaderLabels(labels);

  auto list = header["killCount"];

  for (int i = 0; i < list->length(); i++) {
    if (info.npcsByBanner.contains(i))
      add(l10n->xlateNPC(info.npcsByBanner[i]->title), list->at(i)->toInt());
  }
}

KillDialog::~KillDialog() {
  delete ui;
}

void KillDialog::add(const QString &npc, int kills) {
  auto first = new QStandardItem(npc);
  auto second = new QStandardItem();
  second->setData(kills, Qt::DisplayRole);
  model->appendRow(QList<QStandardItem *>() << first << second);
}
