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
        public bool isHoney;
        public bool half;
        public int wires;
        public byte slope;
        public bool actuator;
        public bool inActive;
		public bool seen;


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
        public bool hasWire
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
    }
    struct Chest
    {
        public Int32 x, y;
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

                using (BinaryReader b = new BinaryReader(File.Open(worlds[i],FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
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
			string playerpath=Path.Combine(terrariapath,"Players");
			if (Directory.Exists(playerpath))
				players=Directory.GetFiles(playerpath,"*.plr");
			else
			{
				players=new string[0];
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
                if (i==0)
                {
                    player=players[i];
                    item.IsChecked=true;
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
                                tiles[x, y].wires = 0;
								tiles[x, y].half = false;
								tiles[x, y].slope = 0;
                                if (version >= 33)
								{
                                    tiles[x, y].wires = b.ReadBoolean()?1:0;
									if (version >= 43)
									{
										tiles[x, y].wires |= b.ReadBoolean()?2:0;
										tiles[x, y].wires |= b.ReadBoolean()?4:0;
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
											tiles[x, y].actuator = b.ReadBoolean();
											tiles[x, y].inActive = b.ReadBoolean();
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
                                        tiles[x, r].wires = tiles[x, y].wires;
										tiles[x, r].half = tiles[x, y].half;
										tiles[x, r].slope = tiles[x, y].slope;
										tiles[x, r].actuator = tiles[x, y].actuator;
										tiles[x, r].inActive = tiles[x, y].inActive;
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
                                        if (prefix != "")
                                            prefix += " ";
                                        chest.items[ii].name = prefix + name;
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

        private string[] prefixes ={
                                       "",
                                       "Large",         //1
                                       "Massive",       //2
                                       "Dangerous",     //3
                                       "Savage",        //4
                                       "Sharp",         //5
                                       "Pointy",        //6
                                       "Tiny",          //7
                                       "Terrible",      //8
                                       "Small",         //9
                                       "Dull",          //10
                                       "Unhappy",       //11
                                       "Bulky",         //12
                                       "Shameful",      //13
                                       "Heavy",         //14
                                       "Light",         //15
                                       "Sighted",       //16
                                       "Rapid",         //17
                                       "Hasty",         //18
                                       "Intimidating",  //19
                                       "Deadly",        //20
                                       "Staunch",       //21
                                       "Awful",         //22
                                       "Lethargic",     //23
                                       "Awkward",       //24
                                       "Powerful",      //25
                                       "Mystic",        //26
                                       "Adept",         //27
                                       "Masterful",     //28
                                       "Inept",         //29
                                       "Ignorant",      //30
                                       "Deranged",      //31
                                       "Intense",       //32
                                       "Taboo",         //33
                                       "Celestial",     //34
                                       "Furious",       //35
                                       "Keen",          //36
                                       "Superior",      //37
                                       "Forceful",      //38
                                       "Broken",        //39
                                       "Damaged",       //40
                                       "Shoddy",        //41
                                       "Quick",         //42
                                       "Deadly",        //43
                                       "Agile",         //44
                                       "Nimble",        //45
                                       "Murderous",     //46
                                       "Slow",          //47
                                       "Sluggish",      //48
                                       "Lazy",          //49
                                       "Annoying",      //50
                                       "Nasty",         //51
                                       "Manic",         //52
                                       "Hurtful",       //53
                                       "Strong",        //54
                                       "Unpleasant",    //55
                                       "Weak",          //56
                                       "Ruthless",      //57
                                       "Frenzying",     //58
                                       "Godly",         //59
                                       "Demonic",       //60
                                       "Zealous",       //61
                                       "Hard",          //62
                                       "Guarding",      //63
                                       "Armored",       //64
                                       "Warding",       //65
                                       "Arcane",        //66
                                       "Precise",       //67
                                       "Lucky",         //68
                                       "Jagged",        //69
                                       "Spiked",        //70
                                       "Angry",         //71
                                       "Menacing",      //72
                                       "Brisk",         //73
                                       "Fleeting",      //74
                                       "Hasty",         //75
                                       "Quick",         //76
                                       "Wild",          //77
                                       "Rash",          //78
                                       "Intrepid",      //79
                                       "Violent",       //80
                                       "Legendary",     //81
                                       "Unreal",        //82
                                       "Mythical"       //83
                                  };
        private string[] itemNames2 ={
                                         "",                        //0
                                         "Gold Pickaxe",            //-1
                                         "Gold Broadsword",         //-2
                                         "Gold Shortsword",         //-3
                                         "Gold Axe",                //-4
                                         "Gold Hammer",             //-5
                                         "Gold Bow",                //-6
                                         "Silver Pickaxe",          //-7
                                         "Silver Broadsword",       //-8
                                         "Silver Shortsword",       //-9
                                         "Silver Axe",              //-10
                                         "Silver Hammer",           //-11
                                         "Silver Bow",              //-12
                                         "Copper Pickaxe",          //-13
                                         "Copper Broadsword",       //-14
                                         "Copper Shortsword",       //-15
                                         "Copper Axe",              //-16
                                         "Copper Hammer",           //-17
                                         "Copper Bow",              //-18
                                         "Blue Phasesaber",         //-19
                                         "Red Phasesaber",          //-20
                                         "Green Phasesaber",        //-21
                                         "Purple Phasesaber",       //-22
                                         "White Phasesaber",        //-23
                                         "Yellow Phasesaber",       //-24
										 "Tin Pickaxe",             //-25
										 "Tin Broadsword",          //-26
										 "Tin Shortsword",          //-27
										 "Tin Axe",                 //-28
										 "Tin Hammer",              //-29
										 "Tin Bow",                 //-30
										 "Lead Pickaxe",            //-31
										 "Lead Broadsword",         //-32
										 "Lead Shortsword",         //-33
										 "Lead Axe",                //-34
										 "Lead Hammer",             //-35
										 "Lead Bow",                //-36
										 "Tungsten Pickaxe",        //-37
										 "Tungsten Broadsword",     //-38
										 "Tungsten Shortsword",     //-39
										 "Tungsten Axe",            //-40
										 "Tungsten Hammer",         //-41
										 "Tungsten Bow",            //-42
										 "Platinum Pickaxe",        //-43
										 "Platinum Broadsword",     //-44
										 "Platinum Shortsword",     //-45
										 "Platinum Axe",            //-46
										 "Platinum Hammer",         //-47
										 "Platinum Bow"             //-48
                                     };
        private string[] itemNames ={
                                        "",                         //0
                                        "Iron Pickaxe",             //1
                                        "Dirt Block",               //2
                                        "Stone Block",              //3
                                        "Iron Broadsword",          //4
                                        "Mushroom",                 //5
                                        "Iron Shortsword",          //6
                                        "Iron Hammer",              //7
                                        "Torch",                    //8
                                        "Wood",                     //9
                                        "Iron Axe",                 //10
                                        "Iron Ore",                 //11
                                        "Copper Ore",               //12
                                        "Gold Ore",                 //13
                                        "Silver Ore",               //14
                                        "Copper Watch",             //15
                                        "Silver Watch",             //16
                                        "Gold Watch",               //17
                                        "Depth Meter",              //18
                                        "Gold Bar",                 //19
                                        "Copper Bar",               //20
                                        "Silver Bar",               //21
                                        "Iron Bar",                 //22
                                        "Gel",                      //23
                                        "Wooden Sword",             //24
                                        "Wooden Door",              //25
                                        "Stone Wall",               //26
                                        "Acorn",                    //27
                                        "Lesser Healing Potion",    //28
                                        "Life Crystal",             //29
                                        "Dirt Wall",                //30
                                        "Bottle",                   //31
                                        "Wooden Table",             //32
                                        "Furnace",                  //33
                                        "Wooden Chair",             //34
                                        "Iron Anvil",               //35
                                        "Work Bench",               //36
                                        "Goggles",                  //37
                                        "Lens",                     //38
                                        "Wooden Bow",               //39
                                        "Wooden Arrow",             //40
                                        "Flaming Arrow",            //41
                                        "Shuriken",                 //42
                                        "Suspicious Looking Eye",   //43
                                        "Demon Bow",                //44
                                        "War Axe of the Night",     //45
                                        "Light's Bane",             //46
                                        "Unholy Arrow",             //47
                                        "Chest",                    //48
                                        "Band of Regeneration",     //49
                                        "Magic Mirror",             //50
                                        "Jester's Arrow",           //51
                                        "Angel Statue",             //52
                                        "Cloud in a Bottle",        //53
                                        "Heremes Boots",            //54
                                        "Enchanted Boomerang",      //55
                                        "Demonite Ore",             //56
                                        "Demonite Bar",             //57
                                        "Heart",                    //58
                                        "Corrupt Seeds",            //59
                                        "Vile Mushroom",            //60
                                        "Ebonstone Block",          //61
                                        "Grass Seeds",              //62
                                        "Sunflower",                //63
                                        "Vilethorn",                //64
                                        "Starfury",                 //65
                                        "Purification Powder",      //66
                                        "Vile Powder",              //67
                                        "Rotten Chunk",             //68
                                        "Worm Tooth",               //69
                                        "Worm Food",                //70
                                        "Copper Coin",              //71
                                        "Silver Coin",              //72
                                        "Gold Coin",                //73
                                        "Platinum Coin",            //74
                                        "Fallen Star",              //75
                                        "Copper Greaves",           //76
                                        "Iron Greaves",             //77
                                        "Silver Greaves",           //78
                                        "Gold Greaves",             //79
                                        "Copper Chainmail",         //80
                                        "Iron Chainmail",           //81
                                        "Silver Chainmail",         //82
                                        "Gold Chainmail",           //83
                                        "Grappling Hook",           //84
                                        "Chain", 	                //85
                                        "Shadow Scale",             //86
                                        "Piggy Bank",               //87
                                        "Mining Helmet",            //88
                                        "Copper Helmet",            //89
                                        "Iron Helmet",              //90
                                        "Silver Helmet",            //91
                                        "Gold Helmet",              //92
                                        "Wood Wall",                //93
                                        "Wood Platform",            //94
                                        "Flintlock Pistol",         //95
                                        "Musket",                   //96
                                        "Musket Ball",              //97
                                        "Minishark",                //98
                                        "Iron Bow",                 //99
                                        "Shadow Greaves",           //100
                                        "Shadow Scalemail",         //101
                                        "Shadow Helmet",            //102
                                        "Nightmare Pickaxe",        //103
                                        "The Breaker",              //104
                                        "Candle",                   //105
                                        "Copper Chandelier",        //106
                                        "Silver Chandelier",        //107
                                        "Gold Chandelier",          //108
                                        "Mana Crystal",             //109
                                        "Lesser Mana Potion",       //110
                                        "Band of Starpower",        //111
                                        "Flower of Fire",           //112
                                        "Magic Missile",            //113
                                        "Dirt Rod",                 //114
                                        "Shadow Orb",               //115
                                        "Meteorite",                //116
                                        "Meteorite Bar",            //117
                                        "Hook",                     //118
                                        "Flamarang",                //119
                                        "Molten Fury",              //120
                                        "Fiery Greatsword",         //121
                                        "Molten Pickaxe",           //122
                                        "Meteor Helmet",            //123
                                        "Meteor Suit",              //124
                                        "Meteor Leggings",          //125
                                        "Bottled Water",            //126
                                        "Space Gun",                //127
                                        "Rocket Boots",             //128
                                        "Gray Brick",               //129
                                        "Gray Brick Wall",          //130
                                        "Red Brick",                //131
                                        "Red Brick Wall",           //132
                                        "Clay Block",               //133
                                        "Blue Brick",               //134
                                        "Blue Brick Wall",          //135
                                        "Chain Lantern",            //136
                                        "Green Brick",              //137
                                        "Green Brick Wall",         //138
                                        "Pink Brick",               //139
                                        "Pink Brick Wall",          //140
                                        "Gold Brick",               //141
                                        "Gold Brick Wall",          //142
                                        "Silver Brick",             //143
                                        "Silver Brick Wall",        //144
                                        "Copper Brick",             //145
                                        "Copper Brick Wall",        //146
                                        "Spike",                    //147
                                        "Water Candle",             //148
                                        "Book",                     //149
                                        "Cobweb",                   //150
                                        "Necro Helmet",             //151
                                        "Necro Breastplate",        //152
                                        "Necro Greaves",            //153
                                        "Bone",                     //154
                                        "Muramasa",                 //155
                                        "Cobalt Shield",            //156
                                        "Aqua Scepter",             //157
                                        "Lucky Horseshoe",          //158
                                        "Shiny Red Balloon",        //159
                                        "Harpoon",                  //160
                                        "Spiky Ball",               //161
                                        "Ball O' Hurt",             //162
                                        "Blue Moon",                //163
                                        "Handgun",                  //164
                                        "Water Bolt",               //165
                                        "Bomb",                     //166
                                        "Dynamite",                 //167
                                        "Grenade",                  //168
                                        "Sand Block",               //169
                                        "Glass",                    //170
                                        "Sign",                     //171
                                        "Ash Block",                //172
                                        "Obsidian",                 //173
                                        "Hellstone",                //174
                                        "Hellstone Bar",            //175
                                        "Mud Block",                //176
                                        "Amethyst",                 //177
                                        "Topaz",                    //178
                                        "Sapphire",                 //179
                                        "Emerald",                  //180
                                        "Ruby",                     //181
                                        "Diamond",                  //182
                                        "Glowing Mushroom",         //183
                                        "Star",                     //184
                                        "Ivy Whip",                 //185
                                        "Breathing Reed",           //186
                                        "Flipper",                  //187
                                        "Healing Potion",           //188
                                        "Mana Potion",              //189
                                        "Blade of Grass",           //190
                                        "Thorn Chakram",            //191
                                        "Obsidian Brick",           //192
                                        "Obsidian Skull",           //193
                                        "Mushroom Grass Seeds",     //194
                                        "Jungle Grass Seeds",       //195
                                        "Wooden Hammer",            //196
                                        "Star Cannon",              //197
                                        "Blue Phaseblade",          //198
                                        "Red Phaseblade",           //199
                                        "Green Phaseblade",         //200
                                        "Purple Phaseblade",        //201
                                        "White Phaseblade",         //202
                                        "Yellow Phaseblade",        //203
                                        "Meteor Hamaxe",            //204
                                        "Empty Bucket",             //205
                                        "Water Bucket",             //206
                                        "Lava Bucket",              //207
                                        "Jungle Rose",              //208
                                        "Stinger",                  //209
                                        "Vine",                     //210
                                        "Feral Claws",              //211
                                        "Anklet of the Wind",       //212
                                        "Staff of Regrowth",        //213
                                        "Hellstone Brick",          //214
                                        "Whoopie Cushion",          //215
                                        "Shackle",                  //216
                                        "Molten Hamaxe",            //217
                                        "Flamelash",                //218
                                        "Phoenix Blaster",          //219
                                        "Sunfury",                  //220
                                        "Hellforge",                //221
                                        "Clay Pot",                 //222
                                        "Nature's Gift",            //223
                                        "Bed",                      //224
                                        "Silk",                     //225
                                        "Lesser Restoration Potion",//226
                                        "Restoration Potion",       //227
                                        "Jungle Hat",               //228
                                        "Jungle Shirt",             //229
                                        "Jungle Pants",             //230
                                        "Molten Helmet",            //231
                                        "Molten Breastplate",       //232
                                        "Molten Greaves",           //233
                                        "Meteor Shot",              //234
                                        "Sticky Bomb",              //235
                                        "Black Lens",               //236
                                        "Sunglasses",               //237
                                        "Wizard Hat",               //238
                                        "Top Hat",                  //239
                                        "Tuxedo Shirt",             //240
                                        "Tuxedo Pants",             //241
                                        "Summer Hat",               //242
                                        "Bunny Hood",               //243
                                        "Plumber's Hat",            //244
                                        "Plumber's Shirt",          //245
                                        "Plumber's Pants",          //246
                                        "Hero's Hat",               //247
                                        "Hero's Shirt",             //248
                                        "Hero's Pants",             //249
                                        "Fish Bowl",                //250
                                        "Archaeologist's Hat",      //251
                                        "Archaeologist's Jacket",   //252
                                        "Archaeologist's Pants",    //253
                                        "Black Thread",             //254
                                        "Green Thread",             //255
                                        "Ninja Hood",               //256
                                        "Ninja Shirt",              //257
                                        "Ninja Pants",              //258
                                        "Leather",                  //259
                                        "Red Hat",                  //260
                                        "Goldfish",                 //261
                                        "Robe",                     //262
                                        "Robot Hat",                //263
                                        "Gold Crown",               //264
                                        "Hellfire Arrow",           //265
                                        "Sandgun",                  //266
                                        "Guide Voodoo Doll",        //267
                                        "Diving Helmet",            //268
                                        "Familiar Shirt",           //269
                                        "Familiar Pants",           //270
                                        "Familiar Wig",             //271
                                        "Demon Scythe",             //272
                                        "Night's Edge",             //273
                                        "Dark Lance",               //274
                                        "Coral",                    //275
                                        "Cactus",                   //276
                                        "Trident",                  //277
                                        "Silver Bullet",            //278
                                        "Throwing Knife",           //279
                                        "Spear",                    //280
                                        "Blowpipe",                 //281
                                        "Glowstick",                //282
                                        "Seed",                     //283
                                        "Wooden Boomerang",         //284
                                        "Aglet",                    //285
                                        "Sticky Glowstick",         //286
                                        "Poisoned Knife",           //287
                                        "Obsidian Skin Potion",     //288
                                        "Regeneration Potion",      //289
                                        "Swiftness Potion",         //290
                                        "Gills Potion",             //291
                                        "Ironskin Potion",          //292
                                        "Mana Regeneration Potion", //293
                                        "Magic Power Potion",       //294
                                        "Featherfall Potion",       //295
                                        "Spelunker Potion",         //296
                                        "Invisibility Potion",      //297
                                        "Shine Potion",             //298
                                        "Night Owl Potion",         //299
                                        "Battle Potion",            //300
                                        "Thorns Potion",            //301
                                        "Water Walking Potion",     //302
                                        "Archery Potion",           //303
                                        "Hunter Potion",            //304
                                        "Gravitation Potion",       //305
                                        "Gold Chest",               //306
                                        "Daybloom Seeds",           //307
                                        "Moonglow Seeds",           //308
                                        "Blinkroot Seeds",          //309
                                        "Deathweed Seeds",          //310
                                        "Waterleaf Seeds",          //311
                                        "Fireblossom Seeds",        //312
                                        "Daybloom",                 //313
                                        "Moonglow",                 //314
                                        "Blinkroot",                //315
                                        "Deathweed",                //316
                                        "Waterleaf",                //317
                                        "Fireblossom",              //318
                                        "Shark Fin",                //319
                                        "Feather",                  //320
                                        "Tombstone",                //321
                                        "Mime Mask",                //322
                                        "Antlion Mandible",         //323
                                        "Illegal Gun Parts",        //324
                                        "The Doctor's Shirt",       //325
                                        "The Doctor's Pants",       //326
                                        "Golden Key",               //327
                                        "Shadow Chest",             //328
                                        "Shadow Key",               //329
                                        "Obsidian Brick Wall",      //330
                                        "Jungle Spores",            //331
                                        "Loom",                     //332
                                        "Piano",                    //333
                                        "Dresser",                  //334
                                        "Bench",                    //335
                                        "Bathtub",                  //336
                                        "Red Banner",               //337
                                        "Green Banner",             //338
                                        "Blue Banner",              //339
                                        "Yellow Banner",            //340
                                        "Lamp Post",                //341
                                        "Tiki Torch",               //342
                                        "Barrel",                   //343
                                        "Chinese Lantern",          //344
                                        "Cooking Pot",              //345
                                        "Safe",                     //346
                                        "Skull Lantern",            //347
                                        "Trash Can",                //348
                                        "Candelabra",               //349
                                        "Pink Vase",                //350
                                        "Mug",                      //351
                                        "Keg",                      //352
                                        "Ale",                      //353
                                        "Bookcase",                 //354
                                        "Throne",                   //355
                                        "Bowl",                     //356
                                        "Bowl of Soup",             //357
                                        "Toilet",                   //358
                                        "Grandfather Clock",        //359
                                        "Armor Statue",             //360
                                        "Goblin Battle Standard",   //361
                                        "Tattered Cloth",           //362
                                        "Sawmill",                  //363
                                        "Cobalt Ore",               //364
                                        "Mythril Ore",              //365
                                        "Adamantite Ore",           //366
                                        "Pwnhammer",                //367
                                        "Excalibur",                //368
                                        "Hallowed Seeds",           //369
                                        "Ebonsand Block",           //370
                                        "Cobalt Hat",               //371
                                        "Cobalt Helmet",            //372
                                        "Cobalt Mask",              //373
                                        "Cobalt Breastplate",       //374
                                        "Cobalt Leggings",          //375
                                        "Mythril Hood",             //376
                                        "Mythril Helmet",           //377
                                        "Mythril Hat",              //378
                                        "Mythril Chainmail",        //379
                                        "Mythril Greaves",          //380
                                        "Cobalt Bar",               //381
                                        "Mythril Bar",              //382
                                        "Cobalt Chainsaw",          //383
                                        "Mythril Chainsaw",         //384
                                        "Cobalt Drill",             //385
                                        "Mythril Drill",            //386
                                        "Adamantite Chainsaw",      //387
                                        "Adamantite Drill",         //388
                                        "Dao of Pow",               //389
                                        "Mythril Halberd",          //390
                                        "Adamantite Bar",           //391
                                        "Glass Wall",               //392
                                        "Compass",                  //393
                                        "Diving Gear",              //394
                                        "GPS",                      //395
                                        "Obsidian Horseshoe",       //396
                                        "Obsidian Shield",          //397
                                        "Tinkerer's Workshop",      //398
                                        "Cloud in a Balloon",       //399
                                        "Adamantite Headgear",      //400
                                        "Adamantite Helmet",        //401
                                        "Adamantite Mask",          //402
                                        "Adamantite Breastplate",   //403
                                        "Adamantite Leggings",      //404
                                        "Spectre Boots",            //405
                                        "Adamantite Glaive",        //406
                                        "Toolbelt",                 //407
                                        "Pearlsand Block",          //408
                                        "Pearlstone Block",         //409
                                        "Mining Shirt",             //410
                                        "Mining Pants",             //411
                                        "Pearlstone Brick",         //412
                                        "Iridescent Brick",         //413
                                        "Mudstone Brick",           //414
                                        "Cobalt Brick",             //415
                                        "Mythril Brick",            //416
                                        "Pearlstone Brick Wall",    //417
                                        "Iridescent Brick Wall",    //418
                                        "Mudstone Brick Wall",      //419
                                        "Cobalt Brick Wall",        //420
                                        "Mythril Brick Wall",       //421
                                        "Holy Water",               //422
                                        "Unholy Water",             //423
                                        "Silt Block",               //424
                                        "Fairy Bell",               //425
                                        "Breaker Blade",            //426
                                        "Blue Torch",               //427
                                        "Red Torch",                //428
                                        "Green Torch",              //429
                                        "Purple Torch",             //430
                                        "White Torch",              //431
                                        "Yellow Torch",             //432
                                        "Demon Torch",              //433
                                        "Clockwork Assault Rifle",  //434
                                        "Cobalt Repeater",          //435
                                        "Mythril Repeater",         //436
                                        "Dual Hook",                //437
                                        "Star Statue",              //438
                                        "Sword Statue",             //439
                                        "Slime Statue",             //440
                                        "Goblin Statue",            //441
                                        "Shield Statue",            //442
                                        "Bat Statue",               //443
                                        "Fish Statue",              //444
                                        "Bunny Statue",             //445
                                        "Skeleton Statue",          //446
                                        "Reaper Statue",            //447
                                        "Woman Statue",             //448
                                        "Imp Statue",               //449
                                        "Gargoyle Statue",          //450
                                        "Gloom Statue",             //451
                                        "Hornet Statue",            //452
                                        "Bomb Statue",              //453
                                        "Crab Statue",              //454
                                        "Hammer Statue",            //455
                                        "Potion Statue",            //456
                                        "Spear Statue",             //457
                                        "Cross Statue",             //458
                                        "Jellyfish Statue",         //459
                                        "Bow Statue",               //460
                                        "Boomerang Statue",         //461
                                        "Boot Statue",              //462
                                        "Chest Statue",             //463
                                        "Bird Statue",              //464
                                        "Axe Statue",               //465
                                        "Corrupt Statue",           //466
                                        "Tree Statue",              //467
                                        "Anvil Statue",             //468
                                        "Pickaxe Statue",           //469
                                        "Mushroom Statue",          //470
                                        "Eyeball Statue",           //471
                                        "Pillar Statue",            //472
                                        "Heart Statue",             //473
                                        "Pot Statue",               //474
                                        "Sunflower Statue",         //475
                                        "King Statue",              //476
                                        "Queen Statue",             //477
                                        "Pirahna Statue",           //478
                                        "Planked Wall",             //479
                                        "Wooden Beam",              //480
                                        "Adamantite Repeater",      //481
                                        "Adamantite Sword",         //482
                                        "Cobalt Sword",             //483
                                        "Mythril Sword",            //484
                                        "Moon Charm",               //485
                                        "Ruler",                    //486
                                        "Crystal Ball",             //487
                                        "Disco Ball",               //488
                                        "Sorcerer Emblem",          //489
                                        "Ranger Emblem",            //490
                                        "Warrior Emblem",           //491
                                        "Demon Wings",              //492
                                        "Angel Wings",              //493
                                        "Magical Harp",             //494
                                        "Rainbow Rod",              //495
                                        "Ice Rod",                  //496
                                        "Neptune's Shell",          //497
                                        "Mannequin",                //498
                                        "Greater Healing Potion",   //499
                                        "Greater Mana Potion",      //500
                                        "Pixie Dust",               //501
                                        "Crystal Shard",            //502
                                        "Clown Hat",                //503
                                        "Clown Shirt",              //504
                                        "Clown Pants",              //505
                                        "Flamethrower",             //506
                                        "Bell",                     //507
                                        "Harp",                     //508
                                        "Wrench",                   //509
                                        "Wire Cutter",              //510
                                        "Active Stone Block",       //511
                                        "Inactive Stone Block",     //512
                                        "Lever",                    //513
                                        "Laser Rifle",              //514
                                        "Crystal Bullet",           //515
                                        "Holy Arrow",               //516
                                        "Magic Dagger",             //517
                                        "Crystal Storm",            //518
                                        "Cursed Flames",            //519
                                        "Soul of Light",            //520
                                        "Soul of Night",            //521
                                        "Cursed Flame",             //522
                                        "Cursed Torch",             //523
                                        "Adamantite Forge",         //524
                                        "Mythril Anvil",            //525
                                        "Unicorn Horn",             //526
                                        "Dark Shard",               //527
                                        "Light Shard",              //528
                                        "Red Pressure Plate",       //529
                                        "Wire",                     //530
                                        "Spell Tome",               //531
                                        "Star Cloak",               //532
                                        "Megashark",                //533
                                        "Shotgun",                  //534
                                        "Philospher's Stone",       //535
                                        "Titan Glove",              //536
                                        "Cobalt Naginata",          //537
                                        "Switch",                   //538
                                        "Dart Trap",                //539
                                        "Boulder",                  //540
                                        "Green Pressure Plate",     //541
                                        "Gray Pressure Plate",      //542
                                        "Brown Pressure Plate",     //543
                                        "Mechanical Eye",           //544
                                        "Cursed Arrow",             //545
                                        "Cursed Bullet",            //546
                                        "Soul of Fright",           //547
                                        "Soul of Might",            //548
                                        "Soul of Sight",            //549
                                        "Gungnir",                  //550
                                        "Hallowed Plate Mail",      //551
                                        "Hallowed Greaves",         //552
                                        "Hallowed Helmet",          //553
                                        "Hallowed Headgear",        //554
                                        "Hallowed Mask",            //555
                                        "Cross Necklace",           //556
                                        "Mana Flower",              //557
                                        "Mechanical Worm",          //558
                                        "Mechanical Skull",         //559
                                        "Slime Crown",              //560
                                        "Light Disc",               //561
                                        "Music Box (Overworld Day)",//562
                                        "Music Box (Eerie)",        //563
                                        "Music Box (Night)",        //564
                                        "Music Box (Title)",        //565
                                        "Music Box (Underground)",  //566
                                        "Music Box (Boss 1)",       //567
                                        "Music Box (Jungle)",       //568
                                        "Music Box (Corruption)",   //569
                                        "Music Box (Underground Corruption)", //570
                                        "Music Box (The Hallow)",   //571
                                        "Music Box (Boss 2)",       //572
                                        "Music Box (Underground Hallow)", //573
                                        "Music Box (Boss 3)",       //574
                                        "Soul of Flight",           //575
                                        "Music Box",                //576
                                        "Demonite Brick",           //577
                                        "Hallowed Repeater",        //578
                                        "Drax",                     //579
                                        "Explosives",               //580
                                        "Inlet Pump",               //581
                                        "Outlet Pump",              //582
                                        "1 Second Timer",           //583
                                        "3 Second Timer",           //584
                                        "5 Second Timer",           //585
                                        "Candy Cane Block",         //586
                                        "Candy Cane Wall",          //587
                                        "Santa Hat",                //588
                                        "Santa Shirt",              //589
                                        "Santa Pants",              //590
                                        "Green Candy Cane Block",   //591
                                        "Green Candy Cane Wall",    //592
                                        "Snow Block",               //593
                                        "Snow Brick",               //594
                                        "Snow Brick Wall",          //595
                                        "Blue Light",               //596
                                        "Red Light",                //597
                                        "Green Light",              //598
                                        "Blue Present",             //599
                                        "Green Present",            //600
                                        "Yellow Present",           //601
                                        "Snow Globe",               //602
                                        "Carrot",                   //603
										"Adamantite Beam",          //604
										"Adamantite Beam Wall",     //605
										"Demonite Brick Wall",      //606
										"Sandstone Brick",          //607
										"Sandstone Brick Wall",     //608
										"Ebonstone Brick",          //609
										"Ebonstone Brick Wall",     //610
										"Red Stucco",               //611
										"Yellow Stucco",            //612
										"Green Stucco",             //613
										"Gray Stucco",              //614
										"Red Stucco Wall",          //615
										"Yellow Stucco Wall",       //616
										"Green Stucco Wall",        //617
										"Gray Stucco Wall",         //618
										"Ebonwood",                 //619
										"Rich Mahogany",            //620
										"Pearlwood",                //621
										"Ebonwood Wall",            //622
										"Rich Mahogany Wall",       //623
										"Pearlwood Wall",           //624
										"Ebonwood Chest",           //625
										"Rich Mahogany Chest",      //626
										"Pearlwood Chest",          //627
										"Ebonwood Chair",           //628
										"Rich Mahogany Chair",      //629
										"Pearlwood Chair",          //630
										"Ebonwood Platform",        //631
										"Rich Mahogany Platform",   //632
										"Pearlwood Platform",       //633
										"Bone Platform",            //634
										"Ebonwood Work Bench",      //635
										"Rich Mahogany Work Bench", //636
										"Pearlwood Work Bench",     //637
										"Ebonwood Table",           //638
										"Rich Mahogany Table",      //639
										"Pearlwood Table",          //640
										"Ebonwood Piano",           //641
										"Rich Mahogany Piano",      //642
										"Pearlwood Piano",          //643
										"Ebonwood Bed",             //644
										"Rich Mahogany Bed",        //645
										"Pearlwood Bed",            //646
										"Ebonwood Dresser",         //647
										"Rich Mahogany Dresser",    //648
										"Pearlwood Dresser",        //649
										"Ebonwood Door",            //650
										"Rich Mahogany Door",       //651
										"Pearlwood Door",           //652
										"Ebonwood Sword",           //653
										"Ebonwood Hammer",          //654
										"Ebonwood Bow",             //655
										"Rich Mahogany Sword",      //656
										"Rich Mahogany Hammer",     //657
										"Rich Mahogany Bow",        //658
										"Pearlwood Sword",          //659
										"Pearlwood Hammer",         //660
										"Pearlwood Bow",            //661
										"Rainbow Brick",            //662
										"Rainbow Brick Wall",       //663
										"Ice Block",                //664
										"Red's Wings",              //665
										"Red's Helmet",             //666
										"Red's Breastplate",        //667
										"Red's Leggings",           //668
										"Fish",                     //669
										"Ice Boomerang",            //670
										"Keybrand",                 //671
										"Cutlass",                  //672
										"Icemourne",                //673
										"True Excalibur",           //674
										"True Night's Edge",        //675
										"Frostbrand",               //676
										"Scythe",                   //677
										"Soul Scythe",              //678
										"Tactical Shotgun",         //679
										"Ivy Chest",                //680
										"Ice Chest",                //681
										"Marrow",                   //682
										"Unholy Trident",           //683
										"Frost Helmet",             //684
										"Frost Breastplate",        //685
										"Frost Leggings",           //686
										"Tin Helmet",               //687
										"Tin Chainmail",            //688
										"Tin Greaves",              //689
										"Lead Helmet",              //690
										"Lead Chainmail",           //691
										"Lead Greaves",             //692
										"Tungsten Helmet",          //693
										"Tungsten Chainmail",       //694
										"Tungsten Greaves",         //695
										"Platinum Helmet",          //696
										"Platinum Chainmail",       //697
										"Platinum Greaves",         //698
										"Tin Ore",                  //699
										"Lead Ore",                 //700
										"Tungsten Ore",             //701
										"Platinum Ore",             //702
										"Tin Bar",                  //703
										"Lead Bar",                 //704
										"Tungsten Bar",             //705
										"Platinum Bar",             //706
										"Tin Watch",                //707
										"Tungsten Watch",           //708
										"Platinum Watch",           //709
										"Tin Chandelier",           //710
										"Tungsten Chandelier",      //711
										"Platinum Chandelier",      //712
										"Platinum Candle",          //713
										"Platinum Candelabra",      //714
										"Platinum Crown",           //715
										"Lead Anvil",               //716
										"Tin Brick",                //717
										"Tungsten Brick",           //718
										"Platinum Brick",           //719
										"Tin Brick Wall",           //720
										"Tungsten Brick Wall",      //721
										"Platinum Brick Wall",      //722
										"Beam Sword",               //723
										"Ice Blade",                //724
										"Icy Bow",                  //725
										"Frost Staff",              //726
										"Wood Helmet",              //727
										"Wood Breastplate",         //728
										"Wood Greaves",             //729
										"Ebonwood Helmet",          //730
										"Ebonwood Breastplate",     //731
										"Ebonwood Greaves",         //732
										"Rich Mahogany Helmet",     //733
										"Rich Mahogany Breastplate",//734
										"Rich Mahogany Greaves",    //735
										"Pearlwood Helmet",         //736
										"Pearlwood Breastplate",    //737
										"Pearlwood Greaves",        //738
										"Amethyst Staff",           //739
										"Topaz Staff",              //740
										"Sapphire Staff",           //741
										"Emerald Staff",            //742
										"Ruby Staff",               //743
										"Diamond Staff",            //744
										"Grass Wall",               //745
										"Jungle Wall",              //746
										"Flower Wall",              //747
										"Jetpack",                  //748
										"Butterfly Wings",          //749
										"Cactus Wall",              //750
										"Cloud",                    //751
										"Cloud Wall",               //752
										"Seaweed",                  //753
										"Rune Hat",                 //754
										"Rune Robe",                //755
										"Mushroom Spear",           //756
										"Terra Blade",              //757
										"Grenade Launcher",         //758
										"Rocket Launcher",          //759
										"Proximity Mine Launcher",  //760
										"Fairy Wings",              //761
										"Slime Block",              //762
										"Flesh Block",              //763
										"Mushroom Wall",            //764
										"Rain Cloud",               //765
										"Bone Block",               //766
										"Frozen Slime Block",       //767
										"Bone Block Wall",          //768
										"Slime Block Wall",         //769
										"Flesh Block Wall",         //770
										"Rocket I",                 //771
										"Rocket II",                //772
										"Rocket III",               //773
										"Rocket IV",                //774
										"Asphalt Block",            //775
										"Cobalt Pickaxe",           //776
										"Mythril Pickaxe",          //777
										"Adamantite Pickaxe",       //778
										"Clentaminator",            //779
										"Green Solution",           //780
										"Blue Solution",            //781
										"Purple Solution",          //782
										"Dark Blue Solution",       //783
										"Red Solution",             //784
										"Harpy Wings",              //785
										"Bone Wings",               //786
										"Hammush",                  //787
										"Nettle Burst",             //788
										"Ankh Banner",              //789
										"Snake Banner",             //790
										"Omega Banner",             //791
										"Crimson Helmet",           //792
										"Crimson Scalemail",        //793
										"Crimson Greaves",          //794
										"Blood Butcherer",          //795
										"Tendon Bow",               //796
										"Meat Grinder",             //797
										"Deathbringer Pickaxe",     //798
										"Blood Lust Cluster",       //799
										"The Undertaker",           //800
										"The Meatball",             //801
										"The Rotted Fork",          //802
										"Eskimo Hood",              //803
										"Eskimo Coat",              //804
										"Eskimo Pants",             //805
										"Living Wood Chair",        //806
										"Cactus Chair",             //807
										"Bone Chair",               //808
										"Flesh Chair",              //809
										"Mushroom Chair",           //810
										"Bone Work Bench",          //811
										"Cactus Work Bench",        //812
										"Flesh Work Bench",         //813
										"Mushroom Work Bench",      //814
										"Slime Work Bench",         //815
										"Cactus Door",              //816
										"Flesh Door",               //817
										"Mushroom Door",            //818
										"Living Wood Door",         //819
										"Bone Door",                //820
										"Flame Wings",              //821
										"Frozen Wings",             //822
										"Ghost Wings",              //823
										"Sunplate Block",           //824
										"Disc Wall",                //825
										"Skyware Chair",            //826
										"Bone Table",               //827
										"Flesh Table",              //828
										"Living Wood Table",        //829
										"Skyware Table",            //830
										"Living Wood Chest",        //831
										"Living Wood Wand",         //832
										"Purple Ice Block",         //833
										"Pink Ice Block",           //834
										"Red Ice Block",            //835
										"Crimstone",                //836
										"Skyware Door",             //837
										"Skyware Chest",            //838
										"Steampunk Hat",            //839
										"Steampunk Shirt",          //840
										"Steampunk Pants",          //841
										"Bee Hat",                  //842
										"Bee Shirt",                //843
										"Bee Pants",                //844
										"World Banner",             //845
										"Sun Banner",               //846
										"Gravity Banner",           //847
										"Pharaoh's Mask",           //848
										"Actuator",                 //849
										"Blue Wrench",              //850
										"Green Wrench",             //851
										"Blue Pressure Plate",      //852
										"Yellow Pressure Plate",    //853
										"Discount Card",            //854
										"Lucky Coin",               //855
										"Stick Unicorn",            //856
										"Sandstorm in a Bottle",    //857
										"bl",                       //858
										"Beach Ball",               //859
										"Charm of Myths",           //860
										"Moon Shell",               //861
										"Star Veil",                //862
										"Water Walking Boots",      //863
										"Tiara",                    //864
										"Princess Dress",           //865
										"Pharaoh's Robe",           //866
										"Green Cap",                //867
										"Mushroom Cap",             //868
										"Tam O' Shanter",           //869
										"Mummy Mask",               //870
										"Mummy Shirt",              //871
										"Mummy Pants",              //872
										"Cowboy Hat",               //873
										"Cowboy Jacket",            //874
										"Cowboy Pants",             //875
										"Pirate Hat",               //876
										"Pirate Shirt",             //877
										"Pirate Pants",             //878
										"Viking Helmet",            //879
										"Crimtane",                 //880
										"Cactus Sword",             //881
										"Cactus Pickaxe",           //882
										"Ice Brick",                //883
										"Ice Brick Wall",           //884
										"Adhesive Bandage",         //885
										"Armor Polish",             //886
										"Bezoar",                   //887
										"Blindfold",                //888
										"Fast Clock",               //889
										"Megaphone",                //890
										"Nazar",                    //891
										"Vitamins",                 //892
										"Trifold Map",              //893
										"Cactus Helmet",            //894
										"Cactus Breastplate",       //895
										"Cactus Leggings",          //896
										"Power Glove",              //897
										"Lightning Boots",          //898
										"Sun Stone",                //899
										"Moon Stone",               //900
										"Armor Bracing",            //901
										"Medicated Bandage",        //902
										"The Plan",                 //903
										"Countercurse Mantra",      //904
										"Coin Gun",                 //905
										"Lava Charm",               //906
										"Obsidian Water Walking Boots", //907
										"Lava Waders",              //908
										"Pure Water Fountain",      //909
										"Desert Water Fountain",    //910
										"Shadewood",                //911
										"Shadewood Door",           //912
										"Shadewood Platform",       //913
										"Shadewood Chest",          //914
										"Shadewood Chair",          //915
										"Shadewood Work Bench",     //916
										"Shadewood Table",          //917
										"Shadewood Dresser",        //918
										"Shadewood Piano",          //919
										"Shadewood Bed",            //920
										"Shadewood Sword",          //921
										"Shadewood Hammer",         //922
										"Shadewood Bow",            //923
										"Shadewood Helmet",         //924
										"Shadewood Breastplate",    //925
										"Shadewood Greaves",        //926
										"Shadewood Wall",           //927
										"Cannon",                   //928
										"Cannonball",               //929
										"Flare Gun",                //930
										"Flare",                    //931
										"Bone Wand",                //932
										"Leaf Wand",                //933
										"Flying Carpet",            //934
										"Avenger Emblem",           //935
										"Mechanical Glove",         //936
										"Land Mine",                //937
										"Paladin's Shield",         //938
										"Web Slinger",              //939
										"Jungle Water Fountain",    //940
										"Icy Water Fountain",       //941
										"Corrupt Water Fountain",   //942
										"Crimson Water Fountain",   //943
										"Hallowed Water Fountain",  //944
										"Blood Water Fountain",     //945
										"Umbrella",                 //946
										"Chlorophyte Ore",          //947
										"Steampunk Wings",          //948
										"Snowball",                 //949
										"Ice Skates",               //950
										"Snowball Launcher",        //951
										"Web Covered Chest",        //952
										"Climbing Claws",           //953
										"Ancient Iron Helmet",      //954
										"Ancient Gold Helmet",      //955
										"Ancient Shadow Helmet",    //956
										"Ancient Shadow Scalemail", //957
										"Ancient Shadow Greaves",   //958
										"Ancient Necro Helmet",     //959
										"Ancient Cobalt Helmet",    //960
										"Ancient Cobalt Breastplate",//961
										"Anceint Cobalt Leggings",  //962
										"Black Belt",               //963
										"Boomstick",                //964
										"Rope",                     //965
										"Campfire",                 //966
										"Marshmellow",              //967
										"Marshmellow on a Stick",   //968
										"Cooked Marshmellow",       //969
										"Red Rocket",               //970
										"Green Rocket",             //971
										"Blue Rocket",              //972
										"Yellow Rocket",            //973
										"Ice Torch",                //974
										"Shoe Spikes",              //975
										"Tiger Climbing Gear",      //976
										"Tabi",                     //977
										"Pink Eskimo Hood",         //978
										"Pink Eskimo Coat",         //979
										"Pink Eskimo Pants",        //980
										"Pink Thread",              //981
										"Mana Regeneration Band",   //982
										"Sandstorm in a Balloon",   //983
										"Master Ninja Gear",        //984
										"Rope Coil",                //985
										"Blowgun",                  //986
										"Blizzard in a Bottle",     //987
										"Frostburn Arrow",          //988
										"Enchanted Sword",          //989
										"Pickaxe Axe",              //990
										"Cobalt Waraxe",            //991
										"Mythril Waraxe",           //992
										"Adamantite Waraxe",        //993
										"Eater's Bone",             //994
										"Blend-O-Matic",            //995
										"Meat Grinder",             //996
										"Silt Extractinator",       //997
										"Solidifier",               //998
										"Amber",                    //999
										"Confetti Gun",             //1000
										"Chlorophyte Mask",         //1001
										"Chlorophyte Helmet",       //1002
										"Chlorophyte Headgear",     //1003
										"Chlorophyte Plate Mail",   //1004
										"Chlorophyte Greaves",      //1005
										"Chlorophyte Bar",          //1006
										"Red Dye",                  //1007
										"Orange Dye",               //1008
										"Yellow Dye",               //1009
										"Lime Dye",                 //1010
										"Green Dye",                //1011
										"Teal Dye",                 //1012
										"Cyan Dye",                 //1013
										"Sky Blue Dye",             //1014
										"Blue Dye",                 //1015
										"Purple Dye",               //1016
										"Violet Dye",               //1017
										"Pink Dye",                 //1018
										"Red and Black Dye",        //1019
										"Orange and Black Dye",     //1020
										"Yellow and Black Dye",     //1021
										"Lime and Black Dye",       //1022
										"Green and Black Dye",      //1023
										"Teal and Black Dye",       //1024
										"Cyan and Black Dye",       //1025
										"Sky Blue and Black Dye",   //1026
										"Blue and Black Dye",       //1027
										"Purple and Black Dye",     //1028
										"Violet and Black Dye",     //1029
										"Pink and Black Dye",       //1030
										"Flame Dye",                //1031
										"Flame and Black Dye",      //1032
										"Green Flame Dye",          //1033
										"Green Flame and Black Dye",//1034
										"Blue Flame Dye",           //1035
										"Blue Flame and Black Dye", //1036
										"Silver Dye",               //1037
										"Bright Red Dye",           //1038
										"Bright Orange Dye",        //1039
										"Bright Yellow Dye",        //1040
										"Bright Lime Dye",          //1041
										"Bright Green Dye",         //1042
										"Bright Teal Dye",          //1043
										"Bright Cyan Dye",          //1044
										"Bright Sky Blue Dye",      //1045
										"Bright Blue Dye",          //1046
										"Bright Purple Dye",        //1047
										"Bright Violet Dye",        //1048
										"Bright Pink Dye",          //1049
										"Black Dye",                //1050
										"Red and Silver Dye",       //1051
										"Orange and Silver Dye",    //1052
										"Yellow and Silver Dye",    //1053
										"Lime and Silver Dye",      //1054
										"Green and Silver Dye",     //1055
										"Teal and Silver Dye",      //1056
										"Cyan and Silver Dye",      //1057
										"Sky Blue and Silver Dye",  //1058
										"Blue and Silver Dye",      //1059
										"Purple and Silver Dye",    //1060
										"Violet and Silver Dye",    //1061
										"Pink and Silver Dye",      //1062
										"Intense Flame Dye",        //1063
										"Intense Green Flame Dye",  //1064
										"Intense Blue Flame Dye",   //1065
										"Rainbow Dye",              //1066
										"Intense Rainbow Dye",      //1067
										"Yellow Gradient Dye",      //1068
										"Cyan Gradient Dye",        //1069
										"Violet Gradient Dye",      //1070
										"Paintbrush",               //1071
										"Paint Roller",             //1072
										"Red Paint",                //1073
										"Orange Paint",             //1074
										"Yellow Paint",             //1075
										"Lime Paint",               //1076
										"Green Paint",              //1077
										"Teal Paint",               //1078
										"Cyan Paint",               //1079
										"Sky Blue Paint",           //1080
										"Blue Paint",               //1081
										"Purple Paint",             //1082
										"Violet Paint",             //1083
										"Pink Paint",               //1084
										"Deep Red Paint",           //1085
										"Deep Orange Paint",        //1086
										"Deep Yellow Paint",        //1087
										"Deep Lime Paint",          //1088
										"Deep Green Paint",         //1089
										"Deep Teal Paint",          //1090
										"Deep Cyan Paint",          //1091
										"Deep Sky Blue Paint",      //1092
										"Deep Blue Paint",          //1093
										"Deep Purple Paint",        //1094
										"Deep Violet Paint",        //1095
										"Deep Pink Paint",          //1096
										"Black Paint",              //1097
										"White Paint",              //1098
										"Grey Paint",               //1099
										"Paint Scraper",            //1100
										"Lihzahrd Brick",           //1101
										"Lihzahrd Brick Wall",      //1102
										"Slush Block",              //1103
										"Palladium Ore",            //1104
										"Orichalcum Ore",           //1105
										"Titanium Ore",             //1106
										"Teal Mushroom",            //1107
										"Green Mushroom",           //1108
										"Sky Blue Flower",          //1109
										"Yellow Marigold",          //1110
										"Blue Berries",             //1111
										"Lime Kelp",                //1112
										"Pink Prickly Pear",        //1113
										"Orange Bloodroot",         //1114
										"Red Husk",                 //1115
										"Cyan Husk",                //1116
										"Violet Husk",              //1117
										"Purple Mucos",             //1118
										"Black Ink",                //1119
										"Dye Vat",                  //1120
										"Beegun",                   //1121
										"Possessed Hatchet",        //1122
										"Bee Keeper",               //1123
										"Hive",                     //1124
										"Honey Block",              //1125
										"Hive Wall",                //1126
										"Crispy Honey Block",       //1127
										"Honey Bucket",             //1128
										"Hive Wand",                //1129
										"Beenade",                  //1130
										"Gravity Globe",            //1131
										"Honey Comb",               //1132
										"Abeemination",             //1133
										"Bottled Honey",            //1134
										"Rain Hat",                 //1135
										"Rain Coat",                //1136
										"Lihzahrd Door",            //1137
										"Dungeon Door",             //1138
										"Lead Door",                //1139
										"Iron Door",                //1140
										"Temple Key",               //1141
										"Lihzahrd Chest",           //1142
										"Lihzahrd Chair",           //1143
										"Lihzahrd Table",           //1144
										"Lihzahrd Work Bench",      //1145
										"Super Dart Trap",          //1146
										"Flame Trap",               //1147
										"Spiky Ball Trap",          //1148
										"Spear Trap",               //1149
										"Wooden Spike",             //1150
										"Lihzahrd Pressure Plate",  //1151
										"Lihzahrd Statue",          //1152
										"Lihzahrd Watcher Statue",  //1153
										"Lihzahrd Guardian Statue", //1154
										"Wasp Gun",                 //1155
										"Piranha Gun",              //1156
										"Pygmy Staff",              //1157
										"Pygmy Necklace",           //1158
										"Tiki Mask",                //1159
										"Tiki Shirt",               //1160
										"Tiki Pants",               //1161
										"Leaf Wings",               //1162
										"Blizzard in a Balloon",    //1163
										"Bundle of Balloons",       //1164
										"Bat Wings",                //1165
										"Bone Sword",               //1166
										"Hercules Beetle",          //1167
										"Smoke Bomb",               //1168
										"Bone Key",                 //1169
										"Nectar",                   //1170
										"Tiki Totem",               //1171
										"Lizard Egg",               //1172
										"Grave Marker",             //1173
										"Cross Grave Marker",       //1174
										"Headstone",                //1175
										"Gravestone",               //1176
										"Obelisk",                  //1177
										"Leaf Blower",              //1178
										"Chlorophyte Bullet",       //1179
										"Parrot Cracker",           //1180
										"Strange Glowing Mushroom", //1181
										"Seedling",                 //1182
										"Wisp in a Bottle",         //1183
										"Palladium Bar",            //1184
										"Palladium Sword",          //1185
										"Palladium Pike",           //1186
										"Palladium Repeater",       //1187
										"Palladium Pickaxe",        //1188
										"Palladium Drill",          //1189
										"Palladium Chainsaw",       //1190
										"Orichalcum Bar",           //1191
										"Orichalcum Sword",         //1192
										"Orichalcum Halberd",       //1193
										"Orichalcum Repeater",      //1194
										"Orichalcum Pickaxe",       //1195
										"Orichalcum Drill",         //1196
										"Orichalcum Chainsaw",      //1197
										"Titanium Bar",             //1198
										"Titanium Sword",           //1199
										"Titanium Trident",         //1200
										"Titanium Repeater",        //1201
										"Titanium Pickaxe",         //1202
										"Titanium Drill",           //1203
										"Titanium Chainsaw",        //1204
										"Palladium Mask",           //1205
										"Palladium Helmet",         //1206
										"Palladium Headgear",       //1207
										"Palladium Breastplate",    //1208
										"Palladium Leggings",       //1209
										"Orichalcum Mask",          //1210
										"Orichalcum Helmet",        //1211
										"Orichalcum Headgear",      //1212
										"Orichalcum Breastplate",   //1213
										"Orichalcum Leggings",      //1214
										"Titanium Mask",            //1215
										"Titanium Helmet",          //1216
										"Titanium Headgear",        //1217
										"Titanium Breastplate",     //1218
										"Titanium Leggings",        //1219
										"Mythril Anvil",            //1220
										"Orichalcum Forge",         //1221
										"Palladium Waraxe",         //1222
										"Orichalcum Waraxe",        //1223
										"Titanium Waraxe",          //1224
										"Hallowed Bar",             //1225
										"Chlorophyte Claymore",     //1226
										"Chlorophyte Saber",        //1227
										"Chlorophyte Partisan",     //1228
										"Chlorophyte Shotbow",      //1229
										"Chlorophyte Pickaxe",      //1230
										"Chlorophyte Drill",        //1231
										"Chlorophyte Chainsaw",     //1232
										"Chlorophyte Greataxe",     //1233
										"Chlorophyte Warhammer",    //1234
										"Chlorophyte Arrow",        //1235
										"Amethyst Hook",            //1236
										"Topaz Hook",               //1237
										"Sapphire Hook",            //1238
										"Emerald Hook",             //1239
										"Ruby Hook",                //1240
										"Diamond Hook",             //1241
										"Amber Mosquito",           //1242
										"Umbrella Hat",             //1243
										"Nimbus Rod",               //1244
										"Orange Torch",             //1245
										"Crimsand Block",           //1246
										"Bee Cloak",                //1247
										"Eye of the Golem",         //1248
										"Honey Balloon",            //1249
										"Blue Horseshoe Balloon",   //1250
										"White Horseshoe Balloon",  //1251
										"Yellow Horseshoe Balloon", //1252
										"Frozen Turtle Shell",      //1253
										"Sniper Rifle",             //1254
										"Venus Magnum",             //1255
										"Crimson Rod",              //1256
										"Crimtane Bar",             //1257
										"Stynger",                  //1258
										"Flower Pow",               //1259
										"Rainbow Gun",              //1260
										"Stynger Bolt",             //1261
										"Chlorophyte Jackhammer",   //1262
										"Teleporter",               //1263
										"Flower of Frost",          //1264
										"Uzi",                      //1265
										"Magnet Sphere",            //1266
										"Purple Stained Glass",     //1267
										"Yellow Stained Glass",     //1268
										"Blue Stained Glass",       //1269
										"Green Stained Glass",      //1270
										"Red Stained Glass",        //1271
										"Multicolored Stained Glass",//1272
										"Skeletron Hand",           //1273
										"Skull",                    //1274
										"Balla Hat",                //1275
										"Gangsta Hat",              //1276
										"Sailor Hat",               //1277
										"Eye Patch",                //1278
										"Sailor Shirt",             //1279
										"Sailor Pants",             //1280
										"Skeletron Mask",           //1281
										"Amethyst Robe",            //1282
										"Topaz Robe",               //1283
										"Sapphire Robe",            //1284
										"Emerald Robe",             //1285
										"Ruby Robe",                //1286
										"Diamond Robe",             //1287
										"White Tuxedo Shirt",       //1288
										"White Tuxedo Pants",       //1289
										"Panic Necklace",           //1290
										"Heart Fruit",              //1291
										"Lihzahrd Altar",           //1292
										"Lihzahrd Power Cell",      //1293
										"Picksaw",                  //1294
										"Heat Ray",                 //1295
										"Staff of Earth",           //1296
										"Golem Fist",               //1297
										"Water Chest",              //1298
										"Binoculars",               //1299
										"Rifle Scope",              //1300
										"Destroyer Emblem",         //1301
										"High Velocity Bullet",     //1302
										"Jellyfish Necklace",       //1303
										"Zombie Arm",               //1304
										"The Axe",                  //1305
										"Ice Sickle",               //1306
										"Clothier Voodoo Doll",     //1307
										"Poison Staff",             //1308
										"Slime Staff",              //1309
										"Poison Dart",              //1310
										"Eyespring",                //1311
										"Toy Sled",                 //1312
										"Book of Skulls",           //1313
										"KO Cannon",                //1314
										"Pirate Map",               //1315
										"Turtle Helmet",            //1316
										"Turtle Scale Mail",        //1317
										"Turtle Leggings",          //1318
										"Snowball Cannon",          //1319
										"Bone Pickaxe",             //1320
										"Magic Quiver",             //1321
										"Magma Stone",              //1322
										"Lava Rose",                //1323
										"Bananarang",               //1324
										"Chain Knife",              //1325
										"Rod of Discord",           //1326
										"Death Sickle",             //1327
										"Turtle Scale",             //1328
										"Tissue Sample",            //1329
										"Vertebrae",                //1330
										"Bloody Spine",             //1331
										"Ichor",                    //1332
										"Ichor Torch",              //1333
										"Ichor Arrow",              //1334
										"Ichor Bullet",             //1335
										"Golden Shower",            //1336
										"Bunny Cannon",             //1337
										"Explosive Bunny",          //1338
										"Vial of Venom",            //1339
										"Flask of Venom",           //1340
										"Venom Arrow",              //1341
										"Venom Bullet",             //1342
										"Fire Gauntlet",            //1343
										"Cog",                      //1344
										"Confetti",                 //1345
										"Nanites",                  //1346
										"Explosive Powder",         //1347
										"Gold Dust",                //1348
										"Party Bullet",             //1349
										"Nano Bullet",              //1350
										"Exploding Bullet",         //1351
										"Golden Bullet",            //1352
										"Flask of Cursed Flames",   //1353
										"Flask of Fire",            //1354
										"Flask of Gold",            //1355
										"Flask of Ichor",           //1356
										"Flask of Nanites",         //1357
										"Flask of Party",           //1358
										"Flask of Poison",          //1359
										"Eye of Cthulu Trophy",     //1360
										"Eater of Worlds Trophy",   //1361
										"Brain of Cthulu Trophy",   //1362
										"Skeletron Trophy",         //1363
										"Queen Bee Trophy",         //1364
										"Wall of Flesh Trophy",     //1365
										"Destroyer Trophy",         //1366
										"Skeletron Prime Trophy",   //1367
										"Retinazer Trophy",         //1368
										"Spazmatism Trophy",        //1369
										"Plantera Trophy",          //1370
										"Golem Trophy",             //1371
										"Blood Moon Rising",        //1372
										"The Hanged Man",           //1373
										"Glory of the Fire",        //1374
										"Bone Warp",                //1375
										"Wall Skeleton",            //1376
										"Hanging Skeleton",         //1377
										"Blue Slab Wall",           //1378
										"Blue Tiled Wall",          //1379
										"Pink Slab Wall",           //1380
										"Pink Tiled Wall",          //1381
										"Green Slab Wall",          //1382
										"Green Tiled Wall",         //1383
										"Blue Brick Platform",      //1384
										"Pink Brick Platform",      //1385
										"Green Brick Platform",     //1386
										"Metal Shelf",              //1387
										"Brass Shelf",              //1388
										"Wood Shelf",               //1389
										"Brass Lantern",            //1390
										"Caged Lantern",            //1391
										"Carriage Lantern",         //1392
										"Alchemy Lantern",          //1393
										"Diablost Lamp",            //1394
										"Oil Rag Sconse",           //1395
										"Blue Dungeon Chair",       //1396
										"Blue Dungeon Table",       //1397
										"Blue Dungeon Work Bench",  //1398
										"Green Dungeon Chair",      //1399
										"Green Dungeon Table",      //1400
										"Green DUngeon Work Bench", //1401
										"Pink Dungeon Chair",       //1402
										"Pink Dungeon Table",       //1403
										"Pink Dungeon Work Bench",  //1404
										"Blue Dungeon Candle",      //1405
										"Green Dungeon Candle",     //1406
										"Pink Dungeon Candle",      //1407
										"Blue Dungeon Vase",        //1408
										"Green Dungeon Vase",       //1409
										"Pink Dungeon Vase",        //1410
										"Blue Dungeon Door",        //1411
										"Green Dungeon Door",       //1412
										"Pink Dungeon Door",        //1413
										"Blue Dungeon Bookcase",    //1414
										"Green Dungeon Bookcase",   //1415
										"Pink Dungeon Bookcase",    //1416
										"Catacomb",                 //1417
										"Dungeon Shelf",            //1418
										"Skellington J Skellingsworth",//1419
										"The Cursed Man",           //1420
										"The Eye Sees the End",     //1421
										"Something Evil is Watching You",//1422
										"The Twins Have Awoken",    //1423
										"The Screamer",             //1424
										"Goblins Playing Poker",    //1425
										"Dryadisque",               //1426
										"Sunflowers",               //1427
										"Terrarian Gothic",         //1428
										"Beanie",                   //1429
										"Imbuing Station",          //1430
										"Star in a Bottle",         //1431
										"Empty Bullet",             //1432
										"Impact",                   //1433
										"Powered by Birds",         //1434
										"The Destroyer",            //1435
										"The Persistency of Eyes",  //1436
										"Unicorn Crossing the Hallows",//1437
										"Great Wave",               //1438
										"Starry Night",             //1439
										"Guide Picasso",            //1440
										"The Guardian's Gaze",      //1441
										"Father of Someone",        //1442
										"Nurse Lisa",               //1443
										"Shadowgate Staff",         //1444
										"Inferno Fork",             //1445
										"Spectre Staff",            //1446
										"Wooden Fence",             //1447
										"Metal Fence",              //1448
										"Bubble Machine",           //1449
										"Bubble Wand",              //1450
										"Marching Bones Banner",    //1451
										"Necromantic Sign",         //1452
										"Rusted Company Standard",  //1453
										"Ragged Brotherhood Sigil", //1454
										"Molten Legion Flag",       //1455
										"Diabolic Sigil",           //1456
										"Obsidian Platform",        //1457
										"Obsidian Door",            //1458
										"Obsidian Chair",           //1459
										"Obsidian Table",           //1460
										"Obisidan Work Bench",      //1461
										"Obsidian Vase",            //1462
										"Obsidian Bookcase",        //1463
										"Hellbound Banner",         //1464
										"Hell Hammer Banner",       //1465
										"Helltower Banner",         //1466
										"Lost Hopes of Man Banner", //1467
										"Obsidian Watcher Banner",  //1468
										"Lava Erupts Banner",       //1469
										"Blue Dungeon Bed",         //1470
										"Green Dungeon Bed",        //1471
										"Red Dungeon Bed",          //1472
										"Obsidian Bed",             //1473
										"Waldo",                    //1474
										"Darkness",                 //1475
										"Dark Soul Reaper",         //1476
										"Land",                     //1477
										"Trapped Ghost",            //1478
										"Demon's Eye",              //1479
										"Finding Gold",             //1480
										"First Encounter",          //1481
										"Good Morning",             //1482
										"Underground Reward",       //1483
										"Through the Window",       //1484
										"Place Above the Clouds",   //1485
										"Do Not Step on the Grass", //1486
										"Cold Waters in the White Land",//1487
										"Lightless Chasms",         //1488
										"The Land of Deceiving Looks",//1489
										"Daylight",                 //1490
										"Secret of the Sands",      //1491
										"Deadland Comes Alive",     //1492
										"Evil Presence",            //1493
										"Sky Guardian",             //1494
										"American Explosive",       //1495
										"Discover",                 //1496
										"Hand Earth",               //1497
										"Old Miner",                //1498
										"Skelehead",                //1499
										"Facing the Cerebral Mastermind",//1500
										"Lake of Fire",             //1501
										"Trio Super Heroes",        //1502
										"Spectre Hood",             //1503
										"Spectre Robe",             //1504
										"Spectre Pants",            //1505
										"Spectre Pickaxe",          //1506
										"Spectre Hamaxe",           //1507
										"Ectoplasm",                //1508
										"Gothic Chair",             //1509
										"Gothic Table",             //1510
										"Gothic Work Bench",        //1511
										"Gothic Bookcase",          //1512
										"Paladin's Hammer",         //1513
										"SWAT Helmet",              //1514
										"Bee Wings",                //1515
										"Giant Harpey Feather",     //1516
										"Bone Feather",             //1517
										"Fire Feather",             //1518
										"Ice Feather",              //1519
										"Broken Bat Wing",          //1520
										"Tattered Bee Wing",        //1521
										"Large Amethyst",           //1522
										"Large Topaz",              //1523
										"Large Sapphire",           //1524
										"Large Emerald",            //1525
										"Large Ruby",               //1526
										"Large Diamond",            //1527
										"Jungle Chest",             //1528
										"Corruption Chest",         //1529
										"Crimson Chest",            //1530
										"Hallowed Chest",           //1531
										"Frozen Chest",             //1532
										"Jungle Key",               //1533
										"Corruption Key",           //1534
										"Crimson Key",              //1535
										"Hallowed Key",             //1536
										"Frozen Key",               //1537
										"Imp Face",                 //1538
										"Ominous Presence",         //1539
										"Shining Moon",             //1540
										"Living Gore",              //1541
										"Flowing Magma",            //1542
										"Spectre Paintbrush",       //1543
										"Spectre Paint Roller",     //1544
										"Spectre Paint Scraper",    //1545
										"Shroomite Headgear",       //1546
										"Shroomite Mask",           //1547
										"Shroomite Helmet",         //1548
										"Shroomite Breastplate",    //1549
										"Shroomite Leggings",       //1550
										"Autohammer",               //1551
										"Shroomite Bar",            //1552
										"S.D.M.G.",                 //1553
										"Cenx's Tiara",             //1554
										"Cenx's Breastplate",       //1555
										"Cenx's Leggings",          //1556
										"Crowno's Mask",            //1557
										"Crowno's Breastplate",     //1558
										"Crowno's Leggings",        //1559
										"Will's Helmet",            //1560
										"Will's Breastplate",       //1561
										"Will's Leggings",          //1562
										"Jim's Helmet",             //1563
										"Jim's Breastplate",        //1564
										"Jim's Leggings",           //1565
										"Aaron's Helmet",           //1566
										"Aaron's Breastplate",      //1567
										"Aaron's Leggings",         //1568
										"Vampire Knives",           //1569
										"Broken Hero Sword",        //1570
										"Scourge of the Corruptor", //1571
										"Staff of the Frost Hydra", //1572
										"The Creation of the Guide",//1573
										"The Merchant",             //1574
										"Crowno Devours His Lunch", //1575
										"Rare Enchantment",         //1576
										"Glorious Night",           //1577
										"Sweetheart Necklace",      //1578
										"Flurry Boots",             //1579
										"D-Town's Helmet",          //1580
										"D-Town's Breastplate",     //1581
										"D-Town's Leggings",        //1582
										"D-Town's Wings",           //1583
										"Will's Wings",             //1584
										"Crowno's Wings",           //1585
										"Cenx's Wings",             //1586
										"Cenx's Dress",             //1587
										"Cenx's Dress Pants",       //1588
										"Palladium Column",         //1589
										"Palladium Column Wall",    //1590
										"Bubblegum Block",          //1591
										"Bubblegum Block Wall",     //1592
										"Titanstone Block",         //1593
										"Titanstone Block Wall",    //1594
										"Magic Cuffs",              //1595
										"Music Box (Snow)",         //1596
										"Music Box (Space)",        //1597
										"Music Box (Crimson)",      //1598
										"Music Box (Boss 4)",       //1599
										"Music Box (Alt Overworld Day)",//1600
										"Music Box (Rain)",         //1601
										"Music Box (Ice)",          //1602
										"Music Box (Desert)",       //1603
										"Music Box (Ocean)",        //1604
										"Music Box (Dungeon)",      //1605
										"Music Box (Plantera)",     //1606
										"Music Box (Boss 5)",       //1607
										"Music Box (Temple)",       //1608
										"Music Box (Eclipse)",      //1609
										"Music Box (Mushrooms)",    //1610
										"Butterfly Dust",           //1611
										"Ankh Charm",               //1612
										"Ankh Shield",              //1613
                                        "Blue Flare"                //1614
                                   };

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
                            items.Add(String.Format("{0} {1}", c.items[i].stack, c.items[i].name));
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
            e.CanExecute = !busy && Players.IsEnabled;
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
                        tilesWide = BitConverter.ToInt32(messages, payload); payload += 4;
                        tilesHigh = BitConverter.ToInt32(messages, payload); payload += 4;
                        spawnX = BitConverter.ToInt32(messages, payload); payload += 4;
                        spawnY = BitConverter.ToInt32(messages, payload); payload += 4;
                        groundLevel = BitConverter.ToInt32(messages, payload); payload += 4;
                        rockLevel = BitConverter.ToInt32(messages, payload); payload += 4;
                        payload += 4; //skip world id
                        byte flags = messages[payload++];
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
                        meteorSpawned = false;
                        killedFrost = false;
                        killedGoblins = false;
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
                                    tiles[x, y].hasWire = false;
                                }
                            SendMessage(8); //request initial tile data
                        }
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
                            tile.isActive = (flags & 1) == 1;
                            tile.hasWire = (flags & 16) == 16;
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
                                tile.isLava = messages[payload++] == 1;
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
                                tiles[r, y].hasWire = tiles[x, y].hasWire;
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
                        payload += 32; //don't care about velocity, target, ai, or direction
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
                    Buffer.BlockCopy(velocity, 0, writeBuffer, payload, 4);
                    payloadLen += 19;
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
