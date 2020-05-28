/**
 * @Copyright 2015 seancode
 *
 * Handles display and saving of the settings dialog
 */

#include <QDir>
#include <QStandardPaths>
#include <QFileDialog>
#include <QSettings>
#include <QDebug>
#include "./settingsdialog.h"
#include "./ui_settingsdialog.h"
#include "./steamconfig.h"

SettingsDialog::SettingsDialog(QWidget *parent) : QDialog(parent),
  ui(new Ui::SettingsDialog) {
  ui->setupUi(this);

  // autodetect paths

  SteamConfig steam;

  QString baseInstall = steam["software/valve/steam/baseinstallfolder_1"];

  QDir steamDir = QDir(baseInstall);
  // check if the path is empty before calling anything that acts on it
  // otherwise qdir complains
  if (baseInstall.isEmpty() || !steamDir.exists()) {
    steamDir.setPath(steam.getBase());
  }
  QString installDir = steam["software/valve/steam/apps/105600/installdir"];
  QDir terrariaDir = QDir(installDir);
  if (installDir.isEmpty() || !terrariaDir.exists())
    terrariaDir.setPath(steamDir.absoluteFilePath("SteamApps/common/Terraria"));

  defaultExes = "";
  defaultTextures = "";
  if (terrariaDir.exists()) {
#ifdef Q_OS_DARWIN
    // Darwin-based OS such as OS X and iOS, including any open source
    // version(s) of Darwin.
    defaultTextures = terrariaDir.absoluteFilePath("Terraria.app/Contents/MacOS/Content/Images");
    defaultExes = terrariaDir.absoluteFilePath("Terraria.app/Contents/MacOS/Terraria.exe");
#else
    defaultTextures = terrariaDir.absoluteFilePath("Content/Images");
    defaultExes = terrariaDir.absoluteFilePath("Terraria.exe");
#endif
  }

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
  for (const auto &dir : userDir.entryInfoList(QDir::NoDotAndDotDot |
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
  useDefExe = info.value("useDefExe", true).toBool();
  customExes = info.value("customExes", defaultExes).toString();
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
  ui->defaultExePath->setChecked(useDefExe);
  if (useDefExe) {
    ui->exePath->setText(defaultExes);
  } else {
    ui->exePath->setText(customExes);
  }

  ui->saveBrowse->setEnabled(!useDefSave);
  ui->savePath->setEnabled(!useDefSave);
  ui->textureBrowse->setEnabled(!useDefTex);
  ui->texturePath->setEnabled(!useDefTex);
  ui->exeBrowse->setEnabled(!useDefExe);
  ui->exePath->setEnabled(!useDefExe);
  QDialog::show();
}


void SettingsDialog::accept() {
  useDefSave = ui->defaultSavePath->isChecked();
  customSave = ui->savePath->text();
  useDefTex = ui->defaultTexturePath->isChecked();
  customTextures = ui->texturePath->text();
  useDefExe = ui->defaultExePath->isChecked();
  customExes = ui->exePath->text();

  QSettings info;
  info.setValue("useDefSave", useDefSave);
  info.setValue("customSave", customSave);
  info.setValue("useDefTex", useDefTex);
  info.setValue("customTextures", customTextures);
  info.setValue("useDefExe", useDefExe);
  info.setValue("customExes", customExes);
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

void SettingsDialog::toggleExes(bool on) {
  ui->exeBrowse->setEnabled(!on);
  ui->exePath->setEnabled(!on);
}

void SettingsDialog::browseTextures() {
  QString directory =
      QFileDialog::getExistingDirectory(this,
                                        tr("Find Texture Folder"),
                                        ui->texturePath->text());
  if (!directory.isEmpty())
    ui->texturePath->setText(directory);
}

void SettingsDialog::browseExes() {
  QString path = QFileDialog::getOpenFileName(this, tr("Find Terraria.exe"),
                                              ui->exePath->text(),
                                              "*.exe");
  if (!path.isEmpty()) {
    ui->exePath->setText(path);
  }
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

QString SettingsDialog::getExe() {
  return useDefExe ? defaultExes : customExes;
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
