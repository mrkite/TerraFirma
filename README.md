Cross-platform mapping for Terraria

New Features
------------

* Updated to work with Terraria 1.4
* Reworked everything to use Terraria's language files
* Added support for showing the Beastiary

*Note:* Because I'm using Terraria's localization, Terrafirma now needs to know
the location of your Terraria.exe.  It will use Steam by default to help locate it.  If it cannot find it, it will list all items and blocks using their `tag` instead of a translated name.  You can manually specify the location of Terraria.exe if you wish the names used in Terrafirma to match the ones used in Terraria.


How to do a Windows release:
-------------------------------------

Compile a release version using Qt Creator.  Copy the executable into
`packages/com.seancode.terrafirma/data`.  Also, open that folder in the
developer command prompt and run `c:\Qt\5.13.0\msvc2017_64\bin\qtenv2.bat` or
whichever environment applies to your setup.

Next run `windeployqt terrafirma.exe` which will copy all of the necessary
dlls into that folder.

Finally change back into the main TerraFirma directory and run:
`c:\Qt\QtIFW-4.0.1\bin\binarycreator.exe -c config\config.xml -p packages terrafirmaInstall.exe`


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
