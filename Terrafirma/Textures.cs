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
                       graphicsDevice=new GraphicsDevice(
                           GraphicsAdapter.DefaultAdapter,
                           GraphicsProfile.HiDef,
                           parms);
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
        ContentManager cm=null;
        public Textures(IntPtr windowHandle)
        {
            textures = new Dictionary<int, Texture>();
            backgrounds = new Dictionary<int, Texture>();
            walls = new Dictionary<int, Texture>();

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
