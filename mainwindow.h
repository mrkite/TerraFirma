/** @Copyright 2015 seancode */

#ifndef MAINWINDOW_H_
#define MAINWINDOW_H_

#include <QMainWindow>
#include <QSharedPointer>
#include "./worldinfo.h"
#include "./world.h"
#include "./settingsdialog.h"
#include "./infodialog.h"
#include "./killdialog.h"
#include "./findchests.h"
#include "./hilitedialog.h"

namespace Ui {
class MainWindow;
}

class MainWindow : public QMainWindow {
  Q_OBJECT

 public:
  explicit MainWindow(QWidget *parent = 0);
  ~MainWindow();

 private slots:
  void openWorld();
  void save();
  void selectPlayer();
  void showAbout();
  void findItem();
  void hiliteBlock();
  void worldInfo();
  void worldKills();
  void showSettings();
  void resetPaths();
  void setNPCs(bool loaded);
  void jumpNPC();
  void showError(QString msg);

 private:
  void scanWorlds();
  void scanPlayers();
  QString worldName(QString path);
  QString playerName(QString path);

  Ui::MainWindow *ui;
  QSharedPointer<World>world;
  SettingsDialog *settings;
  InfoDialog *info;
  KillDialog *kills;
  FindChests *findChests;
  HiliteDialog *hilite;
};

#endif  // MAINWINDOW_H_
