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
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.Text.RegularExpressions;
using System.Threading;

namespace Terrafirma
{
    public class SimpleGraphicsDeviceService : IGraphicsDeviceService
    {
        private IntPtr handle;
        private GraphicsDevice graphicsDevice=null;

        public SimpleGraphicsDeviceService(IntPtr windowHandle)
        {
            handle = windowHandle;
        }
        public GraphicsDevice GraphicsDevice {
               get {
                   if (graphicsDevice==null)
                   {
                       PresentationParameters parms=new PresentationParameters();
                       parms.BackBufferFormat=SurfaceFormat.Color;
                       parms.BackBufferWidth=480;
                       parms.BackBufferHeight=320;
                       parms.DeviceWindowHandle=handle;
                       parms.DepthStencilFormat=DepthFormat.Depth24Stencil8;
                       parms.IsFullScreen=false;
                       if (GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef))
                       {
                           graphicsDevice = new GraphicsDevice(
                               GraphicsAdapter.DefaultAdapter,
                               GraphicsProfile.HiDef,
                               parms);
                       }
                       else
                       {
                           graphicsDevice = new GraphicsDevice(
                               GraphicsAdapter.DefaultAdapter,
                               GraphicsProfile.Reach,
                               parms);
                       }
                       if (DeviceCreated!=null)
                           DeviceCreated(this,EventArgs.Empty);
                   }
                   return graphicsDevice;
               }
        }
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }
    public class SimpleProvider : IServiceProvider
    {
        private IntPtr handle;
        private SimpleGraphicsDeviceService graphicsDeviceService=null;
        public SimpleProvider(IntPtr windowHandle)
        {
            handle = windowHandle;
        }
        public Object GetService(Type type)
        {
            if (type == typeof(IGraphicsDeviceService))
            {
                if (graphicsDeviceService == null)
                    graphicsDeviceService = new SimpleGraphicsDeviceService(handle);
                return graphicsDeviceService;
            }
            return null;
        }
    }
    public struct Texture
    {
        public int width, height;
        public byte[] data;
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
        Dictionary<int, Texture> armorHeads;
        Dictionary<int, Texture> armorBodies;
        Dictionary<int, Texture> armorLegs;
        ContentManager cm=null;
        public Textures(IntPtr windowHandle)
        {
            textures = new Dictionary<int, Texture>();
            backgrounds = new Dictionary<int, Texture>();
            walls = new Dictionary<int, Texture>();
            treeTops = new Dictionary<int, Texture>();
            treeBranches = new Dictionary<int, Texture>();
            shrooms = new Dictionary<int, Texture>();
            npcs = new Dictionary<int, Texture>();
            armorHeads = new Dictionary<int, Texture>();
            armorBodies = new Dictionary<int, Texture>();
            armorLegs = new Dictionary<int, Texture>();

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
            {
                cm = new ContentManager(new SimpleProvider(windowHandle),path);
            }
        }
        public bool Valid {
            get { return cm!=null; }
        }
        public Texture GetTile(int num)
        {
            if (!textures.ContainsKey(num))
            {
                string name = String.Format("Tiles_{0}", num);
                textures[num] = loadTexture(name);
            }
            return textures[num];
        }
        public Texture GetBackground(int num)
        {
            if (!backgrounds.ContainsKey(num))
            {
                string name = String.Format("Background_{0}", num);
                backgrounds[num] = loadTexture(name);
            }
            return backgrounds[num];
        }
        public Texture GetWall(int num)
        {
            if (!walls.ContainsKey(num))
            {
                string name = String.Format("Wall_{0}", num);
                walls[num] = loadTexture(name);
            }
            return walls[num];
        }
        public Texture GetTreeTops(int num)
        {
            if (!treeTops.ContainsKey(num))
            {
                string name = String.Format("Tree_Tops_{0}", num);
                treeTops[num] = loadTexture(name);
            }
            return treeTops[num];
        }
        public Texture GetTreeBranches(int num)
        {
            if (!treeBranches.ContainsKey(num))
            {
                string name = String.Format("Tree_Branches_{0}", num);
                treeBranches[num] = loadTexture(name);
            }
            return treeBranches[num];
        }
        public Texture GetShroomTop(int num)
        {
            if (!shrooms.ContainsKey(num))
            {
                string name = String.Format("Shroom_Tops");
                shrooms[num] = loadTexture(name);
            }
            return shrooms[num];
        }
        public Texture GetNPC(int num)
        {
            if (!npcs.ContainsKey(num))
            {
                string name = String.Format("NPC_{0}", num);
                npcs[num] = loadTexture(name);
            }
            return npcs[num];
        }
        public Texture GetArmorHead(int num)
        {
            if (!armorHeads.ContainsKey(num))
            {
                string name=String.Format("Armor_Head_{0}",num);
                armorHeads[num]=loadTexture(name);
            }
            return armorHeads[num];
        }
        public Texture GetArmorBody(int num)
        {
            if (!armorBodies.ContainsKey(num))
            {
                string name = String.Format("Armor_Body_{0}", num);
                armorBodies[num] = loadTexture(name);
            }
            return armorBodies[num];
        }
        public Texture GetArmorLegs(int num)
        {
            if (!armorLegs.ContainsKey(num))
            {
                string name = String.Format("Armor_Legs_{0}", num);
                armorLegs[num] = loadTexture(name);
            }
            return armorLegs[num];
        }
        private Texture loadTexture(string path)
        {
            Texture2D tex = cm.Load<Texture2D>(path);
            Texture t = new Texture();
            t.width = tex.Width;
            t.height = tex.Height;

            t.data = new byte[t.width * t.height * 4];
            Byte4[] data = new Byte4[t.width * t.height];
            tex.GetData<Byte4>(data);
            int ofs = 0;
            for (int i = 0; i < data.Length; i++)
            {
                uint val = data[i].PackedValue;
                t.data[ofs++] = (byte)((val >> 16) & 0xff);
                t.data[ofs++] = (byte)((val >> 8) & 0xff);
                t.data[ofs++] = (byte)(val & 0xff);
                t.data[ofs++] = (byte)((val >> 24) & 0xff);
            }
            return t;
        }
    }
}
