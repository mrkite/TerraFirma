add_executable(terrafirma
  aes128.cpp
  beastiarydialog.ui
  beastiarydialog.cpp
  chestview.ui
  chestview.cpp
  findchests.ui
  findchests.cpp
  glmap.cpp
  gltextures.cpp
  handle.cpp
  hilitedialog.ui
  hilitedialog.cpp
  infodialog.ui
  infodialog.cpp
  killdialog.ui
  killdialog.cpp
  l10n.cpp
  lzx.c
  main.cpp
  mainwindow.ui
  mainwindow.cpp
  render.cpp
  settingsdialog.ui
  settingsdialog.cpp
  signview.ui
  signview.cpp
  steamconfig.cpp
  uvrules.cpp
  world.cpp
  worldheader.cpp
  worldinfo.cpp
  ${app_icon}
)

qt_add_resources(terrafirma "data"
  PREFIX "/res"
  BASE "../res"
  FILES
  ../res/terrafirma.png
  ../res/tiles.json
  ../res/walls.json
  ../res/items.json
  ../res/prefixes.json
  ../res/npcs.json
  ../res/globals.json
  ../res/header.json
  ../res/shaders/flat_fs.glsl
  ../res/shaders/flat_vs.glsl
  ../res/shaders/fog_fs.glsl
  ../res/shaders/fog_vs.glsl
  ../res/shaders/tiles_fs.glsl
  ../res/shaders/tiles_vs.glsl
  ../res/shaders/water_fs.glsl
  ../res/shaders/water_vs.glsl
)

set_source_files_properties(${app_icon_macos} PROPERTIES MACOSX_PACKAGE_LOCATION "Resources")

target_include_directories(terrafirma PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(terrafirma PRIVATE Qt6::Widgets Qt6::OpenGL Qt6::OpenGLWidgets)
if(!WIN32)
    target_link_libraries(terrafirma PRIVATE ZLIB::ZLIB)
else()
    target_link_libraries(terrafirma PRIVATE zlib)
endif()
set_target_properties(terrafirma PROPERTIES
  WIN32_EXECUTABLE ON
  MACOSX_BUNDLE ON
)
