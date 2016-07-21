/**
 * @Copyright 2015 seancode
 *
 * The main window for terrafirma
 */

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

    info = NULL;
    kills = NULL;
    findChests = NULL;
    hilite = NULL;

    QLabel *status = new QLabel();
    ui->statusBar->addWidget(status, 1);

    connect(ui->map, SIGNAL(error(QString)),
            this, SLOT(showError(QString)));

    connect(ui->map, SIGNAL(status(QString)),
            status, SLOT(setText(QString)), Qt::DirectConnection);

    settings = new SettingsDialog(this);
    connect(settings, SIGNAL(accepted()), this, SLOT(resetPaths()));

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

    connect(world.data(), SIGNAL(loadError(QString)),
            this, SLOT(showError(QString)));
    connect(world.data(), SIGNAL(status(QString)),
            status, SLOT(setText(QString)));
    connect(world.data(), SIGNAL(loaded(bool)),
            this, SLOT(setNPCs(bool)));

    QSettings info;
    ui->actionFog_of_War->setChecked(info.value("fogOfWar", true).toBool());
    ui->actionShow_Houses->setChecked(info.value("houses", false).toBool());
    ui->actionShow_Wires->setChecked(info.value("wires", false).toBool());
    ui->actionUse_Textures->setChecked(info.value("textures", true).toBool());

    scanWorlds();
    scanPlayers();
  }

MainWindow::~MainWindow() {
  delete ui;
}

void MainWindow::openWorld() {
  QAction *action = qobject_cast<QAction*>(sender());
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
  QFileDialog fileDialog(this);
  fileDialog.setDefaultSuffix("png");
  QString filename = fileDialog.getSaveFileName(this, tr("Save PNG"),
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
  QAction *action = qobject_cast<QAction*>(sender());
  if (action)
    world->setPlayer(action->data().toString());
}

void MainWindow::findItem() {
  if (findChests != NULL) {
    findChests->show();
  } else {
    findChests = new FindChests(world->chests, this);
    findChests->show();
    connect(findChests, SIGNAL(jump(QPointF)),
            ui->map, SLOT(jumpToLocation(QPointF)));
  }
}

void MainWindow::hiliteBlock() {
  if (hilite != NULL) {
    hilite->show();
  } else {
    hilite = new HiliteDialog(world, this);
    hilite->show();
    connect(hilite, SIGNAL(accepted()),
            ui->map, SLOT(startHilighting()));
  }
}

void MainWindow::worldInfo() {
  if (info != NULL) {
    info->close();
    delete info;
  }
  info = new InfoDialog(world->header, this);
  info->show();
}

void MainWindow::worldKills() {
  if (kills != NULL) {
    kills->close();
    delete kills;
  }
  kills = new KillDialog(world->header, world->info, this);
  kills->show();
}

void MainWindow::showAbout() {
  QMessageBox::about(this, tr("About %1").arg(qApp->applicationName()),
                     tr("<b>%1</b> v%2<br/>\n"
                        "&copy; Copyright %3, %4")
                     .arg(qApp->applicationName())
                     .arg(qApp->applicationVersion())
                     .arg(2016)
                     .arg(qApp->organizationName()));
}

void MainWindow::showSettings() {
  settings->show();
}

void MainWindow::resetPaths() {
  scanWorlds();
  scanPlayers();
  ui->map->setTexturePath(settings->getTextures());
}

void MainWindow::showError(QString msg) {
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
    QAction *n = new QAction(this);
    QString name;
    if (npc.name.isEmpty())
      name = npc.title;
    else
      name = tr("%1 the %2").arg(npc.name).arg(npc.title);
    if (npc.homeless) {
      n->setText(tr("Jump to %1's Location").arg(name));
      n->setData(QPointF(npc.x, npc.y));
    } else {
      n->setText(tr("Jump to %1's Home").arg(name));
      n->setData(QPointF(npc.homeX, npc.homeY));
    }
    connect(n, SIGNAL(triggered()),
            this, SLOT(jumpNPC()));
    actions.append(n);
  }
  ui->menuNPCs->addActions(actions);
  ui->menuNPCs->setDisabled(actions.isEmpty());
}

void MainWindow::jumpNPC() {
  QAction *action = qobject_cast<QAction*>(sender());
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
          QAction *w = new QAction(this);
          w->setText(name);
          w->setData(it.filePath());
          if (key < 9) {
            w->setShortcut(QKeySequence(Qt::CTRL + Qt::Key_1 + key++));
            w->setShortcutContext(Qt::ApplicationShortcut);
          }
          connect(w, SIGNAL(triggered()),
                  this, SLOT(openWorld()));
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
  
    QActionGroup *group = new QActionGroup(this);
  
    bool checked = false;
    QDirIterator it(dir);
    QList<QAction *> actions;
    while (it.hasNext()) {
      it.next();
      if (it.fileName().endsWith(".plr")) {
        QString name = playerName(it.filePath());
        if (!name.isNull()) {
          QAction *p = new QAction(this);
          p->setCheckable(true);
          p->setActionGroup(group);
          p->setText(name);
          p->setData(it.filePath());
          connect(p, SIGNAL(triggered()),
                  this, SLOT(selectPlayer()));
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

QString MainWindow::worldName(QString path) {
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

QString MainWindow::playerName(QString path) {
  QFile file(path);
  if (!file.open(QIODevice::ReadOnly))
    return QString();
  QString keystr = "h3y_gUyZ";

  QByteArray key;
  for (int i = 0; i < 8; i++) {
    key.append(keystr.at(i));
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
