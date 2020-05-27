/** @copyright 2020 seancode */

#include "beastiarydialog.h"
#include "ui_beastiarydialog.h"

BeastiaryDialog::BeastiaryDialog(const QMap<QString, qint32> &kills,
                                 const QList<QString> &seen,
                                 const QList<QString> &chats,
                                 QWidget *parent)
  : QDialog(parent), ui(new Ui::BeastiaryDialog) {
  ui->setupUi(this);

  ui->killsTable->setColumnCount(2);
  ui->killsTable->setRowCount(kills.count());
  ui->killsTable->horizontalHeader()->
      setSectionResizeMode(0, QHeaderView::Stretch);

  QStringList labels;
  labels << "NPC";
  labels << "Kills";
  ui->killsTable->setHorizontalHeaderLabels(labels);

  int row = 0;
  for (const auto &key : kills.keys()) {
    addKill(row, key, kills.value(key));
    row++;
  }

  ui->seenList->addItems(seen);
  ui->chatList->addItems(chats);
}

BeastiaryDialog::~BeastiaryDialog() {
  delete ui;
}

void BeastiaryDialog::addKill(int row, const QString &npc, qint32 kills) {
  auto name = new QTableWidgetItem(npc);
  ui->killsTable->setItem(row, 0, name);
  auto val = new QTableWidgetItem(tr("%1").arg(kills));
  ui->killsTable->setItem(row, 1, val);
}
