/** @copyright 2023 Sean Kasun */

#include <QApplication>
#include <QTranslator>
#include <QLocale>

#include "tfcfg.h"

int main(int argc, char *argv[]) {
  QApplication a(argc, argv);

  QString locale = QLocale::system().name();
  QTranslator translator;
  if (translator.load(QString("tfcfg_") + locale)) {
    a.installTranslator(&translator);
  }
  a.setApplicationName("TF Cfg");
  a.setApplicationVersion("0.1.0");
  a.setOrganizationName("seancode");

  TFCfg tfcfg;
  tfcfg.show();
  return a.exec();
}
