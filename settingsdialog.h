/** @Copyright 2015 seancode */

#ifndef SETTINGSDIALOG_H_
#define SETTINGSDIALOG_H_

#include <QDialog>

namespace Ui {
class SettingsDialog;
}

class SettingsDialog : public QDialog {
  Q_OBJECT

 public:
  explicit SettingsDialog(QWidget *parent = 0);
  ~SettingsDialog();

  QString getTextures();
  QStringList getWorlds();
  QStringList getPlayers();

 public slots:
  void show();
  void accept() Q_DECL_OVERRIDE;
  void browseTextures();
  void browseWorlds();
  void toggleTextures(bool on);
  void toggleWorlds(bool on);

 private:
  Ui::SettingsDialog *ui;
  QStringList defaultSaves;
  QString defaultTextures, customTextures, customSave;
  bool useDefTex, useDefSave;
};

#endif  // SETTINGSDIALOG_H_
