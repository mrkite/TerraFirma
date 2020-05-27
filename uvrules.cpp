/**
 * @Copyright 2015 seancode
 *
 * Handles the automatic UV rules for tiles and walls
 */

#include <QDebug>
#include "./uvrules.h"
#include "./world.h"


struct UVRule {
  quint16 mask, val;
  quint16 uvs[6];
  quint8 blend;
};

static const UVRule grassRules[] = {
  {0xaaaa, 0xaaaa, { 18,  18,  36,  18,  54,  18}, 0},
  {0x0abf, 0x083f, {  0, 324,  18, 324,  36, 324}, 0},
  {0x0abf, 0x023f, { 54, 324,  72, 324,  90, 324}, 0},
  {0xa0ef, 0x80cf, {  0, 342,  18, 342,  36, 342}, 0},
  {0xa0ef, 0x20cf, { 54, 342,  72, 342,  90, 342}, 0},
  {0x22fb, 0x20f3, { 54, 360,  72, 360,  90, 360}, 0},
  {0x22fb, 0x02f3, {  0, 360,  18, 360,  36, 360}, 0},
  {0x88fe, 0x80fc, {  0, 378,  18, 378,  36, 378}, 0},
  {0x88fe, 0x08fc, { 54, 378,  72, 378,  90, 378}, 0},
  {0x02bb, 0x0033, { 90, 270, 108, 270, 126, 270}, 0},
  {0x08be, 0x003c, {144, 270, 162, 270, 180, 270}, 0},
  {0x20eb, 0x00c3, { 90, 288, 108, 288, 126, 288}, 0},
  {0x80ee, 0x00cc, {144, 288, 162, 288, 180, 288}, 0},
  {0x0abf, 0x003f, {144, 216, 198, 216, 252, 216}, 0},
  {0xa0ef, 0x00cf, {144, 252, 198, 252, 252, 252}, 0},
  {0x22fb, 0x00f3, {126, 234, 180, 234, 234, 234}, 0},
  {0x88fe, 0x00fc, {162, 234, 216, 234, 270, 234}, 0},
  {0x00af, 0x002a, { 36, 270,  54, 270,  72, 270}, 0},
  {0x00af, 0x008a, { 36, 288,  54, 288,  72, 288}, 0},
  {0x00fa, 0x00a2, {  0, 270,   0, 288,   0, 306}, 0},
  {0x00fa, 0x00a8, { 18, 270,  18, 288,  18, 306}, 0},
  {0x00ff, 0x00ea, {198, 288, 216, 288, 234, 288}, 0},
  {0x00ff, 0x00ba, {198, 270, 216, 270, 234, 270}, 0},
  {0x00ff, 0x00ae, {198, 306, 216, 306, 234, 306}, 0},
  {0x00ff, 0x00ab, {144, 306, 162, 306, 180, 306}, 0},
  {0xaaaa, 0x2aaa, { 54, 108,  54, 144,  54, 180}, 0},
  {0xaaaa, 0x8aaa, { 36, 108,  36, 144,  36, 180}, 0},
  {0xaaaa, 0xa2aa, { 54,  90,  54, 126,  54, 162}, 0},
  {0xaaaa, 0xa8aa, { 36,  90,  36, 126,  36, 162}, 0},
  {0x00af, 0x002b, {  0, 198,  18, 198,  36, 198}, 0},
  {0x00af, 0x002e, { 54, 198,  72, 198,  90, 198}, 0},
  {0x00af, 0x008b, {  0, 216,  18, 216,  36, 216}, 0},
  {0x00af, 0x008e, { 54, 216,  72, 216,  90, 216}, 0},
  {0x00fa, 0x00b2, { 72, 144,  72, 162,  72, 180}, 0},
  {0x00fa, 0x00b8, { 90, 144,  90, 162,  90, 180}, 0},
  {0x57ff, 0x02ff, {108, 324, 126, 324, 144, 324}, 0},
  {0x77ff, 0x20ff, {108, 342, 126, 342, 144, 342}, 0},
  {0x7fff, 0x08ff, {108, 360, 126, 360, 144, 360}, 0},
  {0xffff, 0x80ff, {108, 378, 126, 378, 144, 378}, 0},
  {0xffff, 0x00ff, {144, 234, 198, 234, 252, 234}, 0},
  {0x41ff, 0x00ff, { 36, 306,  54, 306,  72, 306}, 0},
  {0x14ff, 0x00ff, { 90, 306, 108, 306, 126, 306}, 0},
  {0x5fff, 0x0fff, { 54, 108,  54, 144,  54, 180}, 0},
  {0xdfff, 0xcfff, { 36, 108,  36, 144,  36, 180}, 0},
  {0xf7ff, 0xf3ff, { 54,  90,  54, 126,  54, 162}, 0},
  {0xfdff, 0xfcff, { 36,  90,  36, 126,  36, 162}, 0},
  {0xa0ff, 0x00ef, {108,  18, 126,  18, 144,  18}, 0},
  {0x0aff, 0x00bf, {108,  36, 126,  36, 144,  36}, 0},
  {0x22ff, 0x00fb, {198,   0, 198,  18, 198,  36}, 0},
  {0x88ff, 0x00fe, {180,   0, 180,  18, 180,  36}, 0},
  {0x20ff, 0x20ef, { 54, 108,  54, 144,  54, 180}, 0},
  {0x80ff, 0x80ef, { 36, 108,  36, 144,  36, 180}, 0},
  {0x02ff, 0x02bf, { 54,  90,  54, 126,  54, 162}, 0},
  {0x08ff, 0x08bf, { 36,  90,  36, 126,  36, 162}, 0},
  {0x80ff, 0x80fe, { 54,  90,  54, 126,  54, 162}, 0},
  {0x08ff, 0x08fe, { 54, 108,  54, 144,  54, 180}, 0},
  {0x20ff, 0x20fb, { 36,  90,  36, 126,  36, 162}, 0},
  {0x02ff, 0x02fb, { 36, 108,  36, 144,  36, 180}, 0},
  {0x00ff, 0x00bf, { 18,  18,  36,  18,  54,  18}, 0},
  {0x00ff, 0x00ef, { 18,  18,  36,  18,  54,  18}, 0},
  {0x00ff, 0x00fb, { 18,  18,  36,  18,  54,  18}, 0},
  {0x00ff, 0x00fe, { 18,  18,  36,  18,  54,  18}, 0}
};

