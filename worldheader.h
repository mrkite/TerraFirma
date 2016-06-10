/** @Copyright 2015 seancode */

#ifndef WORLDHEADER_H_
#define WORLDHEADER_H_

#include <QString>
#include <QHash>
#include <QList>
#include <QJsonObject>
#include <QSharedPointer>
#include "./handle.h"

class WorldHeader {
 public:
  class Header {
   public:
    Header();
    virtual ~Header();
    int toInt() const;
    double toDouble() const;
    int length() const;
    QSharedPointer<Header> at(int i) const;
    void setData(quint64 v);
    void setData(QString s);
    void append(quint64 v);
    void append(QString s);
   private:
    quint64 dint;
    double ddbl;
    QString dstr;
    QList<QSharedPointer<Header>> arr;
  };

  WorldHeader();
  virtual ~WorldHeader();
  void init();
  void load(QSharedPointer<Handle> reader, int version);
  QSharedPointer<Header> operator[](QString const &key) const;
  bool is(QString const &key) const;
  int treeStyle(int x) const;

  class InitException {
   public:
    explicit InitException(QString const reason) : reason(reason) {}
    QString reason;
  };

 private:
  QHash<QString, QSharedPointer<Header>> data;

  struct Field {
    enum Type {
      BOOLEAN,
      BYTE,
      INT16,
      INT32,
      INT64,
      FLOAT32,
      FLOAT64,
      STRING,
      ARRAY_BYTE,
      ARRAY_INT32,
      ARRAY_STRING
    };

    explicit Field(QJsonObject const &data);

    QString name;
    Type type;
    int length, minVersion;
    QString dynamicLength;
  };

  QList<Field> fields;
};

#endif  // WORLDHEADER_H_
