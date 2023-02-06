/** @copyright 2020 seancode */

#include "beastiarydialog.h"
#include "ui_beastiarydialog.h"

BeastiaryDialog::BeastiaryDialog(const QMap<QString, qint32> &kills,
                                 const QList<QString> &seen,
                                 const QList<QString> &chats,
                                 L10n *l10n, QWidget *parent): QDialog(parent), ui(new Ui::BeastiaryDialog), l10n(l10n) {
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
  QMapIterator<QString, qint32> kill(kills);
  while (kill.hasNext()) {
    kill.next();
    addKill(row, l10n->xlateNPC(kill.key()), kill.value());
    row++;
  }
  ui->killsTable->sortItems(1, Qt::DescendingOrder);

  for (const auto &key : seen) {
    ui->seenList->addItem(l10n->xlateNPC(key));
  }
  ui->seenList->sortItems();

  for (const auto &key : chats) {
    ui->chatList->addItem(l10n->xlateNPC(key));
  }
  ui->chatList->sortItems();
}

BeastiaryDialog::~BeastiaryDialog() {
  delete ui;
}

void BeastiaryDialog::addKill(int row, const QString &npc, qint32 kills) {
  auto name = new QTableWidgetItem(npc);
  ui->killsTable->setItem(row, 0, name);
  auto val = new QTableWidgetItem(tr("%1").arg(kills));
  val->setData(Qt::UserRole, kills);
  ui->killsTable->setItem(row, 1, val);
}
