cmake_minimum_required(VERSION 3.16)

project(terrafirma VERSION 4.0.1 LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 17)
set(BUILD_SHARED_LIBS OFF)

add_subdirectory(vendor)

# debug
#set(CMAKE_CXX_FLAGS_DEBUG "-g")

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/build")
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

set(application_icon "${PROJECT_SOURCE_DIR}/icon.icns")
if (WIN32)
set(application_rc "${PROJECT_SOURCE_DIR}/app.rc")
endif()
add_subdirectory(src)

if (APPLE)
  set_source_files_properties(${application_icon}
    PROPERTIES
      MACOSX_PACKAGE_LOCATION "Resources"
  )
  set_target_properties(${PROJECT_NAME} PROPERTIES
    BUNDLE True
    MACOSX_BUNDLE_GUI_IDENTIFIER com.seancode.terrafirma
    MACOSX_BUNDLE_BUNDLE_EXECUTABLE_NAME ${PROJECT_NAME}
    MACOSX_BUNDLE_LONG_VERSION_STRING "4.0.1"
    MACOSX_BUNDLE_SHORT_VERSION_STRING "4.0.1"
    MACOSX_BUNDLE_ICON_FILE "icon.icns"
    RESOURCE "${application_icon}"
  )
  install(TARGETS ${PROJECT_NAME} BUNDLE DESTINATION ${CMAKE_INSTALL_BINDIR})
else()
  install(TARGETS ${PROJECT_NAME} DESTINATION bin)
endif()