static const UVRule blendRules[] = {
  {0x00ff, 0x00bf, {144, 108, 162, 108, 180, 108}, 8},
  {0x00ff, 0x00ef, {144,  90, 162,  90, 180,  90}, 4},
  {0x00ff, 0x00fb, {162, 126, 162, 144, 162, 162}, 2},
  {0x00ff, 0x00fe, {144, 126, 144, 144, 144, 162}, 1},
  {0x00ff, 0x00bb, { 36,  90,  36, 126,  36, 162}, 8|2},
  {0x00ff, 0x00be, { 54,  90,  54, 126,  54, 162}, 8|1},
  {0x00ff, 0x00eb, { 36, 108,  36, 144,  36, 180}, 4|2},
  {0x00ff, 0x00ee, { 54, 108,  54, 144,  54, 180}, 4|1},
  {0x00ff, 0x00fa, {180, 126, 180, 144, 180, 162}, 2|1},
  {0x00ff, 0x00af, {144, 180, 162, 180, 180, 180}, 8|4},
  {0x00ff, 0x00ba, {198,  90, 198, 108, 198, 126}, 8|2|1},
  {0x00ff, 0x00ea, {216, 144, 216, 162, 216, 180}, 4|2|1},
  {0x00ff, 0x00ab, {216,  90, 216, 108, 216, 126}, 8|4|2},
  {0x00ff, 0x00aa, {108, 198, 126, 198, 144, 198}, 8|4|2|1},
  {0x03ff, 0x02ff, {  0,  90,   0, 126,   0, 162}, 0},
  {0x0cff, 0x08ff, { 18,  90,  18, 126,  18, 162}, 0},
  {0x30ff, 0x20ff, {  0, 108,   0, 144,   0, 180}, 0},
  {0xc0ff, 0x80ff, { 18, 108,  18, 144,  18, 180}, 0}
};

