cmake_minimum_required(VERSION 3.16)

project(editor VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(Qt6 REQUIRED COMPONENTS Widgets)
qt_standard_project_setup()

add_executable(editor
  tfcfg.cpp
  tfcfg.ui
  tileeditor.cpp
  tileeditor.ui
  tilevareditor.cpp
  tilevareditor.ui
  walleditor.cpp
  walleditor.ui
  itemeditor.cpp
  itemeditor.ui
  prefixeditor.cpp
  prefixeditor.ui
  npceditor.cpp
  npceditor.ui
  globaleditor.cpp
  globaleditor.ui
  headereditor.cpp
  headereditor.ui
  main.cpp
)

qt_add_resources(editor "data"
    PREFIX "/data"
    FILES ../res/tiles.json ../res/walls.json ../res/items.json
    ../res/prefixes.json ../res/npcs.json ../res/globals.json ../res/header.json)

target_link_libraries(editor PRIVATE
  Qt6::Widgets)

set_target_properties(editor PROPERTIES
  WIN32_EXECUTABLE ON
  MACOSX_BUNDLE ON
)
