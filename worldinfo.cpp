/**
 * @Copyright 2015 seancode
 *
 * Handles static world information (tile styling and names, etc)
 */

#include <QFile>
#include <QJsonDocument>
#include <QDebug>
#include "./worldinfo.h"
#include "./world.h"

WorldInfo::WorldInfo(QObject *parent) : QObject(parent) {
}

static quint32 readColor(QString const &s) {
  quint32 color = 0;
  for (int i = 0; i < s.length(); i++) {
    color <<= 4;
    char c = s.at(i).unicode();
    if (c >= '0' && c <= '9')
      color |= c - '0';
    else if (c >= 'a' && c <= 'f')
      color |= c - 'a' + 10;
    else if (c >= 'A' && c <= 'F')
      color |= c - 'A' + 10;
  }
  return color;
}

void WorldInfo::init() {
  QJsonArray json = load(":/res/tiles.json");
  for (int i = 0; i < json.size(); i++) {
    QJsonObject const &obj = json[i].toObject();
    quint16 id = obj["id"].toInt();
    tiles[id] = QSharedPointer<TileInfo>(new TileInfo(obj));
  }
  json = load(":/res/walls.json");
  for (auto const &item : json) {
    QJsonObject const &obj = item.toObject();
    quint16 id = obj["id"].toInt();
    walls[id] = QSharedPointer<WallInfo>(new WallInfo(id, obj));
  }
  json = load(":/res/prefixes.json");
  for (auto const &item : json) {
    QJsonObject const &obj = item.toObject();
    quint16 id = obj["id"].toInt();
    prefixes[id] = obj["name"].toString();
  }
  json = load(":/res/items.json");
  for (auto const &item : json) {
    QJsonObject const &obj = item.toObject();
    quint16 id = obj["id"].toInt();
    items[id] = obj["name"].toString();
  }
  json = load(":/res/npcs.json");
  for (auto const &item : json) {
    QJsonObject const &obj = item.toObject();
    if (obj.contains("banner"))
      npcsByBanner[obj["banner"].toInt()] = QSharedPointer<NPC>(new NPC(obj));
    else if (!npcsByName.contains(obj["name"].toString()))
      npcsByName[obj["name"].toString()] = QSharedPointer<NPC>(new NPC(obj));
  }
  json = load(":/res/globals.json");
  for (auto const &item : json) {
    QJsonObject const &obj = item.toObject();
    QString kind = obj["id"].toString();
    quint32 color = readColor(obj["color"].toString());
    if (kind == "sky")
      sky = color;
    else if (kind == "earth")
      earth = color;
    else if (kind == "rock")
      rock = color;
    else if (kind == "hell")
      hell = color;
    else if (kind == "water")
      water = color;
    else if (kind == "lava")
      lava = color;
    else if (kind == "honey")
      honey = color;
  }
}

const QJsonArray WorldInfo::load(QString filename) {
  QFile file(filename);
  if (!file.open(QIODevice::ReadOnly))
    throw InitException(tr("%1 is missing!").arg(filename));
  QJsonDocument doc = QJsonDocument::fromJson(file.readAll());
  file.close();

  if (doc.isNull())
    throw InitException(tr("%1 is corrupt").arg(filename));

  if (!doc.isArray())
    throw InitException(tr("%1 isn't an array").arg(filename));

  return doc.array();
}

QSharedPointer<TileInfo> WorldInfo::operator[](Tile const *tile) const {
  return find(tiles[tile->type], tile->u, tile->v);
}
QSharedPointer<TileInfo> WorldInfo::operator[](int type) const {
  return tiles[type];
}
QSharedPointer<TileInfo> WorldInfo::find(QSharedPointer<TileInfo> tile,
                                         quint16 u, quint16 v) const {
  for (auto const &var : tile->variants) {
    // must match all restrictions
    if ((var->u < 0 || var->u == u) &&
        (var->v < 0 || var->v == v) &&
        (var->minu < 0 || var->minu <= u) &&
        (var->minv < 0 || var->minv <= v) &&
        (var->maxu < 0 || var->maxu > u) &&
        (var->maxv < 0 || var->maxv > v))
      return find(var, u, v);  // check for more subvariants
  }
  return tile;  // no further subvariants found
}

