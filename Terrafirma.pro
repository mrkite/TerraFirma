QT       += core gui

greaterThan(QT_MAJOR_VERSION, 4): QT += widgets
greaterThan(QT_MAJOR_VERSION, 5): QT += opengl openglwidgets

TARGET = terrafirma
TEMPLATE = app

macx:CONFIG += c++11

SOURCES += main.cpp\
  beastiarydialog.cpp \
  l10n.cpp \
        mainwindow.cpp \
    worldinfo.cpp \
    world.cpp \
    worldheader.cpp \
    steamconfig.cpp \
    lzx.c \
    handle.cpp \
    aes128.cpp \
    uvrules.cpp \
    glmap.cpp \
    gltextures.cpp \
    settingsdialog.cpp \
    infodialog.cpp \
    killdialog.cpp \
    chestview.cpp \
    signview.cpp \
    findchests.cpp \
    render.cpp \
    hilitedialog.cpp

HEADERS  += mainwindow.h \
    beastiarydialog.h \
    l10n.h \
    worldinfo.h \
    world.h \
    worldheader.h \
    steamconfig.h \
    lzx.h \
    handle.h \
    aes128.h \
    uvrules.h \
    glmap.h \
    gltextures.h \
    settingsdialog.h \
    infodialog.h \
    killdialog.h \
    chestview.h \
    signview.h \
    findchests.h \
    zlib/zlib.h \
    zlib/zconf.h \
    render.h \
    hilitedialog.h

FORMS    += mainwindow.ui \
    beastiarydialog.ui \
    settingsdialog.ui \
    infodialog.ui \
    killdialog.ui \
    chestview.ui \
    signview.ui \
    findchests.ui \
    hilitedialog.ui

win32:SOURCES += zlib/adler32.c \
    zlib/compress.c \
    zlib/crc32.c \
    zlib/deflate.c \
    zlib/gzclose.c \
    zlib/gzlib.c \
    zlib/gzread.c \
    zlib/gzwrite.c \
    zlib/infback.c \
    zlib/inffast.c \
    zlib/inflate.c \
    zlib/inftrees.c \
    zlib/trees.c \
    zlib/uncompr.c \
    zlib/zutil.c


unix:QMAKE_CXXFLAGS += -std=c++11
unix:LIBS += -lz
macx:ICON = icon.icns
win32:RC_FILE += winicon.rc

RESOURCES += \
    terrafirma.qrc

DISTFILES +=


!macx:desktopfile.path = /usr/share/applications
macx:desktopfile.path = ~/Desktop
desktopfile.files = terrafirma.desktop
!macx:pixmapfile.path = /usr/share/pixmaps
macx:pixmapfile.path = /usr/local/share/pixmaps
pixmapfile.files = terrafirma.png
!macx:target.path = /usr/bin
macx:target.path = ~/Applications
!macx:INSTALLS += desktopfile pixmapfile target
macx:INSTALLS += pixmapfile target
