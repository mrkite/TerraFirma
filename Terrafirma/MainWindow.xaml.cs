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
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Terrafirma
{
    public class TileInfo
    {
        public string name;
        public UInt32 color;
        public bool hasExtra;
        public double light;
        public double lightR, lightG, lightB;
        public bool transparent, solid;
        public bool isStone, isGrass;
		public bool canMerge;
        public Int16 blend;
        public int u, v, minu, maxu, minv, maxv;
        public bool isHilighting;
        public List<TileInfo> variants;
    }
    class TileInfos
    {
        public TileInfos(XmlNodeList nodes)
        {
            info = new TileInfo[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                int id = Convert.ToInt32(nodes[i].Attributes["num"].Value);
                info[id] = new TileInfo();
                loadInfo(info[id], nodes[i]);
            }
        }
        public TileInfo this[int id] //no variantions
        {
            get { return info[id]; }
        }
        public TileInfo this[int id, Int16 u, Int16 v]
        {
            get { return find(info[id], u, v); }
        }
        public ArrayList Items()
        {
            ArrayList items = new ArrayList();
            for (int i = 0; i < info.Length; i++)
                items.Add(info[i]);
            return items;
        }

        private TileInfo find(TileInfo info, Int16 u, Int16 v)
        {
            foreach (TileInfo vars in info.variants)
            {
                // must match *all* restrictions... and we take the first match we find.
                if ((vars.u < 0 || vars.u == u) &&
                    (vars.v < 0 || vars.v == v) &&
                    (vars.minu < 0 || vars.minu <= u) &&
                    (vars.minv < 0 || vars.minv <= v) &&
                    (vars.maxu < 0 || vars.maxu > u) &&
                    (vars.maxv < 0 || vars.maxv > v))
                    return find(vars, u, v); //check for sub-variants
            }
            // if we get here, there are no variants that match
            return info;
        }
        private double parseDouble(string value)
        {
            return Double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        private Int16 parseInt(string value)
        {
            return Int16.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        private UInt32 parseColor(string color)
        {
            UInt32 c = 0;
            for (int j = 0; j < color.Length; j++)
            {
                c <<= 4;
                if (color[j] >= '0' && color[j] <= '9')
                    c |= (byte)(color[j] - '0');
                else if (color[j] >= 'A' && color[j] <= 'F')
                    c |= (byte)(10 + color[j] - 'A');
                else if (color[j] >= 'a' && color[j] <= 'f')
                    c |= (byte)(10 + color[j] - 'a');
            }
            return c;
        }
        private void loadInfo(TileInfo info, XmlNode node)
        {
            info.name = node.Attributes["name"].Value;
            info.color = parseColor(node.Attributes["color"].Value);
            info.hasExtra = node.Attributes["hasExtra"] != null;
            info.light = (node.Attributes["light"] == null) ? 0.0 : parseDouble(node.Attributes["light"].Value);
            info.lightR = (node.Attributes["lightr"] == null) ? 0.0 : parseDouble(node.Attributes["lightr"].Value);
            info.lightG = (node.Attributes["lightg"] == null) ? 0.0 : parseDouble(node.Attributes["lightg"].Value);
            info.lightB = (node.Attributes["lightb"] == null) ? 0.0 : parseDouble(node.Attributes["lightb"].Value);
            info.transparent = node.Attributes["letLight"] != null;
            info.solid = node.Attributes["solid"] != null;
            info.isStone = node.Attributes["isStone"] != null;
            info.isGrass = node.Attributes["isGrass"] != null;
			info.canMerge = node.Attributes["merge"] != null;
            if (node.Attributes["blend"] != null)
                info.blend = parseInt(node.Attributes["blend"].Value);
            else
                info.blend = -1;
            info.variants = new List<TileInfo>();
            if (node.HasChildNodes)
                for (int i = 0; i < node.ChildNodes.Count; i++)
                    info.variants.Add(newVariant(info, node.ChildNodes[i]));
        }
        private TileInfo newVariant(TileInfo parent, XmlNode node)
        {
            TileInfo info = new TileInfo();
            info.name = (node.Attributes["name"] == null) ? parent.name : node.Attributes["name"].Value;
            info.color = (node.Attributes["color"] == null) ? parent.color : parseColor(node.Attributes["color"].Value);
            info.transparent = (node.Attributes["letLight"] == null) ? parent.transparent : true;
            info.solid = (node.Attributes["solid"] == null) ? parent.solid : true;
            info.light = (node.Attributes["light"] == null) ? parent.light : parseDouble(node.Attributes["light"].Value);
            info.lightR = (node.Attributes["lightr"] == null) ? parent.lightR : parseDouble(node.Attributes["lightr"].Value);
            info.lightG = (node.Attributes["lightg"] == null) ? parent.lightG : parseDouble(node.Attributes["lightg"].Value);
            info.lightB = (node.Attributes["lightb"] == null) ? parent.lightB : parseDouble(node.Attributes["lightb"].Value);
            info.u = (node.Attributes["u"] == null) ? -1 : parseInt(node.Attributes["u"].Value);
            info.v = (node.Attributes["v"] == null) ? -1 : parseInt(node.Attributes["v"].Value);
            info.minu = (node.Attributes["minu"] == null) ? -1 : parseInt(node.Attributes["minu"].Value);
            info.maxu = (node.Attributes["maxu"] == null) ? -1 : parseInt(node.Attributes["maxu"].Value);
            info.minv = (node.Attributes["minv"] == null) ? -1 : parseInt(node.Attributes["minv"].Value);
            info.maxv = (node.Attributes["maxv"] == null) ? -1 : parseInt(node.Attributes["maxv"].Value);
            info.variants = new List<TileInfo>();
            if (node.HasChildNodes)
                for (int i = 0; i < node.ChildNodes.Count; i++)
                    info.variants.Add(newVariant(info, node.ChildNodes[i]));
            return info;
        }

        private TileInfo[] info;
    };
    struct WallInfo
    {
        public string name;
        public UInt32 color;
		public Int16 blend;
    }
    class Tile
    {
        private UInt32 lite;
        public Int16 u, v, wallu, wallv;
        private byte flags;
        public byte type;
        public byte wall;
        public byte liquid;

        public byte color;
        public byte wallColor;
        public byte slope;


        public bool isActive
        {
            get
            {
                return (flags & 0x01) == 0x01;
            }
            set
            {
                if (value)
                    flags |= 0x01;
                else
                    flags &= 0xfe;
            }
        }
        public bool isLava
        {
            get
            {
                return (flags & 0x02) == 0x02;
            }
            set
            {
                if (value)
                    flags |= 0x02;
                else
                    flags &= 0xfd;
            }
        }
        public bool isHoney
        {
            get
            {
                return (flags & 0x04) == 0x04;
            }
            set
            {
                if (value)
                    flags |= 0x04;
                else
                    flags &= 0xfb;
            }
        }
        public bool seen
        {
            get
            {
                return (flags & 0x08) == 0x08;
            }
            set
            {
                if (value)
                    flags |= 0x08;
                else
                    flags &= 0xf7;
            }
        }

        public bool hasRedWire
        {
            get
            {
                return (flags & 0x10) == 0x10;
            }
            set
            {
                if (value)
                    flags |= 0x10;
                else
                    flags &= 0xef;
            }
        }
        public bool hasBlueWire
        {
            get
            {
                return (flags & 0x20) == 0x20;
            }
            set
            {
                if (value)
                    flags |= 0x20;
                else
                    flags &= 0xdf;
            }
        }
        public bool hasGreenWire
        {
            get
            {
                return (flags & 0x40) == 0x40;
            }
            set
            {
                if (value)
                    flags |= 0x40;
                else
                    flags &= 0xbf;
            }
        }
        public bool half
        {
            get
            {
                return (flags & 0x80) == 0x80;
            }
            set
            {
                if (value)
                    flags |= 0x80;
                else
                    flags &= 0x7f;
            }
        }

        public double light
        {
            get {
                return getRGBA(0);
            }
            set {
                setRGBA(0, value);
            }
        }
        public double lightR
        {
            get
            {
                return getRGBA(24);
            }
            set
            {
                setRGBA(24, value);
            }
        }
        public double lightG
        {
            get
            {
                return getRGBA(16);
            }
            set
            {
                setRGBA(16, value);
            }
        }
        public double lightB
        {
            get
            {
                return getRGBA(8);
            }
            set
            {
                setRGBA(8, value);
            }
        }
        private void setRGBA(int shift,double v)
        {
            int l = (int)Math.Round(v * 255.0);
            if (l>255) l=255;
            else if (l<0) l=0;
            lite &= ~(UInt32)(0xff << shift);
            lite |= (UInt32)(l << shift);
        }
        private double getRGBA(int shift)
        {
            int r=(int)(lite>>shift)&0xff;
            return (double)r/255.0;
        }
    }
    struct ChestItem
    {
        public int stack;
        public string name;
        public string prefix;
    }
    struct Chest
    {
        public Int32 x { get; set; }
        public Int32 y { get; set; }
        public ChestItem[] items;
    }
    struct Sign
    {
        public string text;
        public Int32 x, y;
    }
    class NPC
    {
        public string title;
        public string name;
        public float x, y;
        public bool isHomeless;
        public Int32 homeX, homeY;
        public int sprite;
        public int num;
        public int slot;
        public int order;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int MapVersion = 69;
        const int MaxTile = 250;
        const int MaxWall = 111;
        const int Widest = 8400;
        const int Highest = 2400;

        const double MaxScale = 16.0;
        const double MinScale = 1.0;

        double curX, curY, curScale;
        byte[] bits;
        WriteableBitmap mapbits;
        DispatcherTimer resizeTimer;
        int curWidth, curHeight, newWidth, newHeight;
        bool loaded = false;
        Tile[,] tiles = null;
        Int32 tilesWide = 0, tilesHigh = 0;
        Int32 spawnX, spawnY;
        Int32 groundLevel, rockLevel;
        string[] worlds;
        string currentWorld;
		Int32 worldID=0;
		string[] players;
		string player;
        List<Chest> chests = new List<Chest>();
        List<Sign> signs = new List<Sign>();
        List<NPC> npcs = new List<NPC>();

        byte moonType;
        Int32[] treeX = new Int32[3];
        Int32[] treeStyle = new Int32[4];
        Int32[] caveBackX = new Int32[3];
        Int32[] caveBackStyle = new Int32[4];
        Int32 iceBackStyle, jungleBackStyle, hellBackStyle;
        bool crimson;
        bool killedQueenBee, killedMechBoss1, killedMechBoss2, killedMechBoss3, killedMechBossAny, killedPirates;
        bool isRaining;
        Int32 rainTime;
        float maxRain;
        Int32 oreTier1, oreTier2, oreTier3;
        
        double gameTime;
        bool dayNight,bloodMoon;
        int moonPhase;
        Int32 dungeonX, dungeonY;
        bool killedBoss1, killedBoss2, killedBoss3, killedGoblins, killedClown, killedFrost;
        bool killedPlantBoss, killedGolemBoss;
        bool savedTinkerer, savedWizard, savedMechanic;
        bool smashedOrb, meteorSpawned;
        byte shadowOrbCount;
        Int32 altarsSmashed;
        bool hardMode;
        Int32 goblinsDelay, goblinsSize, goblinsType;
        double goblinsX;
        byte[] styles = {   0, //tree
                            0, //corruption
                            0, //jungle
                            0, //snow
                            0, //hallow
                            0, //crimson
                            0, //desert
                            0 }; //ocean

        Render render;

        TileInfos tileInfos;
        WallInfo[] wallInfo;
        UInt32 skyColor, earthColor, rockColor, hellColor, lavaColor, waterColor, honeyColor;
        bool isHilight = false;

        Socket socket=null;
        byte[] readBuffer, writeBuffer;
        int pendingSize;
        byte[] messages;
        byte playerSlot;
        string status;
        int statusTotal, statusCount;
        int loginLevel;
        bool[,] sentSections;
        int sectionsWide, sectionsHigh;
        bool busy;

        struct FriendlyNPC
        {
            public FriendlyNPC(string title, int id, int num, int order)
            {
                this.title = title;
                this.id = id;
                this.num = num;
                this.order = order;
            }
            public string title;
            public int id; //sprite
            public int num; // number for npc heads
            public int order; //order in name list
        };

        FriendlyNPC[] friendlyNPCs ={
                                        new FriendlyNPC("Merchant", 17, 2, 0),
                                        new FriendlyNPC("Nurse", 18, 3, 1),
                                        new FriendlyNPC("Arms Dealer", 19, 6, 2),
                                        new FriendlyNPC("Dryad", 20, 5, 3),
                                        new FriendlyNPC("Guide", 22, 1, 4),
                                        new FriendlyNPC("Old Man",37, 0, -1),
                                        new FriendlyNPC("Demolitionist", 38, 4, 6),
                                        new FriendlyNPC("Clothier", 54, 7, 5),
                                        new FriendlyNPC("Goblin Tinkerer", 107, 9, 7),
                                        new FriendlyNPC("Wizard", 108, 10, 8),
                                        new FriendlyNPC("Mechanic", 124, 8, 9),
                                        new FriendlyNPC("Santa Claus", 142, 11, -1),
										new FriendlyNPC("Truffle", 160, 12, 10),
										new FriendlyNPC("Steampunker", 178, 13, 11),
										new FriendlyNPC("Dye Trader", 207, 14, 12),
										new FriendlyNPC("Party Girl", 208, 15, 13),
										new FriendlyNPC("Cyborg", 209, 16, 14),
										new FriendlyNPC("Painter", 227, 17, 15),
										new FriendlyNPC("Witch Doctor", 228, 18, 16),
										new FriendlyNPC("Pirate", 229, 19, 17)
                                   };


        public MainWindow()
        {
            InitializeComponent();

            fetchWorlds();



            XmlDocument xml = new XmlDocument();
            string xmlData = string.Empty;

            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("Terrafirma.tiles.xml"))
            {
                xml.Load(stream);
            }
            tileInfos = new TileInfos(xml.GetElementsByTagName("tile"));
            XmlNodeList wallList = xml.GetElementsByTagName("wall");
            wallInfo = new WallInfo[wallList.Count + 1];
            for (int i = 0; i < wallList.Count; i++)
            {
                int id = Convert.ToInt32(wallList[i].Attributes["num"].Value);
                wallInfo[id].name = wallList[i].Attributes["name"].Value;
                wallInfo[id].color = parseColor(wallList[i].Attributes["color"].Value);
            	if (wallList[i].Attributes["blend"] != null)
					wallInfo[id].blend = parseInt(wallList[i].Attributes["blend"].Value);
				else
					wallInfo[id].blend = id;
            }
            XmlNodeList globalList = xml.GetElementsByTagName("global");
            for (int i = 0; i < globalList.Count; i++)
            {
                string kind = globalList[i].Attributes["id"].Value;
                UInt32 color = parseColor(globalList[i].Attributes["color"].Value);
                switch (kind)
                {
                    case "sky":
                        skyColor = color;
                        break;
                    case "earth":
                        earthColor = color;
                        break;
                    case "rock":
                        rockColor = color;
                        break;
                    case "hell":
                        hellColor = color;
                        break;
                    case "water":
                        waterColor = color;
                        break;
                    case "lava":
                        lavaColor = color;
                        break;
                    case "honey":
                        honeyColor = color;
                        break;
                }
            }
            XmlNodeList prefixList = xml.GetElementsByTagName("prefix");
            prefixes = new string[prefixList.Count + 1];
            for (int i = 0; i < prefixList.Count; i++)
            {
                int id = Convert.ToInt32(prefixList[i].Attributes["num"].Value);
                prefixes[id] = prefixList[i].Attributes["name"].Value;
            }
            XmlNodeList itemList = xml.GetElementsByTagName("item");
            //find min/max
            Int32 minItemId = 0, maxItemId = 0;
            for (int i = 0; i < itemList.Count; i++)
            {
                Int32 id = Convert.ToInt32(itemList[i].Attributes["num"].Value);
                if (id < minItemId)
                    minItemId = id;
                if (id > maxItemId)
                    maxItemId = id;
            }
            itemNames2 = new string[(-minItemId) + 1];
            itemNames = new string[maxItemId + 1];
            for (int i = 0; i < itemList.Count; i++)
            {
                int id = Convert.ToInt32(itemList[i].Attributes["num"].Value);
                if (id < 0)
                    itemNames2[-id] = itemList[i].Attributes["name"].Value;
                else
                    itemNames[id] = itemList[i].Attributes["name"].Value;
            }

            render = new Render(tileInfos, wallInfo, skyColor, earthColor, rockColor, hellColor, waterColor, lavaColor, honeyColor);
            //this resize timer is used so we don't get killed on the resize
            resizeTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(20), DispatcherPriority.Normal,
                delegate
                {
                    resizeTimer.IsEnabled = false;
                    curWidth = newWidth;
                    curHeight = newHeight;
                    mapbits = new WriteableBitmap(curWidth, curHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                    Map.Source = mapbits;
                    bits = new byte[curWidth * curHeight * 4];
                    Map.Width = curWidth;
                    Map.Height = curHeight;
                    if (loaded)
                        RenderMap();
                    else
                    {
                        var rect = new Int32Rect(0, 0, curWidth, curHeight);
                        for (int i = 0; i < curWidth * curHeight * 4; i++)
                            bits[i] = 0xff;
                        mapbits.WritePixels(rect, bits, curWidth * 4, 0);
                    }
                },
                Dispatcher) { IsEnabled = false };
            curWidth = 496;
            curHeight = 400;
            newWidth = 496;
            newHeight = 400;
            mapbits = new WriteableBitmap(curWidth, curHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            Map.Source = mapbits;
            bits = new byte[curWidth * curHeight * 4];
            curX = curY = 0;
            curScale = 1.0;

            tiles = new Tile[Widest, Highest];
        }




        private void fetchWorlds()
        {
            string terrariapath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            terrariapath = Path.Combine(terrariapath, "My Games");
            terrariapath = Path.Combine(terrariapath, "Terraria");
            string worldpath = Path.Combine(terrariapath, "Worlds");
            if (Directory.Exists(worldpath))
                worlds = Directory.GetFiles(worldpath, "*.wld");
            else
            {
                worlds = new string[0];
                Worlds.IsEnabled = false;
            }
            int numItems = 0;
            for (int i = 0; i < worlds.Length && numItems < 9; i++)
            {
                MenuItem item = new MenuItem();

                using (BinaryReader b = new BinaryReader(File.Open(worlds[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    b.ReadUInt32(); //skip map version
                    item.Header = b.ReadString();
                }
                item.Command = MapCommands.OpenWorld;
                item.CommandParameter = i;
                CommandBindings.Add(new CommandBinding(MapCommands.OpenWorld, OpenWorld));
                item.InputGestureText = String.Format("Ctrl+{0}", (numItems + 1));
                InputBinding inp = new InputBinding(MapCommands.OpenWorld, new KeyGesture(Key.D1 + numItems, ModifierKeys.Control));
                inp.CommandParameter = i;
                InputBindings.Add(inp);
                Worlds.Items.Add(item);
                numItems++;
            }
            // fetch players too
            string playerpath = Path.Combine(terrariapath, "Players");
            if (Directory.Exists(playerpath))
                players = Directory.GetFiles(playerpath, "*.plr");
            else
            {
                players = new string[0];
                Players.IsEnabled = false;
            }



            for (int i = 0; i < players.Length; i++)
            {
                MenuItem item = new MenuItem();
                //decrypt player file to get the name

                RijndaelManaged encscheme = new RijndaelManaged();
                encscheme.Padding = PaddingMode.None;
                byte[] key = new UnicodeEncoding().GetBytes("h3y_gUyZ");
                FileStream inp = new FileStream(players[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                CryptoStream crypt = new CryptoStream(inp, encscheme.CreateDecryptor(key, key), CryptoStreamMode.Read);
                using (BinaryReader b = new BinaryReader(crypt))
                {
                    b.ReadUInt32(); //skip player version
                    item.Header = b.ReadString();
                }

                item.Command = MapCommands.SelectPlayer;
                item.CommandParameter = i;
                item.IsCheckable = true;
                CommandBindings.Add(new CommandBinding(MapCommands.SelectPlayer, SelectPlayer));
                Players.Items.Add(item);
                if (i == 0)
                {
                    player = players[i];
                    item.IsChecked = true;
                }
            }
        }
        private UInt32 parseColor(string color)
        {
            UInt32 c = 0;
            for (int j = 0; j < color.Length; j++)
            {
                c <<= 4;
                if (color[j] >= '0' && color[j] <= '9')
                    c |= (byte)(color[j] - '0');
                else if (color[j] >= 'A' && color[j] <= 'F')
                    c |= (byte)(10 + color[j] - 'A');
                else if (color[j] >= 'a' && color[j] <= 'f')
                    c |= (byte)(10 + color[j] - 'a');
            }
            return c;
        }

        delegate void Del();
        private void Load(string world, Del done)
        {
            ThreadStart loadThread = delegate()
            {
                try
                {
                    currentWorld = world;
                    bool foundInvalid = false;

                    string invalid = "";
                    using (BinaryReader b = new BinaryReader(File.Open(world,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
                    {
                        int version = b.ReadInt32(); //now we care about the version
                        if (version > MapVersion) // new map format
                            throw new Exception("Unsupported map version: "+version);
                        string title = b.ReadString();
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            Title = title;
                        }));
						worldID=b.ReadInt32();
                        b.BaseStream.Seek(16, SeekOrigin.Current); //skip bounds
                        tilesHigh = b.ReadInt32();
                        tilesWide = b.ReadInt32();
						moonType=0;
						if (version>=63)
							moonType=b.ReadByte();
						treeX[0]=treeX[1]=treeX[2]=0;
						treeStyle[0]=treeStyle[1]=treeStyle[2]=treeStyle[3]=0;
						if (version>=44)
						{
							treeX[0]=b.ReadInt32();
							treeX[1]=b.ReadInt32();
							treeX[2]=b.ReadInt32();
							treeStyle[0]=b.ReadInt32();
							treeStyle[1]=b.ReadInt32();
							treeStyle[2]=b.ReadInt32();
							treeStyle[3]=b.ReadInt32();
						}
						caveBackX[0]=caveBackX[1]=caveBackX[2]=0;
						caveBackStyle[0]=caveBackStyle[1]=caveBackStyle[2]=caveBackStyle[3]=0;
						iceBackStyle=jungleBackStyle=hellBackStyle=0;
						if (version>=60)
						{
							caveBackX[0]=b.ReadInt32();
							caveBackX[1]=b.ReadInt32();
							caveBackX[2]=b.ReadInt32();
							caveBackStyle[0]=b.ReadInt32();
							caveBackStyle[1]=b.ReadInt32();
							caveBackStyle[2]=b.ReadInt32();
							caveBackStyle[3]=b.ReadInt32();
							iceBackStyle=b.ReadInt32();
							if (version>=61)
							{
								jungleBackStyle=b.ReadInt32();
								hellBackStyle=b.ReadInt32();
							}
						}
                        spawnX = b.ReadInt32();
                        spawnY = b.ReadInt32();
                        groundLevel = (int)b.ReadDouble();
                        rockLevel = (int)b.ReadDouble();
                        gameTime = b.ReadDouble();
                        dayNight = b.ReadBoolean();
                        moonPhase = b.ReadInt32();
                        bloodMoon = b.ReadBoolean();
                        dungeonX = b.ReadInt32();
                        dungeonY = b.ReadInt32();
						crimson=false;
						if (version>=56)
							crimson=b.ReadBoolean();
                        killedBoss1 = b.ReadBoolean();
                        killedBoss2 = b.ReadBoolean();
                        killedBoss3 = b.ReadBoolean();
						killedQueenBee = false;
						if (version>=66)
							killedQueenBee=b.ReadBoolean();
						killedMechBoss1 = killedMechBoss2 = killedMechBoss3 = killedMechBossAny = false;
						if (version>=44)
						{
							killedMechBoss1=b.ReadBoolean();
							killedMechBoss2=b.ReadBoolean();
							killedMechBoss3=b.ReadBoolean();
							killedMechBossAny=b.ReadBoolean();
						}
                        killedPlantBoss = killedGolemBoss = false;
                        if (version >= 64)
                        {
                            killedPlantBoss = b.ReadBoolean();
                            killedGolemBoss = b.ReadBoolean();
                        }
                        savedTinkerer = savedWizard = savedMechanic = killedGoblins = killedClown = killedFrost = killedPirates = false;
                        if (version >= 29)
                        {
                            savedTinkerer = b.ReadBoolean();
                            savedWizard = b.ReadBoolean();
                            if (version >= 34)
                                savedMechanic = b.ReadBoolean();
                            killedGoblins = b.ReadBoolean();
                            if (version >= 32)
                                killedClown = b.ReadBoolean();
                            if (version >= 37)
                                killedFrost = b.ReadBoolean();
							if (version >= 56)
								killedPirates = b.ReadBoolean();
                        }
                        smashedOrb = b.ReadBoolean();
                        meteorSpawned = b.ReadBoolean();
                        shadowOrbCount = b.ReadByte();
                        altarsSmashed = 0;
                        hardMode = false;
                        if (version >= 23)
                        {
                            altarsSmashed = b.ReadInt32();
                            hardMode = b.ReadBoolean();
                        }
                        goblinsDelay = b.ReadInt32();
                        goblinsSize = b.ReadInt32();
                        goblinsType = b.ReadInt32();
                        goblinsX = b.ReadDouble();

						isRaining = false;
						rainTime = 0;
						maxRain = 0.0F;
						oreTier1 = 107;
						oreTier2 = 108;
						oreTier3 = 111;
						if (version >= 23 && altarsSmashed == 0)
							oreTier1 = oreTier2 = oreTier3 = -1;
						if (version >= 53)
						{
							isRaining = b.ReadBoolean();
							rainTime = b.ReadInt32();
							maxRain = b.ReadSingle();
							if (version >= 54)
							{
								oreTier1 = b.ReadInt32();
								oreTier2 = b.ReadInt32();
								oreTier3 = b.ReadInt32();
							}
						}
						if (version>=55)
						{
                            int numstyles = 3;
                            if (version >= 60)
                                numstyles = 8;
                            for (int i = 0; i < numstyles; i++)
                                styles[i] = b.ReadByte();
                            //skip clouds
							if (version>=60)
							{
								b.BaseStream.Seek(4, SeekOrigin.Current);
								if (version>=62)
									b.BaseStream.Seek(6, SeekOrigin.Current);
							}
						}

                        ResizeMap();
                        for (int x = 0; x < tilesWide; x++)
                        {
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                            {
                                serverText.Text = ((int)((float)x * 100.0 / (float)tilesWide)) + "% - Reading tiles";
                            }));
                            for (int y = 0; y < tilesHigh; y++)
                            {
                                tiles[x, y].isActive = b.ReadBoolean();
                                if (tiles[x, y].isActive)
                                {
                                    tiles[x, y].type = b.ReadByte();
                                    if (tiles[x, y].type > MaxTile) // something screwy in the map
                                    {
                                        tiles[x, y].isActive = false;
                                        foundInvalid = true;
                                        invalid = String.Format("{0} is not a valid tile type", tiles[x, y].type);
                                    }
                                    else if (tileInfos[tiles[x, y].type].hasExtra)
                                    {
                                        // torches and platforms didn't have extra in older versions.
                                        if ((version < 28 && tiles[x, y].type == 4) || 
											(version < 40 && tiles[x, y].type == 19))
                                        {
                                            tiles[x, y].u = -1;
                                            tiles[x, y].v = -1;
                                        }
                                        else
                                        {
                                            tiles[x, y].u = b.ReadInt16();
                                            tiles[x, y].v = b.ReadInt16();
                                            if (tiles[x, y].type == 144) //timer
                                                tiles[x, y].v = 0;
                                        }
                                    }
                                    else
                                    {
                                        tiles[x, y].u = -1;
                                        tiles[x, y].v = -1;
                                    }

                                    if (version >= 48 && b.ReadBoolean())
                                    {
                                        tiles[x, y].color = b.ReadByte();
                                    }
                                }
                                if (version <= 25)
                                    b.ReadBoolean(); //skip obsolete hasLight
                                if (b.ReadBoolean())
                                {
                                    tiles[x, y].wall = b.ReadByte();
                                    if (tiles[x, y].wall > MaxWall)  // bad wall
                                    {
                                        foundInvalid = true;
                                        invalid = String.Format("{0} is not a valid wall type", tiles[x, y].wall);
                                        tiles[x, y].wall = 0;
                                    }
									if (version >= 48 && b.ReadBoolean())
										tiles[x, y].wallColor=b.ReadByte();
                                    tiles[x, y].wallu = -1;
                                    tiles[x, y].wallv = -1;
                                }
                                else
                                    tiles[x, y].wall = 0;
                                if (b.ReadBoolean())
                                {
                                    tiles[x, y].liquid = b.ReadByte();
                                    tiles[x, y].isLava = b.ReadBoolean();
									if (version >= 51)
										tiles[x, y].isHoney = b.ReadBoolean();
                                }
                                else
                                    tiles[x, y].liquid = 0;
                                tiles[x, y].hasRedWire = false;
                                tiles[x, y].hasGreenWire = false;
                                tiles[x, y].hasBlueWire = false;
								tiles[x, y].half = false;
								tiles[x, y].slope = 0;
                                if (version >= 33)
								{
                                    tiles[x, y].hasRedWire = b.ReadBoolean();
									if (version >= 43)
									{
										tiles[x, y].hasGreenWire = b.ReadBoolean();
										tiles[x, y].hasBlueWire = b.ReadBoolean();
									}
									if (version >= 41)
									{
										tiles[x, y].half = b.ReadBoolean();
										if (version >= 49)
											tiles[x, y].slope = b.ReadByte();
										if (!tileInfos[tiles[x, y].type].solid)
										{
											tiles[x, y].half = false;
											tiles[x, y].slope = 0;
										}
										if (version >= 42)
										{
                                            b.ReadBoolean(); //tile actuator
                                            b.ReadBoolean(); //tile inActive
										}
									}
								}
                                if (version >= 25) //RLE
                                {
                                    int rle = b.ReadInt16();
                                    for (int r = y + 1; r < y + 1 + rle; r++)
                                    {
                                        tiles[x, r].isActive = tiles[x, y].isActive;
                                        tiles[x, r].type = tiles[x, y].type;
                                        tiles[x, r].u = tiles[x, y].u;
                                        tiles[x, r].v = tiles[x, y].v;
                                        tiles[x, r].wall = tiles[x, y].wall;
                                        tiles[x, r].wallu = -1;
                                        tiles[x, r].wallv = -1;
                                        tiles[x, r].liquid = tiles[x, y].liquid;
                                        tiles[x, r].isLava = tiles[x, y].isLava;
										tiles[x, r].isHoney = tiles[x, y].isHoney;
                                        tiles[x, r].hasRedWire = tiles[x, y].hasRedWire;
                                        tiles[x, r].hasGreenWire = tiles[x, y].hasGreenWire;
                                        tiles[x, r].hasBlueWire = tiles[x, y].hasBlueWire;
										tiles[x, r].half = tiles[x, y].half;
										tiles[x, r].slope = tiles[x, y].slope;
									//	tiles[x, r].actuator = tiles[x, y].actuator;
									//	tiles[x, r].inActive = tiles[x, y].inActive;
										tiles[x, r].color = tiles[x, y].color;
										tiles[x, r].wallColor = tiles[x, y].wallColor;
                                    }
                                    y += rle;
                                }
                            }
                        }
						int itemsPerChest=40;
						if (version < 58)
							itemsPerChest=20;
                        chests.Clear();
                        for (int i = 0; i < 1000; i++)
                        {
                            if (b.ReadBoolean())
                            {
                                Chest chest = new Chest();
                                chest.items = new ChestItem[itemsPerChest];
                                chest.x = b.ReadInt32();
                                chest.y = b.ReadInt32();
                                for (int ii = 0; ii < itemsPerChest; ii++)
                                {
									if (version<59)
	                                    chest.items[ii].stack = b.ReadByte();
									else
										chest.items[ii].stack = b.ReadInt16();
                                    if (chest.items[ii].stack > 0)
                                    {
                                        string name="Unknown";
                                        if (version >= 38) //item names not stored
                                        {
                                            Int32 itemid = b.ReadInt32();
                                            if (itemid < 0)
                                            {
                                                itemid = -itemid;
                                                if (itemid < itemNames2.Length)
                                                    name = itemNames2[itemid];
                                            }
                                            else if (itemid < itemNames.Length)
                                                name = itemNames[itemid];
                                        }
                                        else
                                            name = b.ReadString();
                                        string prefix = "";
                                        if (version >= 36) //item prefixes
                                        {
                                            int pfx = b.ReadByte();
                                            if (pfx < prefixes.Length)
                                                prefix = prefixes[pfx];
                                        }
                                        chest.items[ii].name = name;
                                        chest.items[ii].prefix = prefix;
                                    }
                                }
                                chests.Add(chest);
                            }
                        }
                        signs.Clear();
                        for (int i = 0; i < 1000; i++)
                        {
                            if (b.ReadBoolean())
                            {
                                Sign sign = new Sign();
                                sign.text = b.ReadString();
                                sign.x = b.ReadInt32();
                                sign.y = b.ReadInt32();
                                signs.Add(sign);
                            }
                        }
                        npcs.Clear();
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            NPCs.Items.Clear();
                            serverText.Text = "Loading NPCs...";
                        }));
                        while (b.ReadBoolean())
                        {
                            NPC npc = new NPC();
                            npc.title = b.ReadString();
                            npc.name = "";
                            npc.x = b.ReadSingle();
                            npc.y = b.ReadSingle();
                            npc.isHomeless = b.ReadBoolean();
                            npc.homeX = b.ReadInt32();
                            npc.homeY = b.ReadInt32();

                            npc.order = -1;
                            npc.num = 0;
                            npc.sprite = 0;
                            for (int i=0;i<friendlyNPCs.Length;i++)
                                if (friendlyNPCs[i].title == npc.title)
                                {
                                    npc.sprite = friendlyNPCs[i].id;
                                    npc.num = friendlyNPCs[i].num;
                                    npc.order = friendlyNPCs[i].order;
                                }

                            npcs.Add(npc);
                            addNPCToMenu(npc);
                        }
                        if (version >= 31) //read npcs
                        {
                            int numNames = 9;
                            if (version>=34)
                                numNames++;
							if (version>=65)
								numNames+=8;
                            for (int i = 0; i < numNames; i++)
                            {
                                string name = b.ReadString();
                                for (int j = 0; j < npcs.Count; j++)
                                {
                                    if (npcs[j].order == i)
                                    {
                                        npcs[j].name = name;
                                        addNPCToMenu(npcs[j]);
                                    }
                                }
                            }
                        }
                    }
                    if (foundInvalid)
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            MessageBox.Show("Found problems with the map: " + invalid + "\nIt may not display properly.", "Warning");
                        }));
                    }

					//load player's map
					loadPlayerMap();

                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            render.SetWorld(tilesWide, tilesHigh, groundLevel, rockLevel, styles, treeX, treeStyle, npcs);
                            loaded = true;
                            done();
                        }));
                    calculateLight();
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            serverText.Text = "";
                        }));
                }
                catch (Exception e)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            MessageBox.Show(e.Message);
                            loaded = false;
                            serverText.Text = "";
                            done();
                        }));
                }
            };
            new Thread(loadThread).Start();
        }

		private void loadPlayerMap()
		{
			for (int x = 0; x < tilesWide; x++)
				for (int y = 0; y < tilesHigh; y++)
					tiles[x,y].seen=false;

			try
			{
				string path=Path.Combine(player.Substring(0, player.Length-4),string.Concat(worldID,".map"));
                if (!File.Exists(path))
                    return;
				using (BinaryReader b = new BinaryReader(File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
				{
					int version=b.ReadInt32();
					if (version>MapVersion) //new map format
						throw new Exception("Unsupported map version: "+version);
					string title=b.ReadString();
					b.BaseStream.Seek(12, SeekOrigin.Current); //skip worldid and bounds
					for (int x = 0; x < tilesWide; x++)
					{
						for (int y = 0; y < tilesHigh; y++)
						{
							if (b.ReadBoolean())
							{
								tiles[x, y].seen=true;
								byte type=b.ReadByte();
								byte light=b.ReadByte();
								byte misc=b.ReadByte();
								byte misc2=0;
								if (version>=50) misc2=b.ReadByte();
								int rle=b.ReadInt16();
								if (light==255)
								{
									for (int r = y + 1; r < y + 1 + rle; r++)
										tiles[x, r].seen=true;
								}
								else
								{
									for (int r = y + 1; r < y + 1 + rle; r++)
									{
										light=b.ReadByte();
										tiles[x, r].seen=true;
									}
								}
								y+=rle;
							}
							else
								y+=b.ReadInt16(); //skip
						}
					}
				}
			}
			catch (Exception e)
			{
				Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
					{
						MessageBox.Show(e.Message);
					}));
			}
		}

        private void addNPCToMenu(NPC npc)
        {
            string name;
            if (npc.name == "")
                name = npc.title;
            else
                name = npc.name + " the " + npc.title;
            if (!npc.isHomeless)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    MenuItem item;
                    for (int i = 0; i < NPCs.Items.Count; i++)
                    {
                        item=(MenuItem)NPCs.Items[i];
                        if (item.Tag == npc)
                        {
                            item.Header = String.Format("Jump to {0}'s Home", name);
                            return;
                        }
                    }
                    item = new MenuItem();
                    item.Header = String.Format("Jump to {0}'s Home", name);
                    item.Click += new RoutedEventHandler(jumpNPC);
                    item.Tag = npc;
                    NPCs.Items.Add(item);
                    NPCs.IsEnabled = true;
                }));
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    MenuItem item;
                    for (int i = 0; i < NPCs.Items.Count; i++)
                    {
                        item = (MenuItem)NPCs.Items[i];
                        if (item.Tag == npc)
                        {
                            item.Header = String.Format("Jump to {0}'s Location", name);
                            return;
                        }
                    }
                    item = new MenuItem();
                    item.Header = String.Format("Jump to {0}'s Location", name);
                    item.Click += new RoutedEventHandler(jumpNPC);
                    item.Tag = npc;
                    NPCs.Items.Add(item);
                    NPCs.IsEnabled = true;
                }));
            }
        }

        private string[] prefixes;
        private string[] itemNames2;
        private string[] itemNames;

        void jumpNPC(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            NPC npc = (NPC)item.Tag;
            if (npc.isHomeless)
            {
                curX = npc.x / 16;
                curY = npc.y / 16;
            }
            else
            {
                curX = npc.homeX;
                curY = npc.homeY;
            }
            RenderMap();
        }

        private void RenderMap()
        {
            var rect = new Int32Rect(0, 0, curWidth, curHeight);

            double startx = curX - (curWidth / (2 * curScale));
            double starty = curY - (curHeight / (2 * curScale));
            try
            {
                render.Draw(curWidth, curHeight, startx, starty, curScale, ref bits,
                    isHilight, Lighting1.IsChecked ? 1 : Lighting2.IsChecked ? 2 : 0,
                    UseTextures.IsChecked && curScale > 2.0, ShowHouses.IsChecked, ShowWires.IsChecked,
                    FogOfWar.IsChecked, ref tiles);
            }
            catch (System.NotSupportedException e)
            {
                MessageBox.Show(e.ToString(), "Not supported");
            }

            //draw map here with curX,curY,curScale
            mapbits.WritePixels(rect, bits, curWidth * 4, 0);
        }

        private void Map_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
                newHeight = (int)e.NewSize.Height;
            if (e.WidthChanged)
                newWidth = (int)e.NewSize.Width;
            if (e.WidthChanged || e.HeightChanged)
            {
                resizeTimer.IsEnabled = true;
                resizeTimer.Stop();
                resizeTimer.Start();
            }
            e.Handled = true;
        }

        private void Map_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            curScale += (double)e.Delta / 500.0;
            if (curScale < MinScale)
                curScale = MinScale;
            if (curScale > MaxScale)
                curScale = MaxScale;
            if (loaded)
                RenderMap();

        }

        private void Map_MouseMove(object sender, MouseEventArgs e)
        {
            if (Map.IsMouseCaptured)
            {
                Point curPos = e.GetPosition(Map);
                Vector v = start - curPos;
                curX += v.X / curScale;
                curY += v.Y / curScale;
                if (curX < 0) curX = 0;
                if (curY < 0) curY = 0;
                if (curX > tilesWide) curX = tilesWide;
                if (curY > tilesHigh) curY = tilesHigh;
                start = curPos;
                if (loaded)
                    RenderMap();
            }
            else
            {
                Point curPos = e.GetPosition(Map);
                Vector v = start - curPos;
                if (v.X > 50 || v.Y > 50)
                    CloseAllPops();

                int sx, sy;
                getMapXY(curPos, out sx, out sy);
                if (sx >= 0 && sx < tilesWide && sy >= 0 && sy < tilesHigh && loaded)
                {
                    string label = "Nothing";
                    if (tiles[sx, sy].wall > 0)
                        label = wallInfo[tiles[sx, sy].wall].name;
                    if (tiles[sx, sy].liquid > 0)
                        label = tiles[sx, sy].isLava ? "Lava" : tiles[sx, sy].isHoney ? "Honey" : "Water";
                    if (tiles[sx, sy].isActive)
                        label = tileInfos[tiles[sx, sy].type, tiles[sx, sy].u, tiles[sx, sy].v].name;
                    if (FogOfWar.IsChecked && !tiles[sx, sy].seen)
                        label = "Murky blackness";
                    statusText.Text = String.Format("{0},{1} {2}", sx, sy, label);
                }
                else
                    statusText.Text = "";
            }
        }

        Point start;
        private void Map_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAllPops();

            Map.Focus();
            Map.CaptureMouse();
            start = e.GetPosition(Map);
        }

        private void Map_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Map.ReleaseMouseCapture();
        }
        private SignPopup signPop = null;
        private ChestPopup chestPop = null;

        private void CloseAllPops()
        {
            if (signPop != null)
            {
                signPop.IsOpen = false;
                signPop = null;
            }
            if (chestPop != null)
            {
                chestPop.IsOpen = false;
                chestPop = null;
            }
        }

        private void getMapXY(Point p, out int sx, out int sy)
        {
            double startx = curX - (curWidth / (2 * curScale));
            double starty = curY - (curHeight / (2 * curScale));
            int blocksWide = (int)(curWidth / Math.Floor(curScale)) + 2;
            int blocksHigh = (int)(curHeight / Math.Floor(curScale)) + 2;
            double adjustx = ((curWidth / curScale) - blocksWide) / 2;
            double adjusty = ((curHeight / curScale) - blocksHigh) / 2;

            if (UseTextures.IsChecked && curScale > 2.0)
            {
                sx = (int)(p.X / Math.Floor(curScale) + startx + adjustx);
                sy = (int)(p.Y / Math.Floor(curScale) + starty + adjusty);
            }
            else
            {
                sx = (int)(p.X / curScale + startx);
                sy = (int)(p.Y / curScale + starty);
            }
        }

        private void Map_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!loaded)
                return;
            CloseAllPops();
            Point curPos = e.GetPosition(Map);
            start = curPos;
            int sx, sy;
            getMapXY(curPos, out sx, out sy);
            foreach (Chest c in chests)
            {
                //chests are 2x2, and their x/y is upper left corner
                if ((c.x == sx || c.x + 1 == sx) && (c.y == sy || c.y + 1 == sy))
                {
                    ArrayList items = new ArrayList();
                    for (int i = 0; i < c.items.Length; i++)
                    {
                        if (c.items[i].stack > 0)
                        {
                            if (c.items[i].prefix=="")
                                items.Add(String.Format("{0} {1}", c.items[i].stack, c.items[i].name));
                            else
                                items.Add(String.Format("{0} {1} {2}", c.items[i].stack, c.items[i].prefix, c.items[i].name));
                        }
                    }
                    chestPop = new ChestPopup(items);
                    chestPop.IsOpen = true;
                }
            }
            foreach (Sign s in signs)
            {
                //signs are 2x2, and their x/y is upper left corner
                if ((s.x == sx || s.x + 1 == sx) && (s.y == sy || s.y + 1 == sy))
                {
                    signPop = new SignPopup(s.text);
                    signPop.IsOpen = true;
                }
            }
        }

        int moving = 0; //moving bitmask
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool changed = false;
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    moving |= 1;
                    break;
                case Key.Down:
                case Key.S:
                    moving |= 2;
                    break;
                case Key.Left:
                case Key.A:
                    moving |= 4;
                    break;
                case Key.Right:
                case Key.D:
                    moving |= 8;
                    break;
                case Key.PageUp:
                case Key.E:
                    curScale += 1.0;
                    if (curScale > MaxScale)
                        curScale = MaxScale;
                    changed = true;
                    break;
                case Key.PageDown:
                case Key.Q:
                    curScale -= 1.0;
                    if (curScale < MinScale)
                        curScale = MinScale;
                    changed = true;
                    break;
            }
            if (moving != 0)
            {
                double speed = 10.0;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    speed *= 2;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    speed *= 10.0;
                if ((moving & 1) != 0) //up
                    curY -= speed / curScale;
                if ((moving & 2) != 0) //down
                    curY += speed / curScale;
                if ((moving & 4) != 0) //left
                    curX -= speed / curScale;
                if ((moving & 8) != 0) //right
                    curX += speed / curScale;

                if (curX < 0) curX = 0;
                if (curY < 0) curY = 0;
                if (curX > tilesWide) curX = tilesWide;
                if (curY > tilesHigh) curY = tilesHigh;
                changed = true;
            }
            if (changed)
            {
                e.Handled = true;
                if (loaded)
                    RenderMap();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    moving &= ~1;
                    break;
                case Key.Down:
                case Key.S:
                    moving &= ~2;
                    break;
                case Key.Left:
                case Key.A:
                    moving &= ~4;
                    break;
                case Key.Right:
                case Key.D:
                    moving &= ~8;
                    break;
            }
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Terraria Worlds|*.wld";
            var result = dlg.ShowDialog();
            if (result == true)
            {
                busy = true;
                Load(dlg.FileName, delegate()
                {
                    busy = false;
                    if (!loaded)
                        return;
                    curX = spawnX;
                    curY = spawnY;
                    if (render.Textures!=null && render.Textures.Valid)
                    {
                        UseTextures.IsChecked = true;
                        curScale = 16.0;
                    }
                    RenderMap();
                });
            }
        }
        private void OpenWorld(object sender, ExecutedRoutedEventArgs e)
        {
            if (busy) //fail
                return;
            int id = (int)e.Parameter;
            busy = true;
            Load(worlds[id], delegate()
            {
                busy = false;
                if (!loaded)
                    return;
                curX = spawnX;
                curY = spawnY;
                if (render.Textures!=null && render.Textures.Valid)
                {
                    UseTextures.IsChecked = true;
                    curScale = 16.0;
                }
                RenderMap();
            });
        }
        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !busy;
        }
        private void OpenWorld_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !busy;
        }
        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
		private void SelectPlayer(object sender, ExecutedRoutedEventArgs e)
		{
            if (busy) //fail
                return;
            int id = (int)e.Parameter;
			player=players[id];
            //uncheck other players
            foreach (MenuItem item in Players.Items)
            {
                if (item.CommandParameter != e.Parameter)
                    item.IsChecked = false;
            }
            if (!loaded)
                return;

			// should load player map here
			ThreadStart loader = delegate()
				{
					loadPlayerMap();
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                    {
                        RenderMap();
                    }));
				};
			new Thread(loader).Start();
		}
		private void SelectPlayer_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !busy;
		}
        private void FogOfWar_Toggle(object sender, ExecutedRoutedEventArgs e)
        {
            if (FogOfWar.IsChecked)
                FogOfWar.IsChecked = false;
            else
                FogOfWar.IsChecked = true;
            if (loaded)
                RenderMap();
        }
        private void FogOfWar_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Players.IsEnabled;
        }
        private void JumpToSpawn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            curX = spawnX;
            curY = spawnY;
            RenderMap();
        }
        private void Lighting_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == MapCommands.NoLight)
            {
                Lighting0.IsChecked = true;
                Lighting1.IsChecked = false;
                Lighting2.IsChecked = false;
            }
            else if (e.Command == MapCommands.Lighting)
            {
                Lighting0.IsChecked = false;
                Lighting1.IsChecked = true;
                Lighting2.IsChecked = false;
            }
            else
            {
                Lighting0.IsChecked = false;
                Lighting1.IsChecked = false;
                Lighting2.IsChecked = true;
            }
            RenderMap();
        }
        private void Texture_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (render.Textures==null || !render.Textures.Valid)
                return;
            if (UseTextures.IsChecked)
                UseTextures.IsChecked = false;
            else
                UseTextures.IsChecked = true;
            RenderMap();
        }
        private void TexturesUsed(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = UseTextures.IsChecked;
        }
        private void Redraw(object sender, ExecutedRoutedEventArgs e)
        {
            RenderMap();
        }

        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            busy = true;
            Load(currentWorld, delegate()
            {
                busy = false;
                if (loaded)
                    RenderMap();
            });
        }

        private void ConnectToServer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //we should disconnect if connected.

            ConnectToServer c = new ConnectToServer();
            if (c.ShowDialog() == true)
            {
                string serverip = c.ServerIP;
                int port = c.ServerPort;
                if (serverip == "")
                {
                    MessageBox.Show("Invalid server address");
                    return;
                }
                if (port == 0)
                {
                    MessageBox.Show("Invalid port");
                    return;
                }

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                System.Net.IPAddress[] ips;
                try
                {
                    ips = System.Net.Dns.GetHostAddresses(serverip);
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid server address");
                    return;
                }
                foreach (System.Net.IPAddress addr in ips)
                {
                    try
                    {
                        System.Net.IPEndPoint remoteEP = new System.Net.IPEndPoint(addr, port);

                        socket.BeginConnect(remoteEP, new AsyncCallback(connected), null);
                        break;
                    }
                    catch
                    {
                    }
                }
                busy = true;
                serverText.Text = "Connecting...";
            }
        }
        private void Disconnect_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            socket.Close();
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
            {
                serverText.Text = "Connection cancelled.";
            }));
        }
        private void Disconnect_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = socket!=null && socket.Connected;
        }
        private void connected(IAsyncResult ar)
        {
            try
            {
                socket.EndConnect(ar);
                //we connected, huzzah!
                readBuffer = new byte[1024];
                writeBuffer = new byte[1024];
                messages = new byte[8192];
                pendingSize = 0;
                loginLevel = 1;
                SendMessage(1); //greetings server!
                socket.BeginReceive(readBuffer, 0, readBuffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceivedData), null);
            }
            catch (Exception e)
            {
                socket.Close();
                busy = false;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    MessageBox.Show(e.Message);
                    serverText.Text = "Connection failed.";
                }));
            }
        }

        private void ReceivedData(IAsyncResult ar)
        {
            try
            {
                int bytesRead = socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    Buffer.BlockCopy(readBuffer, 0, messages, pendingSize, bytesRead);
                    pendingSize += bytesRead;
                    messagePump();
                    if (socket.Connected)
                        socket.BeginReceive(readBuffer, 0, readBuffer.Length, SocketFlags.None,
                            new AsyncCallback(ReceivedData), null); //restart receive
                }
                else
                {
                    busy = false;
                    socket.Close();
                    // socket was closed?
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                    {
                        serverText.Text = "Connection lost.";
                    }));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                socket.Close();
                busy = false;
            }
        }

        private void messagePump()
        {
            if (pendingSize < 5) //haven't received enough data for even a message header
                return;
            int msgLen = BitConverter.ToInt32(messages, 0);
            int ofs = 0;
            while (ofs + 4 + msgLen <= pendingSize)
            {
                HandleMessage(ofs + 4, msgLen);
                ofs += msgLen + 4;
                if (ofs + 4 <= pendingSize)
                    msgLen = BitConverter.ToInt32(messages, ofs);
                else
                    break;
            }
            if (ofs == pendingSize)
                pendingSize = 0;
            else if (ofs > 0)
            {
                Buffer.BlockCopy(messages, ofs, messages, 0, pendingSize - ofs);
                pendingSize -= ofs;
            }
        }

        private void HandleMessage(int start, int len)
        {
            if (statusTotal > 0)
            {
                statusCount++;
                if (statusCount == statusTotal)
                    statusTotal = 0;
                else
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    serverText.Text = ((int)((float)statusCount * 100.0 / (float)statusTotal)) + "% - " + status;
                }));
            }
            int messageid = messages[start++];
            len--;
            int payload = start;
            switch (messageid)
            {
                case 0x01: // connect request - c2s only
                    break;
                case 0x02: //error
                    {
                        string error = Encoding.ASCII.GetString(messages, payload, len);
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                            {
                                MessageBox.Show(error);
                            }));
                        socket.Close();
                        busy = false;
                    }
                    break;
                case 0x03: //connection approved
                    {
                        if (loginLevel == 1) loginLevel = 2;
                        playerSlot = messages[payload];
                        SendMessage(4);
                        SendMessage(0x10);
                        SendMessage(0x2a);
                        //send buffs
                        //send inventory
                        //send dyes
                        SendMessage(6);
                        if (loginLevel == 2) loginLevel = 3;
                    }
                    break;
                case 0x04: //player appearance
                    //ignore other players
                    break;
                case 0x05: //inventory items
                    //ignore player inventory
                    break;
                case 0x06: //request world info - c2s only
                    break;
                case 0x07: //world info
                    {
                        gameTime = BitConverter.ToInt32(messages, payload); payload += 4;
                        dayNight = messages[payload++] == 1;
                        moonPhase = messages[payload++];
                        bloodMoon = messages[payload++] == 1;
                        payload++; //eclipse
                        tilesWide = BitConverter.ToInt32(messages, payload); payload += 4;
                        tilesHigh = BitConverter.ToInt32(messages, payload); payload += 4;
                        spawnX = BitConverter.ToInt32(messages, payload); payload += 4;
                        spawnY = BitConverter.ToInt32(messages, payload); payload += 4;
                        groundLevel = BitConverter.ToInt32(messages, payload); payload += 4;
                        rockLevel = BitConverter.ToInt32(messages, payload); payload += 4;
                        worldID = BitConverter.ToInt32(messages, payload); payload += 4;
                        payload++; //moon type
                        for (int i = 0; i < 3; i++)
                        {
                            treeX[i] = BitConverter.ToInt32(messages, payload); payload += 4;
                        }
                        for (int i = 0; i < 4; i++)
                            treeStyle[i] = messages[payload++];
                        for (int i = 0; i < 3; i++)
                        {
                            caveBackX[i] = BitConverter.ToInt32(messages, payload); payload += 4;
                        }
                        for (int i = 0; i < 4; i++)
                            caveBackStyle[i] = messages[payload++];
                        for (int i = 0; i < 8; i++)
                            styles[i] = messages[payload++];
                        iceBackStyle = messages[payload++];
                        jungleBackStyle = messages[payload++];
                        hellBackStyle = messages[payload++];
                        payload += 4; //wind speed
                        payload++; //number of clouds
                        byte flags = messages[payload++];
                        byte flags2 = messages[payload++];
                        payload += 4; //max rain
                        string title = Encoding.ASCII.GetString(messages, payload, start + len - payload);
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            Title = title;
                        }));
                        smashedOrb = (flags & 1) == 1;
                        killedBoss1 = (flags & 2) == 2;
                        killedBoss2 = (flags & 4) == 4;
                        killedBoss3 = (flags & 8) == 8;
                        hardMode = (flags & 16) == 16;
                        killedClown = (flags & 32) == 32;
                        killedMechBoss1 = (flags2 & 1) == 1;
                        killedMechBoss2 = (flags2 & 2) == 2;
                        killedMechBoss3 = (flags2 & 4) == 4;
                        killedMechBossAny = (flags2 & 8) == 8;
                        crimson = (flags2 & 32) == 32;
                        meteorSpawned = false;
                        killedFrost = false;
                        killedGoblins = false;
                        killedPirates = false;
                        killedPlantBoss = false;
                        killedQueenBee = false;
                        savedMechanic = false;
                        savedTinkerer = false;
                        savedWizard = false;
                        goblinsDelay = 0;
                        altarsSmashed = 0;
                        ResizeMap();
                        if (loginLevel == 3)
                        {
                            sectionsWide = (tilesWide / 200);
                            sectionsHigh = (tilesHigh / 150);
                            sentSections = new bool[sectionsWide, sectionsHigh];
                            loginLevel = 4;
                            for (int y = 0; y < tilesHigh; y++) //set all tiles to blank
                                for (int x = 0; x < tilesWide; x++)
                                {
                                    tiles[x, y].isActive = false;
                                    tiles[x, y].wall = 0;
                                    tiles[x, y].liquid = 0;
                                    tiles[x, y].hasRedWire = false;
                                    tiles[x, y].hasGreenWire = false;
                                    tiles[x, y].hasBlueWire = false;
                                    tiles[x, y].half = false;
                                    tiles[x, y].color = 0;
                                }
                            SendMessage(8); //request initial tile data
                        }
                        chests.Clear();
                        signs.Clear();
                        npcs.Clear();
                        loadPlayerMap();
                    }
                    break;
                case 0x08: //request initial tile data - c2s only
                    break;
                case 0x09: //status text
                    {
                        statusTotal = BitConverter.ToInt32(messages, payload); payload += 4;
                        statusCount = 0;
                        status = Encoding.ASCII.GetString(messages, payload, start + len - payload);
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            serverText.Text = status;
                        }));
                    }
                    break;
                case 0x0a: //tile row data
                    {
                        int width = BitConverter.ToInt16(messages, payload); payload += 2;
                        int startx = BitConverter.ToInt32(messages, payload); payload += 4;
                        int y = BitConverter.ToInt32(messages, payload); payload += 4;
                        for (int x = startx; x < startx + width; x++)
                        {
                            Tile tile = tiles[x, y];
                            byte flags = messages[payload++];
                            byte flags2 = messages[payload++];
                            tile.isActive = (flags & 1) == 1;
                            tile.hasRedWire = (flags & 16) == 16;
                            tile.half = (flags & 32) == 32;
                            tile.hasGreenWire = (flags2 & 1) == 1;
                            tile.hasBlueWire = (flags2 & 2) == 2;
                            tile.slope = (byte)((flags2 & 0x30) >> 4);
                            if ((flags2 & 4) == 4)
                                tile.color = messages[payload++];
                            else
                                tile.color = 0;
                            if ((flags2 & 8) == 8)
                                tile.wallColor = messages[payload++];
                            else
                                tile.wallColor = 0;
                            if (tile.isActive)
                            {
                                tile.type = messages[payload++];
                                if (tileInfos[tile.type].hasExtra)
                                {
                                    tile.u = BitConverter.ToInt16(messages, payload); payload += 2;
                                    tile.v = BitConverter.ToInt16(messages, payload); payload += 2;
                                }
                                else
                                {
                                    tile.u = -1;
                                    tile.v = -1;
                                }
                            }
                            if ((flags & 4) == 4)
                            {
                                tile.wall = messages[payload++];
                                tile.wallu = -1;
                                tile.wallv = -1;
                            }
                            else
                                tile.wall = 0;
                            if ((flags & 8) == 8)
                            {
                                tile.liquid = messages[payload++];
                                tile.isLava = messages[payload] == 1;
                                tile.isHoney = messages[payload] == 2;
                                payload++;
                            }
                            else
                                tile.liquid = 0;
                            int rle = BitConverter.ToInt16(messages, payload); payload += 2;
                            for (int r = x + 1; r < x + 1 + rle; r++)
                            {
                                tiles[r, y].isActive = tiles[x, y].isActive;
                                tiles[r, y].type = tiles[x, y].type;
                                tiles[r, y].u = tiles[x, y].u;
                                tiles[r, y].v = tiles[x, y].v;
                                tiles[r, y].wall = tiles[x, y].wall;
                                tiles[r, y].wallu = -1;
                                tiles[r, y].wallv = -1;
                                tiles[r, y].liquid = tiles[x, y].liquid;
                                tiles[r, y].isLava = tiles[x, y].isLava;
                                tiles[r, y].isHoney = tiles[x, y].isHoney;
                                tiles[r, y].hasRedWire = tiles[x, y].hasRedWire;
                                tiles[r, y].hasGreenWire = tiles[x, y].hasGreenWire;
                                tiles[r, y].hasBlueWire = tiles[x, y].hasBlueWire;
                                tiles[r, y].half = tiles[x, y].half;
                                tiles[r, y].slope = tiles[x, y].slope;
                                tiles[r, y].color = tiles[x, y].color;
                                tiles[r, y].wallColor = tiles[x, y].wallColor;
                            }
                            x += rle;
                        }
                    }
                    break;
                case 0x0b: //recalculate u/v
                    {
                        int startx = BitConverter.ToInt32(messages, payload); payload += 4;
                        int starty = BitConverter.ToInt32(messages, payload); payload += 4;
                        int endx = BitConverter.ToInt32(messages, payload); payload += 4;
                        int endy = BitConverter.ToInt32(messages, payload);

                        for (int y = starty; y<= endy; y++)
                            for (int x = startx; x <= endx; x++)
                                sentSections[x, y] = true;

                        startx *= 200;
                        starty *= 150;
                        endx = (endx + 1) * 200;
                        endy = (endy + 1) * 150;

                        
                        for (int y=starty;y<endy;y++)
                            for (int x = startx; x < endx; x++)
                            {
                                Tile tile = tiles[x, y];
                                if (tile.isActive && !tileInfos[tile.type].hasExtra)
                                {
                                    tile.u = -1;
                                    tile.v = -1;
                                }
                                if (tile.wall > 0)
                                {
                                    tile.wallu = -1;
                                    tile.wallv = -1;
                                }
                            }
                        if (loginLevel == 5)
                            fetchNextSection();
                    }
                    break;
                case 0x0c: //player spawned
                    break;
                case 0x0d: //player control
                    break;
                case 0x0e: //set active players
                    break;
                case 0x10: //player life
                    break;
                case 0x11: //modify tile
                    break;
                case 0x12: //set time
                    break;
                case 0x13: //open/close door
                    break;
                case 0x14: //update tile block
                    break;
                case 0x15: //update item
                    break;
                case 0x16: //set item owner
                    break;
                case 0x17: //update NPC
                    {
                        int slot = BitConverter.ToInt16(messages, payload); payload += 2;
                        float posx = BitConverter.ToSingle(messages, payload); payload += 4;
                        float posy = BitConverter.ToSingle(messages, payload); payload += 4;
                        payload += 30; //don't care about velocity, target, ai, or direction
                        int id = BitConverter.ToInt16(messages, payload);
                        bool found = false;
                        for (int i = 0; i < npcs.Count; i++)
                        {
                            if (npcs[i].slot == slot)
                            {
                                npcs[i].x = posx;
                                npcs[i].y = posy;
                                npcs[i].sprite = id;
                                found = true;
                                addNPCToMenu(npcs[i]);
                            }
                        }
                        if (!found)
                        {
                            for (int i = 0; i < friendlyNPCs.Length; i++)
                                if (friendlyNPCs[i].id == id) //we found a friendly npc
                                {
                                    NPC npc = new NPC();
                                    npc.isHomeless = true; //homeless for now
                                    npc.title = friendlyNPCs[i].title;
                                    npc.name = "";
                                    npc.num = friendlyNPCs[i].num;
                                    npc.sprite = id;
                                    npc.x = posx;
                                    npc.y = posy;
                                    npc.slot = slot;
                                    npcs.Add(npc);
                                    addNPCToMenu(npc);
                                }
                        }
                    }
                    break;
                case 0x18: //strike npc
                    break;
                case 0x19: //chat
                    break;
                case 0x1a: //damage player
                    break;
                case 0x1b: //update projectile
                    break;
                case 0x1c: //damage npc
                    break;
                case 0x1d: //destroy projectile
                    break;
                case 0x1e: //pvp toggled
                    break;
                case 0x1f: //request open chest - c2s only
                    break;
                case 0x20: //set chest item
                    break;
                case 0x21: //open chest
                    break;
                case 0x22: //destroy chest
                    break;
                case 0x23: //heal player
                    break;
                case 0x24: //set zones
                    break;
                case 0x25: //request password.
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            ServerPassword s = new ServerPassword();
                            if (s.ShowDialog() == true)
                                SendMessage(0x26, s.Password);
                            else
                            {
                                socket.Close();
                                serverText.Text = "Login cancelled.";
                                busy = false;
                            }
                        }));
                    }
                    break;
                case 0x26: //login - c2s only
                    break;
                case 0x27: //unassign item
                    break;
                case 0x28: //talk to npc
                    break;
                case 0x29: //animate flail
                    break;
                case 0x2a: //set mana
                    break;
                case 0x2b: //replenish mana
                    break;
                case 0x2c: //kill player
                    break;
                case 0x2d: //change party
                    break;
                case 0x2e: //read sign - c2s only
                    break;
                case 0x2f: //edit sign
                    break;
                case 0x30: //adjust liquids
                    break;
                case 0x31: //okay to spawn
                    if (loginLevel == 4)
                    {
                        loginLevel = 5;
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                            {
                                serverText.Text = "";
                                render.SetWorld(tilesWide, tilesHigh, groundLevel, rockLevel, styles, treeX, treeStyle, npcs);
                                loaded = true;
                                curX = spawnX;
                                curY = spawnY;
                                if (render.Textures!=null && render.Textures.Valid)
                                {
                                    UseTextures.IsChecked = true;
                                    curScale = 16.0;
                                }
                                RenderMap();
                            }));
                        SendMessage(0x0c); //spawn
                        if (tilesWide == 8400) //large world
                        {
                            //give the user a choice to map a remote large world
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                            {
                                if (MessageBox.Show(this, "Mapping remote large worlds is not recommend.\nContinue to map the rest of the world?", "Map Whole World",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    fetchNextSection(); //start fetching the world
                                }
                                else
                                {
                                    socket.Close();
                                    busy = false;
                                }
                            }));
                        }
                        else
                            fetchNextSection(); //start fetching the world
                    }
                    break;
                case 0x32: //set buffs
                    break;
                case 0x33: //old man answer
                    break;
                case 0x34: //unlock chest
                    break;
                case 0x35: //add npc buff
                    break;
                case 0x36: //set npc buffs
                    break;
                case 0x37: //add player buff
                    break;
                case 0x38: //set npc names
                    {
                        int id = BitConverter.ToInt16(messages, payload); payload += 2;
                        string name = Encoding.ASCII.GetString(messages, payload, start + len - payload);
                        for (int i = 0; i < npcs.Count; i++)
                        {
                            if (npcs[i].sprite == id)
                            {
                                npcs[i].name = name;
                                addNPCToMenu(npcs[i]);
                            }
                        }
                    }
                    break;
                case 0x39: //set balance stats
                    break;
                case 0x3a: //play harp
                    break;
                case 0x3b: //flip switch
                    break;
                case 0x3c: //move npc home
                    {
                        int slot=BitConverter.ToInt16(messages,payload); payload+=2;
                        int x=BitConverter.ToInt16(messages,payload); payload+=2;
                        int y=BitConverter.ToInt16(messages,payload); payload+=2;
                        byte homeless=messages[payload];
                        for (int i = 0; i < npcs.Count; i++)
                        {
                            if (npcs[i].slot == slot)
                            {
                                npcs[i].isHomeless = homeless == 1;
                                npcs[i].homeX = x;
                                npcs[i].homeY = y;
                                addNPCToMenu(npcs[i]);
                                break;
                            }
                        }
                    }
                    break;
                case 0x3d: //summon boss
                    break;
                case 0x3e: //ninja dodge
                    break;
                case 0x3f: //paint tile
                    break;
                case 0x40: //paint wall
                    break;
                case 0x41: //teleport npc
                    break;
                case 0x42: //heal player
                    break;
                case 0x44: //unknown
                    break;
                default: // ignore unknown messages
                    break;
            }
        }

        private void SendMessage(int messageid, string text = null,int x=0,int y=0)
        {
            int payload = 5;
            int payloadLen = 0;
            switch (messageid)
            {
                case 1: //send greeting
                    byte[] greeting = Encoding.ASCII.GetBytes("Terraria" + MapVersion);
                    payloadLen = greeting.Length;
                    Buffer.BlockCopy(greeting, 0, writeBuffer, payload, payloadLen);
                    break;
                case 4: //send player info
                    writeBuffer[payload++] = playerSlot;
                    writeBuffer[payload++] = 0; //hair
                    writeBuffer[payload++] = 1; //male
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //hair color
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //skin color
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //eye color
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //shirt color
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //undershirt color
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //pants color
                    writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; writeBuffer[payload++] = 0; //shoe color
                    writeBuffer[payload++] = 0; //soft core
                    byte[] name = Encoding.ASCII.GetBytes("Terrafirma");
                    Buffer.BlockCopy(name, 0, writeBuffer, payload, name.Length);
                    payloadLen += 25 + name.Length;
                    break;
                case 6: //request world info
                    //no payload
                    break;
                case 8: //request initial tile data
                    Buffer.BlockCopy(BitConverter.GetBytes(spawnX), 0, writeBuffer, payload, 4); payload+=4;
                    Buffer.BlockCopy(BitConverter.GetBytes(spawnY), 0, writeBuffer, payload, 4);
                    payloadLen += 8;
                    break;
                case 0x0c: //spawn
                    writeBuffer[payload++] = playerSlot;
                    Buffer.BlockCopy(BitConverter.GetBytes(spawnX), 0, writeBuffer, payload, 4); payload+=4;
                    Buffer.BlockCopy(BitConverter.GetBytes(spawnY), 0, writeBuffer, payload, 4);
                    payloadLen += 9;
                    break;
                case 0x0d: //player control
                    writeBuffer[payload++] = playerSlot;
                    writeBuffer[payload++] = 0; //no buttons
                    writeBuffer[payload++] = 0; //selected item 0
                    Buffer.BlockCopy(BitConverter.GetBytes((float)(x*16.0)), 0, writeBuffer, payload, 4); payload+=4;
                    Buffer.BlockCopy(BitConverter.GetBytes((float)(y*16.0)), 0, writeBuffer, payload, 4); payload+=4;
                    byte[] velocity = BitConverter.GetBytes((float)0);
                    Buffer.BlockCopy(velocity, 0, writeBuffer, payload, 4); payload+=4;
                    Buffer.BlockCopy(velocity, 0, writeBuffer, payload, 4); payload+=4;
                    writeBuffer[payload] = 0; //not on a rope
                    payloadLen += 20;
                    break;
                case 0x10: //set player life
                    writeBuffer[payload++] = playerSlot;
                    byte[] health = BitConverter.GetBytes((Int16)400);
                    Buffer.BlockCopy(health, 0, writeBuffer, payload, 2); payload+=2;
                    Buffer.BlockCopy(health, 0, writeBuffer, payload, 2);
                    payloadLen += 5;
                    break;
                case 0x26: //send password
                    byte[] password = Encoding.ASCII.GetBytes(text);
                    payloadLen = password.Length;
                    Buffer.BlockCopy(password, 0, writeBuffer, payload, payloadLen);
                    break;
                case 0x2a: //set mana
                    writeBuffer[payload++] = playerSlot;
                    byte[] mana = BitConverter.GetBytes((Int16)0);
                    Buffer.BlockCopy(mana, 0, writeBuffer, payload, 2); payload+=2;
                    Buffer.BlockCopy(mana, 0, writeBuffer, payload, 2);
                    payloadLen += 5;
                    break;
                case 0x2d: //set team
                    writeBuffer[payload++] = playerSlot;
                    writeBuffer[payload++] = (byte)x;
                    payloadLen += 2;
                    break;
                default:
                    throw new Exception(String.Format("Unknown messageid: {0}", messageid));
            }

            byte[] msgLen = BitConverter.GetBytes(payloadLen + 1);
            Buffer.BlockCopy(msgLen, 0, writeBuffer, 0, 4);
            writeBuffer[4] = (byte)messageid;
            if (socket.Connected)
                socket.BeginSend(writeBuffer, 0, payloadLen + 5, SocketFlags.None,
                    new AsyncCallback(SentMessage), null);
        }
        private void SentMessage(IAsyncResult ar)
        {
            try
            {
                if (socket.Connected)
                    socket.EndSend(ar);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void fetchNextSection()
        {
            bool foundOne = false;
            for (int y = 0; y < sectionsHigh && !foundOne; y++)
                for (int x = 0; x < sectionsWide && !foundOne; x++)
                {
                    if (!sentSections[x, y])
                    {
                        SendMessage(0x0d, "", x * 200, y * 150);
                        foundOne = true;
                    }
                }
            if (!foundOne)
            {
                socket.Close();
                loginLevel = 0;
                busy = false;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    serverText.Text = "Map Load Complete. Disconnected.";
                }));
            }
        }

        private void ResizeMap()
        {
            for (int y = 0; y < tilesHigh; y++)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    serverText.Text = ((int)((float)y * 100.0 / (float)tilesHigh)) + "% - Allocating tiles";
                }));
                for (int x = 0; x < tilesWide; x++)
                {
                    if (tiles[x, y] == null)
                        tiles[x, y] = new Tile();
                }
            }
            if (tilesWide < Widest || tilesHigh < Highest) //free unused tiles
            {
                for (int y = 0; y < Highest; y++)
                {
                    int start = tilesWide;
                    if (y >= tilesHigh)
                        start = 0;
                    for (int x = start; x < Widest; x++)
                        tiles[x, y] = null;
                }
            }
        }

        private void Hilight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ArrayList items = tileInfos.Items();
            HilightWin h = new HilightWin(items);
            if (h.ShowDialog() == true)
            {
                foreach(var i in h.SelectedItems){
                    i.isHilighting = true;
                    // also hilight the subvariants
                    hiliteVariants(i);
                }
                isHilight = true;
                RenderMap();
            }
        }
        private void hiliteVariants(TileInfo info)
        {
            foreach (TileInfo v in info.variants)
            {
                v.isHilighting = true;
                hiliteVariants(v);
            }
        }

        private void HilightStop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            isHilight = false;
            RenderMap();
        }
        private void IsHilighting(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isHilight;
        }
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "Png Image|*.png";
            dlg.Title = "Save Map Image";
            if (dlg.ShowDialog() == true)
            {
                var saveOpts = new SaveOptions();
                saveOpts.CanUseTexture = render.Textures!=null && render.Textures.Valid;
                if (saveOpts.ShowDialog() == true)
                {

                    Saving save = new Saving();
                    save.Show();
                    byte[] pixels;
                    int wd, ht;
                    double sc, startx, starty;

                    if (saveOpts.EntireMap)
                    {
                        wd = tilesWide;
                        ht = tilesHigh;
                        sc = 1.0;
                        startx = 0.0;
                        starty = 0.0;
                    }
                    else
                    {
                        if (saveOpts.UseZoom)
                            sc = curScale;
                        else if (saveOpts.UseTextures)
                            sc = 16.0;
                        else
                            sc = 1.0;

                        wd = (int)((curWidth / curScale) * sc);
                        ht = (int)((curHeight / curScale) * sc);
                        startx = curX - (wd / (2 * sc));
                        starty = curY - (ht / (2 * sc));
                    }
                    pixels = new byte[wd * ht * 4];

                    render.Draw(wd, ht, startx, starty, sc,
                        ref pixels, false, Lighting1.IsChecked ? 1 : Lighting2.IsChecked ? 2 : 0,
                        saveOpts.UseTextures && curScale > 2.0, ShowHouses.IsChecked, ShowWires.IsChecked,
                        FogOfWar.IsChecked, ref tiles);

                    BitmapSource source = BitmapSource.Create(wd, ht, 96.0, 96.0,
                        PixelFormats.Bgr32, null, pixels, wd * 4);
                    FileStream stream = new FileStream(dlg.FileName, FileMode.Create);
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(stream);
                    stream.Close();
                    save.Close();
                }
            }

        }
        private void MapLoaded(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = loaded;
        }

        private void JumpToDungeon_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            curX = dungeonX;
            curY = dungeonY;
            RenderMap();
        }
        private void ShowStats_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WorldStats stats = new WorldStats();
            stats.Add("Eye of Cthulhu", killedBoss1 ? "Blackened" : "Undefeated");
            if (crimson)
                stats.Add("Brain of Cthulhu", killedBoss2 ? "Lobotomized" : "Undefeated");
            else
                stats.Add("Eater of Worlds", killedBoss2 ? "Choked" : "Undefeated");
            stats.Add("Skeletron", killedBoss3 ? "Boned" : "Undefeated");
            stats.Add("Wall of Flesh", hardMode ? "Flayed" : "Undefeated");
            stats.Add("Queen Bee", killedQueenBee ? "Swatted" : "Undefeated");
            stats.Add("The Destroyer", killedMechBoss1 ? "Destroyed" : "Undefeated");
            stats.Add("The Twins", killedMechBoss2 ? "Separated" : "Undefeated");
            stats.Add("Skeletron Prime", killedMechBoss3 ? "Boned" : "Undefeated");
            stats.Add("Plantera", killedPlantBoss ? "Weeded" : "Undefeated");
            stats.Add("Golem", killedGolemBoss ? "Stoned" : "Undefeated");
            stats.Add("Goblin Invasion", killedGoblins ? "Thwarted" : "Undefeated");
            stats.Add("Clown", killedClown ? "Eviscerated" : "Undefeated");
            stats.Add("Frost Horde", killedFrost ? "Thawed" : "Undefeated");
            stats.Add("Pirates", killedPirates ? "Keelhauled" : "Undefeated");
            stats.Add("Tinkerer", savedTinkerer ? "Saved" : killedGoblins ? "Bound" : "Not present yet");
            stats.Add("Wizard", savedWizard ? "Saved" : hardMode ? "Bound" : "Not present yet");
            stats.Add("Mechanic", savedMechanic ? "Saved" : killedBoss3 ? "Bound" : "Not present yet");
            stats.Add("Game Mode", hardMode ? "Hard" : "Normal");
            stats.Add("Broke a Shadow Orb", smashedOrb ? "Yes" : "Not Yet");
            stats.Add("Orbs left til EoW", (3 - shadowOrbCount).ToString());
            stats.Add("Altars Smashed", altarsSmashed.ToString());
            stats.Show();
        }

        private void FindItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Dictionary<string,List<int>> items=new Dictionary<string,List<int>>();
            for (int i = 0; i < chests.Count; i++)
            {
                foreach (ChestItem c in chests[i].items)
                {
                    if (c.name == null)
                        continue;
                    if (!items.ContainsKey(c.name))
                        items.Add(c.name, new List<int>());
                    items[c.name].Add(i);
                }
            }

            FindItem fi = new FindItem(items);
            if (fi.ShowDialog() == true)
            {
                int id = fi.SelectedChest;
                curX = chests[id].x;
                curY = chests[id].y;
                RenderMap();
            }
        }

        private void initWindow(object sender, EventArgs e)
        {
            checkVersion();
            try
            {
                render.Textures = new Textures();
                if (!render.Textures.Valid) //couldn't find textures?
                    UseTextures.IsEnabled = false;
            }
            catch (Exception ex)
            {
                render.Textures = null;
                UseTextures.IsEnabled=false;
                MessageBox.Show(ex.Message,"Texture support failed");
            }

        }

        private void calculateLight()
        {
            // turn off all light
            for (int y = 0; y < tilesHigh; y++)
            {
                for (int x = 0; x < tilesWide; x++)
                {
                    Tile tile = tiles[x, y];
                    tile.light = 0.0;
                    tile.lightR = 0.0;
                    tile.lightG = 0.0;
                    tile.lightB = 0.0;
                }
            }
            // light up light sources
            for (int y = 0; y < tilesHigh; y++)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    serverText.Text = ((int)((float)y * 100.0 / (float)tilesHigh)) + "% - Lighting tiles";
                }));
                for (int x = 0; x < tilesWide; x++)
                {
                    Tile tile = tiles[x, y];
                    TileInfo inf = tileInfos[tile.type, tile.u, tile.v];
                    if ((!tile.isActive || inf.transparent) &&
                        (tile.wall == 0 || tile.wall == 21) && tile.liquid < 255 && y < groundLevel) //sunlight
                    {
                        tile.light = 1.0;
                        tile.lightR = 1.0;
                        tile.lightG = 1.0;
                        tile.lightB = 1.0;
                    }
                    if (tile.liquid > 0 && tile.isLava) //lava
                    {
                        tile.light = Math.Max(tile.light, (tile.liquid / 255) * 0.38 + 0.1275);
                        // colored lava light's brightness is not affected by its level
                        tile.lightR = Math.Max(tile.lightR, 0.66);
                        tile.lightG = Math.Max(tile.lightG, 0.39);
                        tile.lightB = Math.Max(tile.lightB, 0.13);
                    }
                    tile.light = Math.Max(tile.light, inf.light);
                    tile.lightR = Math.Max(tile.lightR, inf.lightR);
                    tile.lightG = Math.Max(tile.lightG, inf.lightG);
                    tile.lightB = Math.Max(tile.lightB, inf.lightB);
                }
            }
            // spread light
            for (int y = 0; y < tilesHigh; y++)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    serverText.Text = ((int)((float)y * 50.0 / (float)tilesHigh)) + "% - Spreading light";
                }));
                for (int x = 0; x < tilesWide; x++)
                {
                    double delta = 0.04;
                    Tile tile = tiles[x, y];
                    TileInfo inf = tileInfos[tile.type, tile.u, tile.v];
                    if (tile.isActive && !inf.transparent) delta = 0.16;
                    if (y > 0)
                    {
                        if (tiles[x, y - 1].light - delta > tile.light)
                            tile.light = tiles[x, y - 1].light - delta;
                        if (tiles[x, y - 1].lightR - delta > tile.lightR)
                            tile.lightR = tiles[x, y - 1].lightR - delta;
                        if (tiles[x, y - 1].lightG - delta > tile.lightG)
                            tile.lightG = tiles[x, y - 1].lightG - delta;
                        if (tiles[x, y - 1].lightB - delta > tile.lightB)
                            tile.lightB = tiles[x, y - 1].lightB - delta;
                    }
                    if (x > 0)
                    {
                        if (tiles[x - 1, y].light - delta > tile.light)
                            tile.light = tiles[x - 1, y].light - delta;
                        if (tiles[x - 1, y].lightR - delta > tile.lightR)
                            tile.lightR = tiles[x - 1, y].lightR - delta;
                        if (tiles[x - 1, y].lightG - delta > tile.lightG)
                            tile.lightG = tiles[x - 1, y].lightG - delta;
                        if (tiles[x - 1, y].lightB - delta > tile.lightB)
                            tile.lightB = tiles[x - 1, y].lightB - delta;
                    }
                }
            }
            // spread light backwards
            for (int y = tilesHigh - 1; y >= 0; y--)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    serverText.Text = ((int)((float)(tilesHigh-y) * 50.0 / (float)tilesHigh)+50) + "% - Spreading light";
                }));
                for (int x = tilesWide - 1; x >= 0; x--)
                {
                    double delta = 0.04;
                    Tile tile = tiles[x, y];
                    TileInfo inf = tileInfos[tile.type, tile.u, tile.v];
                    if (tile.isActive && !inf.transparent) delta = 0.16;
                    if (y < tilesHigh - 1)
                    {
                        if (tiles[x, y + 1].light - delta > tile.light)
                            tile.light = tiles[x, y + 1].light - delta;
                        if (tiles[x, y + 1].lightR - delta > tile.lightR)
                            tile.lightR = tiles[x, y + 1].lightR - delta;
                        if (tiles[x, y + 1].lightG - delta > tile.lightG)
                            tile.lightG = tiles[x, y + 1].lightG - delta;
                        if (tiles[x, y + 1].lightB - delta > tile.lightB)
                            tile.lightB = tiles[x, y + 1].lightB - delta;
                    }
                    if (x < tilesWide - 1)
                    {
                        if (tiles[x + 1, y].light - delta > tile.light)
                            tile.light = tiles[x + 1, y].light - delta;
                        if (tiles[x + 1, y].lightR - delta > tile.lightR)
                            tile.lightR = tiles[x + 1, y].lightR - delta;
                        if (tiles[x + 1, y].lightG - delta > tile.lightG)
                            tile.lightG = tiles[x + 1, y].lightG - delta;
                        if (tiles[x + 1, y].lightB - delta > tile.lightB)
                            tile.lightB = tiles[x + 1, y].lightB - delta;
                    }
                }
            }
        }

        private void checkVersion()
        {
            Version newVersion = null;
            string url = "";
            XmlTextReader reader = null;
            ThreadStart start = delegate()
            {
                try
                {
                    reader = new XmlTextReader("http://seancode.com/terrafirma/version.xml");
                    reader.MoveToContent();
                    string elementName = "";
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "terrafirma")
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                                elementName = reader.Name;
                            else
                            {
                                if (reader.NodeType == XmlNodeType.Text && reader.HasValue)
                                {
                                    switch (elementName)
                                    {
                                        case "version":
                                            newVersion = new Version(reader.Value);
                                            break;
                                        case "url":
                                            url = reader.Value;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
                Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (newVersion != null && curVersion.CompareTo(newVersion) < 0)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            if (MessageBox.Show(this, "Download the new version?", "New version detected",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                System.Diagnostics.Process.Start(url);
                            }
                        }));
                }
            };
            new Thread(start).Start();
        }
    }
}
