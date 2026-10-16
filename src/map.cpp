/** @copyright 2025 Sean Kasun */

#include "map.h"
#include "SDL3/SDL_mutex.h"
#include "imgui.h"
#include "textures.h"
#include "tiles.h"
#include "uvrules.h"

#include <SDL3/SDL_gpu.h>
#include <glm/ext/matrix_projection.hpp>
#include <glm/ext/matrix_transform.hpp>
#include <glm/ext/vector_float3.hpp>
#include <glm/mat4x4.hpp>
#include <glm/ext/matrix_clip_space.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/matrix.hpp>

const float MaxZoom = 2.2f;
const float MinZoom = 0.01f;

const float WallLayer = 1.f;
const float OutlineLayer = 1.5f;
const float LiquidEdgeLayer = 1.8f;
const float TileLayer = 2.f;
const float ItemLayer = 3.f;
const float NPCLayer = 3.5f;
const float LiquidLayer = 4.f;
const float WireLayer = 5.f;
const float HouseLayer = 6.f;

Map::Map(World &world) : world(world) {}

std::string Map::init(SDL_GPUDevice *gpu) {
  return renderer.init(gpu);
}

bool Map::load(std::string filename, SDL_Mutex *mutex) {
  if (!world.load(filename, mutex)) {
    world.failed = true;
    return false;
  }
  jumpToSpawn();
  calcBounds();
  return true;
}

std::string Map::progress() {
  return world.progress();
}

bool Map::setTextures(const std::filesystem::path &path) {
  return renderer.setTextures(path);
}

void Map::setSize(int w, int h) {
  winWidth = w;
  winHeight = h;
  calcBounds();
}

glm::ivec2 Map::mouseToTile(float x, float y) {
  if (!world.loaded) {
    return glm::ivec2();
  }
  glm::mat4 m = glm::inverse(project());
  auto pt = m * glm::vec4(x / (winWidth / 2.f) - 1.f, 1.f - y / (winHeight / 2.f), 0, 1.0);
  int tileX = fmin(fmax(pt.x / 16, 0), world.tilesWide - 1);
  int tileY = fmin(fmax(pt.y / 16, 0), world.tilesHigh - 1);
  return glm::ivec2(tileX, tileY);
}

std::string Map::getStatus(const L10n &l10n, float x, float y) {
  if (!world.loaded) {
    return "";
  }
  auto pos = mouseToTile(x, y);
  const auto &tile = world.tiles[pos.y * world.tilesWide + pos.x];
  std::string r = std::to_string(pos.x) + "," + std::to_string(pos.y);
  if (tile.active()) {
    auto info = world.info[tile];
    r += " : " + l10n.xlateItem(info->name);
  } else if (tile.wall > 0) {
    auto info = world.info.walls[tile.wall];
    r += " : " + l10n.xlateItem(info->name);
  }
  return r;
}

void Map::drag(float dx, float dy) {
  centerX += dx / zoom / 16;
  centerY += dy / zoom / 16;
  centerX = fmax(0.f, fmin(centerX, world.tilesWide - 1));
  centerY = fmax(0.f, fmin(centerY, world.tilesHigh - 1));
  calcBounds();
}

void Map::scale(float amt) {
  zoom += amt * 0.05;
  zoom = fmax(MinZoom, fmin(zoom, MaxZoom));
  calcBounds();
}

void Map::jumpToSpawn() {
  jumpToLocation(world.header["spawnX"]->toDouble(), world.header["spawnY"]->toDouble());
}

void Map::jumpToDungeon() {
  jumpToLocation(world.header["dungeonX"]->toDouble(), world.header["dungeonY"]->toDouble());
}

void Map::jumpToLocation(float x, float y) {
  centerX = x;
  centerY = y;
  calcBounds();
}

void Map::npcMenu(const L10n &l10n) {
  for (const auto &npc : world.npcs) {
    std::string name = "Jump to ";
    if (npc.name.empty()) {
      name += l10n.xlateNPC(npc.title);
    } else {
      name += npc.name + " the " + l10n.xlateNPC(npc.title);
    }
    if (npc.homeless) {
      name += "'s Location";
      if (ImGui::MenuItem(name.c_str())) {
        jumpToLocation(npc.x / 16.0, npc.y / 16.0);
      }
    } else {
      name += "'s Home";
      if (ImGui::MenuItem(name.c_str())) {
        jumpToLocation(npc.homeX, npc.homeY);
      }
    }
  }
}

void Map::showTextures(bool textures) {
  this->textures = textures;
  dirty = true;
}

void Map::showWires(bool wires) {
  this->wires = wires;
  dirty = true;
}

void Map::showHouses(bool houses) {
  this->houses = houses;
  dirty = true;
}

bool Map::loaded() {
  return world.loaded;
}

bool Map::failed() {
  return world.failed;
}

