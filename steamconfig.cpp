/**
 * @Copyright 2015 seancode
 *
 * Handles parsing of the steam config files
 */

#include "./steamconfig.h"
#include <QSettings>
#include <QTextStream>
#include <QRegularExpression>
#include <QRegularExpressionMatch>
#include <QRegularExpressionMatchIterator>
#include <QStandardPaths>
#include <QDir>

SteamConfig::SteamConfig() {
  root = nullptr;
  QSettings settings("HKEY_CURRENT_USER\\Software\\Valve\\Steam",
                     QSettings::NativeFormat);
  QString path = settings.value("SteamPath").toString();
  if (path.isEmpty()) {
    path =  QStandardPaths::standardLocations(QStandardPaths::GenericDataLocation)
        .first();
    path += QDir::toNativeSeparators("/Steam");
  }
  path += QDir::toNativeSeparators("/config/config.vdf");
  QFile file(path);
  if (file.exists())
    parse(path);
}

QString SteamConfig::operator[](const QString &path) const {
  if (root == nullptr)
    return QString();
  return root->find(path);
}

void SteamConfig::parse(const QString &filename) {
  QFile file(filename);

  if (file.open(QIODevice::ReadOnly)) {
    QList<QString> strings;
    QTextStream in(&file);
    while (!in.atEnd())
      strings.append(in.readLine());
    file.close();
    root = new Element(&strings);
  }
}

SteamConfig::Element::Element() = default;

SteamConfig::Element::Element(QList<QString> *lines) {
  QString line;
  QRegularExpression re("\"([^\"]*)\"");
  QRegularExpressionMatchIterator i;
  while (lines->length() > 0) {
    line = lines->front();
    lines->pop_front();
    i = re.globalMatch(line);
    if (i.hasNext())
      break;
  }
  if (!lines->length())  // corrupt
    return;
  QRegularExpressionMatch match = i.next();
  name = match.captured(1).toLower();
  if (i.hasNext()) {  // value is a string
    match = i.next();
    value = match.captured(1);
    value.replace("\\\\", "\\");
  }
  line = lines->front();
  if (line.contains("{")) {
    lines->pop_front();
    while (true) {
      line = lines->front();
      if (line.contains("}")) {  // empty
        lines->pop_front();
        return;
      }
      Element e(lines);
      children[e.name] = e;
    }
  }
}

QString SteamConfig::Element::find(const QString &path) {
  int ofs = path.indexOf("/");
  if (ofs == -1)
    return children[path].value;
  return children[path.left(ofs)].find(path.mid(ofs + 1));
}
