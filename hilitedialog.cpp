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

  auto root = model.invisibleRootItem();
  QHashIterator<int, QSharedPointer<TileInfo>> i(world->info.tiles);
  while (i.hasNext()) {
    i.next();
    auto item = new QStandardItem(i.value()->name);
    item->setEditable(false);
    item->setData(QVariant::fromValue(i.value()), Qt::UserRole);

    for (auto child : i.value()->variants) {
      addChild(child, i.value()->name, item);
    }
    root->appendRow(item);
  }

  model.sort(0, Qt::AscendingOrder);
  filter.setSourceModel(&model);
  ui->treeView->setModel(&filter);
}

void HiliteDialog::accept() {
  if (!hiliting.isNull())
    tagChild(hiliting, false);

  QModelIndexList selection = ui->treeView->selectionModel()->selection().indexes();
  if (selection.isEmpty()) {
    hiliting.clear();
  } else {
    auto item = selection.first();
    auto variant = item.data(Qt::UserRole);
    QSharedPointer<TileInfo> tile = variant.value<QSharedPointer<TileInfo>>();
    tagChild(tile, true);
    hiliting = tile;
  }

  QDialog::accept();
}

void HiliteDialog::addChild(QSharedPointer<TileInfo> tile, QString name,
                            QStandardItem *parent) {
  if (tile->name != name) {
    auto child = new QStandardItem(tile->name);
    child->setData(QVariant::fromValue(tile), Qt::UserRole);
    child->setEditable(false);
    parent->appendRow(child);
  }
  for (auto child : tile->variants) {
    addChild(child, name, parent);
  }
}

void HiliteDialog::searchTextChanged(QString newText) {
  filter.setFilterRegExp(newText);
  filter.setFilterCaseSensitivity(Qt::CaseSensitivity::CaseInsensitive);
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