void Map::copy(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  if (!world.loaded) {
    return;
  }
  if (!dirty) {
    return;
  }
  dirty = false;

  renderer.clear();

  if (textures && zoom >= 0.3f) {
    if (wires) {
      drawWires(gpu, copy);
    }
    drawNPCs(gpu, copy);
    drawTiles(gpu, copy);
    drawWalls(gpu, copy);
    drawBackground(gpu, copy);
    drawLiquids(gpu, copy);
  } else {
    drawFlat(gpu, copy);
  }
  drawHilited(gpu, copy);
  renderer.copy(copy);
}

static int trackUVs[] = {
  0, 0, 0,  1, 0, 0,  2, 1, 1,  3, 1, 1,  0, 2, 8,  1, 2, 4,  0, 1, 0,  1, 1, 0,
  0, 3, 4,  1, 3, 8,  4, 1, 9,  5, 1, 5,  6, 1, 1,  7, 1, 1,  2, 0, 0,  3, 0, 0,
  4, 0, 8,  5, 0, 4,  6, 0, 0,  7, 0, 0,  0, 4, 0,  1, 4, 0,  0, 5, 0,  1, 5, 0,
  2, 2, 2,  3, 2, 2,  4, 2, 10, 5, 2, 6,  6, 2, 2,  7, 2, 2,  2, 3, 0,  3, 3, 0,
  4, 3, 4,  5, 3, 8,  6, 3, 4,  7, 3, 8,  0, 6, 0,  1, 6, 0,  1, 7, 0,  0, 7, 0,
};

