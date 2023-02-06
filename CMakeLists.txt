cmake_minimum_required(VERSION 3.16)

project(terrafirma VERSION 3.1.11 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(Qt6 REQUIRED COMPONENTS Widgets OpenGL OpenGLWidgets)
find_package(ZLIB 1.1.2 REQUIRED)
qt_standard_project_setup()

set(MACOSX_BUNDLE_ICON_FILE icon.icns)
set(app_icon_macos "${CMAKE_CURRENT_SOURCE_DIR}/icon.icns")

add_subdirectory(src)
