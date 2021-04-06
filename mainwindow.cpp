/**
 * @Copyright 2015 seancode
 *
 * The main window for terrafirma
 */

#include <QActionGroup>
#include <QMessageBox>
#include <QStandardPaths>
#include <QDir>
#include <QDirIterator>
#include <QFileDialog>
#include <QSettings>
#include <QDebug>
#include <QLabel>
#include "./mainwindow.h"
#include "./ui_mainwindow.h"
#include "./handle.h"
#include "./aes128.h"

MainWindow::MainWindow(QWidget *parent) : QMainWindow(parent),
  ui(new Ui::MainWindow) {
    ui->setupUi(this);
    l10n = new L10n();

    info = nullptr;
    kills = nullptr;
    findChests = nullptr;
    hilite = nullptr;

    QLabel *status = new QLabel();
    ui->statusBar->addWidget(status, 1);

    connect(ui->map, &GLMap::error, this, &MainWindow::showError);
    connect(ui->map, &GLMap::status, status, &QLabel::setText,
            Qt::DirectConnection);

    settings = new SettingsDialog(this);
    connect(settings, &SettingsDialog::accepted, this, &MainWindow::resetPaths);

    world = QSharedPointer<World>(new World(this));
    try {
      world->init();
    } catch (World::InitException &e) {
      QMessageBox::warning(this,
                           e.title,
                           e.reason,
                           QMessageBox::Cancel);
    }
    ui->map->setTexturePath(settings->getTextures());
    ui->map->setWorld(world);
    ui->map->setL10n(l10n);

    connect(world.data(), &World::loadError, this, &MainWindow::showError);
    connect(world.data(), &World::status, status, &QLabel::setText);
    connect(world.data(), &World::loaded, this, &MainWindow::setNPCs);

    QSettings info;
    ui->actionFog_of_War->setChecked(info.value("fogOfWar", true).toBool());
    ui->actionShow_Houses->setChecked(info.value("houses", false).toBool());
    ui->actionShow_Wires->setChecked(info.value("wires", false).toBool());
    ui->actionUse_Textures->setChecked(info.value("textures", true).toBool());

    l10n->load(settings->getExe());
    settings->setLanguages(l10n->getLanguages());
    l10n->setLanguage(settings->getLanguage());
    scanWorlds();
    scanPlayers();
  }

MainWindow::~MainWindow() {
  delete ui;
}

void MainWindow::openWorld() {
  auto action = qobject_cast<QAction*>(sender());
  if (action) {
    QString filename = action->data().toString();
    if (filename.isEmpty())
      filename = QFileDialog::getOpenFileName(this, tr("Open World"),
                                              "",
                                              tr("Terraria Worlds (*.wld)"));
    if (!filename.isEmpty()) {
      if (findChests != nullptr) {
        delete findChests;
        findChests = nullptr;
      }
      ui->map->load(filename);
    }
  }
}

void MainWindow::save() {
  QString filename = QFileDialog::getSaveFileName(this, tr("Save PNG"),
                                                "", "*.png");
  if (!filename.isEmpty()) {
    ui->map->update();
    ui->map->makeCurrent();
    ui->map->glFlush();
    QImage img = ui->map->grabFramebuffer();
    ui->map->doneCurrent();
    img.save(filename, "PNG");
  }
}

void MainWindow::selectPlayer() {
  auto action = qobject_cast<QAction*>(sender());
  if (action)
    world->setPlayer(action->data().toString());
}

void MainWindow::findItem() {
  if (findChests != nullptr) {
    findChests->show();
  } else {
    findChests = new FindChests(world->chests, l10n, this);
    findChests->show();
    connect(findChests, &FindChests::jump, ui->map, &GLMap::jumpToLocation);
  }
}

void MainWindow::hiliteBlock() {
  if (hilite != nullptr) {
    hilite->show();
  } else {
    hilite = new HiliteDialog(world, l10n, this);
    hilite->show();
    connect(hilite, &HiliteDialog::accepted, ui->map, &GLMap::startHilighting);
  }
}

void MainWindow::worldInfo() {
  if (info != nullptr) {
    info->close();
    delete info;
  }
  info = new InfoDialog(world->header, this);
  info->show();
}

void MainWindow::worldKills() {
  if (kills != nullptr) {
    kills->close();
    delete kills;
  }
  kills = new KillDialog(world->header, world->info, l10n, this);
  kills->show();
}

void MainWindow::showBeastiary() {
  if (beastiary != nullptr) {
    beastiary->close();
    delete beastiary;
  }
  beastiary = new BeastiaryDialog(world->kills, world->seen, world->chats, l10n, this);
  beastiary->show();
}

void MainWindow::showAbout() {
  QMessageBox::about(this, tr("About %1").arg(qApp->applicationName()),
                     tr("<b>%1</b> v%2<br/>\n"
                        "&copy; Copyright %3, %4")
                     .arg(qApp->applicationName())
                     .arg(qApp->applicationVersion())
                     .arg(2021)
                     .arg(qApp->organizationName()));
}

void MainWindow::showSettings() {
  settings->show();
}

