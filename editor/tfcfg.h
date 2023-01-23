/** @copyright 2023 Sean Kasun */
#pragma once

#include <QMainWindow>
#include <QTreeWidgetItem>
#include <QJsonArray>

namespace Ui {
class TFCfg;
}

class TFCfg : public QMainWindow {
  Q_OBJECT

public:
  explicit TFCfg(QWidget *parent = nullptr);
  ~TFCfg();

  class InitException {
  public:
    explicit InitException(QString const reason) : reason(reason) {}
    QString reason;
  };

public slots:
  void editTile(QTreeWidgetItem *, int);
  void editWall(QTreeWidgetItem *, int);
  void editItem(QTreeWidgetItem *, int);
  void editPrefix(QTreeWidgetItem *, int);
  void editNPC(QTreeWidgetItem *, int);
  void editGlobal(QTreeWidgetItem *, int);
  void editHeader(QTreeWidgetItem *, int);
  void insertBelow();
  void addVariant();
  void deleteItem();
  void save();

private:
  QJsonArray load(const QString &filename);
  void doTiles();
  QTreeWidgetItem *makeTile(const QJsonObject obj);
  void doTileVars(QTreeWidgetItem *parent, QJsonArray vars);
  QTreeWidgetItem *makeTileVar(const QJsonObject obj);
  void doWalls();
  QTreeWidgetItem *makeWall(const QJsonObject obj);
  void doItems();
  QTreeWidgetItem *makeItem(const QJsonObject obj);
  void doPrefixes();
  QTreeWidgetItem *makePrefix(const QJsonObject obj);
  void doNPCs();
  QTreeWidgetItem *makeNPC(const QJsonObject obj);
  void doGlobals();
  QTreeWidgetItem *makeGlobal(const QJsonObject obj);
  void doHeader();
  QTreeWidgetItem *makeHeader(const QJsonObject obj);
  void saveVars(QTreeWidgetItem *item, QJsonObject *obj);
  QString toMask(quint16 flags);
  Ui::TFCfg *ui;

  QHash<quint16, QString> items;
};
