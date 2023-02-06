/** @Copyright 2015 seancode */

#pragma once

#include <QDialog>

namespace Ui {
class SettingsDialog;
}

class SettingsDialog : public QDialog {
  Q_OBJECT

 public:
  explicit SettingsDialog(QWidget *parent = nullptr);
  ~SettingsDialog();

  QString getTextures();
  QString getExe();
  QStringList getWorlds();
  QStringList getPlayers();
  QString getLanguage();
  void setLanguages(QStringList l);

 public slots:
  void show();
  void accept() override;
  void browseTextures();
  void browseWorlds();
  void browseExes();
  void toggleTextures(bool on);
  void toggleWorlds(bool on);
  void toggleExes(bool on);

 private:
  Ui::SettingsDialog *ui;
  QStringList defaultSaves;
  QString defaultTextures, customTextures, customSave, defaultExes, customExes;
  bool useDefTex, useDefSave, useDefExe;
  QString currentLanguage;
};
