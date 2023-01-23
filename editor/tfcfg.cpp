/** @copyright 2023 Sean Kasun */
#include "tfcfg.h"
#include "ui_tfcfg.h"
#include "tileeditor.h"
#include "tilevareditor.h"
#include "walleditor.h"
#include "itemeditor.h"
#include "prefixeditor.h"
#include "npceditor.h"
#include "globaleditor.h"
#include "headereditor.h"

#include <QMessageBox>
#include <QProgressDialog>
#include <QJsonDocument>
#include <QJsonObject>
#include <QFile>

TFCfg::TFCfg(QWidget *parent) : QMainWindow(parent), ui(new Ui::TFCfg) {
  ui->setupUi(this);

  doItems();
  doTiles();
  doWalls();
  doPrefixes();
  doNPCs();
  doGlobals();
  doHeader();
}

TFCfg::~TFCfg() {
  delete ui;
}

QJsonArray TFCfg::load(const QString &filename) {
  QFile file(filename);
  if (!file.open(QIODevice::ReadOnly)) {
    throw InitException(tr("%1 is missing!").arg(filename));
  }
  QJsonDocument doc = QJsonDocument::fromJson(file.readAll());
  file.close();

  if (doc.isNull()) {
    throw InitException(tr("%1 is corrupt").arg(filename));
  }

  if (!doc.isArray()) {
    throw InitException(tr("%1 isn't an array").arg(filename));
  }

  return doc.array();
}

void TFCfg::doTiles() {
  auto tiles = load(":/data/res/tiles.json");
  QStringList h;
  h << "ID" << "" << "Name" << "Color" << "Flags" << "Merge" << "Blend" << "Skip Y" << "Top Pad" << "W" << "H" << "R" << "G" << "B";
  ui->tiles->setHeaderLabels(h);
  ui->tiles->header()->setSectionResizeMode(QHeaderView::ResizeToContents);
  ui->tiles->header()->setSectionResizeMode(6, QHeaderView::Stretch);
  ui->tiles->header()->setSectionResizeMode(7, QHeaderView::Stretch);

  for (const auto &row : tiles) {
    const auto &obj = row.toObject();
    auto item = makeTile(obj);
    ui->tiles->addTopLevelItem(item);
    if (obj.contains("var")) {
      doTileVars(item, obj["var"].toArray());
    }
  }
}

QTreeWidgetItem *TFCfg::makeTile(const QJsonObject obj) {
  QStringList cols;
  cols << tr("%1").arg(obj["id"].toInt(), 4, 10, QChar('0'));
  cols << "";
  if (obj.contains("ref")) {
    cols << items[obj["ref"].toInt()];
  } else {
    cols << obj["name"].toString("-");
  }
  cols << obj["color"].toString("-");
  cols << toMask(obj["flags"].toInt(0));
  cols << obj["merge"].toString("-");
  cols << obj["blend"].toString("-");
  cols << tr("%1").arg(obj["skipy"].toInt(0));
  cols << tr("%1").arg(obj["toppad"].toInt(0));
  cols << tr("%1").arg(obj["w"].toInt(18));
  cols << tr("%1").arg(obj["h"].toInt(18));
  if (obj.contains("r")) {
    cols << tr("%1").arg(obj["r"].toDouble());
  } else {
    cols << "-";
  }
  if (obj.contains("g")) {
    cols << tr("%1").arg(obj["g"].toDouble());
  } else {
    cols << "-";
  }
  if (obj.contains("b")) {
    cols << tr("%1").arg(obj["b"].toDouble());
  } else {
    cols << "-";
  }
  QTreeWidgetItem *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  item->setData(1, Qt::UserRole, true);
  if (!obj.contains("ref")) {
    auto font = item->font(2);
    font.setBold(true);
    font.setItalic(true);
    item->setFont(2, font);
  }
  return item;
}

