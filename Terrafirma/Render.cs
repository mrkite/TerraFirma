/*
Copyright (c) 2011, Sean Kasun
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terrafirma
{
    class Render
    {
        private TileInfos tileInfos;
        private WallInfo[] wallInfo;
        private UInt32 skyColor, earthColor, rockColor, hellColor;
        private UInt32 waterColor, lavaColor;
        private Int32 tilesWide, tilesHigh;
        private int groundLevel, rockLevel;
        private List<NPC> npcs;

        Random rand;

        public Textures Textures { set; get; }

        public Render(TileInfos tileInfos, WallInfo[] wallInfo,
            UInt32 skyColor, UInt32 earthColor, UInt32 rockColor, UInt32 hellColor,
            UInt32 waterColor, UInt32 lavaColor)
        {
            this.tileInfos = tileInfos;
            this.wallInfo = wallInfo;
            this.skyColor = skyColor;
            this.earthColor = earthColor;
            this.rockColor = rockColor;
            this.hellColor = hellColor;
            this.waterColor = waterColor;
            this.lavaColor = lavaColor;
            rand = new Random();
        }

        public void SetWorld(Int32 tilesWide, Int32 tilesHigh,
            int groundLevel, int rockLevel, List<NPC> npcs)
        {
            this.tilesWide = tilesWide;
            this.tilesHigh = tilesHigh;
            this.groundLevel = groundLevel;
            this.rockLevel = rockLevel;
            this.npcs = npcs;
        }


        private struct Delayed
        {
            public int px, py;
            public int sx, sy;
        };

        public void Draw(int width, int height,
            double startx, double starty,
            double scale, ref byte[] pixels,
            bool isHilight,
            int light, bool texture, bool houses, bool wires, ref Tile[,] tiles)
        {
            if (texture)
            {
                int blocksWide = (int)(width / Math.Floor(scale)) + 2; //scale=1.0 to 16.0
                int blocksHigh = (int)(height / Math.Floor(scale)) + 2;

                double adjustx = ((width / scale) - blocksWide) / 2;
                double adjusty = ((height / scale) - blocksHigh) / 2;
                startx += adjustx;
                starty += adjusty;

                int skipx = 0, skipy = 0;
                if (startx < 0) skipx = (int)-startx;
                if (starty < 0) skipy = (int)-starty;

                double shiftx = (startx - Math.Floor(startx)) * scale;
                double shifty = (starty - Math.Floor(starty)) * scale;
                int py, px;

                double lightR, lightG, lightB;

                // draw backgrounds

                int hellLevel = ((tilesHigh - 230) - groundLevel) / 6; //rounded
                hellLevel = hellLevel * 6 + groundLevel - 5;

                py = skipy * (int)scale;
                for (int y = skipy; y < blocksHigh; y++)
                {
                    int sy = (int)(y + starty);
                    px = skipx * (int)scale;

                    int bg = 0;
                    int v = sy;
                    if (sy < groundLevel)
                        bg = 0;
                    else if (sy == groundLevel)
                    {
                        bg = 1;
                        v = sy - groundLevel;
                    }
                    else if (sy < rockLevel)
                    {
                        bg = 2;
                        v = sy - groundLevel;
                    }
                    else if (sy == rockLevel)
                    {
                        bg = 4;
                        v = sy - rockLevel;
                    }
                    else if (sy < hellLevel)
                    {
                        bg = 3;
                        v = sy - rockLevel;
                    }
                    else
                    {
                        bg = 5;
                        v = sy - hellLevel;
                    }

                    Texture tex = Textures.GetBackground(bg);

                    for (int x = skipx; x < blocksWide; x++)
                    {
                        int sx = (int)(x + startx);
                        if (sx < 0 || sx >= tilesWide || sy < 0 || sy >= tilesHigh)
                            continue;

                        Tile tile = tiles[sx, sy];

                        if (light == 1)
                            lightR = lightG = lightB = tile.light;
                        else if (light == 2)
                        {
                            lightR = tile.lightR;
                            lightG = tile.lightG;
                            lightB = tile.lightB;
                        }
                        else
                            lightR = lightG = lightB = 1.0;

                        if (isHilight)
                        {
                            lightR *= 0.3;
                            lightG *= 0.3;
                            lightB *= 0.3;
                        }


                        int u = (sx % (tex.width / 16)) * 16;
                        int vv = (v % (tex.height / 16)) * 16;
                        if (bg == 0) //sky
                            vv = sy * tex.height / groundLevel;
                        drawTexture(tex, 16, 16, vv * tex.width * 4 + u * 4,
                            pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB);

                        px += (int)scale;
                    }
                    py += (int)scale;
                }

                //draw walls

                py = skipy * (int)scale - (int)(scale / 2);
                for (int y = skipy; y < blocksHigh; y++)
                {
                    int sy = (int)(y + starty);

                    px = skipx * (int)scale - (int)(scale / 2);
                    for (int x = skipx; x < blocksWide; x++)
                    {
                        int sx = (int)(x + startx);

                        if (sx < 0 || sx >= tilesWide || sy < 0 || sy >= tilesHigh)
                            continue;

                        Tile tile = tiles[sx, sy];

                        if (tile.wall > 0)
                        {
                            if (tile.wallu == -1) fixWall(sx, sy, ref tiles);
                            Texture tex = Textures.GetWall(tile.wall);
                            if (light == 1)
                                lightR = lightG = lightB = tile.light;
                            else if (light == 2)
                            {
                                lightR = tile.lightR;
                                lightG = tile.lightG;
                                lightB = tile.lightB;
                            }
                            else
                                lightR = lightG = lightB = 1.0;

                            if (isHilight)
                            {
                                lightR *= 0.3;
                                lightG *= 0.3;
                                lightB *= 0.3;
                            }

                            drawTexture(tex, 32, 32, tile.wallv * tex.width * 4 * 2 + tile.wallu * 4 * 2,
                                pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB);
                        }

                        px += (int)scale;
                    }
                    py += (int)scale;
                }

                List<Delayed> delayed = new List<Delayed>();

                //draw tiles
                py = skipy * (int)scale;
                for (int y = skipy; y < blocksHigh; y++)
                {
                    int sy = (int)(y + starty);
                    px = skipx * (int)scale;
                    for (int x = skipx; x < blocksWide; x++)
                    {
                        int sx = (int)(x + startx);

                        if (sx < 0 || sx >= tilesWide || sy < 0 || sy >= tilesHigh)
                            continue;

                        Tile tile = tiles[sx, sy];

                        if (light == 1)
                            lightR = lightG = lightB = tiles[sx, sy].light;
                        else if (light == 2)
                        {
                            lightR = tile.lightR;
                            lightG = tile.lightG;
                            lightB = tile.lightB;
                        }
                        else
                            lightR = lightG = lightB = 1.0;

                        if (isHilight && (!tile.isActive || !tileInfos[tile.type, tile.u, tile.v].isHilighting))
                        {
                            lightR *= 0.3;
                            lightG *= 0.3;
                            lightB *= 0.3;
                        }

                        if (tile.isActive)
                        {
                            if (tile.u == -1 || tile.v == -1) fixTile(sx, sy, ref tiles);

                            int texw = 16;
                            int texh = 16;

                            if (tile.type == 5) //tree
                                texw = 20;
                            if (tile.type == 72) //mushroom
                                drawMushroom(tile.u, tile.v,
                                    pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB);

                            if (tile.type == 103) //bowl
                                if (tile.u == 18) texw = 14;

                            if (tile.type == 5 && tile.v >= 198 && tile.u >= 22) //tree leaves
                            {
                                Delayed delay = new Delayed();
                                delay.px = (int)(px - shiftx);
                                delay.py = (int)(py - shifty);
                                delay.sx = sx;
                                delay.sy = sy;
                                delayed.Add(delay);
                            }
                            else if (tile.type == 128) //armor
                            {
                                int au = tile.u % 100;
                                //draw armor stand
                                Texture tex = Textures.GetTile(tile.type);
                                drawTexture(tex, texw, texh, tile.v * tex.width * 4 + au * 4,
                                    pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB);
                                //draw armor
                                int armor = tile.u / 100;
                                if (armor > 0)
                                {
                                    Delayed delay = new Delayed();
                                    delay.sx = sx;
                                    delay.sy = sy;
                                    delay.px = (int)(px - shiftx);
                                    delay.py = (int)(py - shifty);
                                    delayed.Add(delay);
                                }
                            }
                            else
                            {
                                Texture tex = Textures.GetTile(tile.type);
                                drawTexture(tex, texw, texh, tile.v * tex.width * 4 + tile.u * 4,
                                    pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB);
                            }
                        }
                        // draw liquid
                        if (tile.liquid > 0)
                        {
                            UInt32 c = tile.isLava ? lavaColor : waterColor;
                            uint r = (uint)((c >> 16) * lightR);
                            uint g = (uint)(((c >> 8) & 0xff) * lightG);
                            uint b = (uint)((c & 0xff) * lightB);
                            c = (r << 16) | (g << 8) | b;
                            drawOverlay(c, tile.isLava ? 0.85 : 0.5, tile.liquid,
                                pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);
                        }
                        // draw wires if necessary
                        if (wires && tile.hasWire)
                        {
                            drawWire(sx, sy, pixels,
                                (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB, ref tiles);
                        }
                        px += (int)scale;
                    }
                    py += (int)scale;
                }

                //draw delayed blocks
                foreach (Delayed delay in delayed)
                {
                    Texture tex;
                    Tile tile = tiles[delay.sx, delay.sy];
                    int texw = 16, texh = 16;
                    if (light == 1)
                        lightR = lightG = lightB = tiles[delay.sx, delay.sy].light;
                    else if (light == 2)
                    {
                        lightR = tile.lightR;
                        lightG = tile.lightG;
                        lightB = tile.lightB;
                    }
                    else
                        lightR = lightG = lightB = 1.0;

                    if (isHilight && !tileInfos[tile.type, tile.u, tile.v].isHilighting)
                    {
                        lightR *= 0.3;
                        lightG *= 0.3;
                        lightB *= 0.3;
                    }

                    if (tile.type == 128) //armor
                    {
                        int dy = 8;
                        int au = tile.u % 100;
                        int armor = tile.u / 100;
                        switch (tile.v)
                        {
                            case 0: //head
                                tex = Textures.GetArmorHead(armor);
                                texw = 40;
                                texh = 36;
                                dy = 12;
                                break;
                            case 18: //body
                                tex = Textures.GetArmorBody(armor);
                                texw = 40;
                                texh = 54;
                                dy = 28;
                                break;
                            default: //legs
                                tex = Textures.GetArmorLegs(armor);
                                texw = 40;
                                texh = 54;
                                dy = 44;
                                break;
                        }
                        if (au >= 36) //reverse
                            drawTexture(tex, texw, texh, 0,
                                pixels, delay.px - 4, delay.py - dy, width, height, scale / 16.0, lightR, lightG, lightB);
                        else
                            drawTextureFlip(tex, texw, texh, 0,
                                pixels, delay.px - 4, delay.py - dy, width, height, scale / 16.0, lightR, lightG, lightB);
                    }
                    else if (tile.type == 5) //tree leaves
                    {
                        drawLeaves(tile.u, tile.v, delay.sx, delay.sy,
                                   pixels, delay.px, delay.py, width, height, scale / 16.0, lightR, lightG, lightB, ref tiles);
                    }
                }

                double minx = skipx + startx;
                double maxx = minx + blocksWide;
                double miny = skipy + starty;
                double maxy = miny + blocksHigh;
                // draw npcs at sx,sy
                foreach (NPC npc in npcs)
                {
                    if (npc.sprite != 0 && (npc.x / 16) >= minx && (npc.x / 16) < maxx &&
                        (npc.y / 16) >= miny && (npc.y / 16) < maxy) //npc on screen
                    {
                        Tile t = tiles[(int)(npc.x / 16), (int)(npc.y / 16)];
                        if (light == 1)
                            lightR = lightG = lightB = t.light;
                        else if (light == 2)
                        {
                            lightR = t.lightR;
                            lightG = t.lightG;
                            lightB = t.lightB;
                        }
                        else
                            lightR = lightG = lightB = 1.0;
                        if (isHilight)
                        {
                            lightR *= 0.3;
                            lightG *= 0.3;
                            lightB *= 0.3;
                        }
                        Texture tex = Textures.GetNPC(npc.sprite);
                        px = (int)(skipx + npc.x / 16 - (int)startx) * (int)scale - (int)(scale / 4);
                        py = (int)(skipy + npc.y / 16 - (int)starty) * (int)scale - (int)(scale / 4);
                        drawTexture(tex, 40, 56, 0, pixels,
                            (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0, lightR, lightG, lightB);
                    }
                    if (houses && npc.num != 0)
                    {
                        //calc home x and y
                        int hx = npc.homeX;
                        int hy = npc.homeY - 1;
                        while (!tiles[hx, hy].isActive || !tileInfos[tiles[hx, hy].type].solid)
                        {
                            hy--;
                            if (hy < 10) break;
                        }
                        hy++;
                        if (hx >= minx && hx < maxx && hy >= miny && hy < maxy) //banner on screen
                        {
                            Tile t = tiles[hx, hy];
                            if (light == 1)
                                lightR = lightG = lightB = t.light;
                            else if (light == 2)
                            {
                                lightR = t.lightR;
                                lightG = t.lightG;
                                lightB = t.lightB;
                            }
                            else
                                lightR = lightG = lightB = 1.0;
                            if (isHilight)
                            {
                                lightR *= 0.3;
                                lightG *= 0.3;
                                lightB *= 0.3;
                            }

                            int dy = 18;
                            if (tiles[hx, hy - 1].type == 19) //platform
                                dy -= 8;
                            px = (int)(skipx + hx - (int)startx) * (int)scale + (int)(scale / 2);
                            py = (int)(skipy + hy - (int)starty) * (int)scale + (int)(dy * scale / 16);
                            Texture tex = Textures.GetBanner(1); //house banner
                            int npx = (int)(px - tex.width * scale / 32.0);
                            int npy = (int)(py - tex.height * scale / 32.0);
                            drawTexture(tex, 32, 40, 0, pixels,
                                (int)(npx - shiftx), (int)(npy - shifty), width, height, scale / 16.0, lightR, lightG, lightB);
                            tex = Textures.GetNPCHead(npc.num);
                            npx = (int)(px - tex.width * scale / 32.0);
                            npy = (int)(py - tex.height * scale / 32.0);
                            drawTexture(tex, tex.width, tex.height, 0, pixels,
                                (int)(npx - shiftx), (int)(npy - shifty), width, height, scale / 16.0, lightR, lightG, lightB);
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    int bofs = y * width * 4;
                    int sy = (int)(y / scale + starty);
                    for (int x = 0; x < width; x++)
                    {
                        int sx = (int)(x / scale + startx);
                        UInt32 c = 0xffffff;
                        if (sx >= 0 && sx < tilesWide && sy >= 0 && sy < tilesHigh)
                        {
                            Tile tile = tiles[sx, sy];
                            if (sy < groundLevel)
                                c = skyColor;
                            else if (sy < rockLevel)
                                c = earthColor;
                            else
                            {
                                //fade between rockColor and hellColor...
                                double alpha = (double)(sy - rockLevel) / (double)(tilesHigh - rockLevel);
                                c = alphaBlend(rockColor, hellColor, alpha);
                            }
                            if (tile.wall > 0)
                            {
                                c = wallInfo[tile.wall].color;
                            }
                            if (tile.isActive)
                            {
                                c = tileInfos[tile.type, tile.u, tile.v].color;
                                if (isHilight && tileInfos[tile.type, tile.u, tile.v].isHilighting)
                                    c = alphaBlend(c, 0xff88ff, 0.9);
                            }
                            if (tile.liquid > 0)
                                c = alphaBlend(c, tile.isLava ? lavaColor : waterColor, 0.5);
                            if (light == 1)
                                c = alphaBlend(0, c, tile.light);
                            else if (light == 2)
                            {
                                uint r, g, b;
                                r = (uint)((c >> 16) * tile.lightR);
                                g = (uint)(((c >> 8) & 0xff) * tile.lightG);
                                b = (uint)((c & 0xff) * tile.lightB);
                                c = (r << 16) | (g << 8) | b;
                            }
                            if (isHilight && (!tile.isActive || !tileInfos[tile.type, tile.u, tile.v].isHilighting))
                                c = alphaBlend(0, c, 0.3);
                        }
                        pixels[bofs++] = (byte)(c & 0xff);
                        pixels[bofs++] = (byte)((c >> 8) & 0xff);
                        pixels[bofs++] = (byte)((c >> 16) & 0xff);
                        pixels[bofs++] = 0xff;
                    }
                }
            }
        }
        private int findCorruptGrass(int x, int y, ref Tile[,] tiles)
        {
            for (int i = 0; i < 100; i++)
            {
                Tile tile = tiles[x, y + i];
                if (tile.type == 2) //normal grass
                    return 0;
                if (tile.type == 23) //corrupt grass
                    return 1;
                if (tile.type == 60) //jungle grass
                    return 2;
                if (tile.type == 109) //hallowed grass
                    return 3;
                if (tile.type == 147) //snow block
                    return 4;
            }
            return 0;
        }

        private void drawWire(int sx, int sy, byte[] pixels, int px, int py, int w, int h, double zoom, double lightR, double lightG, double lightB, ref Tile[,] tiles)
        {
            int mask = 0;
            //udlr
            if (sx < tilesWide - 1 && tiles[sx + 1, sy].hasWire) mask |= 1; //right
            if (sx > 0 && tiles[sx - 1, sy].hasWire) mask |= 2; //left
            if (sy < tilesHigh - 1 && tiles[sx, sy + 1].hasWire) mask |= 4; //down
            if (sy > 0 && tiles[sx, sy - 1].hasWire) mask |= 8; //up
            Texture tex = Textures.GetWire(0);
            drawTexture(tex, 16, 16, uvWires[mask * 2 + 1] * tex.width * 4 + uvWires[mask * 2] * 4, pixels,
                px, py, w, h, zoom, lightR, lightG, lightB);
        }

        private void drawLeaves(int u, int v, int sx, int sy,
            byte[] pixels, int px, int py,
            int w, int h, double zoom, double lightR, double lightG, double lightB, ref Tile[,] tiles)
        {
            if (u < 22 || v < 198) return; //not a leaf
            int variant = 0;
            if (v == 220) variant = 1;
            else if (v == 242) variant = 2;

            Texture tex;
            int leafType;
            switch (u)
            {
                case 22: //tree top
                    leafType = findCorruptGrass(sx, sy, ref tiles);
                    tex = Textures.GetTreeTops(leafType);
                    if (leafType == 3) //hallowed
                    {
                        variant += (sx % 3) * 3;
                        drawTexture(tex, 80, 140, variant * 82 * 4, pixels,
                            px - (int)(30 * zoom), py - (int)(124 * zoom), w, h, zoom, lightR, lightG, lightB);
                    }
                    else if (leafType == 2)
                        drawTexture(tex, 114, 96, variant * 116 * 4, pixels,
                            px - (int)(46 * zoom), py - (int)(80 * zoom), w, h, zoom, lightR, lightG, lightB);
                    else
                        drawTexture(tex, 80, 80, variant * 82 * 4, pixels,
                            px - (int)(30 * zoom), py - (int)(62 * zoom), w, h, zoom, lightR, lightG, lightB);
                    break;
                case 44: //left branch
                    leafType = findCorruptGrass(sx + 1, sy, ref tiles);
                    tex = Textures.GetTreeBranches(leafType);
                    if (leafType == 3) //hallowed
                        variant += (sx % 3) * 3;
                    drawTexture(tex, 40, 40, variant * 42 * tex.width * 4, pixels,
                        px - (int)(22 * zoom), py - (int)(12 * zoom), w, h, zoom, lightR, lightG, lightB);
                    break;
                case 66: //right branch
                    leafType = findCorruptGrass(sx - 1, sy, ref tiles);
                    tex = Textures.GetTreeBranches(leafType);
                    if (leafType == 3) //hallowed
                        variant += (sx % 3) * 3;
                    drawTexture(tex, 40, 40, variant * 42 * tex.width * 4 + 42 * 4, pixels,
                        px, py - (int)(12 * zoom), w, h, zoom, lightR, lightG, lightB);
                    break;
            }
        }
        private void drawMushroom(int u, int v,
            byte[] pixels, int px, int py,
            int w, int h, double zoom, double lightR, double lightG, double lightB)
        {
            if (u < 36) return; //not mushroom top
            int variant = 0;
            if (v == 18)
                variant = 1;
            else if (v == 36)
                variant = 2;
            Texture tex = Textures.GetShroomTop(0);
            drawTexture(tex, 60, 42, variant * 62 * 4, pixels,
                px - (int)(22 * zoom), py - (int)(26 * zoom), w, h, zoom, lightR, lightG, lightB);
        }

        void drawTexture(Texture tex, int bw, int bh, int tofs,
            byte[] pixels, int px, int py,
            int w, int h, double zoom, double lightR, double lightG, double lightB)
        {
            int tw = (int)(bw * zoom);
            int th = (int)(bh * zoom);
            int skipx = 0, skipy = 0;
            if (px < 0) skipx = -px;
            if (px + tw >= w) tw = w - px;
            if (bw <= 0) return;
            if (py < 0) skipy = -py;
            if (py + th >= h) th = h - py;
            if (bh <= 0) return;

            int bofs = py * w * 4 + px * 4;
            for (int y = 0; y < th; y++)
            {
                if (y < skipy)
                {
                    bofs += w * 4;
                    continue;
                }
                int t = tofs + (int)(y / zoom) * tex.width * 4;
                if (t >= tex.data.Length) continue;
                int b = bofs;
                for (int x = 0; x < tw; x++)
                {
                    int tx = t + (int)(x / zoom) * 4;
                    if (x < skipx || tex.data[tx + 3] == 0)
                    {
                        b += 4;
                        continue;
                    }
                    if (tex.data[tx + 3] < 255) //alpha blend, damn
                    {
                        UInt32 c = (uint)(tex.data[tx] * lightB) | ((uint)(tex.data[tx + 1] * lightG) << 8) | ((uint)(tex.data[tx + 2] * lightR) << 16);
                        UInt32 orig = (UInt32)(pixels[b] | (pixels[b + 1] << 8) | (pixels[b + 2] << 16));
                        orig = alphaBlend(orig, c, tex.data[tx + 3] / 255.0);
                        pixels[b++] = (byte)(orig & 0xff);
                        pixels[b++] = (byte)((orig >> 8) & 0xff);
                        pixels[b++] = (byte)((orig >> 16) & 0xff);
                        pixels[b++] = 0xff;
                    }
                    else
                    {
                        pixels[b++] = (byte)(tex.data[tx++] * lightB);
                        pixels[b++] = (byte)(tex.data[tx++] * lightG);
                        pixels[b++] = (byte)(tex.data[tx++] * lightR);
                        pixels[b++] = 0xff;
                    }
                }
                bofs += w * 4;
            }
        }
        void drawTextureFlip(Texture tex, int bw, int bh, int tofs,
            byte[] pixels, int px, int py,
            int w, int h, double zoom, double lightR, double lightG, double lightB)
        {
            int tw = (int)(bw * zoom);
            int th = (int)(bh * zoom);
            int skipx = 0, skipy = 0;
            if (px < 0) skipx = -px;
            if (px + tw >= w) tw = w - px;
            if (bw <= 0) return;
            if (py < 0) skipy = -py;
            if (py + th >= h) th = h - py;
            if (bh <= 0) return;

            int bofs = py * w * 4 + px * 4;
            for (int y = 0; y < th; y++)
            {
                if (y < skipy)
                {
                    bofs += w * 4;
                    continue;
                }
                int t = tofs + (int)(y / zoom) * tex.width * 4;
                if (t >= tex.data.Length) continue;
                int b = bofs;
                for (int x = 0; x < tw; x++)
                {
                    int tx = t + (int)(bw - x / zoom) * 4;
                    if (x < skipx || tex.data[tx + 3] == 0)
                    {
                        b += 4;
                        continue;
                    }
                    if (tex.data[tx + 3] < 255) //alpha blend
                    {
                        UInt32 c = (uint)(tex.data[tx] * lightB) | ((uint)(tex.data[tx + 1] * lightG) << 8) | ((uint)(tex.data[tx + 2] * lightR) << 16);
                        UInt32 orig = (UInt32)(pixels[b] | (pixels[b + 1] << 8) | (pixels[b + 2] << 16));
                        orig = alphaBlend(orig, c, tex.data[tx + 3] / 255.0);
                        pixels[b++] = (byte)(orig & 0xff);
                        pixels[b++] = (byte)((orig >> 8) & 0xff);
                        pixels[b++] = (byte)((orig >> 16) & 0xff);
                        pixels[b++] = 0xff;
                    }
                    else
                    {
                        pixels[b++] = (byte)(tex.data[tx++] * lightB);
                        pixels[b++] = (byte)(tex.data[tx++] * lightG);
                        pixels[b++] = (byte)(tex.data[tx++] * lightR);
                        pixels[b++] = 0xff;
                    }
                }
                bofs += w * 4;
            }
        }
        void drawOverlay(UInt32 color, double alpha, int amount, byte[] pixels, int px, int py, int w, int h, double zoom)
        {
            int skipx = 0, skipy = 0;
            if (px < 0) skipx = -px;
            int bw = (int)(zoom * 16);
            if (px + bw >= w) bw = w - px;
            if (bw <= 0) return;
            if (py < 0) skipy = -py;
            int bh = (int)(zoom * 16);
            skipy += bh - (amount * bh / 255);
            if (py + bh >= h) bh = h - py;
            if (bh <= 0) return;
            int bofs = py * w * 4 + px * 4;
            for (int y = 0; y < bh; y++)
            {
                if (y < skipy)
                {
                    bofs += w * 4;
                    continue;
                }
                for (int x = 0; x < bw; x++)
                {
                    if (x < skipx)
                    {
                        bofs += 4;
                        continue;
                    }
                    UInt32 orig = (UInt32)(pixels[bofs] | (pixels[bofs + 1] << 8) | (pixels[bofs + 2] << 16));
                    orig = alphaBlend(orig, color, alpha);
                    pixels[bofs++] = (byte)(orig & 0xff);
                    pixels[bofs++] = (byte)((orig >> 8) & 0xff);
                    pixels[bofs++] = (byte)((orig >> 16) & 0xff);
                    pixels[bofs++] = 0xff;
                }
                bofs += (w - bw) * 4;
            }
        }

        class UVRule
        {
            public UInt32 mask, val;
            public Int16[] uvs;
            public byte blend;
            public UVRule(UInt32 m, UInt32 v, Int16 u1, Int16 v1, Int16 u2, Int16 v2,
                Int16 u3, Int16 v3)
            {
                mask = m;
                val = v;
                blend = 0;
                uvs = new Int16[] { u1, v1, u2, v2, u3, v3 };
            }
            public UVRule(UInt32 m, UInt32 v, Int16 u1, Int16 v1, Int16 u2, Int16 v2,
                Int16 u3, Int16 v3, byte blendDirs)
            {
                mask = m;
                val = v;
                blend = blendDirs;
                uvs = new Int16[] { u1, v1, u2, v2, u3, v3 };
            }
        };

        UVRule[][] grassRules ={
            new UVRule[] {}, //0000
            new UVRule[] {}, //0001
            new UVRule[] {}, //0010
            new UVRule[] {}, //0011
            new UVRule[] {}, //0100
            new UVRule[] { //0101
                new UVRule(0xf5100,0x50000, 90,270,  108,270, 126,270)
            },
            new UVRule[] { //0110
                new UVRule(0xf6200,0x60000, 144,270, 162,270, 180,270)
            },
            new UVRule[] { //0111
                new UVRule(0xf3000,0x72000, 0,198,   18,198,  36,198, 6), //down,left
                new UVRule(0xf3000,0x71000, 54,198,  72,198,  90,198, 5), //down,right
                new UVRule(0xf7300,0x70000, 144,216, 198,216, 252,216),
                new UVRule(0xf3000,0x73000, 36,270,  54,270,  72,270, 7), //left,right,down
                new UVRule(0xf7300,0x70200, 0,324,   18,324,   36,324),
                new UVRule(0xf7300,0x70100, 54,324,  72,324,   90,324)
            },
            new UVRule[] {}, //1000
            new UVRule[] { //1001
                new UVRule(0xf9400,0x90000, 90,288,  108,288, 126,288)
            },
            new UVRule[] { //1010
                new UVRule(0xfa800,0xa0000, 144,288, 162,288, 180,288)
            },
            new UVRule[] { //1011
                new UVRule(0xf3000,0xb2000, 0,216,   18,216,  36,216, 10), //top,left
                new UVRule(0xf3000,0xb1000, 54,216,  72,216,  90,216, 9), //top,right
                new UVRule(0xfbc00,0xb0000, 144,252, 198,252, 252,252),
                new UVRule(0xf3000,0xb3000, 36,288,  54,288,  72,288, 11), //top,left,right
                new UVRule(0xfbc00,0xb0800, 0,342,   18,342,   36,342),
                new UVRule(0xfbc00,0xb0400, 54,342,  72,342,   90,342)
            },
            new UVRule[] {}, //1100
            new UVRule[] { //1101
                new UVRule(0xfc000,0xd8000, 72,144,  72,162,  72,180, 9), //top,right
                new UVRule(0xfc000,0xd4000, 72,90,   72,108,  72,126, 5), //bottom,right
                new UVRule(0xfd500,0xd0000, 126,234, 180,234, 234,234),
                new UVRule(0xfc000,0xdc000, 0,270,   0,288,   0,306, 13), //top,bottom,right
                new UVRule(0xfd500,0xd0400, 54,360,  72,360,   90,360),
                new UVRule(0xfd500,0xd0100, 0,360,   18,360,   36,360)
            },
            new UVRule[] { //1110
                new UVRule(0xfc000,0xe8000, 90,144,  90,162,  90,180, 10), //top,left
                new UVRule(0xfc000,0xe4000, 90,90,   90,108,  90,126, 6), //bottom,left
                new UVRule(0xfea00,0xe0000, 162,234, 216,234, 270,234),
                new UVRule(0xfc000,0xec000, 18,270,  18,288,  18,306, 14), //top,bottom,left
                new UVRule(0xfea00,0xe0800, 0,378,   18,378,   36,378),
                new UVRule(0xfea00,0xe0200, 54,378,  72,378,   90,378)
            },
            new UVRule[] { //1111
                new UVRule(0xff0f1,0xf0001, 108,324, 126,324, 144,324),
                new UVRule(0xff0f4,0xf0004, 108,342, 126,342, 144,342),
                new UVRule(0xff0f2,0xf0002, 108,360, 126,360, 144,360),
                new UVRule(0xff0f8,0xf0008, 108,378, 126,378, 144,378),
                new UVRule(0xff0f0,0xf0000, 144,234, 198,234, 252,234),
                new UVRule(0xff090,0xf0000, 36,306,  54,306,  72,306),
                new UVRule(0xff060,0xf0000, 90,306,  108,306, 126,306),
                new UVRule(0xff0f0,0xf0070, 54,108,  54,144,  54,180, 5), //bottom,right
                new UVRule(0xff0f0,0xf00b0, 36,108,  36,144,  36,180, 6), //bottom,left
                new UVRule(0xff0f0,0xf00d0, 54,90,   54,126,  54,162, 9), //top,right
                new UVRule(0xff0f0,0xf00e0, 36,90,   36,126,  36,162, 10), //top,left
                new UVRule(0xffc00,0xf4000, 108,18,  126,18,  144,18, 4), //bottom
                new UVRule(0xff300,0xf8000, 108,36,  126,36,  144,36, 8), //top
                new UVRule(0xffc00,0xf2000, 198,0,   198,18,  198,36, 2), //left
                new UVRule(0xffa00,0xf1000, 180,0,   180,18,  180,36, 1), //right
                new UVRule(0xff400,0xf4400, 54,108,  54,144,  54,180, 5), //bottom,right
                new UVRule(0xff800,0xf4800, 36,108,  36,144,  36,180, 6), //bottom,left
                new UVRule(0xff100,0xf8100, 54,90,   54,126,  54,162, 9), //top,right
                new UVRule(0xff200,0xf8200, 36,90,   36,126,  36,162, 10), //top,left
                new UVRule(0xff800,0xf1800, 54,90,   54,126,  54,162, 9), //top,right
                new UVRule(0xff200,0xf1200, 54,108,  54,144,  54,180, 5), //bottom,right
                new UVRule(0xff400,0xf2400, 36,90,   36,126,  36,162, 10), //top,left
                new UVRule(0xff100,0xf2100, 36,108,  36,144,  36,180, 6), //bottom,left
                new UVRule(0xfb000,0xf0000, 18,18,   36,18,   54,18, 15), //all
                new UVRule(0xff000,0xf2000, 18,18,   36,18,   54,18, 15), //all
                new UVRule(0xff000,0xf1000, 18,18,   36,18,   54,18, 15), //all
                new UVRule(0xf0f00,0xf0700, 54,108,  54,144,  54,180, 5), //bottom,right
                new UVRule(0xf0f00,0xf0b00, 36,108,  36,144,  36,180, 6), //bottom,left
                new UVRule(0xf0f00,0xf0d00, 54,90,   54,126,  54,162, 9), //top,right
                new UVRule(0xf0f00,0xf0e00, 36,90,   36,126,  36,162, 10), //top,left
                new UVRule(0xff000,0xf7000, 198,288, 216,288, 234,288, 7), //left,right,bottom
                new UVRule(0xff000,0xfb000, 198,270, 216,270, 234,270, 11), //left,right,top
                new UVRule(0xff000,0xfd000, 198,306, 216,306, 234,306, 13), //top,bottom,right
                new UVRule(0xff000,0xfe000, 144,306, 162,306, 180,306, 14), //top,bottom,left
                new UVRule(0xf0f00,0xf0f00, 18,18,   36,18,    54,18, 15) //all
            }
        };
        UVRule[][] blendRules ={
            new UVRule[] {}, //0000
            new UVRule[] {}, //0001
            new UVRule[] {}, //0010
            new UVRule[] {}, //0011
            new UVRule[] {}, //0100
            new UVRule[] {}, //0101
            new UVRule[] {}, //0110
            new UVRule[] { //0111
                new UVRule(0xf7000,0x74000, 234,0,   252,0,   270,0, 4) //down
            },
            new UVRule[] {}, //1000
            new UVRule[] {}, //1001
            new UVRule[] {}, //1010
            new UVRule[] { //1011
                new UVRule(0xfb000,0xb8000, 234,18,  252,18,  270,18, 8) //up
            },
            new UVRule[] {}, //1100
            new UVRule[] { //1101
                new UVRule(0xfd000,0xd1000, 234,36,  252,36,  270,36, 1) //right
            },
            new UVRule[] { //1110
                new UVRule(0xfe000,0xe2000, 234,54,  252,54,  270,54, 2) //left
            },
            new UVRule[] {} //1111
        };
        UVRule[][] blendGrassRules ={
            new UVRule[] {}, //0000
            new UVRule[] { //0001
                new UVRule(0xf1000,0x11000, 54,234,  72,234,  90,234, 1) //right
            },
            new UVRule[] { //0010
                new UVRule(0xf2000,0x22000, 0,234,   18,234,  36,234, 2) //left
            },
            new UVRule[] { //0011
                new UVRule(0xf3000,0x33000, 162,198, 180,198, 198,198, 3), //left,right
                new UVRule(0xf2000,0x32000, 0,252,   18,252,  36,252, 2), //left
                new UVRule(0xf1000,0x31000, 54,252,  72,252,  90,252, 1) //right
            },
            new UVRule[] { //0100
                new UVRule(0xf4000,0x44000, 108,90,  108,108, 108,126, 4) //down
            },
            new UVRule[] {}, //0101
            new UVRule[] {}, //0110
            new UVRule[] { //0111
                new UVRule(0xf7000,0x72000, 0,198,   18,198,  36,198, 2), //left
                new UVRule(0xf7000,0x71000, 54,198,  72,198,  90,198, 1) //right
            },
            new UVRule[] { //1000
                new UVRule(0xf8000,0x88000, 108,144, 108,162, 108,180, 8) //up
            },
            new UVRule[] {}, //1001
            new UVRule[] {}, //1010
            new UVRule[] { //1011
                new UVRule(0xfb000,0xb2000, 0,216,   18,216,  36,216, 2), //left
                new UVRule(0xfb000,0xb1000, 54,216,  72,216,  90,216, 1) //right
            },
            new UVRule[] { //1100
                new UVRule(0xfc000,0xcc000, 108,216, 108,234, 108,252, 12), //up,down
                new UVRule(0xf8000,0xc8000, 126,144, 126,162, 126,180, 8), //up
                new UVRule(0xf4000,0xc4000, 126,90,  126,108, 126,126, 4) //down
            },
            new UVRule[] { //1101
                new UVRule(0xfd000,0xd8000, 72,144,  72,162,  72,180, 8), //up
                new UVRule(0xfd000,0xd4000, 72,90,   72,108,  72,126, 4) //down
            },
            new UVRule[] { //1110
                new UVRule(0xfe000,0xe8000, 90,144,  90,162,  90,180, 8), //up
                new UVRule(0xfe000,0xe4000, 90,90,   90,108,  90,126, 4) //down
            },
            new UVRule[] { //1111
                new UVRule(0xff000,0xf8000, 144,108, 162,108, 180,108, 8), //up
                new UVRule(0xff000,0xf4000, 144,90,  162,90,  180,90, 4), //down
                new UVRule(0xff000,0xf2000, 162,126, 162,144, 162,162, 2), //left
                new UVRule(0xff000,0xf1000, 144,126, 144,144, 144,162, 1), //right
                new UVRule(0xff000,0xfa000, 36,90,   36,126,  36,162,  10), //top,left
                new UVRule(0xff000,0xf9000, 54,90,   54,126,  54,162, 9), //top,right
                new UVRule(0xff000,0xf6000, 36,108,  36,144,  36,180, 6), //bottom,left
                new UVRule(0xff000,0xf5000, 54,108,  54,144,  54,180, 5), //bottom,right
                new UVRule(0xff000,0xf3000, 180,126, 180,144, 180,162, 3), //left,right
                new UVRule(0xff000,0xfc000, 144,180, 162,180, 180,180, 12), //top,bottom
                new UVRule(0xff000,0xfb000, 198,90,  198,108, 198,126, 11), //top,left,right
                new UVRule(0xff000,0xf7000, 198,144, 198,162, 198,180, 7), //bottom,left,right
                new UVRule(0xff000,0xfd000, 216,144, 216,162, 216,180, 13), //top,bottom,right
                new UVRule(0xff000,0xfe000, 216,90,  216,108, 216,126, 14), //top,bottom,left
                new UVRule(0xff000,0xff000, 108,198, 126,198, 144,198, 15), //all
                new UVRule(0xff008,0xf0008, 18,108,  18,144,  18,180),
                new UVRule(0xff004,0xf0004, 0,108,   0,144,   0,180),
                new UVRule(0xff002,0xf0002, 18,90,   18,126,  18,162),
                new UVRule(0xff001,0xf0001, 0,90,    0,126,   0,162)
            }
        };
        UVRule[][] uvRules ={
            new UVRule[] { //0000
                new UVRule(0xf0000,0x00000, 162,54,  180,54,  198,54)
            },
            new UVRule[] { //0001
                new UVRule(0xf0000,0x10000, 162,0,   162,18,  162,36, 1)
            },
            new UVRule[] { //0010
                new UVRule(0xf0000,0x20000, 216,0,   216,18,  216,36, 2)
            },
            new UVRule[] { //0011
                new UVRule(0xf0000,0x30000, 108,72,  126,72,  144,72, 3)
            },
            new UVRule[] { //0100
                new UVRule(0xf0000,0x40000, 108,0,   126,0,   144,0, 4)
            },
            new UVRule[] { //0101
                new UVRule(0xf0000,0x50000, 0,54,    36,54,   72,54, 5)
            },
            new UVRule[] { //0110
                new UVRule(0xf0000,0x60000, 18,54,   54,54,   90,54, 6)
            },
            new UVRule[] { //0111
                new UVRule(0xf0000,0x70000, 18,0,    36,0,    54,0, 7)
            },
            new UVRule[] { //1000
                new UVRule(0xf0000,0x80000, 108,54,  126,54,  144,54, 8)
            },
            new UVRule[] { //1001
                new UVRule(0xf0000,0x90000, 0,72,    36,72,   72,72, 9)
            },
            new UVRule[] { //1010
                new UVRule(0xf0000,0xa0000, 18,72,   54,72,   90,72, 10)
            },
            new UVRule[] { //1011
                new UVRule(0xf0000,0xb0000, 18,36,   36,36,   54,36, 11)
            },
            new UVRule[] { //1100
                new UVRule(0xf0000,0xc0000, 90,0,    90,18,   90,36, 12)
            },
            new UVRule[] { //1101
                new UVRule(0xf0000,0xd0000, 0,0,     0,18,    0,36, 13)
            },
            new UVRule[] { //1110
                new UVRule(0xf0000,0xe0000, 72,0,    72,18,   72,36, 14)
            },
            new UVRule[] { //1111
                new UVRule(0xf0c00,0xf0000, 108,18,  126,18,  144,18, 15),
                new UVRule(0xf0300,0xf0000, 108,36,  126,36,  144,36, 15),
                new UVRule(0xf0a00,0xf0000, 180,0,   180,18,  180,36, 15),
                new UVRule(0xf0500,0xf0000, 198,0,   198,18,  198,36, 15),
                new UVRule(0xf0000,0xf0000, 18,18,   36,18,   54,18, 15)
            }
        };


        UVRule[] cactusRules ={
                new UVRule(0xbf,0x30, 90,0,   0,0, 0,0),
                new UVRule(0xab,0x20, 72,0,   0,0, 0,0),
                new UVRule(0x97,0x10, 18,0,   0,0, 0,0),
                new UVRule(0x83,0x00, 0,0,    0,0, 0,0),
                new UVRule(0x3f,0x30, 90,36,  0,0, 0,0),
                new UVRule(0x2b,0x20, 72,36,  0,0, 0,0),
                new UVRule(0x17,0x10, 18,36,  0,0, 0,0),
                new UVRule(0x43,0x40, 0,36,   0,0, 0,0),
                new UVRule(0x03,0x00, 0,18,   0,0, 0,0),
                new UVRule(0xd3,0x11, 108,36, 0,0, 0,0),
                new UVRule(0xd3,0x91, 54,36,  0,0, 0,0),
                new UVRule(0x83,0x01, 54,0,   0,0, 0,0),
                new UVRule(0x83,0x81, 54,18,  0,0, 0,0),
                new UVRule(0xe3,0x22, 108,18, 0,0, 0,0),
                new UVRule(0xe3,0xa2, 36,36,  0,0, 0,0),
                new UVRule(0x83,0x02, 36,0,   0,0, 0,0),
                new UVRule(0x83,0x82, 36,18,  0,0, 0,0)
                             };

        Int16[] uvPlatforms ={      //LRlr
                        0,0,        //0000 impossible
                        108,0,      //0001 mount left no right
                        126,0,      //0010 no left, mount right
                        90,0,       //0011 float
                        54,0,       //0100 mount left, go right
                        0,0,        //0101 impossible
                        36,0,       //0110 no left, go right
                        0,0,        //0111 impossible
                        72,0,       //1000 go left mount right
                        18,0,       //1001 go left no right
                        0,0,        //1010 impossible
                        0,0,        //1011 impossible
                        0,0,        //1100 middle
                        0,0,        //1101 impossible
                        0,0,        //1110 impossible
                        0,0};       //1111 impossible

        Int16[] uvWires = {         //udlr
                        0,54,       //0000
                        72,36,      //0001
                        54,36,      //0010
                        18,0,       //0011
                        18,36,      //0100
                        0,36,       //0101
                        72,18,      //0110
                        72,0,       //0111
                        36,36,      //1000
                        36,18,      //1001
                        54,18,      //1010
                        0,18,       //1011
                        0,0,        //1100
                        36,0,       //1101
                        54,0,       //1110
                        18,18       //1111
                          };

        private byte fixTile(int x, int y, ref Tile[,] tiles)
        {
            int t = -1, l = -1, r = -1, b = -1;
            int tl = -1, tr = -1, bl = -1, br = -1;
            byte c = tiles[x, y].type;
            Int16 u;
            int set = rand.Next(0, 3) * 2;

            if (x > 0)
            {
                if (tiles[x - 1, y].isActive)
                    l = tiles[x - 1, y].type;
                if (y > 0)
                {
                    if (tiles[x - 1, y - 1].isActive)
                        tl = tiles[x - 1, y - 1].type;
                }
                if (y < tilesHigh - 1)
                {
                    if (tiles[x - 1, y + 1].isActive)
                        bl = tiles[x - 1, y + 1].type;
                }
            }
            if (x < tilesWide - 1)
            {
                if (tiles[x + 1, y].isActive)
                    r = tiles[x + 1, y].type;
                if (y > 0)
                {
                    if (tiles[x + 1, y - 1].isActive)
                        tr = tiles[x + 1, y - 1].type;
                }
                if (y < tilesHigh - 1)
                {
                    if (tiles[x + 1, y + 1].isActive)
                        br = tiles[x + 1, y + 1].type;
                }
            }
            if (y > 0)
                if (tiles[x, y - 1].isActive)
                    t = tiles[x, y - 1].type;
            if (y < tilesHigh - 1)
                if (tiles[x, y + 1].isActive)
                    b = tiles[x, y + 1].type;

            int mask;
            if (c == 33 || c == 49) //candles
            {
                // these tiles don't have u/v, but are single tiles
                tiles[x, y].u = 0;
                tiles[x, y].v = 0;
                return 0;
            }
            if (c == 4) //torch
            {
                //check if left is moutable or right is mountable.. set to -1 if not
                u = 0;
                if (b < 0) //no bottom
                {
                    if (l >= 0) u = 22;
                    else u = 44;
                }
                tiles[x, y].u = u;
                tiles[x, y].v = 0;
                return 0;
            }
            if (c == 80) //cactus
            {
                fixCactus(x, y, t, b, l, r, bl, br, ref tiles);
                return 0;
            }
            if (c == 19) //wooden platform
            {
                // check if left is mountable or right is mountable.. set to -1 if not
                mask = 0;
                if (l == c) mask |= 8;
                if (r == c) mask |= 4;
                if (l == -1) mask |= 2;
                if (r == -1) mask |= 1;
                tiles[x, y].u = uvPlatforms[mask * 2];
                tiles[x, y].v = uvPlatforms[mask * 2 + 1];
                return 0;
            }

            mask = 0;
            //if tile is stone and next to stone, treat them as the same
            if (tileInfos[c].isStone)
            {
                if (t > -1 && tileInfos[t].isStone) mask |= 0x80000;
                if (b > -1 && tileInfos[b].isStone) mask |= 0x40000;
                if (l > -1 && tileInfos[l].isStone) mask |= 0x20000;
                if (r > -1 && tileInfos[r].isStone) mask |= 0x10000;
                if (tl > -1 && tileInfos[tl].isStone) mask |= 0x880;
                if (tr > -1 && tileInfos[tr].isStone) mask |= 0x440;
                if (bl > -1 && tileInfos[bl].isStone) mask |= 0x220;
                if (br > -1 && tileInfos[br].isStone) mask |= 0x110;
            }

            //if tile blends with current tile, treat as if it were current tile
            if (t > -1 && (t == c || (tileInfos[t].blend == c && (fixTile(x, y - 1, ref tiles) & 4) == 4))) mask |= 0x80000;
            if (b > -1 && (b == c || (tileInfos[b].blend == c && (fixTile(x, y + 1, ref tiles) & 8) == 8))) mask |= 0x40000;
            if (l > -1 && (l == c || (tileInfos[l].blend == c && (fixTile(x - 1, y, ref tiles) & 1) == 1))) mask |= 0x20000;
            if (r > -1 && (r == c || (tileInfos[r].blend == c && (fixTile(x + 1, y, ref tiles) & 2) == 2))) mask |= 0x10000;
            if (tl > -1 && (tileInfos[tl].blend == c || tl == c)) mask |= 0x880;
            if (tr > -1 && (tileInfos[tr].blend == c || tr == c)) mask |= 0x440;
            if (bl > -1 && (tileInfos[bl].blend == c || bl == c)) mask |= 0x220;
            if (br > -1 && (tileInfos[br].blend == c || br == c)) mask |= 0x110;

            //if current tile can blend, set up the blend rules.
            Int16 blend = tileInfos[c].blend;
            if (blend > -1)
            {
                if (t == blend) mask |= 0x88000;
                if (b == blend) mask |= 0x44000;
                if (l == blend) mask |= 0x22000;
                if (r == blend) mask |= 0x11000;
                if (tl == blend) mask |= 0x808;
                if (tr == blend) mask |= 0x404;
                if (bl == blend) mask |= 0x202;
                if (br == blend) mask |= 0x101;
            }

            if (c == 32 && b == 23) mask |= 0x40000; //corrupted vines above corrupted grass
            if (c == 69 && b == 60) mask |= 0x40000; //jungle thorn above jungle grass
            if (c == 51) //cobwebs blend with everything
            {
                if (t > -1) mask |= 0x80000;
                if (b > -1) mask |= 0x40000;
                if (l > -1) mask |= 0x20000;
                if (r > -1) mask |= 0x10000;
                if (tl > -1) mask |= 0x880;
                if (tr > -1) mask |= 0x440;
                if (bl > -1) mask |= 0x220;
                if (br > -1) mask |= 0x110;
            }

            if (tileInfos[c].isGrass) //do grasses
            {
                foreach (UVRule rule in grassRules[mask >> 16])
                {
                    if ((mask & rule.mask) == rule.val)
                    {
                        tiles[x, y].u = rule.uvs[set];
                        tiles[x, y].v = rule.uvs[set + 1];
                        return rule.blend;
                    }
                }
            }
            if (tileInfos[c].blend > -1 && !tileInfos[c].isGrass) //do blend-onlys
            {
                foreach (UVRule rule in blendRules[mask >> 16])
                {
                    if ((mask & rule.mask) == rule.val)
                    {
                        tiles[x, y].u = rule.uvs[set];
                        tiles[x, y].v = rule.uvs[set + 1];
                        return rule.blend;
                    }
                }
            }
            if (tileInfos[c].blend > -1) //do blend and unmatched grasses
            {
                foreach (UVRule rule in blendGrassRules[mask >> 16])
                {
                    if ((mask & rule.mask) == rule.val)
                    {
                        tiles[x, y].u = rule.uvs[set];
                        tiles[x, y].v = rule.uvs[set + 1];
                        return rule.blend;
                    }
                }
            }
            // no match, delete blends
            if (tileInfos[c].isGrass) //with grasses, blends are equal
                mask &= 0xf0f00;
            else
                mask = ((mask & 0xf0000) ^ ((mask & 0xf000) << 4)) | ((mask & 0xf0) << 4);
            foreach (UVRule rule in uvRules[mask >> 16])
            {
                if ((mask & rule.mask) == rule.val)
                {
                    tiles[x, y].u = rule.uvs[set];
                    tiles[x, y].v = rule.uvs[set + 1];
                    return rule.blend;
                }
            }
            //should be impossible to get here.
            return 0;
        }
        private void fixWall(int x, int y, ref Tile[,] tiles)
        {
            byte t = 0, l = 0, r = 0, b = 0;
            byte tl = 0, tr = 0, bl = 0, br = 0;
            byte c = tiles[x, y].wall;
            int set = rand.Next(0, 3) * 2;

            if (x > 0)
            {
                l = tiles[x - 1, y].wall;
                if (y > 0)
                    tl = tiles[x - 1, y - 1].wall;
                if (y < tilesHigh - 1)
                    bl = tiles[x - 1, y + 1].wall;
            }
            if (x < tilesWide - 1)
            {
                r = tiles[x + 1, y].wall;
                if (y > 0)
                    tr = tiles[x + 1, y - 1].wall;
                if (y < tilesHigh - 1)
                    br = tiles[x + 1, y + 1].wall;
            }
            if (y > 0)
                t = tiles[x, y - 1].wall;
            if (y < tilesHigh - 1)
                b = tiles[x, y + 1].wall;

            int mask = 0;
            if (t == c) mask |= 0x80000;
            if (b == c) mask |= 0x40000;
            if (l == c) mask |= 0x20000;
            if (r == c) mask |= 0x10000;
            if (tl == c) mask |= 0x880;
            if (tr == c) mask |= 0x440;
            if (bl == c) mask |= 0x220;
            if (br == c) mask |= 0x110;

            foreach (UVRule rule in uvRules[mask >> 16])
            {
                if ((mask & rule.mask) == rule.val)
                {
                    tiles[x, y].wallu = rule.uvs[set];
                    tiles[x, y].wallv = rule.uvs[set + 1];
                    return;
                }
            }
            //again, impossible to be here.
        }

        private void fixCactus(int x, int y, int t, int b, int l, int r, int bl, int br, ref Tile[,] tiles)
        {
            //find the base of the cactus
            int basex = x;
            int basey = y;
            while (tiles[basex, basey].isActive &&
                tiles[basex, basey].type == 80)
            {
                basey++;
                if (!tiles[basex, basey].isActive || tiles[basex, basey].type != 80)
                {
                    if (tiles[basex - 1, basey].isActive &&
                        tiles[basex - 1, basey].type == 80 &&
                        tiles[basex - 1, basey - 1].isActive &&
                        tiles[basex - 1, basey - 1].type == 80 && basex >= x)
                        basex--;
                    if (tiles[basex + 1, basey].isActive &&
                        tiles[basex + 1, basey].type == 80 &&
                        tiles[basex + 1, basey - 1].isActive &&
                        tiles[basex + 1, basey - 1].type == 80 && basex <= x)
                        basex++;
                }
            }

            Int16 mask = 0;
            if (x < basex) mask |= 1;
            if (x > basex) mask |= 2;
            if (br == 80) mask |= 4;
            if (bl == 80) mask |= 8;
            if (r == 80) mask |= 0x10;
            if (l == 80) mask |= 0x20;
            if (b == 80) mask |= 0x40;
            if (t == 80) mask |= 0x80;

            foreach (UVRule rule in cactusRules)
            {
                if ((mask & rule.mask) == rule.val)
                {
                    tiles[x, y].u = rule.uvs[0];
                    tiles[x, y].v = rule.uvs[1];
                    return;
                }
            }
        }


        private UInt32 alphaBlend(UInt32 from, UInt32 to, double alpha)
        {
            uint fr = (from >> 16) & 0xff;
            uint fg = (from >> 8) & 0xff;
            uint fb = from & 0xff;
            uint tr = (to >> 16) & 0xff;
            uint tg = (to >> 8) & 0xff;
            uint tb = to & 0xff;
            fr = (uint)(tr * alpha + fr * (1 - alpha));
            fg = (uint)(tg * alpha + fg * (1 - alpha));
            fb = (uint)(tb * alpha + fb * (1 - alpha));
            return (fr << 16) | (fg << 8) | fb;
        }
    }
}
