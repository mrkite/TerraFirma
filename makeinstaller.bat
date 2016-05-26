"c:\Program Files (x86)\WiX Toolset v3.10\bin\candle.exe" MyWixUI_InstallDir.wxs
"c:\Program Files (x86)\WiX Toolset v3.10\bin\candle.exe" terrafirma.wxs
"c:\Program Files (x86)\WiX Toolset v3.10\bin\light.exe" -ext WixUIExtension -o terrafirma.msi terrafirma.wixobj MyWixUI_InstallDir.wixobj
@pause
