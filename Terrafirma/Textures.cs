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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Terrafirma
{
    public class Texture
    {
        public int width, height;
        public byte[] data;

        private static string GetName(string path, string xnb)
        {
            string fn = Path.Combine(path, xnb);
            if (File.Exists(fn))
                return fn;
            if (File.Exists(fn + ".xnb"))
                return fn + ".xnb";
            if (File.Exists(fn + ".png"))
                return fn + ".png";
            return null;
        }

        public Texture(string path, string xnb)
        {
            string fn = GetName(path, xnb);
            if (fn == null)
                throw new Exception(String.Format("Couldn't locate {0}", xnb));
            using (BinaryReader b = new BinaryReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                UInt32 header = b.ReadUInt32();
                // xnb header is XNBw, XNBx, XNBm for win, unix, mac, respectively.
                if (header != 0x77424e58 && header != 0x78424e58 && header != 0x6d424e58)
                    throw new Exception(String.Format("{0} is not a valid XNB", xnb));
                UInt16 version = b.ReadUInt16();
                bool compressed = (version & 0x8000) == 0x8000;
                version &= 0xff; //ignore graphics profile
                if (version != 4 && version != 5)
                    throw new Exception(String.Format("{0}: Invalid XNB Version", xnb));
                int length = b.ReadInt32(); //length of entire file
                if (compressed)
                {
                    int decompSize = b.ReadInt32();
                    MemoryStream ms = new MemoryStream(decompSize);
                    LzxDecoder lzx = new LzxDecoder(16);
                    for (int pos = 14; pos < length; )
                    {
                        b.BaseStream.Seek(pos, SeekOrigin.Begin);
                        byte hi = b.ReadByte();
                        byte lo = b.ReadByte();
                        pos += 2;
                        int compLen = (hi << 8) | lo;
                        int decompLen = 0x8000;
                        if (hi == 0xff)
                        {
                            hi = lo;
                            lo = b.ReadByte();
                            decompLen = (hi << 8) | lo;
                            hi = b.ReadByte();
                            lo = b.ReadByte();
                            compLen = (hi << 8) | lo;
                            pos += 3;
                        }
                        if (compLen == 0 || decompLen == 0) //done
                            break;
                        if (lzx.Decompress(b.BaseStream, compLen, ms, decompLen) < 0)
                            throw new Exception("Failed to decompress");
                        pos += compLen;
                    }
                    ms.Seek(0, SeekOrigin.Begin);
                    ReadTexture(ms);
                }
                else
                    ReadTexture(b.BaseStream);
            }
        }

        private void ReadTexture(Stream s)
        {
            using (BinaryReader d = new BinaryReader(s))
            {
                // skip readers
                int numReaders = d.ReadByte();
                for (int i = 0; i < numReaders; i++)
                {
                    d.ReadString(); //name of reader
                    d.ReadInt32(); //reader version
                }
                d.ReadByte(); //padding
                d.ReadByte(); //reader index
                // we should probably verify that the reader is the correct one.. if this isn't a
                // texture 2d, we're totally screwed here.
                int format = d.ReadInt32();
                width = d.ReadInt32();
                height = d.ReadInt32();
                d.ReadInt32(); //level count
                int imageLen = d.ReadInt32(); //image length
                data = new byte[width * height * 4];
                // now convert all formats to RGBA32
                int r, g, b, a;

                switch (format)
                {
                    case 0: //Color     (rrrrrrrr gggggggg bbbbbbbb aaaaaaaa)
                        for (int ofs = 0, outofs = 0; ofs < width * height; ofs++, outofs += 4)
                        {
                            data[outofs + 2] = d.ReadByte(); //r
                            data[outofs + 1] = d.ReadByte(); //g
                            data[outofs] = d.ReadByte(); //b
                            data[outofs + 3] = d.ReadByte(); //a
                        }
                        break;
                    case 1: //Bgr565    (bbbbbggg gggrrrrr) 
                        // this may not be correct.  I think it may be stored little-endian
                        for (int ofs = 0, outofs = 0; ofs < width * height; ofs++, outofs += 4)
                        {
                            byte bg = d.ReadByte();
                            byte gr = d.ReadByte();
                            r = gr & 0x1f;
                            g = (gr >> 5) | ((bg & 7) << 3);
                            b = bg >> 3;
                            data[outofs + 2] = (byte)((255 * r) / 0x1f);
                            data[outofs + 1] = (byte)((255 * g) / 0x3f);
                            data[outofs] = (byte)((255 * b) / 0x1f);
                            data[outofs + 3] = 255;
                        }
                        break;
                    case 2: //Bgra5551  (bbbbbggg ggrrrrra)
                        // This may not be correct, it may be little-endian
                        for (int ofs = 0, outofs = 0; ofs < width * height; ofs++, outofs += 4)
                        {
                            byte bg = d.ReadByte();
                            byte gr = d.ReadByte();
                            r = (gr & 0x3e) >> 1;
                            g = (gr >> 6) | ((bg & 7) << 2);
                            b = bg >> 3;
                            a = gr & 1;
                            data[outofs + 2] = (byte)((255 * r) / 0x1f);
                            data[outofs + 1] = (byte)((255 * g) / 0x1f);
                            data[outofs] = (byte)((255 * b) / 0x1f);
                            data[outofs + 3] = (byte)(255 * a);
                        }
                        break;
                    case 3: //Bgra4444  (bbbbgggg rrrraaaa)
                        // this may not be correct, it may be little-endian
                        for (int ofs = 0, outofs = 0; ofs < width * height; ofs++, outofs += 4)
                        {
                            byte bg = d.ReadByte();
                            byte ra = d.ReadByte();
                            r = ra >> 4;
                            g = bg & 0xf;
                            b = bg >> 4;
                            a = ra & 0xf;
                            data[outofs + 2] = (byte)((255 * r) / 0xf);
                            data[outofs + 1] = (byte)((255 * g) / 0xf);
                            data[outofs] = (byte)((255 * b) / 0xf);
                            data[outofs + 3] = (byte)((255 * a) / 0xf);
                        }
                        break;
                    case 4: //Dxt1  (compressed, then Color)
                    case 5: //Dxt3  (compressed, then Color)
                    case 6: //Dxt5  (compressed, then Color)
                    case 7: //NormalizedByte2 (16-bit signed bump map)
                    case 8: //NormalizedByte4 (32-bit signed bump map)
                    case 9: //Rgba1010102   (rrrrrrrr rrgggggg ggggbbbb bbbbbbaa)
                    case 10://Rg32          (rrrrrrrr rrrrrrrr gggggggg gggggggg)
                    case 11://Rgba64        (r16 g16 b16 a16)
                    case 12://Alpha8        (aaaaaaaa)
                    case 13://Single        red channel only, 32-bit float
                    case 14://Vector2       float red, green
                    case 15://Vector4       float alpha, blue, green, red
                    case 16://HalfSingle    Same as single, but 16-bit float
                    case 17://HalfVector2   Same as vector2, but 16-bit float
                    case 18://HalfVector4   Same as vector4, but 16-bit float
                    case 19://HdrBlendable  floats
                        // we don't support any of these, for now.
                        throw new Exception("Invalid format");
                }
            }
        }
    }
    class Textures
    {
        Dictionary<int, Texture> textures;
        Dictionary<int, Texture> backgrounds;
        Dictionary<int, Texture> walls;
        Dictionary<int, Texture> treeTops;
        Dictionary<int, Texture> treeBranches;
        Dictionary<int, Texture> shrooms;
        Dictionary<int, Texture> npcs;
        Dictionary<int, Texture> npcHeads;
        Dictionary<int, Texture> banners;
        Dictionary<int, Texture> armorHeads;
        Dictionary<int, Texture> armorBodies;
        Dictionary<int, Texture> armorLegs;
        Dictionary<int, Texture> wires;
        Dictionary<int, Texture> liquids;
        Dictionary<int, Texture> woods;
        string rootDir;

        public Textures()
        {
            rootDir = null;

            textures = new Dictionary<int, Texture>();
            backgrounds = new Dictionary<int, Texture>();
            walls = new Dictionary<int, Texture>();
            treeTops = new Dictionary<int, Texture>();
            treeBranches = new Dictionary<int, Texture>();
            shrooms = new Dictionary<int, Texture>();
            npcs = new Dictionary<int, Texture>();
            npcHeads = new Dictionary<int, Texture>();
            banners = new Dictionary<int, Texture>();
            armorHeads = new Dictionary<int, Texture>();
            armorBodies = new Dictionary<int, Texture>();
            armorLegs = new Dictionary<int, Texture>();
            wires = new Dictionary<int, Texture>();
            liquids = new Dictionary<int, Texture>();
            woods = new Dictionary<int, Texture>();

            // find steam
            string path="";
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\\Valve\\Steam");
            if (key!=null)
                path = key.GetValue("SteamPath") as string;

            //no steam key, let's try the default
            if (path.Equals("") || !Directory.Exists(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                path = Path.Combine(path, "Steam");
            }
            path = Path.Combine(path, "steamapps");
            path = Path.Combine(path, "common");
            path = Path.Combine(path, "terraria");
            path = Path.Combine(path, "Content");
            path = Path.Combine(path, "Images");
            if (Directory.Exists(path))
                rootDir=path;
        }
        public bool Valid {
            get { return rootDir!=null; }
        }
        public Texture GetTile(int num)
        {
            if (!textures.ContainsKey(num))
            {
                string name = String.Format("Tiles_{0}", num);
                textures[num] = new Texture(rootDir, name);
            }
            return textures[num];
        }
        public Texture GetWood(int wood)
        {
            if (!woods.ContainsKey(wood))
            {
                string name = String.Format("Tiles_5_{0}", wood);
                woods[wood] = new Texture(rootDir, name);
            }
            return woods[wood];
        }
        public Texture GetBackground(int num)
        {
            if (!backgrounds.ContainsKey(num))
            {
                string name = String.Format("Background_{0}", num);
                backgrounds[num] = new Texture(rootDir, name);
            }
            return backgrounds[num];
        }
        public Texture GetWall(int num)
        {
            if (!walls.ContainsKey(num))
            {
                string name = String.Format("Wall_{0}", num);
                walls[num] = new Texture(rootDir, name);
            }
            return walls[num];
        }
        public Texture GetTreeTops(int num)
        {
            if (!treeTops.ContainsKey(num))
            {
                string name = String.Format("Tree_Tops_{0}", num);
                treeTops[num] = new Texture(rootDir, name);
            }
            return treeTops[num];
        }
        public Texture GetTreeBranches(int num)
        {
            if (!treeBranches.ContainsKey(num))
            {
                string name = String.Format("Tree_Branches_{0}", num);
                treeBranches[num] = new Texture(rootDir, name);
            }
            return treeBranches[num];
        }
        public Texture GetShroomTop(int num)
        {
            if (!shrooms.ContainsKey(num))
            {
                string name = String.Format("Shroom_Tops");
                shrooms[num] = new Texture(rootDir, name);
            }
            return shrooms[num];
        }
        public Texture GetNPC(int num)
        {
            if (!npcs.ContainsKey(num))
            {
                string name = String.Format("NPC_{0}", num);
                npcs[num] = new Texture(rootDir, name);
            }
            return npcs[num];
        }
        public Texture GetNPCHead(int num)
        {
            if (!npcHeads.ContainsKey(num))
            {
                string name = String.Format("NPC_Head_{0}", num);
                npcHeads[num] = new Texture(rootDir, name);
            }
            return npcHeads[num];
        }
        public Texture GetBanner(int num)
        {
            if (!banners.ContainsKey(num))
            {
                string name = String.Format("House_Banner_{0}", num);
                banners[num] = new Texture(rootDir, name);
            }
            return banners[num];
        }
        public Texture GetArmorHead(int num)
        {
            if (!armorHeads.ContainsKey(num))
            {
                string name = String.Format("Armor_Head_{0}", num);
                armorHeads[num] = new Texture(rootDir, name);
            }
            return armorHeads[num];
        }
        public Texture GetArmorBody(int num)
        {
            if (!armorBodies.ContainsKey(num))
            {
                string name = String.Format("Armor_Body_{0}", num);
                armorBodies[num] = new Texture(rootDir, name);
            }
            return armorBodies[num];
        }
        public Texture GetArmorLegs(int num)
        {
            if (!armorLegs.ContainsKey(num))
            {
                string name = String.Format("Armor_Legs_{0}", num);
                armorLegs[num] = new Texture(rootDir, name);
            }
            return armorLegs[num];
        }
        public Texture GetWire(int num)
        {
            if (!wires.ContainsKey(num))
            {
                string name = String.Format("Wires");
                wires[num] = new Texture(rootDir, name);
            }
            return wires[num];
        }
        public Texture GetLiquid(int num)
        {
            if (!liquids.ContainsKey(num))
            {
                string name = String.Format("Liquid_{0}", num);
                liquids[num] = new Texture(rootDir, name);
            }
            return liquids[num];
        }
    }
}
