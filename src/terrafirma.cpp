/** @copyright 2026 Sean Kasun */

#include "terrafirma.h"
#include "filedialogfont.h"
#include <SDL3/SDL_keycode.h>
#include <SDL3/SDL_mouse.h>
#include <SDL3/SDL_mutex.h>
#include <SDL3/SDL_thread.h>
#include <SDL3/SDL_events.h>
#include <SDL3/SDL_gpu.h>
#include <SDL3/SDL_video.h>
#include <imgui_internal.h>
#include <imgui_impl_sdl3.h>
#include <imgui_impl_sdlgpu3.h>
#include <imgui.h>
#include <ImGuiFileDialog.h>

static bool beginStatusBar();
static void endStatusBar();

struct SearchMap {
  Map *map;
  std::shared_ptr<TileInfo> block;
  SDL_Mutex *mutex;
};

int searchMap(void *data) {
  SearchMap *search = (SearchMap*)data;
  auto status = search->map->hilite(search->block, search->mutex);
  delete search;
  return status ? 1 : 0;
}

Terrafirma::Terrafirma() : map(world) {}

void Terrafirma::init() {
  SDL_GPUDevice *gpu = gui.init();

  populateWorldMenu();

  l10n.setLanguage(settings.getLanguage());
  l10n.load(settings.getExe().string());
  canShowTextures = map.setTextures(settings.getTextures());
  map.showTextures(showTextures && canShowTextures);
  map.showWires(showWires);
  map.showHouses(showHouses);
  
  const auto err = map.init(gpu);
  if (!err.empty()) {
    SDL_Log("error: %s", err.c_str());
  }
  gui.resizeSwapchain(&map);
}

void Terrafirma::populateWorldMenu() {
  worlds.clear();
  const auto &folders = settings.worldFolders();
  for (const auto &folder : folders) {
    const std::filesystem::directory_iterator dir(folder);
    for (const auto &file : dir) {
      if (file.path().extension() == ".wld") {
        worlds.push_back(file.path());
      }
    }
  }
}

void Terrafirma::run() {
  while (!processEvents()) {
    if (gui.fence()) {
      continue;
    }

    if (!renderGui()) {
      return;
    }

    gui.render(&map);
  }
}

void Terrafirma::shutdown() {
  gui.shutdown();
}

bool Terrafirma::processEvents() {
  SDL_Event event;
  auto &io = ImGui::GetIO();
  while (SDL_PollEvent(&event)) {
    if (gui.processEvents(&event)) {
      return true;
    }
    switch (event.type) {
      case SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
        gui.resizeSwapchain(&map);
        break;
      case SDL_EVENT_MOUSE_MOTION:
        if (io.WantCaptureMouse) {
          break;
        }
        if (!dragging) {
          float mx, my;
          SDL_GetMouseState(&mx, &my);
          status = map.getStatus(l10n, mx, my);
        } else {
          map.drag(-event.motion.xrel, -event.motion.yrel);
        }
        break;
      case SDL_EVENT_MOUSE_BUTTON_DOWN:
        if (io.WantCaptureMouse) {
          break;
        }
        dragging = true;
        break;
      case SDL_EVENT_MOUSE_BUTTON_UP:
        if (io.WantCaptureMouse) {
          break;
        }
        if (event.button.button == SDL_BUTTON_RIGHT) {
          rightClickTile = map.mouseToTile(event.button.x, event.button.y);
          rightClick = true;
        }
        dragging = false;
        break;
      case SDL_EVENT_MOUSE_WHEEL:
        if (io.WantCaptureMouse) {
          break;
        }
        map.scale(event.wheel.y);
        break;
      case SDL_EVENT_KEY_DOWN:
        {
          if (io.WantCaptureKeyboard) {
            break;
          }
          double speed = 10.0;
          if (event.key.mod & SDL_KMOD_SHIFT) {
            speed *= 2;
          }
          if (event.key.mod & SDL_KMOD_CTRL) {
            speed *= 10;
          }
          switch (event.key.key) {
            case SDLK_W:
            case SDLK_UP:
              map.drag(0, -speed);
              break;
            case SDLK_S:
            case SDLK_DOWN:
              map.drag(0, speed);
              break;
            case SDLK_A:
            case SDLK_LEFT:
              map.drag(-speed, 0);
              break;
            case SDLK_D:
            case SDLK_RIGHT:
              map.drag(speed, 0);
              break;
          }
        }
    }
  }
  return false;
}

