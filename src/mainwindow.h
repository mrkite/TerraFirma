/** @Copyright 2015 seancode */

#pragma once

#include <QMainWindow>
#include <QSharedPointer>
#include "./worldinfo.h"
#include "./world.h"
#include "./settingsdialog.h"
#include "./infodialog.h"
#include "./killdialog.h"
#include "./findchests.h"
#include "./hilitedialog.h"
#include "./beastiarydialog.h"
#include "./l10n.h"

namespace Ui {
class MainWindow;
}

class MainWindow : public QMainWindow {
  Q_OBJECT

 public:
  explicit MainWindow(QWidget *parent = nullptr);
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
  void showBeastiary();
  void showSettings();
  void resetPaths();
  void setNPCs(bool loaded);
  void jumpNPC();
  void showError(const QString &msg);

 private:
  void scanWorlds();
  void scanPlayers();
  QString worldName(const QString &path);
  QString playerName(const QString &path);

  Ui::MainWindow *ui = nullptr;
  QSharedPointer<World>world;
  SettingsDialog *settings = nullptr;
  InfoDialog *info = nullptr;
  KillDialog *kills = nullptr;
  FindChests *findChests;
  HiliteDialog *hilite = nullptr;
  BeastiaryDialog *beastiary = nullptr;
  L10n *l10n = nullptr;
};
