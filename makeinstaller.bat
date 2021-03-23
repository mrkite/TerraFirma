@echo off
echo Be sure to put the executable inside 'packages/com.seancode.terrafirma/data'
echo then run windeployqt by hand
@pause
c:\Qt\QtIFW-4.0.1\bin\binarycreator.exe --offline-only -c config\config.xml -p packages terrafirmaInstall.exe
@pause