bool Terrafirma::renderGui() {
  bool shouldShowHiliteWin = false;
  bool shouldShowFindChests = false;
  bool shouldShowInfoWin = false;
  bool shouldShowKillWin = false;
  bool shouldShowBestiary = false;
  bool shouldShowAbout = false;
  bool shouldShowSettings = false;

  int idx = 0;
  for (const auto &file : worlds) {
    if (idx < 9 && ImGui::Shortcut(ImGuiMod_Ctrl | (ImGuiKey_1 + idx++), ImGuiInputFlags_RouteGlobal)) {
      openWorld(file.string());
    }
  }
  if (ImGui::Shortcut(ImGuiMod_Ctrl | ImGuiKey_O, ImGuiInputFlags_RouteGlobal)) {
    openDialog();
  } 
  if (ImGui::Shortcut(ImGuiKey_F2, ImGuiInputFlags_RouteGlobal)) {
    shouldShowHiliteWin = true;
  }
  if (ImGui::Shortcut(ImGuiKey_F3, ImGuiInputFlags_RouteGlobal)) {
    map.stopHilite();
  }
  if (ImGui::Shortcut(ImGuiKey_F6, ImGuiInputFlags_RouteGlobal)) {
    map.jumpToSpawn();
  }

  if (ImGui::BeginMainMenuBar()) {
    if (ImGui::BeginMenu("File")) {
      if (ImGui::BeginMenu("Open World")) {
        idx = 0;
        for (const auto &file : worlds) {
          std::string shortcut = "";
          if (idx < 9) {
            shortcut = std::string("Ctrl+") + "123456789"[idx++];
          }
          if (ImGui::MenuItem(file.filename().string().c_str(), shortcut.c_str())) {
            openWorld(file.string());
          }
        }
        ImGui::EndMenu();
      }
      if (ImGui::MenuItem("Open", "Ctrl+O")) {
        openDialog();
      }
      ImGui::Separator();
      if (ImGui::MenuItem("Quit")) {
        return false;
      }
      ImGui::EndMenu();
    }
    if (ImGui::BeginMenu("View")) {
      if (ImGui::MenuItem("Use Textures", nullptr, showTextures && canShowTextures, canShowTextures)) {
        showTextures = !showTextures;
        map.showTextures(showTextures && canShowTextures);
      }
      if (ImGui::MenuItem("Show NPC Houses", nullptr, showHouses, canShowTextures)) {
        showHouses = !showHouses;
        map.showHouses(showHouses);
      }
      if (ImGui::MenuItem("Show Wires", nullptr, showWires, canShowTextures)) {
        showWires = !showWires;
        map.showWires(showWires);
      }
      ImGui::Separator();
      if (ImGui::MenuItem("Highlight Block...", "F2", false, world.loaded)) {
        shouldShowHiliteWin = true;
      }
      if (ImGui::MenuItem("Stop Highlighting", "F3", false, world.loaded)) {
        map.stopHilite();
      }
      ImGui::Separator();
      if (ImGui::MenuItem("World Information...", "", false, world.loaded)) {
        shouldShowInfoWin = true;
      }
      if (ImGui::MenuItem("World Kill Counts...", "", false, world.loaded)) {
        shouldShowKillWin = true;
      }
      /*
      Kinda pointless info.. let's remove it for now.
      if (ImGui::MenuItem("Bestiary...", "", false, world.loaded)) {
        shouldShowBestiary = true;
      }
      */
      ImGui::EndMenu();
    }
    if (ImGui::BeginMenu("Navigate")) {
      if (ImGui::MenuItem("Jump to Spawn", "F6", false, world.loaded)) {
        map.jumpToSpawn();
      }
      if (ImGui::MenuItem("Jump to Dungeon", "", false, world.loaded)) {
        map.jumpToDungeon();
      }
      if (ImGui::BeginMenu("NPCs")) {
        map.npcMenu(l10n);
        ImGui::EndMenu();
      }
      ImGui::Separator();
      if (ImGui::MenuItem("Find Chest...", "", false, world.loaded)) {
        shouldShowFindChests = true;
      }
      ImGui::EndMenu();
    }
    if (ImGui::BeginMenu("Help")) {
      if (ImGui::MenuItem("About Terrafirma...")) {
        shouldShowAbout = true;
      }
      ImGui::Separator();
      if (ImGui::MenuItem("Settings...")) {
        shouldShowSettings = true;
      }
      ImGui::EndMenu();
    }
    ImGui::EndMainMenuBar();
  }

  // handle loading progressbar
  if (loadMutex != nullptr) {
    bool loadOver = false;
    SDL_LockMutex(loadMutex);
    ImGui::SetNextWindowSize(ImVec2(300, 70));
    ImGui::Begin("Loading...", nullptr, ImGuiWindowFlags_NoScrollbar);
    ImGui::ProgressBar(ImGui::GetTime() * -0.2f, ImVec2(0, 0), map.progress().c_str());
    ImGui::End();
    if (map.loaded() || map.failed()) {
      loadOver = true;
    }
    SDL_UnlockMutex(loadMutex);
    if (loadOver) {
      int status;
      SDL_WaitThread(loadThread, &status);
      loadThread = nullptr;
      if (!status) {  // error
        loadError = map.progress();
        ImGui::OpenPopup("Error");
        const auto center = ImGui::GetMainViewport()->GetCenter();
        ImGui::SetNextWindowPos(center, ImGuiCond_Appearing, ImVec2(0.5f, 0.5f));
      }
      SDL_DestroyMutex(loadMutex);
      loadMutex = nullptr;
    }
  }
  if (searchMutex != nullptr) {
    bool searchOver = false;
    SDL_LockMutex(searchMutex);
    ImGui::SetNextWindowSize(ImVec2(300, 70));
    ImGui::Begin("Searching...", nullptr, ImGuiWindowFlags_NoScrollbar);
    ImGui::ProgressBar(ImGui::GetTime() * -0.2f, ImVec2(0, 0), "Searching for blocks...");
    ImGui::End();
    if (map.doneSearching()) {
      searchOver = true;
    }
    SDL_UnlockMutex(searchMutex);
    if (searchOver) {
      int status;
      SDL_WaitThread(searchThread, &status);
      searchThread = nullptr;
      if (!status) {
        loadError = "Too many blocks found, halting search";
        ImGui::OpenPopup("Error");
      }
      SDL_DestroyMutex(searchMutex);
      searchMutex = nullptr;
    }
  }

  if (shouldShowHiliteWin) {
    ImGui::OpenPopup("HiliteBlock");
    if (!hiliteWin) {
      hiliteWin = new HiliteWin(world, l10n);
    }
  }

  if (ImGui::BeginPopup("HiliteBlock")) {
    auto h = hiliteWin->pickBlock();
    map.stopHilite();
    if (h != nullptr) {
      searchMutex = SDL_CreateMutex();
      SearchMap *search = new SearchMap;
      search->map = &map;
      search->block = h;
      search->mutex = searchMutex;
      searchThread = SDL_CreateThread(searchMap, "search", search);
    }
    ImGui::EndPopup();
  }

  if (ImGui::BeginPopup("Error")) {
    ImGui::Text("Failed: %s", loadError.c_str());
    ImGui::Spacing();
    if (ImGui::Button("Okay")) {
      ImGui::CloseCurrentPopup();
    }
    ImGui::EndPopup();
  }

  if (shouldShowFindChests) {
    ImGui::OpenPopup("FindChests");
    if (!findChests) {
      findChests = new FindChests(world, l10n);
    }
  }

  if (ImGui::BeginPopup("FindChests")) {
    auto dest = findChests->pickChest();
    if (dest.x != 0 || dest.y != 0) {
      map.jumpToLocation(dest.x, dest.y);
    }
    ImGui::EndPopup();
  }

  if (shouldShowInfoWin) {
    ImGui::OpenPopup("WorldInfo");
    if (!infoWin) {
      infoWin = new InfoWin(world);
    }
  }

  if (ImGui::BeginPopup("WorldInfo")) {
    infoWin->show();
    ImGui::EndPopup();
  }

  if (shouldShowKillWin) {
    ImGui::OpenPopup("Kills");
    if (!killWin) {
      killWin = new KillWin(world, l10n);
    }
  }
  if (ImGui::BeginPopup("Kills")) {
    killWin->show();
    ImGui::EndPopup();
  }

  if (shouldShowBestiary) {
    ImGui::OpenPopup("Bestiary");
    if (!bestiary) {
      bestiary = new Bestiary(world, l10n);
    }
  }
  if (ImGui::BeginPopup("Bestiary")) {
    bestiary->show();
    ImGui::EndPopup();
  }

  if (shouldShowAbout) {
    ImGui::OpenPopup("About");
  }
  if (ImGui::BeginPopup("About")) {
    ImGui::Text("Terrafirma v4.0.5");
    ImGui::Text("© Copright 2026 Sean Kasun");
    ImGui::EndPopup();
  }

  if (shouldShowSettings) {
    ImGui::OpenPopup("Settings");
  }
  if (ImGui::BeginPopup("Settings")) {
    if (settings.show(l10n)) {
      reloadSettings();
    }
    ImGui::EndPopup();
  }

  if (rightClick) {
    rightClick = false;
    viewChest.clear();
    for (const auto &chest : world.chests) {
      if ((chest.x == rightClickTile.x || chest.x + 1 == rightClickTile.x) &&
          (chest.y == rightClickTile.y || chest.y + 1 == rightClickTile.y)) {
        for (const auto &item : chest.items) {
          if (item.stack > 0) {
            if (item.prefix.empty()) {
              viewChest.push_back(std::to_string(item.stack) + " " + l10n.xlateItem(item.name));
            } else {
              viewChest.push_back(std::to_string(item.stack) + " " + l10n.xlatePrefix(item.prefix) + " " + l10n.xlateItem(item.name));
            }
          }
        }
        ImGui::OpenPopup("ViewChest");
      }
    }
    for (const auto &sign : world.signs) {
      if ((sign.x == rightClickTile.x || sign.x + 1 == rightClickTile.x) &&
          (sign.y == rightClickTile.y || sign.y + 1 == rightClickTile.y)) {
        viewSign = sign.text;
        ImGui::OpenPopup("ViewSign");
      }
    }
  }

  if (ImGui::BeginPopup("ViewChest")) {
    for (const auto &item : viewChest) {
      ImGui::Text("%s", item.c_str());
    }
    ImGui::EndPopup();
  }

  if (ImGui::BeginPopup("ViewSign")) {
    ImGui::Text("%s", viewSign.c_str());
    ImGui::EndPopup();
  }

  if (ImGuiFileDialog::Instance()->Display("ChooseFileDlgKey", ImGuiWindowFlags_NoCollapse, ImVec2(600, 400))) {
    if (ImGuiFileDialog::Instance()->IsOk()) {
      std::string filePathName = ImGuiFileDialog::Instance()->GetFilePathName();
      openWorld(filePathName);
    }
    ImGuiFileDialog::Instance()->Close();
  }

  if (beginStatusBar()) {
    ImGui::Text("%s", status.c_str());
    endStatusBar();
  }

  ImGui::Render();
  return true;
}