static const UVRule noGrassRules[] = {
  {0x00fb, 0x00b3, { 72, 144,  72, 162,  72, 180}, 8},
  {0x00fb, 0x00e3, { 72,  90,  72, 108,  72, 126}, 4},
  {0x00fe, 0x00bc, { 90, 144,  90, 162,  90, 180}, 8},
  {0x00fe, 0x00ec, { 90,  90,  90, 108,  90, 126}, 4},
  {0x00bf, 0x003b, {  0, 198,  18, 198,  36, 198}, 2},
  {0x00bf, 0x003e, { 54, 198,  72, 198,  90, 198}, 1},
  {0x00ef, 0x00cb, {  0, 216,  18, 216,  36, 216}, 2},
  {0x00ef, 0x00ce, { 54, 216,  72, 216,  90, 216}, 1},
  {0x00fa, 0x00a0, {108, 216, 108, 234, 108, 252}, 8|4},
  {0x00ca, 0x0080, {126, 144, 126, 162, 126, 180}, 8},
  {0x003a, 0x0020, {126,  90, 126, 108, 126, 126}, 4},
  {0x00af, 0x000a, {162, 198, 180, 198, 198, 198}, 2|1},
  {0x00ac, 0x0008, {  0, 252,  18, 252,  36, 252}, 2},
  {0x00a3, 0x0002, { 54, 252,  72, 252,  90, 252}, 1},
  {0x00ea, 0x0080, {108, 144, 108, 162, 108, 180}, 8},
  {0x00ba, 0x0020, {108,  90, 108, 108, 108, 126}, 4},
  {0x00ae, 0x0008, {  0, 234,  18, 234,  36, 234}, 2},
  {0x00ab, 0x0002, { 54, 234,  72, 234,  90, 234}, 1},
  {0x00bf, 0x002f, {234,   0, 252,   0, 270,   0}, 4},
  {0x00ef, 0x008f, {234,  18, 252,  18, 270,  18}, 8},
  {0x00fb, 0x00f2, {234,  36, 252,  36, 270,  36}, 1},
  {0x00fe, 0x00f8, {234,  54, 252,  54, 270,  54}, 2}
};


static const UVRule uvRules[] = {
  {0x50ff, 0x00ff, {108, 18, 126, 18, 144, 18}, 0},
  {0x05ff, 0x00ff, {108, 36, 126, 36, 144, 36}, 0},
  {0x44ff, 0x00ff, {180,  0, 180, 18, 180, 36}, 0},
  {0x11ff, 0x00ff, {198,  0, 198, 18, 198, 36}, 0},
  {0x00ff, 0x00ff, { 18, 18,  36, 18,  54, 18}, 0},
  {0x007f, 0x003f, { 18,  0,  36,  0,  54,  0}, 0},
  {0x00df, 0x00cf, { 18, 36,  36, 36,  54, 36}, 0},
  {0x00f7, 0x00f3, {  0,  0,   0, 18,   0, 36}, 0},
  {0x00fd, 0x00fc, { 72,  0,  72, 18,  72, 36}, 0},
  {0x0077, 0x0033, {  0, 54,  36, 54,  72, 54}, 0},
  {0x007d, 0x003c, { 18, 54,  54, 54,  90, 54}, 0},
  {0x00d7, 0x00c3, {  0, 72,  36, 72,  72, 72}, 0},
  {0x00dd, 0x00cc, { 18, 72,  54, 72,  90, 72}, 0},
  {0x00f5, 0x00f0, { 90,  0,  90, 18,  90, 36}, 0},
  {0x005f, 0x000f, {108, 72, 126, 72, 144, 72}, 0},
  {0x0075, 0x0030, {108,  0, 126,  0, 144,  0}, 0},
  {0x00d5, 0x00c0, {108, 54, 126, 54, 144, 54}, 0},
  {0x0057, 0x0003, {162,  0, 162, 18, 162, 36}, 0},
  {0x005d, 0x000c, {216,  0, 216, 18, 216, 36}, 0},
  {0x0055, 0x0000, {162, 54, 180, 54, 198, 54}, 0},
  {0x0000, 0x0000, { 18, 18,  36, 18,  54, 18}, 0}
};

