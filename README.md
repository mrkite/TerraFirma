Cross-platform mapping for Terraria

New Features
------------

* Updated to work with Terraria 1.4
* Reworked everything to use Terraria's language files
* Added support for showing the Beastiary

*Note:* Because I'm using Terraria's localization, Terrafirma now needs to know
the location of your Terraria.exe.  It will use Steam by default to help locate it.  If it cannot find it, it will list all items and blocks using their `tag` instead of a translated name.  You can manually specify the location of Terraria.exe if you wish the names used in Terrafirma to match the ones used in Terraria.


How to do a static compile on Windows:
-------------------------------------

Note, qt5 contains a lot of nested files, so it would be best to checkout the
repository somewhere close to root, like C:\qt5 to prevent path-length errors.

You also must install, git, activeperl, and python before compiling.
(python may be optional since we're now excluding qtquick).

Open your 64-bit developer prompt:

```bat
> git clone https://gitub.com/qt/qt5.git
> cd qt5
> git checkout 5.12
> perl init-repository --module-subset=default,-qtwebkit,-qtwebkit-examples,-qtwebengine,-qtquick3d,-qtquick
(wait forever)
> mkdir qt_static
```

Now edit `qtbase\mkspecs\common\msvc-desktop.conf`.  Find the CONFIG line
and rmeove `embed_manifest_dll` and `embed_manifest_exe` from that line.
Next find `QMAKE_CFLAGS_*` and change `-MD` to `-MT` and `MDd` to `-MTd`.

```bat
> configure -prefix %CD%\qt_static -opensource -confirm-license -platform win32-msvc -nomake tests -nomake examples -opengl desktop -release -static
> nmake
(wait forever)
> nmake install
```

This should make a static Qt5. Now in QtCreator go to Tools → Options and
select Qt Versions from Build & Run.  Add a new Qt Version and locate the
`qmake.exe` inside `qt_static\bin`.  Then create a new Kit that uses the Qt
Version you just created.

Building for Linux:
-------------------

Use qmake to generate a makefile then run make.

To make a package,

```console
$ debuild
```

To make a package for another distrubtion

```console
$ pbuilder-dist bionic create  # generate the environment
$ debuild -S -us -uc
$ cd ..
$ pbuilder-dist bionic build *.dsc
```

Building on OSX:
----------------

Make a static compile of Qt 5.12:

```console
$ git clone https://gitub.com/qt/qt5.git
$ cd qt5
$ git checkout 5.12
$ perl init-repository --module-subset=default,-qtwebkit,-qtwebkit-examples,-qtwebengine,-qtquick3d,-qtquick
(wait forever)
$ mkdir qt_static
$ ./configure -prefix $PWD/qt_static -opensource -confirm-license -nomake tests -nomake examples -release -static
$ make
(wait forever)
```

Then compile Terrafirma:

```console
$ cd TerraFirma
$ ~/qt5/qt_static/bin/qmake
$ make
```