void Map::drawTiles(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  int stride = world.tilesWide;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      auto &tile = world.tiles[offset];
      auto info = world.info[tile];
      if (tile.active()) {
        if (tile.u < 0) {
          UVRules::mapTile(world, x, y);
        }
        bool fliph = info->flip && (x & 1);
        bool flipv = false;
        if (tile.type == TileMoss) {
          if (tile.v < 108) {
            fliph = x & 1;
          } else {
            flipv = y & 1;
          }
        } else if (tile.type == TileChunks && tile.v == 0) {
          fliph = x & 1;
        }

        // calculate paint
        int paint = tile.paint;
        if (paint >= 28) {
          paint = 40 + paint - 28;
        } else if (paint > 0 && paint < 13 && (info->grass || tile.type == TileTrees)) {
          paint += 27;
        }

        int texw = info->width - 2;
        int texh = info->height - 2 - (tile.half() ? 8 : 0);
        int topPad = y * 16 + info->toppad + (tile.half() ? 8 : 0);
        int leftPad = x * 16 + ((texw - 16) / 2);
        int u = tile.u;
        int v = tile.v;

        // draw special tiles on top of the tile layer

        if (tile.type == TileMushroom && u >= 36) {
          int variant = 0;
          switch (v) {
            case 18:
              variant = 1;
              break;
            case 36:
              variant = 2;
              break;
          }
          renderer.addTile(copy, Textures::Shroom, x * 16 - 22, y * 16 - 26, ItemLayer, 60, 42, variant * 62, 0, paint);
        }

        if (tile.type == TileTrees && v >= 198 && u >= 22) {
          int variant = 0;
          if (v == 220) {
            variant = 1;
          } else if (v == 242) {
            variant = 2;
          }
          int treew, treeh;
          int style = getFoliage(x, y, &variant, &treew, &treeh);
          switch (u) {
            case 22:
              renderer.addTile(copy, Textures::TreeTops | style, x * 16 + 12 - (treew >> 1), y * 16 + 16 - treeh, ItemLayer, treew, treeh, variant * (treew + 2), 0, paint);
              break;
            case 44:
              renderer.addTile(copy, Textures::TreeBranches | style, x * 16 - 24, y * 16 - 12, ItemLayer, 40, 40, 0, variant * 42, paint);
              break;
            case 66:
              renderer.addTile(copy, Textures::TreeBranches | style, x * 16, y * 16 - 12, ItemLayer, 40, 40, 42, variant * 42, paint);
              break;
          }
        }
        if (tile.type >= TileTopazTree && tile.type <= TileAmberTree && tile.v >= 198 && tile.u >= 22) {
          int variant = 0;
          if (v == 220) {
            variant = 1;
          } else if (v == 242) {
            variant = 2;
          }
          int style = tile.type - TileTopazTree + 22;
          switch (u) {
            case 22:
              renderer.addTile(copy, Textures::TreeTops | style, x * 16 - 48, y * 16 - 80, ItemLayer, 116, 96, variant * 118, 0, paint);
              break;
            case 44:
              renderer.addTile(copy, Textures::TreeBranches | style, x * 16 - 20, y * 16 - 12, ItemLayer, 40, 40, 0, variant * 42, paint);
              break;
            case 66:
              renderer.addTile(copy, Textures::TreeBranches | style, x * 16, y * 16 - 18, ItemLayer, 40, 40, 42, variant * 42, paint);
              break;
          }
        }
        if ((tile.type == TileSakuraTree || tile.type == TileWillowTree) && tile.v >= 198 && tile.u >= 22) {
          int variant = 0;
          if (v == 220) {
            variant = 1;
          } else if (v == 242) {
            variant = 2;
          }
          int style = 29;
          if (tile.type == TileWillowTree) {
            style = 30;
          }
          switch (u) {
            case 22:
              renderer.addTile(copy, Textures::TreeTops | style, x * 16 - 48, y * 16 - 80, ItemLayer, 118, 96, variant * 120, 0, paint);
              break;
            case 44:
              renderer.addTile(copy, Textures::TreeBranches | style, x * 16 - 20, y * 16 - 12, ItemLayer, 40, 40, 0, variant * 42, paint);
              break;
            case 66:
              renderer.addTile(copy, Textures::TreeBranches | style, x * 16, y * 16 - 18, ItemLayer, 40, 40, 42, variant * 42, paint);
              break;
          }
        }
        if (tile.type == TileAshTree && tile.v >= 198 && tile.u >= 22) {
          int variant = 0;
          if (v == 220) {
            variant = 1;
          } else if (v == 242) {
            variant = 2;
          }
          switch (u) {
            case 22:
              renderer.addTile(copy, Textures::TreeTops | 31, x * 16 - 48, y * 16 - 80, ItemLayer, 116, 96, variant * 118, 0, paint);
              break;
            case 44:
              renderer.addTile(copy, Textures::TreeBranches | 31, x * 16 - 20, y * 16 - 12, ItemLayer, 40, 40, 0, variant * 42, paint);
              break;
            case 66:
              renderer.addTile(copy, Textures::TreeBranches | 31, x * 16, y * 16 - 18, ItemLayer, 40, 40, 42, variant * 42, paint);
              break;
          }
        }

        if (tile.type == TilePalm && u >= 88 && u <= 132) {
          int palmu = 0;
          if (u == 110) {
            palmu = 1;
          } else if (u == 132) {
            palmu = 2;
          }
          int poff = offset;
          while (world.tiles[poff].active() && world.tiles[poff].type == TilePalm) {
            poff += stride;
          }
          int variant = getPalmVariant(poff);
          if (variant >= 4 && variant <= 7) {
            renderer.addTile(copy, Textures::TreeTops | 21, x * 16 - 48 + tile.v, y * 16 - 80, ItemLayer, 114, 98, palmu * 116, (variant - 4) * 98, paint);
          } else {
            renderer.addTile(copy, Textures::TreeTops | 15, x * 16 - 32 + tile.v, y * 16 - 64, ItemLayer, 80, 80, palmu * 82, variant * 82, paint);
          }
        }
        if (tile.type == TilePylon && (tile.u % 54) == 0 && tile.v == 0) {
          int variant = tile.u / 54;
          renderer.addTile(copy, Textures::Extra | 181, x * 16 + 10, y * 16 + 2, ItemLayer, 28, 44, (variant + 3) * 30, tile.v, 0, false);
        }
        /*
        if (tile.type == TileMasterTrophies) {
          int variant = tile.u / 54;
          renderer.addTile(copy, Textures::Extra | 198, x * 16 + 10, y * 16 + 2, ItemLayer, 28, 44, 0, variant * 46, 0, false);
        }*/
        /*
        if (tile.type == TileMannequin && tile.v == 0) {
          for (const auto &doll : world.dolls) {
            if (doll.x == x && doll.y == y) {
            }
          }
        }
        */

        // adjust tile positioning

        switch (tile.type) {
          case TileTrees:
            {
              int toff = offset;
              if (tile.u == 66 && tile.v <= 45) {
                toff++;
              }
              if (tile.u == 88 && tile.v >= 66 && tile.v <= 110) {
                toff--;
              }
              if (tile.v >= 198) {
                switch (tile.u) {
                  case 66:
                    toff--;
                    break;
                  case 44:
                    toff++;
                    break;
                }
              } else if (tile.v >= 132) {
                switch (tile.u) {
                  case 22:
                    toff--;
                    break;
                  case 44:
                    toff++;
                    break;
                }
              }
              while (world.tiles[toff].active() && world.tiles[toff].type == tile.type) {
                toff += stride;
              }
              u += 176 * getTreeVariant(toff);
            }
            break;
          case TileSwitches:
            switch (u / 18) {
              case 1:
                leftPad -= 2;
                break;
              case 2:
                leftPad += 2;
                break;
            }
            break;
          case TileTealPressure:
            if (u / 22 == 3) {
              leftPad += 2;
            }
            break;
          case TileCrystals:
            if (v < 36) {
              topPad += v == 0 ? 2 : -2;
            } else {
              topPad += v == 36 ? 2 : -2;
            }
            break;
          case TilePlating:
            {
              int variant = ((x & 1) + (y & 1) + (x % 3) + (y % 3)) % 2;
              v += variant * 90;
            }
            break;
          case TileCactus:
            {
              int coff = offset;
              switch (u) {
                case 36:
                  coff--;
                  break;
                case 54:
                  coff++;
                  break;
                case 108:
                  if (v == 18) {
                    coff--;
                  } else {
                    coff++;
                  }
                  break;
              }
              int end = offset + 20 * stride;
              while (!world.tiles[coff].active() && world.tiles[coff].type == TileCactus && coff < end) {
                     coff += stride;
              }
              switch (world.tiles[coff].type) {
                case TileEbonSand:
                  v += 54;
                  break;
                case TilePearlSand:
                  v += 108;
                  break;
                case TileCrimSand:
                  v += 162;
                  break;
              }
            }
            break;
          case TilePalm:
             {
               int poff = offset;
               while (world.tiles[poff].active() && world.tiles[poff].type == TilePalm) {
                 poff += stride;
               }
               v = 22 * getPalmVariant(poff);
               if (u >= 88 && u <= 132) {
                 continue;
               }
               leftPad += tile.v;
             }
            break;
          case TileTinker:
            if (v > 0) {
              texh += 2;
            }
            break;
          case TileChandeliers:
          case TileLamps:
          case TileBanners:
          case TileChineseLantern:
          case TileDiscoBall:
          case TileFirefly:
          case TileLightningBug:
          case TileBeehive:
          case TilePigronata:  
          case TileWarBanner:
          case TileSoulBottle:
          case TileLavafly:
          case TileHangingPots:
          case TileHangingBrazier:
          case TileFaeling:    
            {
              int toff = offset;
              while (toff > 0 && world.tiles[toff].type == tile.type) {
                toff -= stride;
              }
              // banner under a platform?
              if (world.tiles[toff].type == TilePlatforms && !world.tiles[toff].half()) {
                topPad -= 8;
              }
            }
            break;
        }

        if (tile.type == TileTrack) {
          renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, ItemLayer, 16, 16, trackUVs[tile.u * 3] * 18, trackUVs[tile.u * 3 + 1] * 18, paint);
          if ((tile.v >= 0 && tile.v < 36) || (tile.u >= 0 && tile.u <= 36)) {  // bumpers or connections
            int mask = trackUVs[tile.u * 3 + 2] | trackUVs[tile.v * 3 + 2];
            if (mask & 8) {  // left side connection
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 16, ItemLayer, 16, 16, trackUVs[36 * 3] * 18, trackUVs[36 * 3 + 1] * 18, paint);
            }
            if (mask & 4) {  // right side connection
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 16, ItemLayer, 16, 16, trackUVs[37 * 3] * 18, trackUVs[37 * 3 + 1] * 18, paint);
            }
            if (mask & 2) {  // bouncy bumper
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad - 16, ItemLayer, 16, 16, trackUVs[38 * 3] * 18, trackUVs[38 * 3 + 1] * 18, paint);
            }
            if (mask & 1) {  // bumper
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad - 16, ItemLayer, 16, 16, trackUVs[39 * 3] * 18, trackUVs[39 * 3 + 1] * 18, paint);
            }
          }
        } else if (tile.type == TileXmasTree) {
          if (tile.u >= 10) {
            int topper = tile.v & 7;
            int garland = (tile.v >> 3) & 7;
            int ornaments = (tile.v >> 6) & 0xf;
            int lights = (tile.v >> 10) & 0xf;
            renderer.addTile(copy, Textures::Xmas | 0, leftPad, topPad, TileLayer, 64, 128, 0, 0, paint);
            if (topper > 0) {
              renderer.addTile(copy, Textures::Xmas | 3, leftPad, topPad, ItemLayer, 64, 128, 66 * (topper - 1), 0, paint);
            }
            if (garland > 0) {
              renderer.addTile(copy, Textures::Xmas | 1, leftPad, topPad, ItemLayer, 64, 128, 66 * (garland - 1), 0, paint);
            }
            if (ornaments > 0) {
              renderer.addTile(copy, Textures::Xmas | 2, leftPad, topPad, ItemLayer, 64, 128, 66 * (ornaments- 1), 0, paint);
            }
            if (lights > 0) {
              renderer.addTile(copy, Textures::Xmas | 4, leftPad, topPad, ItemLayer, 64, 128, 66 * (lights - 1), 0, paint);
            }
          }
        } else if (tile.slope > 0) {
          if (tile.type == TilePlatforms) {
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, texw, texh, u, v, paint);
            const auto &br = world.tiles[offset + stride + 1];
            const auto &bl = world.tiles[offset + stride - 1];
            if (tile.slope == 1 && br.active() && br.slope != 2 && !br.half()) {
              u = 198;
              if (br.type == TilePlatforms && br.slope == 0) {
                u = 324;
              }
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 16, TileLayer, 16, 16, u, v, paint);
            } else if (tile.slope == 2 && bl.active() && bl.slope != 1 && !bl.half()) {
              u = 162;
              if (bl.type == TilePlatforms && bl.slope == 0) {
                u = 306;
              }
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 16, TileLayer, 16, 16, u, v, paint);
            }
          } else if (tile.type == TileConveyorL || tile.type == TileConveyorR) {
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, 16, 16, u, v, paint);
          } else {  // slope
            renderer.addSlope(copy, Textures::Tile | tile.type, tile.slope, leftPad, topPad, TileLayer, texw, texh, u, v, paint);
          }
        }  else if (tile.type != TilePlatforms && tile.type != TilePlanters && info->solid && !tile.half() &&
                    ((x > 0 && world.tiles[offset - 1].half()) ||
                  ((x < world.tilesWide - 1 && world.tiles[offset + 1].half())))) {
          // adjacent to half block
          if (world.tiles[offset - 1].half() && world.tiles[offset + 1].half()) {
            // both sides are half
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 8, TileLayer, texw, 8, u, v + 8, paint);
            if (world.tiles[offset - stride].slope < 3 && world.tiles[offset - stride].type == tile.type) {
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, 16, 8, 90, 0, paint);
            } else {
              renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, 16, 8, 126, 0, paint);
            }
          } else if (world.tiles[offset - 1].half()) {
            // just left side
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 8, TileLayer, texw, 8, u, v + 8, paint);
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad + 4, topPad, TileLayer, texw - 4, texh, u + 4, v, paint);
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, 4, 8, 144, 0, paint);
          } else {
            // just right side
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 8, TileLayer, texw, 8, u, v + 8, paint);
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, texw - 4, texh, u, v, paint);
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad + 12, topPad, TileLayer, 4, 8, 144, 0, paint);
          }
        } else if (tile.half() && y < world.tilesHigh - 1 &&
                   (!world.tiles[offset + stride].active() ||
                   !world.info[world.tiles[offset + stride].type]->solid ||
                   world.tiles[offset + stride].half())) {
          // half block over nothing
          if (tile.type == TilePlatforms) {
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, texw, texh, u, v, paint);
          } else {
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, texw, texh - 4, u, v, paint);
            renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad + 4, TileLayer, texw, 4, 144, 66, paint);
          }
        } else {  // normal
          renderer.addTile(copy, Textures::Tile | tile.type, leftPad, topPad, TileLayer, texw, texh, u, v, paint, fliph, flipv);
        }
      }
    }
  }
}

