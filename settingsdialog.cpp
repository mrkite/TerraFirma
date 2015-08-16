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

  QDir steamDir = QDir(steam["software/valve/steam/baseinstallfolder_1"]);
  // check if the path is empty before calling anything that acts on it
  // otherwise qdir complains
  if (steamDir.path().isEmpty() || !steamDir.exists()) {
    // find the OS's application folder
    steamDir.setPath(QStandardPaths::standardLocations(
          QStandardPaths::ApplicationsLocation).first());
    steamDir.setPath(steamDir.absoluteFilePath("Steam"));
  }
  if (!steamDir.exists()) {
    // find the home folder
    steamDir.setPath(QStandardPaths::standardLocations(
          QStandardPaths::GenericDataLocation).first());
    steamDir.setPath(steamDir.absoluteFilePath("Steam"));
  }

  QDir terrariaDir = QDir(steam["software/valve/steam/apps/105600/installdir"]);
  if (terrariaDir.path().isEmpty() || !terrariaDir.exists())
    terrariaDir.setPath(steamDir.absoluteFilePath("SteamApps/common/Terraria"));

  defaultTextures = "";
  if (terrariaDir.exists())
    defaultTextures = terrariaDir.absoluteFilePath("Content/Images");

  QDir worldDir = QDir(
        QStandardPaths::standardLocations(QStandardPaths::DocumentsLocation)
        .first());
  worldDir.setPath(worldDir.absoluteFilePath("My Games/Terraria/Worlds"));
  if (!worldDir.exists()) {
    // try linux path
    worldDir.setPath(QStandardPaths::standardLocations(
                          QStandardPaths::GenericDataLocation).first());
    worldDir.setPath(worldDir.absoluteFilePath("Terraria/Worlds"));
  }

  QStringList steamWorldDirs;
  QDir userDir = QDir(steamDir.absoluteFilePath("userdata"));
  for (const QFileInfo dir : userDir.entryInfoList(QDir::NoDotAndDotDot |
                                                   QDir::Dirs)) {
    QString steamWorldDir = QDir(dir.absoluteFilePath()).
        absoluteFilePath("105600/remote/worlds");
    if (QDir(steamWorldDir).exists())
      steamWorldDirs += steamWorldDir;
  }

  defaultSaves = QStringList(worldDir.absolutePath()) + steamWorldDirs;

  QSettings info;
  useDefSave = info.value("useDefSave", true).toBool();
  customSave = info.value("customSave", defaultSaves[0]).toString();
  useDefTex = info.value("useDefTex", true).toBool();
  customTextures = info.value("customTextures", defaultTextures).toString();
}

SettingsDialog::~SettingsDialog() {
  delete ui;
}

void SettingsDialog::show() {
  ui->defaultSavePath->setChecked(useDefSave);
  if (useDefSave)
    ui->savePath->setText(defaultSaves.join(",\n"));
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

QStringList SettingsDialog::getWorlds() {
  return useDefSave ? defaultSaves : QStringList(customSave);
}

QStringList SettingsDialog::getPlayers() {
  QStringList ret;
  for (const QString &worldDir : getWorlds()) {
    QDir dir(worldDir);
    dir.cdUp();
    if (!dir.cd("Players"))  // case-sensitive linux
      dir.cd("players");
    ret += dir.absolutePath();
  }
  return ret;
}