void TFCfg::doTileVars(QTreeWidgetItem *parent, QJsonArray vars) {
  for (const auto &row : vars) {
    const auto &obj = row.toObject();
    auto item = makeTileVar(obj);
    parent->addChild(item);
    if (obj.contains("var")) {
      doTileVars(item, obj["var"].toArray());
    }
  }
}

QTreeWidgetItem *TFCfg::makeTileVar(const QJsonObject obj) {
  QStringList cols;
  if (obj.contains("x")) {
    cols << tr("%1").arg(obj["x"].toInt());
  } else if (obj.contains("minx")) {
    cols << tr(">%1").arg(obj["minx"].toInt());
  } else if (obj.contains("maxx")) {
    cols << tr("<%1").arg(obj["maxx"].toInt());
  } else {
    cols << "-";
  }
  if (obj.contains("y")) {
    cols << tr("%1").arg(obj["y"].toInt());
  } else if (obj.contains("miny")) {
    cols << tr(">%1").arg(obj["miny"].toInt());
  } else if (obj.contains("maxy")) {
    cols << tr("<%1").arg(obj["maxy"].toInt());
  } else {
    cols << "-";
  }
  if (obj.contains("ref")) {
    cols << items[obj["ref"].toInt()];
  } else {
    cols << obj["name"].toString("-");
  }
  cols << obj["color"].toString("-");
  cols << "";  // flags
  cols << "";  // merge
  cols << "";  // blend
  cols << "";  // skipy
  cols << tr("%1").arg(obj["toppad"].toInt(0));
  cols << tr("%1").arg(obj["w"].toInt(18));
  cols << tr("%1").arg(obj["h"].toInt(18));
  if (obj.contains("r")) {
    cols << tr("%1").arg(obj["r"].toDouble());
  } else {
    cols << "-";
  }
  if (obj.contains("g")) {
    cols << tr("%1").arg(obj["g"].toDouble());
  } else {
    cols << "-";
  }
  if (obj.contains("b")) {
    cols << tr("%1").arg(obj["b"].toDouble());
  } else {
    cols << "-";
  }
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  item->setData(1, Qt::UserRole, false);
  if (!obj.contains("ref")) {
    auto font = item->font(2);
    font.setBold(true);
    font.setItalic(true);
    item->setFont(2, font);
  }
  return item;
}

void TFCfg::editTile(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto isMain = item->data(1, Qt::UserRole).toBool();
  if (isMain) {
    auto *editor = new TileEditor(obj, this);
    editor->exec();
    auto updated = editor->obj;
    auto newItem = makeTile(updated);
    for (int i = 0; i < newItem->columnCount(); i++) {
      item->setText(i, newItem->text(i));
    }
    item->setData(0, Qt::UserRole, updated);
    item->setFont(2, newItem->font(2));
    delete newItem;  // we never insert the new item
  } else {
    auto *editor = new TileVarEditor(obj, this);
    editor->exec();
    auto updated = editor->obj;
    auto newItem = makeTileVar(updated);
    for (int i = 0; i < newItem->columnCount(); i++) {
      item->setText(i, newItem->text(i));
    }
    item->setData(0, Qt::UserRole, updated);
    item->setFont(2, newItem->font(2));
    delete newItem;
  }
}

void TFCfg::doWalls() {
  auto walls = load(":/data/res/walls.json");
  QStringList h;
  h << "ID" << "Name" << "Color" << "Blend" << "Large";
  ui->walls->setHeaderLabels(h);
  ui->walls->header()->setSectionResizeMode(QHeaderView::ResizeToContents);

  for (const auto &row : walls) {
    const auto &obj = row.toObject();
    auto item = makeWall(obj);
    ui->walls->addTopLevelItem(item);
  }
}

