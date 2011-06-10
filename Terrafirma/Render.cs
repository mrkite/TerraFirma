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
        private Tile[] tiles;
        private TileInfo[] tileInfo;
        private WallInfo[] wallInfo;
        private UInt32 skyColor, earthColor, rockColor, hellColor;
        private UInt32 waterColor, lavaColor;
        private Int32 tilesWide, tilesHigh;
        private int groundLevel, rockLevel;

        Random rand;

        public Textures Textures { set; get; }

        public Render(TileInfo[] tileInfo, WallInfo[] wallInfo,
            UInt32 skyColor, UInt32 earthColor, UInt32 rockColor, UInt32 hellColor,
            UInt32 waterColor, UInt32 lavaColor)
        {
            this.tileInfo = tileInfo;
            this.wallInfo = wallInfo;
            this.skyColor = skyColor;
            this.earthColor = earthColor;
            this.rockColor = rockColor;
            this.hellColor = hellColor;
            this.waterColor = waterColor;
            this.lavaColor = lavaColor;
            rand = new Random();
        }

        public void SetWorld(Tile[] tiles, Int32 tilesWide, Int32 tilesHigh,
            int groundLevel, int rockLevel)
        {
            this.tiles=tiles;
            this.tilesWide = tilesWide;
            this.tilesHigh = tilesHigh;
            this.groundLevel = groundLevel;
            this.rockLevel = rockLevel;
        }

        public void Draw(int width, int height,
            double startx, double starty,
            double scale, byte[] pixels,
            bool isHilight,byte hilight,int hilightTick,
            bool light,bool texture)
        {
            UInt32 blendcolor = 0;
            UInt32 tocolor = 0xffffff;
            double hialpha = 0.0;
            if (isHilight)
            {
                UInt32 hicolor = tileInfo[hilight].color;

                int r = ((hicolor >> 16) & 0xff) > 127 ? 1 : 0;
                int g = ((hicolor >> 8) & 0xff) > 127 ? 1 : 0;
                int b = (hicolor & 0xff) > 127 ? 1 : 0;

                if ((r ^ g ^ b) == 0) tocolor = 0;

                if (hilightTick > 7)
                    hialpha = (15 - hilightTick) / 7.0;
                else
                    hialpha = hilightTick / 7.0;
                blendcolor = alphaBlend(hicolor, tocolor, hialpha);
            }

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
                int py = (int)-(scale / 2), px;

                //draw background
                int bofs = 0;
                for (int y = 0; y < blocksHigh; y++)
                {
                    int sy = (int)(y + starty);
                    UInt32 c;
                    if (sy < groundLevel)
                        c = skyColor;
                    else if (sy < rockLevel)
                        c = earthColor;
                    else
                    {
                        double alpha = (double)(sy - rockLevel) / (double)(tilesHigh - rockLevel);
                        c = alphaBlend(rockColor, hellColor, alpha);
                    }
                    for (int x = 0; x < width * Math.Floor(scale) && bofs < pixels.Length; x++)
                    {
                        pixels[bofs++] = (byte)(c & 0xff);
                        pixels[bofs++] = (byte)((c >> 8) & 0xff);
                        pixels[bofs++] = (byte)((c >> 16) & 0xff);
                        pixels[bofs++] = 0xff;
                    }
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
                        int offset = sy + sx * tilesHigh;

                        if (sx < 0 || sx >= tilesWide || sy < 0 || sy >= tilesHigh)
                            continue;

                        if (tiles[offset].wall > 0)
                        {
                            if (tiles[offset].wallu == -1) fixWall(sx, sy);
                            Texture tex = Textures.GetWall(tiles[offset].wall);
                            drawTexture(tex, 32, 32, tiles[offset].wallv * tex.width * 4 * 2 + tiles[offset].wallu * 4 * 2,
                                pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);
                        }
                        px += (int)scale;
                    }
                    py += (int)scale;
                }
                //draw tiles
                py = skipy*(int)scale;
                for (int y = skipy; y < blocksHigh; y++)
                {
                    int sy = (int)(y + starty);
                    px = skipx*(int)scale;
                    for (int x = skipx; x < blocksWide; x++)
                    {
                        int sx = (int)(x + startx);
                        int offset = sy + sx * tilesHigh;

                        if (sx < 0 || sx >= tilesWide || sy < 0 || sy >= tilesHigh)
                            continue;

                        if (tiles[offset].isActive)
                        {
                            if (tiles[offset].u == -1) fixTile(sx, sy);

                            int texw=16;
                            int texh=16;

                            if (tiles[offset].type == 5) //tree
                            {
                                texw=20;
                                drawLeaves(tiles[offset].u, tiles[offset].v, sx, sy,
                                    pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);
                            }
                            if (tiles[offset].type == 72) //mushroom
                                drawMushroom(tiles[offset].u, tiles[offset].v,
                                    pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);

                            Texture tex = Textures.GetTile(tiles[offset].type);
                            drawTexture(tex, texw, texh, tiles[offset].v * tex.width * 4 + tiles[offset].u * 4,
                                pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);
                            if (isHilight && tiles[offset].type == hilight)
                            {
                                drawOverlay(tocolor, hialpha, 16, pixels,
                                    (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);
                            }
                        }
                        if (tiles[offset].liquid > 0)
                        {
                            drawOverlay(tiles[offset].isLava ? lavaColor : waterColor, 0.5, tiles[offset].liquid,
                                pixels, (int)(px - shiftx), (int)(py - shifty), width, height, scale / 16.0);
                        }
                        px += (int)scale;
                    }
                    py += (int)scale;
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
                        int offset = sy + sx * tilesHigh;
                        UInt32 c = 0xffffff;
                        if (sx >= 0 && sx < tilesWide && sy >= 0 && sy < tilesHigh)
                        {
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
                            if (tiles[offset].wall > 0)
                                c = wallInfo[tiles[offset].wall].color;
                            if (tiles[offset].isActive)
                            {
                                c = tileInfo[tiles[offset].type].color;
                                if (isHilight && hilight == tiles[offset].type)
                                    c = blendcolor;
                            }
                            if (tiles[offset].liquid > 0)
                                c = alphaBlend(c, tiles[offset].isLava ? lavaColor : waterColor, 0.5);
                            if (light && !tiles[offset].hasLight)
                                c = 0;
                        }
                        pixels[bofs++] = (byte)(c & 0xff);
                        pixels[bofs++] = (byte)((c >> 8) & 0xff);
                        pixels[bofs++] = (byte)((c >> 16) & 0xff);
                        pixels[bofs++] = 0xff;
                    }
                }
            }
        }

        private int findCorruptGrass(int ofs)
        {
            for (int i = 0; i < 100; i++)
            {
                if (tiles[ofs + i].type == 2) //normal grass
                    return 0;
                if (tiles[ofs + i].type == 23) //corrupt grass
                    return 1;
            }
            return 0;
        }

        private void drawLeaves(int u, int v, int sx, int sy,
            byte[] pixels, int px, int py,
            int w, int h, double zoom)
        {
            if (u < 22 || v < 198) return; //not a leaf
            int variant = 0;
            if (v == 220) variant = 1;
            else if (v == 242) variant = 2;

            Texture tex;
            switch (u)
            {
                case 22: //tree top
                    tex = Textures.GetTreeTops(findCorruptGrass(sy + sx * tilesHigh));
                    drawTexture(tex, 80, 80, variant * 82 * 4, pixels, px - (int)(30 * zoom), py - (int)(62 * zoom), w, h, zoom);
                    break;
                case 44: //left branch
                    tex = Textures.GetTreeBranches(findCorruptGrass(sy + (sx + 1) * tilesHigh));
                    drawTexture(tex, 40, 40, variant * 42 * tex.width * 4, pixels, px - (int)(22 * zoom), py - (int)(12 * zoom), w, h, zoom);
                    break;
                case 66: //right branch
                    tex = Textures.GetTreeBranches(findCorruptGrass(sy + (sx - 1) * tilesHigh));
                    drawTexture(tex, 40, 40, variant * 42 * tex.width * 4 + 42 * 4, pixels, px, py - (int)(12 * zoom), w, h, zoom);
                    break;
            }
        }
        private void drawMushroom(int u, int v,
            byte[] pixels, int px, int py,
            int w, int h, double zoom)
        {
            if (u < 36) return; //not mushroom top
            int variant = 0;
            if (v == 18)
                variant = 1;
            else if (v == 36)
                variant = 2;
            Texture tex = Textures.GetShroomTop(0);
            drawTexture(tex, 60, 42, variant * 62 * 4, pixels, px - (int)(22 * zoom), py - (int)(26 * zoom), w, h, zoom);
        }

        void drawTexture(Texture tex, int bw, int bh, int tofs,
            byte[] pixels, int px, int py,
            int w, int h, double zoom)
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
                int b = bofs;
                for (int x = 0; x < tw; x++)
                {
                    int tx = t + (int)(x / zoom) * 4;
                    if (x < skipx || tex.data[tx + 3] == 0)
                    {
                        b += 4;
                        continue;
                    }
                    pixels[b++] = tex.data[tx++];
                    pixels[b++] = tex.data[tx++];
                    pixels[b++] = tex.data[tx++];
                    pixels[b++] = tex.data[tx++];
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

        Int16[] uvMap ={            //tblr
                        162,54, 180,54, 198,54,     //0000
                        162,0,  162,18, 162,36,     //0001
                        216,0,  216,18, 216,36,     //0010
                        108,72, 126,72, 144,72,     //0011
                        108,0,  126,0,  144,0,      //0100
                        0,54,   36,54,  72,54,      //0101
                        18,54,  54,54,  90,54,      //0110
                        18,0,   36,0,   54,0,       //0111
                        108,54, 126,54, 144,54,     //1000
                        0,72,   36,72,  72,72,      //1001
                        18,72,  54,72,  90,72,      //1010
                        18,36,  36,36,  54,36,      //1011
                        90,0,   90,18,  90,36,      //1100
                        0,0,    0,18,   0,36,       //1101
                        72,0,   72,18,  72,36,      //1110
                        18,18,  36,18,  54,18       //1111
                  };
        Int16[] uvCorners ={
                        108,18, 126,18, 144,18, //top
                        108,36, 126,36, 144,36, //bottom
                        180,0,  180,18, 180,36, //left
                        198,0,  198,18, 198,36, //right
                        18,18,  36,18,  54,18   //all
                          };
        private void fixTile(int x, int y)
        {
            int t = -1, l = -1, r = -1, b = -1;
            int tl = -1, tr = -1, bl = -1, br = -1;
            byte c = tiles[y + x * tilesHigh].type;
            Int16 u;
            int set = rand.Next(0, 3) * 2;

            if (x > 0)
            {
                int xx = (x - 1) * tilesHigh;
                if (tiles[y + xx].isActive)
                    l = tiles[y + xx].type;
                if (y > 0)
                {
                    if (tiles[(y - 1) + xx].isActive)
                        tl = tiles[(y - 1) + xx].type;
                }
                if (y < tilesHigh - 1)
                {
                    if (tiles[(y + 1) + xx].isActive)
                        bl = tiles[(y + 1) + xx].type;
                }
            }
            if (x < tilesWide - 1)
            {
                int xx = (x + 1) * tilesHigh;
                if (tiles[y + xx].isActive)
                    r = tiles[y + xx].type;
                if (y > 0)
                {
                    if (tiles[(y - 1) + xx].isActive)
                        tr = tiles[(y - 1) + xx].type;
                }
                if (y < tilesHigh - 1)
                {
                    if (tiles[(y + 1) + xx].isActive)
                        br = tiles[(y + 1) + xx].type;
                }
            }
            if (y > 0)
            {
                int xx = x * tilesHigh;
                if (tiles[(y - 1) + xx].isActive)
                    t = tiles[(y - 1) + xx].type;
            }
            if (y < tilesHigh - 1)
            {
                int xx = x * tilesHigh;
                if (tiles[(y + 1) + xx].isActive)
                    b = tiles[(y + 1) + xx].type;
            }

            int mask;
            if (c == 33 || c==49) //candles
            {
                // these tiles don't have u/v, but are single tiles
                tiles[y + x * tilesHigh].u = 0;
                tiles[y + x * tilesHigh].v = 0;
                return;
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
                tiles[y + x * tilesHigh].u = u;
                tiles[y + x * tilesHigh].v = 0;
                return;
            }
            if (c == 19) //wooden platform
            {
                // check if left is mountable or right is mountable.. set to -1 if not
                mask = 0;
                if (l == c) mask |= 8;
                if (r == c) mask |= 4;
                if (l == -1) mask |= 2;
                if (r == -1) mask |= 1;
                tiles[y + x * tilesHigh].u = uvPlatforms[mask * 2];
                tiles[y + x * tilesHigh].v = uvPlatforms[mask * 2 + 1];
                return;
            }
            if (c == 2 || c == 23 || c == 60 || c == 70) //grasses
            {
                //if it's mud or dirt, it's grass
                if (t == 0 || t == 59) t = c;
                if (l == 0 || l == 59) l = c;
                if (b == 0 || b == 59) b = c;
                if (r == 0 || r == 59) r = c;
                if (tl == 0 || tl == 59) tl = c;
                if (tr == 0 || tr == 59) tr = c;
                if (bl == 0 || bl == 59) bl = c;
                if (br == 0 || br == 59) br = c;
            }
            if (c == 0 || c == 59) //dirt and mud
            {
                //consider grass to be same as dirt
                if (t == 2 || t == 23 || t == 60 || t == 70) t = c;
                if (l == 2 || l == 23 || l == 60 || l == 70) l = c;
                if (b == 2 || b == 23 || b == 60 || b == 70) b = c;
                if (r == 2 || r == 23 || r == 60 || r == 70) r = c;
                if (tl == 2 || tl == 23 || tl == 60 || tl == 70) tl = c;
                if (tr == 2 || tr == 23 || tr == 60 || tr == 70) tr = c;
                if (bl == 2 || bl == 23 || bl == 60 || bl == 70) bl = c;
                if (br == 2 || br == 23 || br == 60 || br == 70) br = c;
            }
            /*if (c == 1) //stones should be grouped together
            {
            } */

            mask = 0;
            if (r == c) mask |= 1;
            if (l == c) mask |= 2;
            if (b == c) mask |= 4;
            if (t == c) mask |= 8;
            if (mask == 0xf) //all on
            {
                int num;
                if (tl != c && tr != c)
                    num = 0;
                else if (bl != c && br != c)
                    num = 1;
                else if (tl != c && bl != c)
                    num = 2;
                else if (tr != c && br != c)
                    num = 3;
                else
                    num = 4;
                tiles[y + x * tilesHigh].u = uvCorners[num * 6 + set];
                tiles[y + x * tilesHigh].v = uvCorners[num * 6 + 1 + set];
            }
            else
            {
                tiles[y + x * tilesHigh].u = uvMap[mask * 6 + set];
                tiles[y + x * tilesHigh].v = uvMap[mask * 6 + 1 + set];
            }
        }
        private void fixWall(int x, int y)
        {
            byte t = 0, l = 0, r = 0, b = 0;
            byte tl = 0, tr = 0, bl = 0, br = 0;
            byte c = tiles[y + x * tilesHigh].wall;
            int set = rand.Next(0, 3) * 2;

            if (x > 0)
            {
                int xx = (x - 1) * tilesHigh;
                l = tiles[y + xx].wall;
                if (y > 0)
                    tl = tiles[(y - 1) + xx].wall;
                if (y < tilesHigh - 1)
                    bl = tiles[(y + 1) + xx].wall;
            }
            if (x < tilesWide - 1)
            {
                int xx = (x + 1) * tilesHigh;
                r = tiles[y + xx].wall;
                if (y > 0)
                    tr = tiles[(y - 1) + xx].wall;
                if (y < tilesHigh - 1)
                    br = tiles[(y + 1) + xx].wall;
            }
            if (y > 0)
                t = tiles[(y - 1) + x * tilesHigh].wall;
            if (y < tilesHigh - 1)
                b = tiles[(y + 1) + x * tilesHigh].wall;

            int mask = 0;
            if (r == c) mask |= 1;
            if (l == c) mask |= 2;
            if (b == c) mask |= 4;
            if (t == c) mask |= 8;
            if (mask == 0xf) //all on
            {
                int num;
                if (tl != c && tr != c)
                    num = 0;
                else if (bl != c && br != c)
                    num = 1;
                else if (tl != c && bl != c)
                    num = 2;
                else if (tr != c && br != c)
                    num = 3;
                else
                    num = 4;
                tiles[y + x * tilesHigh].wallu = uvCorners[num * 6 + set];
                tiles[y + x * tilesHigh].wallv = uvCorners[num * 6 + 1 + set];
            }
            else
            {
                tiles[y + x * tilesHigh].wallu = uvMap[mask * 6 + set];
                tiles[y + x * tilesHigh].wallv = uvMap[mask * 6 + 1 + set];
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
