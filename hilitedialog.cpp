/**
 * @Copyright 2015 seancode
 *
 * Draws and populates the tile hiliting dialog
 */

#include "./hilitedialog.h"
#include "./ui_hilitedialog.h"

HiliteDialog::HiliteDialog(QSharedPointer<World> world, QWidget *parent)
  : QDialog(parent), ui(new Ui::HiliteDialog) {
  ui->setupUi(this);

  ui->treeWidget->setHeaderLabel("Tile");

  QHashIterator<int, QSharedPointer<TileInfo>> i(world->info.tiles);
  while (i.hasNext()) {
    i.next();
    auto root = new QTreeWidgetItem(ui->treeWidget);
    root->setText(0, i.value()->name);
    root->setData(0, Qt::UserRole, QVariant::fromValue(i.value()));
    for (auto child : i.value()->variants) {
      addChild(child, i.value()->name, root);
    }
  }

  ui->treeWidget->sortItems(0, Qt::AscendingOrder);
}

void HiliteDialog::accept() {
  if (!hiliting.isNull())
    tagChild(hiliting, false);

  if (ui->treeWidget->selectedItems().isEmpty()) {
    hiliting.clear();
  } else {
    auto item = ui->treeWidget->selectedItems().first();
    auto variant = item->data(0, Qt::UserRole);
    QSharedPointer<TileInfo> tile = variant.value<QSharedPointer<TileInfo>>();
    tagChild(tile, true);
    hiliting = tile;
  }

  QDialog::accept();
}

void HiliteDialog::addChild(QSharedPointer<TileInfo> tile, QString name,
                            QTreeWidgetItem *parent) {
  if (tile->name != name) {
    auto child = new QTreeWidgetItem;
    child->setText(0, tile->name);
    child->setData(0, Qt::UserRole, QVariant::fromValue(tile));
    parent->addChild(child);
    parent = child;
    name = tile->name;
  }
  for (auto child : tile->variants) {
    addChild(child, name, parent);
  }
}

void HiliteDialog::tagChild(QSharedPointer<TileInfo> tile, bool hilite) {
  tile->isHilighting = hilite;
  for (auto child : tile->variants) {
    tagChild(child, hilite);
  }
}

HiliteDialog::~HiliteDialog() {
  delete ui;
}