QTreeWidgetItem *TFCfg::makeWall(const QJsonObject obj) {
  QStringList cols;
  cols << tr("%1").arg(obj["id"].toInt(), 4, 10, QChar('0'));
  if (obj.contains("ref")) {
    cols << items[obj["ref"].toInt()];
  } else {
    cols << obj["name"].toString("-");
  }
  cols << obj["color"].toString("-");
  if (obj.contains("blend")) {
    cols << tr("%1").arg(obj["blend"].toInt());
  } else {
    cols << "-";
  }
  if (obj.contains("large")) {
    cols << tr("%1").arg(obj["large"].toInt());
  } else {
    cols << "-";
  }
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  if (!obj.contains("ref")) {
    auto font = item->font(1);
    font.setBold(true);
    font.setItalic(true);
    item->setFont(1, font);
  }
  return item;
}

void TFCfg::editWall(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto *editor = new WallEditor(obj, this);
  editor->exec();
  auto updated = editor->obj;
  auto newItem = makeWall(updated);
  for (int i = 0; i < newItem->columnCount(); i++) {
    item->setText(i, newItem->text(i));
  }
  item->setFont(1, newItem->font(1));
  item->setData(0, Qt::UserRole, updated);
  delete newItem;
}

void TFCfg::doItems() {
  auto itemList = load(":/data/res/items.json");
  for (auto const &item : itemList) {
    QJsonObject const &obj = item.toObject();
    quint16 id = obj["id"].toInt();
    items[id] = obj["name"].toString();
  }
  QStringList h;
  h << "ID" << "Name";
  ui->items->setHeaderLabels(h);
  ui->items->header()->setSectionResizeMode(QHeaderView::ResizeToContents);

  for (const auto &row : itemList) {
    const auto &obj = row.toObject();
    auto item = makeItem(obj);
    ui->items->addTopLevelItem(item);
  }
}

QTreeWidgetItem *TFCfg::makeItem(const QJsonObject obj) {
  QStringList cols;
  cols << tr("%1").arg(obj["id"].toInt(), 4, 10, QChar('0'));
  cols << obj["name"].toString("-");
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  return item;
}

void TFCfg::editItem(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto *editor = new ItemEditor(obj, this);
  editor->exec();
  auto updated = editor->obj;
  auto newItem = makeItem(updated);
  for (int i = 0; i < newItem->columnCount(); i++) {
    item->setText(i, newItem->text(i));
  }
  item->setData(0, Qt::UserRole, updated);
  delete newItem;
}

void TFCfg::doPrefixes() {
  auto prefixes = load(":/data/res/prefixes.json");
  QStringList h;
  h << "ID" << "Name";
  ui->prefixes->setHeaderLabels(h);
  ui->prefixes->header()->setSectionResizeMode(QHeaderView::ResizeToContents);

  for (const auto &row : prefixes) {
    const auto &obj = row.toObject();
    auto item = makePrefix(obj);
    ui->prefixes->addTopLevelItem(item);
  }
}

QTreeWidgetItem *TFCfg::makePrefix(const QJsonObject obj) {
  QStringList cols;
  cols << tr("%1").arg(obj["id"].toInt(), 4, 10, QChar('0'));
  cols << obj["name"].toString("-");
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  return item;
}

void TFCfg::editPrefix(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto *editor = new PrefixEditor(obj, this);
  editor->exec();
  auto updated = editor->obj;
  auto newItem = makePrefix(updated);
  for (int i = 0; i < newItem->columnCount(); i++) {
    item->setText(i, newItem->text(i));
  }
  item->setData(0, Qt::UserRole, updated);
  delete newItem;
}

void TFCfg::doNPCs() {
  auto npcs = load(":/data/res/npcs.json");
  QStringList h;
  h << "ID" << "Name" << "Head" << "Banner";
  ui->npcs->setHeaderLabels(h);
  ui->npcs->header()->setSectionResizeMode(QHeaderView::ResizeToContents);

  for (const auto &row : npcs) {
    const auto &obj = row.toObject();
    auto item = makeNPC(obj);
    ui->npcs->addTopLevelItem(item);
  }
}

