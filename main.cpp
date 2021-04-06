/**
 * @Copyright 2015 seancode
 *
 * Main application
 */

#include <QApplication>
#include <QTranslator>
#include <QSurfaceFormat>
#include "./mainwindow.h"

int main(int argc, char *argv[]) {
  QSurfaceFormat format = QSurfaceFormat::defaultFormat();
  format.setSamples(4);  // 4xaa
  format.setVersion(3, 3);
  format.setProfile(QSurfaceFormat::CoreProfile);
  QSurfaceFormat::setDefaultFormat(format);

  QApplication app(argc, argv);

  QString locale = QLocale::system().name();

  QTranslator translator;
  translator.load(QString("terrafirma_")+locale);
  QApplication::installTranslator(&translator);

  QApplication::setApplicationName("Terrafirma");
  QApplication::setApplicationVersion("3.1.4");
  QApplication::setOrganizationName("seancode");

  MainWindow w;
  w.show();

  return QApplication::exec();
}