void Map::drawWalls(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  int stride = world.tilesWide;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      const auto &tile = world.tiles[offset];
      if (tile.wall > 0) {
        if (tile.wallu < 0) {
          UVRules::mapWall(world, x, y);
        }

        int paint = tile.wallPaint;
        if (paint == 30) {
          paint = 43;
        } else if (paint >= 28) {
          paint = 40 + paint - 28;
        }

        renderer.addTile(copy, Textures::Wall | tile.wall, x * 16 - 8, y * 16 - 8, WallLayer, 32, 32, tile.wallu, tile.wallv, paint, false);
        int blend = world.info.walls[tile.wall]->blend;
        if (x > 0) {
          int wall = world.tiles[offset - 1].wall;
          if (wall > 0 && world.info.walls[wall]->blend != blend) {
            renderer.addTile(copy, Textures::Outline, x * 16, y * 16, OutlineLayer, 2, 16, 0, 0, 0, false);
          }
        }
        if (x < world.tilesWide - 2) {
          int wall = world.tiles[offset + 1].wall;
          if (wall > 0 && world.info.walls[wall]->blend != blend) {
            renderer.addTile(copy, Textures::Outline, x * 16 + 14, y * 16, OutlineLayer, 2, 16, 14, 0, 0, false);
          }
        }
        if (y > 0) {
          int wall = world.tiles[offset - stride].wall;
          if (wall > 0 && world.info.walls[wall]->blend != blend) {
            renderer.addTile(copy, Textures::Outline, x * 16, y * 16, OutlineLayer, 16, 2, 0, 0, 0, false);
          }
        }
        if (y < world.tilesHigh - 2) {
          int wall = world.tiles[offset + stride].wall;
          if (wall > 0 && world.info.walls[wall]->blend != blend) {
            renderer.addTile(copy, Textures::Outline, x * 16, y * 16 + 14, OutlineLayer, 16, 2, 0, 14, 0, false);
          }
        }
      }
    }
  }
}