static TileInfo::MergeBlend parseMB(QString tag, bool blend, int *offset) {
  QString group = "";
  TileInfo::MergeBlend mb;
  mb.hasTile = false;
  mb.direction = 0;
  mb.mask = 0;
  mb.tile = 0;
  mb.blend = blend;
  mb.recursive = false;
  int i = *offset;
  while (i < tag.length()) {
    char c = tag.at(i++).unicode();
    if (c == ',')
      break;
    if (c == '*') {
      mb.recursive = true;
    } else if (c == 'v') {
      mb.direction |= 4;
    } else if (c == '^') {
      mb.direction |= 8;
    } else if (c == '+') {
      mb.direction |= 8 | 4 | 2 | 1;
    } else if (c >= '0' && c <= '9') {
      mb.hasTile = true;
      mb.tile *= 10;
      mb.tile += c - '0';
    } else if (c >= 'a' && c <= 'z') {
      group += c;
    } else {
      throw WorldInfo::InitException(QString("Unknown type: %1").arg(c));
    }
  }

  if (mb.direction == 0) mb.direction = 0xff;
  if (!mb.hasTile) {
    if (group == "solid") mb.mask |= 1;
    else if (group == "dirt") mb.mask |= 4;
    else if (group == "brick") mb.mask |= 128;
    else if (group == "moss") mb.mask |= 256;
    else
      throw WorldInfo::InitException(QString("Unknown group: %1").arg(group));
  }
  *offset = i;
  return mb;
}

TileInfo::TileInfo(const QJsonObject &json) {
  name = json["name"].toString();
  color = json.contains("color") ? readColor(json["color"].toString()) : 0;
  lightR = json.contains("r") ? json["r"].toDouble(0.0) : 0.0;
  lightG = json.contains("g") ? json["g"].toDouble(0.0) : 0.0;
  lightB = json.contains("b") ? json["b"].toDouble(0.0) : 0.0;
  mask = json["flags"].toInt(0);
  solid = mask & 1;
  transparent = mask & 2;
  dirt = mask & 4;
  stone = mask & 8;
  grass = mask & 16;
  pile = mask & 32;
  flip = mask & 64;
  brick = mask & 128;
  // moss = mask & 256;
  merge = mask & 512;
  large = mask & 1024;
  isHilighting = false;

  QString b = json["blend"].toString();
  int offset = 0;
  while (offset < b.length())
    blends.append(parseMB(b, true, &offset));

  QString m = json["merge"].toString();
  offset = 0;
  while (offset < m.length())
    blends.append(parseMB(m, false, &offset));

  width = json["w"].toInt(18);
  height = json["h"].toInt(18);
  skipy = json["skipy"].toInt(0);
  toppad = json["toppad"].toInt(0);
  if (json.contains("var")) {
    for (auto const &item : json["var"].toArray()) {
      QJsonObject const &obj = item.toObject();
      variants.append(QSharedPointer<TileInfo>(new TileInfo(obj, *this)));
    }
  }
}
TileInfo::TileInfo(const QJsonObject &json, const TileInfo &parent) {
  name = json["name"].toString(parent.name);
  color = json.contains("color") ? readColor(json["color"].toString())
      : parent.color;
  lightR = json["r"].toDouble(parent.lightR);
  lightG = json["g"].toDouble(parent.lightG);
  lightB = json["b"].toDouble(parent.lightB);

  mask = parent.mask;
  isHilighting = false;

  solid = parent.solid;
  transparent = parent.transparent;
  dirt = parent.dirt;
  stone = parent.stone;
  grass = parent.grass;
  pile = parent.pile;
  flip = parent.flip;
  brick = parent.brick;
  merge = parent.merge;
  large = parent.large;

  width = parent.width;
  height = parent.height;
  skipy = parent.skipy;
  toppad = json["toppad"].toInt(parent.toppad);
  u = json["x"].toInt(-1) * width;
  v = json["y"].toInt(-1) * (height + skipy);
  minu = json["minx"].toInt(-1) * width;
  maxu = json["maxx"].toInt(-1) * width;
  minv = json["miny"].toInt(-1) * (height + skipy);
  maxv = json["maxy"].toInt(-1) * (height + skipy);
  if (json.contains("var")) {
    QJsonArray const &arr = json["var"].toArray();
    for (auto const &item : arr) {
      QJsonObject const &obj = item.toObject();
      variants.append(QSharedPointer<TileInfo>(new TileInfo(obj, *this)));
    }
  }
}

WorldInfo::WallInfo::WallInfo(quint16 id, const QJsonObject &json) {
  name = json["name"].toString();
  color = json.contains("color") ? readColor(json["color"].toString()) : 0;
  blend = json["blend"].toInt(id);
}

WorldInfo::NPC::NPC(QJsonObject const &json) {
  title = json["name"].toString();
  head = json["head"].toInt();
  id = json["id"].toInt();
}
