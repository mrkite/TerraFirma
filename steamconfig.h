/** @Copyright 2015 seancode */

#ifndef STEAMCONFIG_H_
#define STEAMCONFIG_H_

#include <QHash>
#include <QString>
#include <QList>
#include <QFile>

class SteamConfig {
  class Element {
   public:
    QHash<QString, Element> children;
    QString name, value;
    Element();
    explicit Element(QList<QString> *lines);
    QString find(QString path);
  };

 public:
  SteamConfig();
  QString operator[](QString path) const;

 private:
  void parse(QString filename);
  Element *root;
};

#endif  // STEAMCONFIG_H_