static int backStyles[] = {
  66, 67, 68, 69, 128, 125, 185,
  70, 71, 68, 72, 128, 125, 185,
  73, 74, 75, 76, 134, 125, 185,
  77, 78, 79, 82, 134, 125, 185,
  83, 84, 85, 86, 137, 125, 185,
  83, 87, 88, 89, 137, 125, 185,
  121, 122, 123, 124, 140, 125, 185,
  153, 147, 148, 149, 150, 125, 185,
  146, 154, 155, 156, 157, 125, 185
};

void Map::drawBackground(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  int groundLevel = world.header["groundLevel"]->toInt();
  int rockLevel = world.header["rockLevel"]->toInt();
  int hellLevel = ((world.tilesHigh - 330) - groundLevel) / 6;
  hellLevel = hellLevel * 6 + groundLevel - 5;
  int hellBottom = ((world.tilesHigh - 200) - hellLevel) / 6;
  hellBottom = hellBottom * 6 + hellLevel - 5;

  int hellStyle = world.header["hellBackStyle"]->toInt();

  renderer.addHBG(copy, Textures::Background | 0, 0, 0, world.tilesWide, groundLevel);

  int lastX = 0;
  for (int i = 0; i <= 3; i++) {
    int style = world.header["caveBackStyle"]->at(i)->toInt() * 7;
    int nextX = i == 3 ? world.tilesWide : world.header["caveBackX"]->at(i)->toInt();
    renderer.addBG(copy, Textures::Background | backStyles[style], lastX, groundLevel - 1, nextX - lastX, 1);
    renderer.addBG(copy, Textures::Background | backStyles[style + 1], lastX, groundLevel, nextX - lastX, rockLevel - groundLevel);
    renderer.addBG(copy, Textures::Background | backStyles[style + 2], lastX, rockLevel, nextX - lastX, 1);
    renderer.addBG(copy, Textures::Background | backStyles[style + 3], lastX, rockLevel + 1, nextX - lastX, hellLevel - (rockLevel + 1));
    renderer.addBG(copy, Textures::Background | backStyles[style + 4] + hellStyle, lastX, hellLevel, nextX - lastX, 1);
    renderer.addBG(copy, Textures::Background | backStyles[style + 5] + hellStyle, lastX, hellLevel + 1, nextX - lastX, hellBottom - (hellLevel + 1));
    renderer.addBG(copy, Textures::Background | backStyles[style + 6] + hellStyle, lastX, hellBottom, nextX - lastX, 1);
    lastX = nextX;
  }
  renderer.addHBG(copy, Textures::Underworld | 4, 0, hellBottom, world.tilesWide, world.tilesHigh - hellBottom);
}