struct LoadWorld {
  Map *map;
  std::string file;
  SDL_Mutex *mutex;
};

int loadWorld(void *data) {
  LoadWorld *info = (LoadWorld*)data;
  auto status = info->map->load(info->file, info->mutex);
  delete info;
  return status ? 1 : 0;
}


void Terrafirma::openWorld(std::string file) {
  if (loadThread) {
    // world is still opening.. we should error out
    return;    
  }
  // force a reload of various windows
  if (findChests) {
    delete findChests;
    findChests = nullptr;
  }
  if (infoWin) {
    delete infoWin;
    infoWin = nullptr;
  }
  if (killWin) {
    delete killWin;
    killWin = nullptr;
  }
  if (bestiary) {
    delete bestiary;
    bestiary = nullptr;
  }
  loadMutex = SDL_CreateMutex();
  LoadWorld *info = new LoadWorld;
  world.loaded = false;
  world.failed = false;
  info->map = &map;
  info->file = file;
  info->mutex = loadMutex;
  loadThread = SDL_CreateThread(loadWorld, "load", info);
}

static bool beginStatusBar() {
  ImGuiContext &g = *GImGui;
  ImGuiViewportP *viewport = (ImGuiViewportP*)ImGui::GetMainViewport();

  g.NextWindowData.MenuBarOffsetMinVal = ImVec2(g.Style.DisplaySafeAreaPadding.x, ImMax(g.Style.DisplaySafeAreaPadding.y - g.Style.FramePadding.y, 0.f));
  ImGuiWindowFlags flags = ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoSavedSettings | ImGuiWindowFlags_MenuBar;
  float height = ImGui::GetFrameHeight();
  bool open = ImGui::BeginViewportSideBar("##StatusBar", viewport, ImGuiDir_Down, height, flags);
  g.NextWindowData.MenuBarOffsetMinVal = ImVec2(0.f, 0.f);
  if (!open) {
    ImGui::End();
    return false;
  }
  g.CurrentWindow->Flags &= ~ImGuiWindowFlags_NoSavedSettings;
  ImGui::BeginMenuBar();
  return open;
}

