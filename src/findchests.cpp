/** @copyright 2025 Sean Kasun */

#include "findchests.h"
#include <imgui.h>
#include <misc/cpp/imgui_stdlib.h>
#include <algorithm>

static bool contains(const std::string &haystack, const std::string &needle) {
  auto it = std::search(
    haystack.begin(), haystack.end(),
    needle.begin(), needle.end(),
    [](unsigned char ch1, unsigned char ch2) {
      return std::tolower(ch1) == std::tolower(ch2);
    }
  );
  return it != haystack.end();
}

FindChests::FindChests(const World &world, const L10n &l10n) {
  std::unordered_map<std::string, Item> items;
  search[0] = 0;
  selected = glm::vec2(0, 0);
  int i = 1;
  for (const auto &chest : world.chests) {
    Chest c;
    c.name = chest.name.empty() ? "Chest #" + std::to_string(i) : chest.name;
    c.location = glm::vec2(chest.x, chest.y);
    for (const auto &item : chest.items) {
      std::string name = l10n.xlateItem(item.name);
      // if an item is in the chest twice, without being stacked, it'll appear twice
      if (!items[name].seen.contains(std::pair(c.location.x, c.location.y))) {
        items[name].chests.push_back(c); 
        items[name].seen.emplace(std::pair(c.location.x, c.location.y));
      }
    }
    i++;
  }
  for (auto &item : items) {
    item.second.name = item.first;
    std::sort(item.second.chests.begin(), item.second.chests.end(), [](const Chest &a, const Chest &b) {
      return a.name < b.name;
    });
    this->items.push_back(item.second);
  }
  std::sort(this->items.begin(), this->items.end(), [](const Item &a, const Item &b) {
    return a.name < b.name;
  });
}

glm::vec2 FindChests::pickChest() {
  ImGui::InputText("Search", &search);
  ImGui::BeginChild("##chests", ImVec2(400, 400));
  for (const auto &item : items) {
    if (item.name.empty() || (!search.empty() && !contains(item.name, search))) {
      continue;
    }
    if (ImGui::TreeNodeEx(item.name.c_str(), ImGuiTreeNodeFlags_DefaultOpen)) {
      for (const auto &chest : item.chests) {
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags_Leaf;
        if (chest.location == selected) {
          flags |= ImGuiTreeNodeFlags_Selected;
        }
        if (ImGui::TreeNodeEx(chest.name.c_str(), flags)) {
          if (ImGui::IsItemClicked()) {
            selected = chest.location;
          }
          ImGui::TreePop();
        }
      }
      ImGui::TreePop();
    }
  }
  ImGui::EndChild();
  if (ImGui::Button("Cancel")) {
    ImGui::CloseCurrentPopup();
  }
  ImGui::SameLine();
  if (ImGui::Button("Okay")) {
    ImGui::CloseCurrentPopup();
    return selected;
  }
  return selected;
}