void Map::drawLiquids(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  int stride = world.tilesWide;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      const auto &tile = world.tiles[offset];
      const auto &info = world.info[tile];
      // draw liquid behind edge tiles
      if (tile.active() && info->solid && !tile.inactive() && x > 0 && y > 0 && x < world.tilesWide - 1 && y < world.tilesHigh - 1) {
        const auto &right = world.tiles[offset + 1];
        const auto &left = world.tiles[offset - 1];
        const auto &up = world.tiles[offset - stride];
        const auto &down = world.tiles[offset + stride];
        uint8_t sideLevel = 0;
        int v = 4;
        int waterw = 16;
        int waterh = 16;
        int xpad = 0, ypad = 0;
        int mask = 0;
        double alpha = 0.5;
        int variant = 0;

        if (left.liquid > 0 && tile.slope != 1 && tile.slope != 3) {
          sideLevel = left.liquid;
          mask |= 8;
          if (left.shimmer()) {
            variant = 14;
            alpha = 0.85;
          } else if (left.honey()) {
            variant = 11;
            alpha = 0.85;
          } else if (left.lava()) {
            variant = 1;
            alpha = 0.9;
          }
        }
        if (right.liquid > 0 && tile.slope != 2 && tile.slope != 4) {
          if (sideLevel < right.liquid) {
            sideLevel = right.liquid;
          }
          mask |= 4;
          if (right.shimmer()) {
            variant = 14;
            alpha = 0.85;
          } else if (right.honey()) {
            variant = 11;
            alpha = 0.85;
          } else if (right.lava()) {
            variant = 1;
            alpha = 0.9;
          }
        }
        if (up.liquid > 0 && tile.slope != 3 && tile.slope != 4) {
          mask |= 2;
          if (up.shimmer()) {
            variant = 14;
            alpha = 0.85;
          } else if (up.honey()) {
            variant = 11;
            alpha = 0.85;
          } else if (up.lava()) {
            variant = 1;
            alpha = 0.9;
          }
        } else if (!up.active() || !world.info[up.type]->solid || tile.slope == 3 || tile.slope == 4) {
          v = 0;  // water has a ripple
        }
        if (down.liquid > 0 && tile.slope != 1 && tile.slope != 2) {
          if (down.liquid > 240) {
            mask |= 1;
          }
          if (down.shimmer()) {
            variant = 14;
            alpha = 0.85;
          } else if (down.honey()) {
            variant = 11;
            alpha = 0.85;
          } else if (down.lava()) {
            variant = 1;
            alpha = 0.9;
          }
        }
        if (mask) {
          if ((mask & 0xc) && (mask & 1)) {  // down + any side is the same as both sides
            mask |= 0xc;
          }
          if (tile.half() || tile.slope) {
            mask |= 0x10;
          }

          sideLevel = (255 - sideLevel) / 16;
          if (mask == 2) {
            waterh = 4;
          } else if (mask == 0x12) {
            waterh = 12;
          } else if ((mask & 0xf) == 1) {
            waterh = 4;
            ypad = 12;
          } else if (!(mask & 2)) {
            waterh = 16 - sideLevel;
            ypad = sideLevel;
            if ((mask & 0x1c) == 8) {
              waterw = 4;
            }
            if ((mask & 0x1c) == 4) {
              waterw = 4;
              xpad = 12;
            }
          }

          renderer.addLiquid(copy, Textures::LiquidEdge | variant, x * 16 + xpad, y * 16 + ypad, LiquidEdgeLayer, waterw, waterh, v, alpha);
        }
      }
      if (tile.liquid > 0 && (!tile.active() || !info->solid)) {
        int waterLevel = (255 - tile.liquid) / 16.0;
        int variant = 0;
        double alpha = 0.5;
        if (tile.shimmer()) {
          variant = 14;
          alpha = 0.85;
        } else if (tile.honey()) {
          variant = 11;
          alpha = 0.85;
        } else if (tile.lava()) {
          variant = 1;
          alpha = 0.9;
        }
        int v = 0;
        // ripple?
        const auto &up = world.tiles[offset - stride];
        if (up.liquid > 32 || (up.active() && world.info[up.type]->solid)) {
          v = 4;
        }
        renderer.addLiquid(copy, Textures::Liquid | variant, x * 16, y * 16 + waterLevel, LiquidLayer, 16, 16 - waterLevel, v, alpha);
      }
    }
  }
}