static const UVRule cactusRules[] = {
  {0x37b, 0x003, { 90,  0, 0, 0, 0, 0}, 0},
  {0x36a, 0x002, { 72,  0, 0, 0, 0, 0}, 0},
  {0x319, 0x001, { 18,  0, 0, 0, 0, 0}, 0},
  {0x308, 0x000, {  0,  0, 0, 0, 0, 0}, 0},
  {0x37b, 0x00b, { 90, 36, 0, 0, 0, 0}, 0},
  {0x36a, 0x00a, { 72, 36, 0, 0, 0, 0}, 0},
  {0x319, 0x009, { 18, 36, 0, 0, 0, 0}, 0},
  {0x380, 0x080, {  0, 36, 0, 0, 0, 0}, 0},
  {0x300, 0x000, {  0, 18, 0, 0, 0, 0}, 0},
  {0x30d, 0x101, {108, 36, 0, 0, 0, 0}, 0},
  {0x305, 0x101, { 54, 36, 0, 0, 0, 0}, 0},
  {0x309, 0x101, { 54,  0, 0, 0, 0, 0}, 0},
  {0x301, 0x101, { 54, 18, 0, 0, 0, 0}, 0},
  {0x309, 0x100, { 54,  0, 0, 0, 0, 0}, 0},
  {0x300, 0x100, { 54, 18, 0, 0, 0, 0}, 0},
  {0x30e, 0x202, {108, 18, 0, 0, 0, 0}, 0},
  {0x306, 0x202, { 36, 36, 0, 0, 0, 0}, 0},
  {0x30a, 0x202, { 36,  0, 0, 0, 0, 0}, 0},
  {0x302, 0x202, { 36, 18, 0, 0, 0, 0}, 0},
  {0x30a, 0x200, { 36,  0, 0, 0, 0, 0}, 0},
  {0x300, 0x200, { 36, 18, 0, 0, 0, 0}, 0}
};

static const int phlebasTiles[4][3] = {
  {2, 4, 2},
  {1, 3, 1},
  {2, 2, 4},
  {1, 1, 3}
};
static const int lazureTiles[2][2] = {
  {1, 2},
  {3, 4}
};
static const int wallRandom[3][3] = {
  {2, 0, 0},
  {0, 1, 4},
  {0, 3, 0}
};

static const int walluvs[][8] = {
  {324, 108, 360, 108, 396, 108, 216, 216},
  {216, 108, 252, 108, 288, 108, 144, 216},
  {432,   0, 432,  36, 432,  72, 432, 180},
  { 36, 144, 108, 144, 180, 144, 108, 216},
  {324,   0, 324,  36, 324,  72, 324, 180},
  {  0, 144,  72, 144, 144, 144,  72, 216},
  {216, 144, 252, 144, 288, 144, 180, 216},
  { 36,  72,  72,  72, 108,  72, 108, 180},
  {216,   0, 252,   0, 288,   0, 216, 180},
  {180,   0, 180,  36, 180,  72, 180, 180},
  { 36, 108, 108, 108, 180, 108,  36, 216},
  {144,   0, 144,  36, 144,  72, 144, 180},
  {  0, 108,  72, 108, 144, 108,   0, 216},
  {  0,   0,   0,  36,   0,  72,   0, 180},
  { 36,   0,  72,   0, 108,   0,  36, 216},
  { 36,  36,  72,  36, 108,  36,  72, 180},
  {216,  36, 252,  36, 288,  36, 252, 180},
  {216,  72, 252,  72, 288,  72, 288, 180},
  {360,   0, 360,  36, 360,  72, 360, 180},
  {396,   0, 396,  36, 396,  72, 396, 180}
};