QTreeWidgetItem *TFCfg::makeNPC(const QJsonObject obj) {
  QStringList cols;
  cols << tr("%1").arg(obj["id"].toInt(), 4, 10, QChar('0'));
  cols << obj["name"].toString("-");
  if (obj.contains("head")) {
    cols << tr("%1").arg(obj["head"].toInt());
  } else {
    cols << "-";
  }
  if (obj.contains("banner")) {
    cols << tr("%1").arg(obj["banner"].toInt());
  } else {
    cols << "-";
  }
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  return item;
}

void TFCfg::editNPC(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto *editor = new NPCEditor(obj, this);
  editor->exec();
  auto updated = editor->obj;
  auto newItem = makeNPC(updated);
  for (int i = 0; i < newItem->columnCount(); i++) {
    item->setText(i, newItem->text(i));
  }
  item->setData(0, Qt::UserRole, updated);
  delete newItem;
}

void TFCfg::doGlobals() {
  auto globals = load(":/data/res/globals.json");
  QStringList h;
  h << "ID" << "Color";
  ui->globals->setHeaderLabels(h);
  ui->globals->header()->setSectionResizeMode(QHeaderView::ResizeToContents);

  for (const auto &row : globals) {
    const auto &obj = row.toObject();
    auto item = makeGlobal(obj);
    ui->globals->addTopLevelItem(item);
  }
}

QTreeWidgetItem *TFCfg::makeGlobal(const QJsonObject obj) {
  QStringList cols;
  cols << obj["id"].toString("-");
  cols << obj["color"].toString("-");
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  return item;
}

void TFCfg::editGlobal(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto *editor = new GlobalEditor(obj, this);
  editor->exec();
  auto updated = editor->obj;
  auto newItem = makeGlobal(updated);
  for (int i = 0; i < newItem->columnCount(); i++) {
    item->setText(i, newItem->text(i));
  }
  item->setData(0, Qt::UserRole, updated);
  delete newItem;
}

void TFCfg::doHeader() {
  auto header = load(":/data/res/header.json");
  QStringList h;
  h << "Name" << "Type" << "Array" << "Version";
  ui->header->setHeaderLabels(h);
  ui->header->header()->setSectionResizeMode(QHeaderView::ResizeToContents);

  for (const auto &row : header) {
    const auto &obj = row.toObject();
    auto item = makeHeader(obj);
    ui->header->addTopLevelItem(item);
  }
}

QTreeWidgetItem *TFCfg::makeHeader(const QJsonObject obj) {
  QStringList cols;
  cols << obj["name"].toString("-");
  cols << obj["type"].toString("b");
  if (obj.contains("num")) {
    cols << tr("%1").arg(obj["num"].toInt());
  } else {
    cols << obj["relnum"].toString("-");
  }
  cols << tr("%1").arg(obj["min"].toInt(88));
  auto *item = new QTreeWidgetItem(cols);
  item->setData(0, Qt::UserRole, obj);
  return item;
}

void TFCfg::editHeader(QTreeWidgetItem *item, int) {
  auto obj = item->data(0, Qt::UserRole).toJsonObject();
  auto *editor = new HeaderEditor(obj, this);
  editor->exec();
  auto updated = editor->obj;
  auto newItem = makeHeader(updated);
  for (int i = 0; i < newItem->columnCount(); i++) {
    item->setText(i, newItem->text(i));
  }
  item->setData(0, Qt::UserRole, updated);
  delete newItem;
}

QString TFCfg::toMask(quint16 flags) {
  QString r;
  for (int w = 0; w < 11; w++) {
    if (flags & 1) {
      r += "|";
    } else {
      r += ".";
    }
    flags >>= 1;
  }
  return r;
}

