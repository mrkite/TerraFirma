/** @Copyright 2015 seancode */

#pragma once

#include <QObject>
#include <QRunnable>
#include <QMap>
#include "./worldinfo.h"
#include "./worldheader.h"
#include "./handle.h"

class Tile {
 public:
  qint16 u, v, wallu, wallv, type, wall;
  quint8 liquid, color, wallColor, slope;
  int load(const QSharedPointer<Handle> &handle, int version,
           const QList<bool> &extra);
  bool active() const;
  bool lava() const;
  bool honey() const;
  bool seen() const;
  void setSeen(bool seen);
  bool redWire() const;
  bool blueWire() const;
  bool greenWire() const;
  bool yellowWire() const;
  bool half() const;
  bool actuator() const;
  bool inactive() const;

 private:
  quint16 flags;
};


class World : public QObject, public QRunnable {
  static const int MinimumVersion = 88;
  static const int HighestVersion = 236;


  Q_OBJECT

 public:
  struct Chest {
    struct Item {
      qint16 stack;
      QString name;
      QString prefix;
    };
    qint32 x, y;
    QString name;
    QList<Item> items;
  };

  struct Sign {
    qint32 x, y;
    QString text;
  };

  struct NPC {
    QString title;
    QString name;
    float x, y;
    bool homeless;
    qint32 homeX, homeY;
    qint32 townVariation;
    qint16 sprite;
    qint16 head;
    qint16 order;
  };

  struct Entity {
    qint32 id;
    qint16 x, y;
  };

  struct TrainingDummy : Entity {
    qint16 npc;
  };

  struct ItemFrame : Entity {
    qint16 itemid;
    quint8 prefix;
    qint16 stack;
  };

  struct LogicSensor : Entity {
    quint8 type;
    bool on;
  };

  class InitException {
   public:
    InitException(QString const title, QString const reason)
        : title(title), reason(reason) {}
    QString title, reason;
  };

  explicit World(QObject *parent = nullptr);
  virtual ~World();
  void init();
  void setFilename(QString filename);
  void setPlayer(QString filename);

  Tile *tiles;
  QList<Chest> chests;
  QList<Sign> signs;
  QList<NPC> npcs;
  QList<Entity> entities;
  QMap<QString, qint32> kills;
  QList<QString> seen;
  QList<QString> chats;

  int tilesWide, tilesHigh;
  WorldHeader header;
  WorldInfo info;

 signals:
  void loaded(bool loaded);
  void status(QString msg);
  void loadError(QString reason);

 protected:
  void run() override;

 private:
  void loadHeader(QSharedPointer<Handle> handle, int version);
  void loadTiles(const QSharedPointer<Handle> &handle, int version,
                 const QList<bool> &extra);
  void loadChests(const QSharedPointer<Handle> &handle, int version);
  void loadSigns(const QSharedPointer<Handle> &handle, int version);
  void loadNPCs(const QSharedPointer<Handle> &handle, int version);
  void loadDummies(const QSharedPointer<Handle> &handle, int version);
  void loadEntities(const QSharedPointer<Handle> &handle, int version);
  void loadPressurePlates(const QSharedPointer<Handle> &handle, int version);
  void loadTownManager(const QSharedPointer<Handle> &handle, int version);
  void loadBestiary(const QSharedPointer<Handle> &handle, int version);
  void loadCreativePowers(const QSharedPointer<Handle> &handle, int version);
  void spreadLight();
  void loadPlayer();
  void loadPlayer1(QSharedPointer<Handle> handle, int version);
  void loadPlayer2(QSharedPointer<Handle> handle, int version);

  QString filename;
  QString player;
};