void Map::drawWires(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  int stride = world.tilesWide;
  for (int y = startY; y < endY; y++) {
    int offset = y * stride + startX;
    for (int x = startX; x < endX; x++, offset++) {
      const auto &tile = world.tiles[offset];
      if (tile.actuator()) {
        renderer.addTile(copy, Textures::Actuator, x * 16, y * 16, WireLayer, 16, 16, 0, 0, 0, false);
      }
      int voffset = 0;
      if (tile.type == TileJunction) {
        voffset = (tile.u / 18 + 1) * 72;
      }
      if (tile.type == TilePixel) {
        voffset = 72;
      }
      int wires = tile.Is() & (IsRedWire | IsBlueWire | IsGreenWire | IsYellowWire);
      if (wires) {
        if (wires & IsRedWire) {
          int mask = wireMask(x, y, IsRedWire);
          renderer.addTile(copy, Textures::Wires, x * 16, y * 16, WireLayer, 16, 16, mask * 18, voffset, 0, false);
        }
        if (wires & IsBlueWire) {
          int mask = wireMask(x, y, IsBlueWire);
          renderer.addTile(copy, Textures::Wires, x * 16, y * 16, WireLayer, 16, 16, mask * 18, 18 + voffset, 0, false);
        }
        if (wires & IsGreenWire) {
          int mask = wireMask(x, y, IsGreenWire);
          renderer.addTile(copy, Textures::Wires, x * 16, y * 16, WireLayer, 16, 16, mask * 18, 36 + voffset, 0, false);
        }
        if (wires & IsYellowWire) {
          int mask = wireMask(x, y, IsYellowWire);
          renderer.addTile(copy, Textures::Wires, x * 16, y * 16, WireLayer, 16, 16, mask * 18, 54 + voffset, 0, false);
        }
      }
    }
  }
}

void Map::drawNPCs(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  int stride = world.tilesWide;
  for (const auto &npc : world.npcs) {
    if (npc.sprite != 0 && (npc.x + 32) / 16 >= startX && npc.x / 16 < endX && (npc.y + 56) / 16 >= startY && npc.y / 16 < endY) {
      int offset = static_cast<int>(npc.y / 16) * stride + static_cast<int>(npc.x / 16);
      int ht = 56;
      renderer.addTile(copy, Textures::NPC | npc.sprite, npc.x, npc.y - 14, NPCLayer, 0, ht, 0, 0, 0, false);
    }
    if (houses && npc.head != 0 && !npc.homeless) {
      int hx = npc.homeX;
      int hy = npc.homeY - 1;
      int offset = hy * stride + hx;
      while (!world.tiles[offset].active() || !world.info[world.tiles[offset].type]->solid) {
        hy--;
        offset -= stride;
        if (hy < 10) {
          break;
        }
      }
      hy++;
      offset += stride;
      if (hx >= startX && hx < endX && hy >= startY && hy < endY) {
        int dy = 18;
        if (world.tiles[offset - stride].type == TilePlatforms) {
          dy -= 8;
        }
        renderer.addHouse(copy, Textures::NPCHead | npc.head, hx * 16, hy * 16 + dy, HouseLayer);
      }
    }
  }
}

int Map::wireMask(int x, int y, uint16_t color) {
  int mask = 0;
  int offset = x + y * world.tilesWide;
  if (y > 0 && (world.tiles[offset - world.tilesWide].Is() & color)) {
    mask |= 1;
  }
  if (x < world.tilesWide && (world.tiles[offset + 1].Is() & color)) {
    mask |= 2;
  }
  if (y < world.tilesHigh - 1 && (world.tiles[offset + world.tilesWide].Is() & color)) {
    mask |= 4;
  }
  if (x > 0 && (world.tiles[offset - 1].Is() & color)) {
    mask |= 8;
  }
  return mask;
}

void Map::drawFlat(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  renderer.addFlat(copy, world.colors, startX, startY, endX, endY, world.tilesWide, world.tilesHigh);
}

void Map::drawHilited(SDL_GPUDevice *gpu, SDL_GPUCopyPass *copy) {
  for (const auto &h : hilited) {
    renderer.addHilite(copy, h.x, h.y, hiliteSize.x, hiliteSize.y);
  }
}

glm::mat4 Map::project() {
  float w = static_cast<float>(winWidth) / zoom;
  float h = static_cast<float>(winHeight) / zoom;
  glm::mat4 ortho = glm::orthoLH_ZO(-w / 2.0f, w / 2.0f, h / 2.0f, -h / 2.0f, 0.1f, 100.f);
  return glm::translate(ortho, glm::vec3(-centerX * 16, -centerY * 16, 0.f));
}