void endStatusBar() {
  ImGuiContext &g = *GImGui;
  ImGui::EndMenuBar();
  g.CurrentWindow->Flags |= ImGuiWindowFlags_NoSavedSettings;

  if (g.CurrentWindow == g.NavWindow && g.NavLayer == ImGuiNavLayer_Main && !g.NavAnyRequest && g.ActiveId == 0) {
    ImGui::FocusTopMostWindowUnderOne(g.NavWindow, nullptr, nullptr, ImGuiFocusRequestFlags_UnlessBelowModal | ImGuiFocusRequestFlags_RestoreFocusedChild);
  }

  ImGui::End();
}

void Terrafirma::openDialog() {
  IGFD::FileDialogConfig config {
    .path = ".",
    .countSelectionMax = 1,
    .flags = ImGuiFileDialogFlags_Modal,
  };
  ImGuiFileDialog::Instance()->SetFileStyle(IGFD_FileStyleByTypeDir, nullptr, ImVec4(0.5f, 1.0f, 0.9f, 0.9f), (const char *)ICON_IGFD_FOLDER);
  ImGuiFileDialog::Instance()->OpenDialog("ChooseFileDlgKey", "Choose a World", ".wld,.wld.bak", config);
}

void Terrafirma::reloadSettings() {
  l10n.setLanguage(settings.getLanguage());
  l10n.load(settings.getExe().string());
  canShowTextures = map.setTextures(settings.getTextures());
  populateWorldMenu();
}