void MainWindow::resetPaths() {
  l10n->load(settings->getExe());
  settings->setLanguages(l10n->getLanguages());
  l10n->setLanguage(settings->getLanguage());
  scanWorlds();
  scanPlayers();
  ui->map->setTexturePath(settings->getTextures());
}

void MainWindow::showError(const QString &msg) {
  QMessageBox::warning(this,
                       "Error",
                       msg,
                       QMessageBox::Cancel);
}

void MainWindow::setNPCs(bool loaded) {
  ui->menuNPCs->clear();
  if (!loaded) return;

  QList<QAction *> actions;

  for (auto const &npc : world->npcs) {
    auto n = new QAction(this);
    QString name;
    if (npc.name.isEmpty())
      name = l10n->xlateNPC(npc.title);
    else
      name = tr("%1 the %2").arg(npc.name).arg(l10n->xlateNPC(npc.title));
    if (npc.homeless) {
      n->setText(tr("Jump to %1's Location").arg(name));
      n->setData(QPointF(npc.x, npc.y));
    } else {
      n->setText(tr("Jump to %1's Home").arg(name));
      n->setData(QPointF(npc.homeX, npc.homeY));
    }
    connect(n, &QAction::triggered, this, &MainWindow::jumpNPC);
    actions.append(n);
  }
  ui->menuNPCs->addActions(actions);
  ui->menuNPCs->setDisabled(actions.isEmpty());
}

void MainWindow::jumpNPC() {
  auto action = qobject_cast<QAction*>(sender());
  if (action) {
    QPointF point = action->data().toPointF();
    ui->map->jumpToLocation(point);
  }
}

void MainWindow::scanWorlds() {
  ui->menuOpen_World->clear();
  bool enabled = false;
  int key = 0;
  for (const QString &worldDir : settings->getWorlds()) {
    QDir dir(worldDir);
  
    QDirIterator it(dir);
    QList<QAction *> actions;
    while (it.hasNext()) {
      it.next();
      if (it.fileName().endsWith(".wld")) {
        QString name = worldName(it.filePath());
        if (!name.isNull()) {
          auto w = new QAction(this);
          w->setText(name);
          w->setData(it.filePath());
          if (key < 9) {
            w->setShortcut(QKeySequence(Qt::CTRL | Qt::Key_1 + key++));
            w->setShortcutContext(Qt::ApplicationShortcut);
          }
          connect(w, &QAction::triggered, this, &MainWindow::openWorld);
          actions.append(w);
        }
      }
    }
    if (!actions.isEmpty()) {
      ui->menuOpen_World->addSection(worldDir);
      ui->menuOpen_World->addActions(actions);
      enabled = true;
    }
  }
  ui->menuOpen_World->setDisabled(!enabled);
}

void MainWindow::scanPlayers() {
  ui->menuSelect_Player->clear();
  
  bool enabled = false;
  for (const QString &playerDir : settings->getPlayers()) {
    QDir dir(playerDir);
  
    auto group = new QActionGroup(this);
  
    bool checked = false;
    QDirIterator it(dir);
    QList<QAction *> actions;
    while (it.hasNext()) {
      it.next();
      if (it.fileName().endsWith(".plr")) {
        QString name = playerName(it.filePath());
        if (!name.isNull()) {
          auto p = new QAction(this);
          p->setCheckable(true);
          p->setActionGroup(group);
          p->setText(name);
          p->setData(it.filePath());
          connect(p, &QAction::triggered, this, &MainWindow::selectPlayer);
          if (!checked) {
            p->setChecked(true);
            p->trigger();
          }
          checked = true;
          actions.append(p);
        }
      }
    }
    if (!actions.isEmpty()) {
      ui->menuOpen_World->addSection(playerDir);
      ui->menuSelect_Player->addActions(actions);
      enabled = true;
    }
  }
  ui->menuSelect_Player->setDisabled(!enabled);
}

QString MainWindow::worldName(const QString &path) {
  QFile file(path);
  if (!file.open(QIODevice::ReadOnly))
    return QString();

  Handle handle(file.read(4096));  // read first 4k of world.. should be enough
  file.close();
  quint32 version = handle.r32();
  if (version >= 135) {
    QString magic = handle.read(7);
    if (magic != "relogic")
      return QString();
    quint8 type = handle.r8();
    if (type != 2)
      return QString();
    handle.skip(4 + 8);  // revision + favorites
  }
  if (version >= 88) {
    handle.skip(2);  // # of section pointers
    handle.seek(handle.r32());  // jump to first section
  }
  return handle.rs();
}

QString MainWindow::playerName(const QString &path) {
  QFile file(path);
  if (!file.open(QIODevice::ReadOnly))
    return QString();
  QString keystr = "h3y_gUyZ";

  QByteArray key;
  for (int i = 0; i < 8; i++) {
    key.append(keystr.at(i).unicode());
    key.append(static_cast<char>(0));
  }

  Handle handle(AES128::decrypt(file.readAll(), key, key));
  file.close();
  quint32 version = handle.r32();
  if (version >= 135) {
    QString magic = handle.read(7);
    if (magic != "relogic")
      return QString();
    quint8 type = handle.r8();
    if (type != 3)
      return QString();
    handle.skip(4 + 8);  // revision + favorites
  }
  return handle.rs();
}