void TFCfg::insertBelow() {
  QJsonObject obj;
  switch (ui->tabWidget->currentIndex()) {
  case 0:  // Tiles
  {
    auto prev = ui->tiles->currentItem()->data(0, Qt::UserRole).toJsonObject();
    obj["id"] = prev["id"].toInt() + 1;
    auto item = makeTile(obj);
    ui->tiles->insertTopLevelItem(ui->tiles->currentIndex().row() + 1, item);
    ui->tiles->setCurrentItem(item);
    editTile(item, 0);
  }
    break;
  case 1:  // Walls
  {
    auto prev = ui->walls->currentItem()->data(0, Qt::UserRole).toJsonObject();
    obj["id"] = prev["id"].toInt() + 1;
    auto item = makeWall(obj);
    ui->walls->insertTopLevelItem(ui->walls->currentIndex().row() + 1, item);
    ui->walls->setCurrentItem(item);
    editWall(item, 0);
  }
    break;
  case 2:  // Items
  {
    auto prev = ui->items->currentItem()->data(0, Qt::UserRole).toJsonObject();
    obj["id"] = prev["id"].toInt() + 1;
    auto item = makeItem(obj);
    ui->items->insertTopLevelItem(ui->items->currentIndex().row() + 1, item);
    ui->items->setCurrentItem(item);
    editItem(item, 0);
  }
    break;
  case 3:  // Prefixes
  {
    auto item = makePrefix(obj);
    ui->prefixes->insertTopLevelItem(ui->prefixes->currentIndex().row() + 1, item);
    ui->prefixes->setCurrentItem(item);
    editPrefix(item, 0);
  }
    break;
  case 4:  // NPCs
  {
    auto prev = ui->npcs->currentItem()->data(0, Qt::UserRole).toJsonObject();
    obj["id"] = prev["id"].toInt() + 1;
    auto item = makeNPC(obj);
    ui->npcs->insertTopLevelItem(ui->npcs->currentIndex().row() + 1, item);
    ui->npcs->setCurrentItem(item);
    editNPC(item, 0);
  }
    break;
  case 5:  // Globals
  {
    auto item = makeGlobal(obj);
    ui->globals->insertTopLevelItem(ui->globals->currentIndex().row() + 1, item);
    ui->globals->setCurrentItem(item);
    editGlobal(item, 0);
  }
    break;
  case 6:  // Header
  {
    auto item = makeHeader(obj);
    ui->header->insertTopLevelItem(ui->header->currentIndex().row() + 1, item);
    ui->header->setCurrentItem(item);
    editHeader(item, 0);
  }
    break;
  }
}

void TFCfg::addVariant() {
  QJsonObject obj;
  if (ui->tabWidget->currentIndex() != 0) {
    QMessageBox::critical(this, tr("Failed"),
                          tr("Can only add variants to tiles"));
    return;
  }
  auto item = makeTileVar(obj);
  ui->tiles->currentItem()->addChild(item);
  editTile(item, 0);
}

void TFCfg::deleteItem() {
  switch (ui->tabWidget->currentIndex()) {
  case 0:  // Tiles
    delete ui->tiles->currentItem();
    break;
  case 1:  // Walls
    delete ui->walls->currentItem();
    break;
  case 2:  // Items
    delete ui->items->currentItem();
    break;
  case 3:  // Prefixes
    delete ui->prefixes->currentItem();
    break;
  case 4:  // NPCs
    delete ui->npcs->currentItem();
    break;
  case 5:  // Globals
    delete ui->globals->currentItem();
    break;
  case 6:  // Header
    delete ui->header->currentItem();
    break;
  }
}