void Map::render(SDL_GPUCommandBuffer *cmd, SDL_GPURenderPass *renderPass) {
  renderer.render(cmd, renderPass, project());
}

// call when scale is changed or map panned
void Map::calcBounds() {
  if (!world.loaded) {
    return;
  }
  dirty = true;
  glm::mat4 m = glm::inverse(project());
  auto pt = m * glm::vec4(-1, 1, 0, 1.0);  // top right corner 
  startX = fmax(pt.x / 16 - 2, 0);
  startY = fmax(pt.y / 16 - 2, 0);
  pt = m * glm::vec4(1, -1, 0.0, 1.0);  // bottom left corner
  endX = fmin(pt.x / 16 + 2, world.tilesWide);
  endY = fmin(pt.y / 16 + 2, world.tilesHigh);
}

int Map::getPalmVariant(int offset) {
  int var = 0;
  switch (world.tiles[offset].type) {
    case TileSand:
      var = 0;
      break;
    case TileCrimSand:
      var = 1;
      break;
    case TilePearlSand:
      var = 2;
    case TileEbonSand:
      var = 3;
  }
  int x = offset % world.tilesWide;
  // oasis palm
  if (x >= 380 && x <= world.tilesWide - 380) {
    var += 4;
  }
  return var;
}

int Map::getTreeVariant(int offset) {
  switch (world.tiles[offset].type) {
    case TileCorruptGrass:
    case TileCorruptJungle:
      return 1;
    case TileJungleGrass:
      return offset <= world.header["groundLevel"]->toInt() * world.tilesWide ? 2 : 6;
    case TileMushroomGrass:
      return 7;
    case TileHallowGrass:
    case TileHallowMowed:
      return 3;
    case TileSnow:
      return 4;
    case TileCrimsonGrass:
    case TileCrimsonJungle:
      return 5;
  }
  return 0;
}

int Map::getFoliage(int x, int y, int *variant, int *texw, int *texh) {
  *texw = 80;
  *texh = 80;
  int offset = y * world.tilesWide + x;
  for (int i = 0; i < 100; i++) {
    if (world.tiles[offset].active()) {
      switch (world.tiles[offset].type) {
        case TileGrass:
        case TileMowed:
          return world.header.treeStyle(x);
        case TileCorruptGrass:
        case TileCorruptJungle:  
          return 1;
        case TileMushroomGrass:
          return 14;
        case TileCrimsonGrass:
        case TileCrimsonJungle:
          return 5;
        case TileJungleGrass:
          *texw = 114;
          *texh = 96;
          if (offset >= world.header["groundLevel"]->toInt() * world.tilesWide) {
            *texw = 116;
            return 13;
          }
          if (world.header["treeTops"]->at(5)->toInt() == 1) {
            *texw = 116;
            return 11;
          }
          return 2;
        case TileSnow:
          {
            int alt = world.header["treeTops"]->at(6)->toInt();
            if (alt == 0) {
              if (x % 10 == 0) {
                return 18;
              }
              return 12;
            }
            if (alt == 2 || alt == 3 || alt == 32 || alt == 4 || alt == 42 || alt == 5 || alt == 7) {
              int style = 16;
              if (x >= world.tilesWide / 2) {
                style++;
              }
              return style ^ (alt & 1);
            }
            return 4;
          }
        case TileHallowGrass:
        case TileHallowMowed:
          *texh = 140;
          switch (world.header["treeTops"]->at(7)->toInt()) {
            case 2:
            case 3:
              (*variant) += (x % 6) * 3;
              return 20;
            case 4:
              *texw = 120;
              (*variant) += (x % 3) * 3;
              return 19;
          }
          (*variant) += (x % 3) * 3;
          return 3;
      }
    }
    offset += world.tilesWide;
  }
  return 0;
}

bool Map::doneSearching() {
  return hiliteSize.x != -1;
}

void Map::stopHilite() {
  renderer.hiliteBlock(false);
  hilited.clear();
  dirty = true;
}

bool Map::hilite(std::shared_ptr<TileInfo> hilite, SDL_Mutex *mutex) {
  renderer.hiliteBlock(true);
  int offset = 0;
  int count = 0;
  SDL_LockMutex(mutex);
  hiliteSize.x = -1;
  SDL_UnlockMutex(mutex);
  for (int y = 0; y < world.tilesHigh; y++) {
    for (int x = 0; x < world.tilesWide; x++, offset++) {
      const auto &tile = world.tiles[offset];
      if (tile.active()) {
        if (world.info[tile] == hilite && count < 1000) {
          SDL_LockMutex(mutex);
          hilited.push_back(glm::vec2(x * 16, y * 16));
          SDL_UnlockMutex(mutex);
          count++;
        }
      }
    }
  }
  SDL_LockMutex(mutex);
  hiliteSize = glm::vec2(hilite->width - 2, hilite->height - 2);
  dirty = true;
  SDL_UnlockMutex(mutex);
  return count < 1000;
}
