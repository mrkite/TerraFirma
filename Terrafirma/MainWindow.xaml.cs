﻿/*
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
        public byte stack;
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
        const int MapVersion = 0x27;
        const int MaxTile = 149;
        const int MaxWall = 31;
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
        List<Chest> chests = new List<Chest>();
        List<Sign> signs = new List<Sign>();
        List<NPC> npcs = new List<NPC>();

        double gameTime;
        bool dayNight,bloodMoon;
        int moonPhase;
        Int32 dungeonX, dungeonY;
        bool killedBoss1, killedBoss2, killedBoss3, killedGoblins, killedClown, killedFrost;
        bool savedTinkerer, savedWizard, savedMechanic;
        bool smashedOrb, meteorSpawned;
        byte shadowOrbCount;
        Int32 altarsSmashed;
        bool hardMode;
        Int32 goblinsDelay, goblinsSize, goblinsType;
        double goblinsX;

        Render render;

        TileInfos tileInfos;
        WallInfo[] wallInfo;
        UInt32 skyColor, earthColor, rockColor, hellColor, lavaColor, waterColor;
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
                                        new FriendlyNPC("Santa Claus", 142, 11, -1)
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
                }
            }

            render = new Render(tileInfos, wallInfo, skyColor, earthColor, rockColor, hellColor, waterColor, lavaColor);
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
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(path, "My Games");
            path = Path.Combine(path, "Terraria");
            path = Path.Combine(path, "Worlds");
            if (Directory.Exists(path))
                worlds = Directory.GetFiles(path, "*.wld");
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
                        uint version = b.ReadUInt32(); //now we care about the version
                        if (version > MapVersion) // new map format
                            throw new Exception("Unsupported map version: "+version);
                        string title = b.ReadString();
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            Title = title;
                        }));
                        b.BaseStream.Seek(20, SeekOrigin.Current); //skip id and bounds
                        tilesHigh = b.ReadInt32();
                        tilesWide = b.ReadInt32();
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
                        killedBoss1 = b.ReadBoolean();
                        killedBoss2 = b.ReadBoolean();
                        killedBoss3 = b.ReadBoolean();
                        savedTinkerer = savedWizard = savedMechanic = killedGoblins = killedClown = killedFrost = false;
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
                                        // torches didn't have extra in older versions.
                                        if (version < 0x1c && tiles[x, y].type == 4)
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
                                }
                                if (version <= 0x19)
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
                                    tiles[x, y].wallu = -1;
                                    tiles[x, y].wallv = -1;
                                }
                                else
                                    tiles[x, y].wall = 0;
                                if (b.ReadBoolean())
                                {
                                    tiles[x, y].liquid = b.ReadByte();
                                    tiles[x, y].isLava = b.ReadBoolean();
                                }
                                else
                                    tiles[x, y].liquid = 0;
                                if (version >= 0x21)
                                    tiles[x, y].hasWire = b.ReadBoolean();
                                else
                                    tiles[x, y].hasWire = false;
                                if (version >= 0x19) //RLE
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
                                        tiles[x, r].hasWire = tiles[x, y].hasWire;
                                    }
                                    y += rle;
                                }
                            }
                        }
                        chests.Clear();
                        for (int i = 0; i < 1000; i++)
                        {
                            if (b.ReadBoolean())
                            {
                                Chest chest = new Chest();
                                chest.items = new ChestItem[20];
                                chest.x = b.ReadInt32();
                                chest.y = b.ReadInt32();
                                for (int ii = 0; ii < 20; ii++)
                                {
                                    chest.items[ii].stack = b.ReadByte();
                                    if (chest.items[ii].stack > 0)
                                    {
                                        string name="Unknown";
                                        if (version >= 0x26) //item names not stored
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
                                        if (version >= 0x24) //item prefixes
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
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                        {
                            render.SetWorld(tilesWide, tilesHigh, groundLevel, rockLevel, npcs);
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
                                       "",              //34
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
                                         "Yellow Phasesaber"        //-24
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
                                        "Siler Chainmail",          //82
                                        "Gold Chainmail",           //83
                                        "Grappling Hook",           //84
                                        "Iron Chain",               //85
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
                                        "Orb of Light",             //115
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
                                        "Black Dye",                //254
                                        "Green Dye",                //255
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
                                        "Hamdrax",                  //579
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
                                        "Carrot"                    //603
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
                    UseTextures.IsChecked && curScale > 2.0, ShowHouses.IsChecked, ShowWires.IsChecked, ref tiles);
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
                        label = tiles[sx, sy].isLava ? "Lava" : "Water";
                    if (tiles[sx, sy].isActive)
                        label = tileInfos[tiles[sx, sy].type, tiles[sx, sy].u, tiles[sx, sy].v].name;
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
                Lighting1.IsChecked = false;
                Lighting2.IsChecked = false;
            }
            else if (e.Command == MapCommands.Lighting)
            {
                Lighting0.IsChecked = false;
                Lighting2.IsChecked = false;
            }
            else
            {
                Lighting0.IsChecked = false;
                Lighting1.IsChecked = false;
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
                                render.SetWorld(tilesWide, tilesHigh, groundLevel, rockLevel, npcs);
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
                            socket.Close();
                            busy = false;
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                            {
                                MessageBox.Show("Will not map remote large worlds\nother than the spawn point");
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
                        saveOpts.UseTextures && curScale > 2.0, ShowHouses.IsChecked, ShowWires.IsChecked, ref tiles);

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
            stats.Add("Eye of Cthulu", killedBoss1 ? "Defeated" : "Undefeated");
            stats.Add("Eater of Worlds", killedBoss2 ? "Defeated" : "Undefeated");
            stats.Add("Skeletron", killedBoss3 ? "Defeated" : "Undefeated");
            stats.Add("Wall of Flesh", hardMode ? "Defeated" : "Undefeated");
            stats.Add("Goblin Invasion", killedGoblins ? "Destroyed" : goblinsDelay == 0 ? "Ongoing" : "In " + goblinsDelay);
            stats.Add("Clown", killedClown ? "Dead" : "Nope!");
            stats.Add("Frost Horde", killedFrost ? "Destroyed" : "Unsummoned");
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
