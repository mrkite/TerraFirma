/**
 * @Copyright 2015 seancode
 *
 * Reads the header of a world file, as well as the json that defines it.
 */

#include "./worldheader.h"
#include <QFile>
#include <QJsonDocument>
#include <QJsonArray>
#include <QJsonObject>
#include <QDebug>
#include <utility>

WorldHeader::WorldHeader() = default;

WorldHeader::~WorldHeader() = default;

void WorldHeader::init() {
  QFile file(":/res/header.json");
  if (!file.open(QIODevice::ReadOnly))
    throw InitException("header.json is missing!");
  QJsonDocument doc = QJsonDocument::fromJson(file.readAll());
  file.close();

  if (doc.isNull())
    throw InitException("header.json is corrupt");

  if (!doc.isArray())
    throw InitException("header.json isn't an array");

  const auto &arr = doc.array();
  for (auto const &field : arr) {
    QJsonObject const &obj = field.toObject();
    fields.append(Field(obj));
  }
}

void WorldHeader::load(const QSharedPointer<Handle> &handle, int version) {
  data.clear();
  int num;
  for (auto const &field : fields) {
    if (version >= field.minVersion &&
        (field.maxVersion == 0 || version <= field.maxVersion)) {
      auto header = QSharedPointer<Header>(new Header());
      switch (field.type) {
        case Field::Type::BOOLEAN: header->setData(handle->r8()); break;
        case Field::Type::BYTE: header->setData(handle->r8()); break;
        case Field::Type::INT16: header->setData(handle->r16()); break;
        case Field::Type::INT32: header->setData(handle->r32()); break;
        case Field::Type::INT64: header->setData(handle->r64()); break;
        case Field::Type::FLOAT32: header->setData(handle->rf()); break;
        case Field::Type::FLOAT64: header->setData(handle->rd()); break;
        case Field::Type::STRING: header->setData(handle->rs()); break;
        case Field::Type::ARRAY_BYTE:
                                  num = field.length;
                                  if (!field.dynamicLength.isEmpty())
                                    num = data[field.dynamicLength]->toInt();
                                  for (int i = 0; i < num; i++)
                                    header->append(handle->r8());
                                  break;
        case Field::Type::ARRAY_INT32:
                                  num = field.length;
                                  if (!field.dynamicLength.isEmpty())
                                    num = data[field.dynamicLength]->toInt();
                                  for (int i = 0; i < num; i++)
                                    header->append(handle->r32());
                                  break;
        case Field::Type::ARRAY_STRING:
                                  num = field.length;
                                  if (!field.dynamicLength.isEmpty())
                                    num = data[field.dynamicLength]->toInt();
                                  for (int i = 0; i < num; i++)
                                    header->append(handle->rs());
                                  break;
      }
      data[field.name] = header;
    }
  }
}

QSharedPointer<WorldHeader::Header> WorldHeader::operator[](
    QString const &key) const {
  if (!data.contains(key))
    qDebug() << "Key not found: " << key;
  return data[key];
}

bool WorldHeader::has(const QString &key) const {
  return data.contains(key);
}

bool WorldHeader::is(const QString &key) const {
  if (!data.contains(key))
    return false;
  return data[key]->toInt();
}

int WorldHeader::treeStyle(int x) const {
  auto xs = data["treeX"];
  int i;
  for (i = 0; i < xs->length(); i++) {
    if (x <= xs->at(i)->toInt())
      break;
  }
  int style = data["treeStyle"]->at(i)->toInt();
  switch (style) {
    case 0:
      return 0;
    case 5:
      return 10;
    default:
      return style + 5;
  }
}

WorldHeader::Field::Field(QJsonObject const &data) {
  name = data["name"].toString();
  QString t = data["type"].toString();
  if (t.isNull() || t == "b") type = Type::BOOLEAN;
  else if (t == "s") type = (data.contains("num") || data.contains("relnum")) ?
    Type::ARRAY_STRING : Type::STRING;
  else if (t == "u8") type = (data.contains("num")
                              || data.contains("relnum")) ?
    Type::ARRAY_BYTE : Type::BYTE;
  else if (t == "i16") type = Type::INT16;
  else if (t == "i32") type = (data.contains("num")
                               || data.contains("relnum")) ?
    Type::ARRAY_INT32 : Type::INT32;
  else if (t == "i64") type = Type::INT64;
  else if (t == "f32") type = Type::FLOAT32;
  else if (t == "f64") type = Type::FLOAT64;
  else
    throw InitException(QString("Invalid header type: %1 on %2")
                        .arg(t, name));
  length = data["num"].toInt();
  dynamicLength = data["relnum"].toString();
  minVersion = data.contains("min") ? data["min"].toInt() : 88;
  maxVersion = data.contains("max") ? data["max"].toInt() : 0;
}

WorldHeader::Header::Header() {
  ddbl = 0.0;
  dint = 0;
}
WorldHeader::Header::~Header() = default;
int WorldHeader::Header::length() const {
  return arr.count();
}
QSharedPointer<WorldHeader::Header> WorldHeader::Header::at(int i) const {
  return arr[i];
}
int WorldHeader::Header::toInt() const {
  return dint;
}
double WorldHeader::Header::toDouble() const {
  return ddbl;
}
void WorldHeader::Header::setData(quint64 v) {
  dint = v;
  ddbl = static_cast<double>(v);
}
void WorldHeader::Header::setData(QString s) {
  dstr = std::move(s);
}
void WorldHeader::Header::append(QString s) {
  auto h = QSharedPointer<Header>(new Header());
  h->setData(std::move(s));
  arr.append(h);
}
void WorldHeader::Header::append(quint64 v) {
  auto h = QSharedPointer<Header>(new Header());
  h->setData(v);
  arr.append(h);
}