void UVRules::fixWall(const QSharedPointer<World> &world, int x, int y) {
  int stride = world->tilesWide;
  int offset = y * stride + x;
  int mask = 0;
  if (y > 0) {
    auto top = &world->tiles[offset - stride];
    if (top->wall || (top->active() && top->type == 54))
      mask |= 1;
  }
  if (x > 0) {
    auto left = &world->tiles[offset - 1];
    if (left->wall || (left->active() && left->type == 54))
      mask |= 2;
  }
  if (x < world->tilesWide - 1) {
    auto right = &world->tiles[offset + 1];
    if (right->wall || (right->active() && right->type == 54))
      mask |= 4;
  }
  if (y < world->tilesHigh - 1) {
    auto bottom = &world->tiles[offset + stride];
    if (bottom->wall || (bottom->active() && bottom->type == 54))
      mask |= 8;
  }
  int set;

  auto tile = &world->tiles[offset];

  switch (world->info.walls[tile->wall]->large) {
    case 1:
      set = (phlebasTiles[y % 4][x % 3] - 1) * 2;
      break;
    case 2:
      set = (lazureTiles[x % 2][y % 2] - 1) * 2;
      break;
    default:
      set = (qrand() % 3) * 2;
      break;
  }

  if (mask == 15)
    mask += wallRandom[x % 3][y % 3];

  tile->wallu = walluvs[mask][set];
  tile->wallv = walluvs[mask][set + 1];
}

