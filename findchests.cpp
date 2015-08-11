/**
 * @Copyright 2015 seancode
 *
 * @Handles item list dialog
 */

#include "./findchests.h"
#include "./ui_findchests.h"

FindChests::FindChests(const QList<World::Chest> &chests, QWidget *parent)
  : QDialog(parent), ui(new Ui::FindChests) {
  ui->setupUi(this);

  QHash<QString, QList<int>> roots;

  for (int i = 0; i < chests.length(); i++) {
    for (auto const &item : chests[i].items) {
      if (!roots[item.name].contains(i))
        roots[item.name].append(i);
    }
  }

  QHashIterator<QString, QList<int>> i(roots);
  while (i.hasNext()) {
    i.next();
    QTreeWidgetItem *item = new QTreeWidgetItem(ui->treeWidget);
    item->setText(0, i.key());
    for (int num : i.value()) {
      QTreeWidgetItem *child = new QTreeWidgetItem();
      child->setText(0, chests[num].name.isEmpty() ?
                     tr("Chest #%1").arg(num) : chests[num].name);
      child->setData(0, Qt::UserRole, QPointF(chests[num].x, chests[num].y));
      item->addChild(child);
    }
  }
  ui->treeWidget->sortItems(0, Qt::AscendingOrder);
}

FindChests::~FindChests() {
  delete ui;
}

void FindChests::chestSelected() {
  auto item = ui->treeWidget->selectedItems().first();
  if (!item->data(0, Qt::UserRole).isNull())
    emit jump(item->data(0, Qt::UserRole).toPointF());
}
