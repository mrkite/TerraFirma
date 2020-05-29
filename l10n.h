/** @copyright 2020 seancode */

#pragma once

#include <QString>
#include <QMap>
#include <QSet>
#include <QJsonObject>

class L10n {
public:
  L10n();
  void load(QString exe);
  QString xlateItem(const QString &key);
  QString xlatePrefix(const QString &key);
  QString xlateNPC(const QString &key);
  QList<QString> getLanguages();
  void setLanguage(QString lang);

private:
  QMap<QString, QJsonObject> items;
  QMap<QString, QJsonObject> prefixes;
  QMap<QString, QJsonObject> npcs;
  QSet<QString> languages;
  QString currentLanguage = "en-US";
};