quint8 UVRules::fixTile(const QSharedPointer<World> &world, int x, int y) {
  int t = -1, l = -1, r = -1, b = -1;
  int tl = -1, tr = -1, bl = -1, br = -1;

  int stride = world->tilesWide;
  int offset = y * stride + x;

  auto tile = &world->tiles[offset];
  qint16 c = tile->type;
  if (world->info[c]->stone) c = 1;

  if (c == 80) {  // cactus
    fixCactus(world, x, y);
    return 0;
  }


  if (x > 0) {
    auto left = &world->tiles[offset - 1];
    if (left->active() && left->slope != 1 && left->slope != 3) {
      l = left->type;
      if (world->info[l]->stone) l = 1;
    }
    if (y > 0 && world->tiles[offset - stride - 1].active()) {
      tl = world->tiles[offset - stride - 1].type;
      if (world->info[tl]->stone) tl = 1;
    }
    if (y < world->tilesHigh - 1 &&
        world->tiles[offset + stride - 1].active()) {
      bl = world->tiles[offset + stride - 1].type;
      if (world->info[bl]->stone) bl = 1;
    }
  }
  if (x < world->tilesWide - 1) {
    auto right = &world->tiles[offset + 1];
    if (right->active() && right->slope != 2 && right->slope != 4) {
      r = right->type;
      if (world->info[r]->stone) r = 1;
    }
    if (y > 0 && world->tiles[offset - stride + 1].active()) {
      tr = world->tiles[offset - stride + 1].type;
      if (world->info[tr]->stone) tr = 1;
    }
    if (y < world->tilesHigh - 1 &&
        world->tiles[offset + stride + 1].active()) {
      br = world->tiles[offset + stride + 1].type;
      if (world->info[br]->stone) br = 1;
    }
  }
  if (y > 0) {
    auto top = &world->tiles[offset - stride];
    if (top->active() && top->slope != 3 && top->slope != 4) {
      t = top->type;
      if (world->info[t]->stone) t = 1;
    }
  }
  if (y < world->tilesHigh - 1) {
    auto bottom = &world->tiles[offset + stride];
    if (bottom->active() && bottom->slope != 1 && bottom->slope != 2) {
      b = bottom->type;
      if (world->info[b]->stone) b = 1;
    }
  }


  // fix slopes
  switch (tile->slope) {
    case 1: t = r = -1; break;
    case 2: t = l = -1; break;
    case 3: b = r = -1; break;
    case 4: b = l = -1; break;
  }

  // check blends and merges (blends should be first)
  for (auto const &blend : world->info[c]->blends) {
    quint8 dir = 0;
    if (blend.hasTile) {
      dir |= t == blend.tile ? 8 : 0;
      dir |= b == blend.tile ? 4 : 0;
      dir |= l == blend.tile ? 2 : 0;
      dir |= r == blend.tile ? 1 : 0;
      dir |= tl == blend.tile ? 0x80 : 0;
      dir |= tr == blend.tile ? 0x40 : 0;
      dir |= bl == blend.tile ? 0x20 : 0;
      dir |= br == blend.tile ? 0x10 : 0;
    } else {
      dir |= (t > -1 && (world->info[t]->mask & blend.mask)) ? 8 : 0;
      dir |= (b > -1 && (world->info[b]->mask & blend.mask)) ? 4 : 0;
      dir |= (l > -1 && (world->info[l]->mask & blend.mask)) ? 2 : 0;
      dir |= (r > -1 && (world->info[r]->mask & blend.mask)) ? 1 : 0;
      dir |= (tl > -1 && (world->info[tl]->mask & blend.mask)) ? 0x80 : 0;
      dir |= (tr > -1 && (world->info[tr]->mask & blend.mask)) ? 0x40 : 0;
      dir |= (bl > -1 && (world->info[bl]->mask & blend.mask)) ? 0x20 : 0;
      dir |= (br > -1 && (world->info[br]->mask & blend.mask)) ? 0x10 : 0;
    }
    dir &= blend.direction;

    if ((dir & 8) && (!blend.recursive || (fixTile(world, x, y - 1) & 4)))
      t = blend.blend ? -2 : c;
    if ((dir & 4) && (!blend.recursive || (fixTile(world, x, y + 1) & 8)))
      b = blend.blend ? -2 : c;
    if ((dir & 2) && (!blend.recursive || (fixTile(world, x - 1, y) & 1)))
      l = blend.blend ? -2 : c;
    if ((dir & 1) && (!blend.recursive || (fixTile(world, x + 1, y) & 2)))
      r = blend.blend ? -2 : c;
    if (dir & 0x80) tl = blend.blend ? -2 : c;
    if (dir & 0x40) tr = blend.blend ? -2 : c;
    if (dir & 0x20) bl = blend.blend ? -2 : c;
    if (dir & 0x10) br = blend.blend ? -2 : c;
  }
  if (world->info[c]->brick) {  // brick merges with brick
    if (t > -1 && world->info[t]->brick) t = c;
    if (b > -1 && world->info[b]->brick) b = c;
    if (l > -1 && world->info[l]->brick) l = c;
    if (r > -1 && world->info[r]->brick) r = c;
    if (tl > -1 && world->info[tl]->brick) tl = c;
    if (tr > -1 && world->info[tr]->brick) tr = c;
    if (bl > -1 && world->info[bl]->brick) bl = c;
    if (br > -1 && world->info[br]->brick) br = c;
  }
  if (world->info[c]->pile) {  // pile merges with pile
    if (t > -1 && world->info[t]->pile) t = c;
    if (b > -1 && world->info[b]->pile) b = c;
    if (l > -1 && world->info[l]->pile) l = c;
    if (r > -1 && world->info[r]->pile) r = c;
    if (tl > -1 && world->info[tl]->pile) tl = c;
    if (tr > -1 && world->info[tr]->pile) tr = c;
    if (bl > -1 && world->info[bl]->pile) bl = c;
    if (br > -1 && world->info[br]->pile) br = c;
  }
  if (world->info[c]->dirt) {
    if (t == 0) t = -2;
    if (b == 0) b = -2;
    if (l == 0) l = -2;
    if (r == 0) r = -2;
    if (tl == 0) tl = -2;
    if (tr == 0) tr = -2;
    if (bl == 0) bl = -2;
    if (br == 0) br = -2;
  }
  // everything merges with 357
  if (t == 357) t = c;
  if (b == 357) b = c;
  if (l == 357) l = c;
  if (r == 357) r = c;
  if (tl == 357) tl = c;
  if (tr == 357) tr = c;
  if (bl == 357) bl = c;
  if (br == 357) br = c;
  // fix rope
  if (c == 213) {
    if (t != c) {
      if (l > -1 && world->info[l]->solid) l = c;
      if (r > -1 && world->info[r]->solid) r = c;
    }
  }
  // fix cobweb
  if (c == 51) {
    if (t > -1) t = c;
    if (b > -1) b = c;
    if (l > -1) l = c;
    if (r > -1) r = c;
    if (tl > -1) tl = c;
    if (tr > -1) tr = c;
    if (bl > -1) bl = c;
    if (br > -1) br = c;
  }

  // slope and half rules
  if ((tile->slope == 1 || tile->slope == 2) && b > -1 && b != 19) b = c;
  if (t > -1) {
    auto top = &world->tiles[offset - stride];
    if ((top->slope == 1 || top->slope == 2) && t != 19) t = c;
    if (top->half() && t != 19) t = c;
  }
  if ((tile->slope == 3 || tile->slope == 4) && t > -1 && t != 19) t = c;
  if (b > -1) {
    auto bottom = &world->tiles[offset + stride];
    if ((bottom->slope == 3 || bottom->slope == 4) && b != 19) b = c;
    if (bottom->half()) b = -1;
  }
  if (l > -1) {
    auto left = &world->tiles[offset - 1];
    if (left->half()) {
      if (tile->half()) l = c;
      else if (left->type != c) l = -1;
    }
  }
  if (r > -1) {
    auto right = &world->tiles[offset + 1];
    if (right->half()) {
      if (tile->half()) r = c;
      else if (right->type != c) r = -1;
    }
  }
  if (tile->half()) {
    if (l != c) l = -1;
    if (r != c) r = -1;
    t = -1;
  }

  int blend = 0;

  // fix color mismatches
  if (!world->info[c]->grass) {
    if (t == -2 && tile->color != world->tiles[offset - stride].color) {
      blend |= 8;
      t = c;
    }
    if (b == -2 && tile->color != world->tiles[offset + stride].color) {
      blend |= 4;
      b = c;
    }
    if (l == -2 && tile->color != world->tiles[offset - 1].color) {
      blend |= 2;
      l = c;
    }
    if (r == -2 && tile->color != world->tiles[offset + 1].color) {
      blend |= 1;
      r = c;
    }
  }

  int mask = 0;
  mask |= (t == c) ? 0xc0 : (t == -2) ? 0x80 : 0;
  mask |= (b == c) ? 0x30 : (b == -2) ? 0x20 : 0;
  mask |= (l == c) ? 0x0c : (l == -2) ? 0x08 : 0;
  mask |= (r == c) ? 0x03 : (r == -2) ? 0x02 : 0;
  mask |= (tl == c) ? 0xc000 : (tl == -2) ? 0x8000 : 0;
  mask |= (tr == c) ? 0x3000 : (tr == -2) ? 0x2000 : 0;
  mask |= (bl == c) ? 0x0c00 : (bl == -2) ? 0x0800 : 0;
  mask |= (br == c) ? 0x0300 : (br == -2) ? 0x0200 : 0;

  int set = (qrand() % 3) * 2;
  if (world->info[c]->large)
    set = (phlebasTiles[y % 4][x % 3] - 1) * 2;

  if (world->info[c]->grass) {
    for (auto const &rule : grassRules) {
      if ((mask & rule.mask) == rule.val) {
        world->tiles[offset].u = rule.uvs[set];
        world->tiles[offset].v = rule.uvs[set + 1];
        return rule.blend | blend;
      }
    }
  }

  if (world->info[c]->merge || world->info[c]->dirt) {
    for (auto const &rule : blendRules) {
      if ((mask & rule.mask) == rule.val) {
        world->tiles[offset].u = rule.uvs[set];
        world->tiles[offset].v = rule.uvs[set + 1];
        if (world->info[c]->large && set == 6)
          world->tiles[offset].v += 90;
        return rule.blend | blend;
      }
    }
    if (!world->info[c]->grass) {
      for (auto const &rule : noGrassRules) {
        if ((mask & rule.mask) == rule.val) {
          world->tiles[offset].u = rule.uvs[set];
          world->tiles[offset].v = rule.uvs[set + 1];
          if (world->info[c]->large && set == 6)
            world->tiles[offset].v += 90;
          return rule.blend | blend;
        }
      }
    }
  }
  // no match, blends become merges
  if (world->info[c]->grass)
    mask |= (mask & 0xaaaa) >> 1;

  for (auto const &rule : uvRules) {
    if ((mask & rule.mask) == rule.val) {
      world->tiles[offset].u = rule.uvs[set];
      world->tiles[offset].v = rule.uvs[set + 1];
      if (world->info[c]->large && set == 6)
        world->tiles[offset].v += 90;
      return rule.blend | blend;
    }
  }
  // should never get here.. since there's a catch-all rule in uvRules
  return blend;
}

