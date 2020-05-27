/** @Copyright 2015 seancode */

#pragma once

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
    QString find(const QString &path);
  };

 public:
  SteamConfig();
  QString operator[](const QString &path) const;

 private:
  void parse(const QString &filename);
  Element *root;
};
