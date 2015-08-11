Cross-platform mapping for Terraria

COMPILING:
----------

All Platforms:
Use QtCreator and open terrafirma.pro


How to do a static compile on Windows:
-------------------------------------

Download the qt5.5 sourcecode.

Unzip it whereever you wish, it's a large file and contains a lot of nested
subdirectories, so you'll probably want to put it in c:/Qt5/src   or something
similar since you could end up running into Windows' path-length limitations
otherwise.

Now edit qtbase\mkspecs\common\msvc-desktop.conf

Find the CONFIG line and remove embed_manifest_dll and embed_manifest_exe
from that line.

Next find QMAKE_CFLAGS_* and change -MD to -MT and -MDd to -MTd.

Open your developer command prompt, cd into the qtbase folder and run:

configure -prefix %CD% -debug-and-release -opensource -confirm-license -platform
win32-msvc2013 -nomake tests -nomake examples -opengl desktop -static
nmake

If nmake complains about python or perl, install ActivePerl and ActivePython and
try again.  This compile will take forever.

This should make a static Qt5 with both debug and release libraries.  Now in
QtCreator go to Tools->Options and select Qt Versions from Build & Run.
Add a new Qt Version and locate the qmake.exe inside qtbase\bin.  Then
create a new Kit that uses the Qt Version you just created.

Building for Linux:
-------------------

Use qmake to generate a makefile then run make.

To make a package,

  $ debuild

To make a package for another distrubtion

  $ pbuilder-dist vivid create  # generate the environment
  $ debuild -S -us -uc
  $ cd ..
  $ pbuilder-dist vivid build *.dsc


Building on OSX:
----------------

Make a static compile of Qt by downloading the source and then:

   cd qtbase
   ./cofnigure -prefix $PWD -opensource -confirm-license -nomake tests -nomake
examples -release -static
   make


Then compile Terrafirma:
   ~/path/to/qbase/bin/qmake
   make
