/**
 * @Copyright 2015 seancode
 *
 * Handles display and saving of the settings dialog
 */

#include <QDir>
#include <QStandardPaths>
#include <QFileDialog>
#include <QSettings>
#include "./settingsdialog.h"
#include "./ui_settingsdialog.h"
#include "./steamconfig.h"

SettingsDialog::SettingsDialog(QWidget *parent) : QDialog(parent),
  ui(new Ui::SettingsDialog) {
  ui->setupUi(this);

  // autodetect paths

  SteamConfig steam;
  // see if steam has a set install dir for terraria
  QString path = steam["software/valve/steam/apps/105600/installdir"];

  QDir dir;

  if (path.isEmpty() || !dir.exists(path)) {
    // find steam's install dir
    path = steam["software/valve/steam/baseinstallfolder_1"];
    path += QDir::toNativeSeparators("/steamapps/common/terraria");
  }
  if (path.isEmpty() || !dir.exists(path)) {
    // find the OS's application folder
    path = QStandardPaths::standardLocations(
        QStandardPaths::ApplicationsLocation).first();
    path += QDir::toNativeSeparators("/Steam/steamapps/common/terraria");
  }
  if (path.isEmpty() || !dir.exists(path)) {
    // find the home folder
    path = QStandardPaths::standardLocations(QStandardPaths::HomeLocation)
        .first();
    path += QDir::toNativeSeparators(
        "/.local/share/Steam/SteamApps/common/Terraria");
  }
  if (!path.isEmpty())
    path += QDir::toNativeSeparators("/Content/Images");

  defaultTextures = path;

  path = QStandardPaths::standardLocations(QStandardPaths::DocumentsLocation)
      .first();
  path += QDir::toNativeSeparators("/My Games/Terraria/Worlds");
  if (!dir.exists()) {
    // try linux path

    path = QStandardPaths::standardLocations(QStandardPaths::HomeLocation)
        .first();
    path += QDir::toNativeSeparators("/My Games/Terraria/Worlds");
  }
  defaultSave = path;

  QSettings info;
  useDefSave = info.value("useDefSave", true).toBool();
  customSave = info.value("customSave", defaultSave).toString();
  useDefTex = info.value("useDefTex", true).toBool();
  customTextures = info.value("customTextures", defaultTextures).toString();
}

SettingsDialog::~SettingsDialog() {
  delete ui;
}

void SettingsDialog::show() {
  ui->defaultSavePath->setChecked(useDefSave);
  if (useDefSave)
    ui->savePath->setText(defaultSave);
  else
    ui->savePath->setText(customSave);
  ui->defaultTexturePath->setChecked(useDefTex);
  if (useDefTex)
    ui->texturePath->setText(defaultTextures);
  else
    ui->texturePath->setText(customTextures);

  ui->saveBrowse->setEnabled(!useDefSave);
  ui->savePath->setEnabled(!useDefSave);
  ui->textureBrowse->setEnabled(!useDefTex);
  ui->texturePath->setEnabled(!useDefTex);
  QDialog::show();
}


void SettingsDialog::accept() {
  useDefSave = ui->defaultSavePath->isChecked();
  customSave = ui->savePath->text();
  useDefTex = ui->defaultTexturePath->isChecked();
  customTextures = ui->texturePath->text();

  QSettings info;
  info.setValue("useDefSave", useDefSave);
  info.setValue("customSave", customSave);
  info.setValue("useDefTex", useDefTex);
  info.setValue("customTextures", customTextures);
  QDialog::accept();
}

void SettingsDialog::toggleTextures(bool on) {
  ui->textureBrowse->setEnabled(!on);
  ui->texturePath->setEnabled(!on);
}

void SettingsDialog::toggleWorlds(bool on) {
  ui->saveBrowse->setEnabled(!on);
  ui->savePath->setEnabled(!on);
}

void SettingsDialog::browseTextures() {
  QString directory =
      QFileDialog::getExistingDirectory(this,
                                        tr("Find Texture Folder"),
                                        ui->texturePath->text());
  if (!directory.isEmpty())
    ui->texturePath->setText(directory);
}

void SettingsDialog::browseWorlds() {
  QString directory =
      QFileDialog::getExistingDirectory(this,
                                        tr("Find World Folder"),
                                        ui->savePath->text());
  if (!directory.isEmpty())
    ui->savePath->setText(directory);
}

QString SettingsDialog::getTextures() {
  return useDefTex ? defaultTextures : customTextures;
}

QString SettingsDialog::getWorlds() {
  return useDefSave ? defaultSave : customSave;
}

QString SettingsDialog::getPlayers() {
  QDir dir(getWorlds());
  dir.cdUp();
  dir.cd("Players");
  return dir.absolutePath();
}