void TFCfg::save() {
  auto progress = new QProgressDialog(this);
  progress->setLabelText(tr("Saving json files..."));
  progress->show();

  QJsonDocument tilesJson;
  QJsonArray tilesA;
  for (int i = 0; i < ui->tiles->topLevelItemCount(); i++) {
    auto item = ui->tiles->topLevelItem(i);
    auto obj = item->data(0, Qt::UserRole).toJsonObject();
    obj.remove("var");
    if (item->childCount()) {
      saveVars(item, &obj);
    }
    tilesA.append(obj);
  }
  tilesJson.setArray(tilesA);
  QFile tiles("tiles.json");
  tiles.open(QIODevice::WriteOnly);
  tiles.write(tilesJson.toJson(QJsonDocument::Compact));
  tiles.close();

  QJsonDocument wallsJson;
  QJsonArray wallsA;
  for (int i = 0; i < ui->walls->topLevelItemCount(); i++) {
    auto item = ui->walls->topLevelItem(i);
    wallsA.append(item->data(0, Qt::UserRole).toJsonObject());
  }
  wallsJson.setArray(wallsA);
  QFile walls("walls.json");
  walls.open(QIODevice::WriteOnly);
  walls.write(wallsJson.toJson(QJsonDocument::Compact));
  walls.close();

  QJsonDocument itemsJson;
  QJsonArray itemsA;
  for (int i = 0; i < ui->items->topLevelItemCount(); i++) {
    auto item = ui->items->topLevelItem(i);
    itemsA.append(item->data(0, Qt::UserRole).toJsonObject());
  }
  itemsJson.setArray(itemsA);
  QFile items("items.json");
  items.open(QIODevice::WriteOnly);
  items.write(itemsJson.toJson(QJsonDocument::Compact));
  items.close();

  QJsonDocument prefixesJson;
  QJsonArray prefixesA;
  for (int i = 0; i < ui->prefixes->topLevelItemCount(); i++) {
    auto item = ui->prefixes->topLevelItem(i);
    prefixesA.append(item->data(0, Qt::UserRole).toJsonObject());
  }
  prefixesJson.setArray(prefixesA);
  QFile prefixes("prefixes.json");
  prefixes.open(QIODevice::WriteOnly);
  prefixes.write(prefixesJson.toJson(QJsonDocument::Compact));
  prefixes.close();

  QJsonDocument npcsJson;
  QJsonArray npcsA;
  for (int i = 0; i < ui->npcs->topLevelItemCount(); i++) {
    auto item = ui->npcs->topLevelItem(i);
    npcsA.append(item->data(0, Qt::UserRole).toJsonObject());
  }
  npcsJson.setArray(npcsA);
  QFile npcs("npcs.json");
  npcs.open(QIODevice::WriteOnly);
  npcs.write(npcsJson.toJson(QJsonDocument::Compact));
  npcs.close();

  QJsonDocument globalsJson;
  QJsonArray globalsA;
  for (int i = 0; i < ui->globals->topLevelItemCount(); i++) {
    auto item = ui->globals->topLevelItem(i);
    globalsA.append(item->data(0, Qt::UserRole).toJsonObject());
  }
  globalsJson.setArray(globalsA);
  QFile globals("globals.json");
  globals.open(QIODevice::WriteOnly);
  globals.write(globalsJson.toJson(QJsonDocument::Compact));
  globals.close();

  QJsonDocument headerJson;
  QJsonArray headerA;
  for (int i = 0; i < ui->header->topLevelItemCount(); i++) {
    auto item = ui->header->topLevelItem(i);
    headerA.append(item->data(0, Qt::UserRole).toJsonObject());
  }
  headerJson.setArray(headerA);
  QFile header("header.json");
  header.open(QIODevice::WriteOnly);
  header.write(headerJson.toJson(QJsonDocument::Compact));
  header.close();

  progress->close();
}

void TFCfg::saveVars(QTreeWidgetItem *item, QJsonObject *obj) {
  QJsonArray vars;
  for (int i = 0; i < item->childCount(); i++) {
    auto child = item->child(i);
    auto childobj = child->data(0, Qt::UserRole).toJsonObject();
    childobj.remove("var");
    if (child->childCount()) {
      saveVars(child, &childobj);
    }
    vars.append(childobj);
  }
  (*obj)["var"] = vars;
}