void UVRules::fixCactus(const QSharedPointer<World> &world, int x, int y) {
  int stride = world->tilesWide;
  int offset =  y * stride + x;
  // find base of cactus
  int basex = x;
  int base = offset;
  while (world->tiles[base].active() && world->tiles[base].type == 80) {
    base += stride;
    if (!world->tiles[base].active() || world->tiles[base].type != 80) {
      if (world->tiles[base - 1].active() &&
          world->tiles[base - 1].type == 80 &&
          world->tiles[base - stride - 1].active() &&
          world->tiles[base - stride - 1].type == 80 && basex >= x) {
        basex--;
        base--;
      }
      if (world->tiles[base + 1].active() &&
          world->tiles[base + 1].type == 80 &&
          world->tiles[base - stride + 1].active() &&
          world->tiles[base - stride + 1].type == 80 && basex <= x) {
        basex++;
        base--;
      }
    }
  }

  int mask = 0;
  if (x < world->tilesWide - 1) {
    auto right = &world->tiles[offset + 1];
    if (right->active() && right->type == 80)
      mask |= 0x01;
  }
  if (x > 0) {
    auto left = &world->tiles[offset - 1];
    if (left->active() && left->type == 80)
      mask |= 0x02;
    if (x > 1) {
      auto fl = &world->tiles[offset - 2];
      if (fl->active() && fl->type == 80)
        mask |= 0x40;
    }
  }
  if (y < world->tilesHigh - 1) {
    auto bottom = &world->tiles[offset + stride];
    if (bottom->active() && bottom->type == 80)
      mask |= 0x04;
    if (bottom->active() && world->info[bottom->type]->solid)
      mask |= 0x80;
    if (x < world->tilesWide - 1) {
      auto br = &world->tiles[offset + stride + 1];
      if (br->active() && br->type == 80)
        mask |= 0x10;
    }
    if (x > 0) {
      auto bl = &world->tiles[offset + stride - 1];
      if (bl->active() && bl->type == 80)
        mask |= 0x20;
    }
  }
  if (y > 0) {
    auto top = &world->tiles[offset - stride];
    if (top->active() && (top->type == 80 || top->type == 227))
      mask |= 0x08;
  }
  if (x > basex) mask |= 0x200;
  if (x < basex) mask |= 0x100;

  for (auto const &rule : cactusRules) {
    if ((mask & rule.mask) == rule.val) {
      world->tiles[offset].u = rule.uvs[0];
      world->tiles[offset].v = rule.uvs[1];
      return;
    }
  }
}
















