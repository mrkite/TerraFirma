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

namespace Terrafirma
{
    struct TileInfo
    {
        public string name;
        public UInt32 color;
        public bool hasExtra;
        public double light;
        public bool transparent;
        public bool isStone, isGrass;
        public Int16 blend;
    }
    struct WallInfo
    {
        public string name;
        public UInt32 color;
    }
    struct Tile
    {
        public bool isActive;
        public byte type;
        public byte wall;
        public byte liquid;
        public bool isLava;
        public Int16 u, v, wallu, wallv;
        public double light;
        public bool hasWire;
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
    struct NPC
    {
        public string name;
        public float x, y;
        public bool isHomeless;
        public Int32 homeX, homeY;
        public int sprite;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const UInt32 MapVersion=9;
        const double MaxScale = 16.0;
        const double MinScale = 1.0;

        double curX, curY, curScale;
        byte[] bits;
        WriteableBitmap mapbits;
        DispatcherTimer resizeTimer;
        int curWidth, curHeight, newWidth, newHeight;
        bool loaded = false;
        Tile[] tiles;
        Int32 tilesWide, tilesHigh;
        Int32 spawnX, spawnY;
        Int32 groundLevel,rockLevel;
        string[] worlds;
        string currentWorld;
        List<Chest> chests = new List<Chest>();
        List<Sign> signs = new List<Sign>();
        List<NPC> npcs = new List<NPC>();
        Render render;

        TileInfo[] tileInfo;
        WallInfo[] wallInfo;
        UInt32 skyColor, earthColor, rockColor, hellColor, lavaColor, waterColor;
        byte hilight=0;
        bool isHilight = false;

        public MainWindow()
        {
            InitializeComponent();

            fetchWorlds();

           

            XmlDocument xml=new XmlDocument();
            string xmlData = string.Empty;
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("Terrafirma.tiles.xml"))
            {
                xml.Load(stream);
            }
            XmlNodeList tileList=xml.GetElementsByTagName("tile");
            tileInfo = new TileInfo[tileList.Count];
            for (int i = 0; i < tileList.Count; i++)
            {
                int id = Convert.ToInt32(tileList[i].Attributes["num"].Value);
                tileInfo[id].name = tileList[i].Attributes["name"].Value;
                tileInfo[id].color = parseColor(tileList[i].Attributes["color"].Value);
                tileInfo[id].hasExtra = tileList[i].Attributes["hasExtra"] != null;
                if (tileList[i].Attributes["light"] != null)
                    tileInfo[id].light = Double.Parse(tileList[i].Attributes["light"].Value,System.Globalization.CultureInfo.InvariantCulture);
                else
                    tileInfo[id].light = 0.0;
                tileInfo[id].transparent = tileList[i].Attributes["letLight"] != null;
                tileInfo[id].isStone = tileList[i].Attributes["isStone"] != null;
                tileInfo[id].isGrass = tileList[i].Attributes["isGrass"] != null;
                if (tileList[i].Attributes["blend"] != null)
                    tileInfo[id].blend = Int16.Parse(tileList[i].Attributes["blend"].Value, System.Globalization.CultureInfo.InvariantCulture);
                else
                    tileInfo[id].blend = -1;
            }
            XmlNodeList wallList = xml.GetElementsByTagName("wall");
            wallInfo = new WallInfo[wallList.Count+1];
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
                UInt32 color=parseColor(globalList[i].Attributes["color"].Value);
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

            render = new Render(tileInfo,wallInfo,skyColor,earthColor,rockColor,hellColor,waterColor,lavaColor);
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
                        var rect=new Int32Rect(0,0,curWidth,curHeight);
                        for (int i=0;i<curWidth*curHeight*4;i++)
                            bits[i]=0xff;
                        mapbits.WritePixels(rect,bits,curWidth*4,0);
                    }
                },
                Dispatcher) {IsEnabled=false};
            curWidth = 496;
            curHeight = 400;
            newWidth = 496;
            newHeight = 400;
            mapbits = new WriteableBitmap(curWidth, curHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            Map.Source = mapbits;
            bits = new byte[curWidth * curHeight * 4];
            curX = curY = 0;
            curScale = 1.0;
        }
        private void fetchWorlds()
        {
            string path=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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

                using (BinaryReader b = new BinaryReader(File.OpenRead(worlds[i])))
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

        private void Load(string world)
        {
            currentWorld = world;
            using (BinaryReader b = new BinaryReader(File.OpenRead(world)))
            {
                uint version = b.ReadUInt32(); //now we care about the version
                Title = b.ReadString();
                b.BaseStream.Seek(20, SeekOrigin.Current); //skip id and bounds
                tilesHigh = b.ReadInt32();
                tilesWide = b.ReadInt32();
                spawnX = b.ReadInt32();
                spawnY = b.ReadInt32();
                groundLevel = (int)b.ReadDouble();
                rockLevel = (int)b.ReadDouble();
                int flaglen = 48;
                if (version >= 0x17)
                    flaglen += 5;
                if (version >= 0x1d)
                    flaglen += 3;
                if (version >= 0x20)
                    flaglen++;
                if (version >= 0x22)
                    flaglen++;
                b.BaseStream.Seek(flaglen, SeekOrigin.Current); //skip flags and other settings
                tiles = new Tile[tilesWide * tilesHigh];
                for (int i = 0; i < tilesWide * tilesHigh; i++)
                {
                    tiles[i].isActive = b.ReadBoolean();
                    if (tiles[i].isActive)
                    {
                        tiles[i].type = b.ReadByte();
                        if (tiles[i].type == 0x7f)
                            tiles[i].isActive = false;
                        if (tileInfo[tiles[i].type].hasExtra)
                        {
                            // torches didn't have extra in older versions.
                            if (version < 0x1c && tiles[i].type == 4)
                            {
                                tiles[i].u = 0;
                                tiles[i].v = 0;
                            }
                            else
                            {
                                tiles[i].u = b.ReadInt16();
                                tiles[i].v = b.ReadInt16();
                                if (tiles[i].type == 128) //armor stand
                                    tiles[i].u %=100;
                                if (tiles[i].type == 144) //timer
                                    tiles[i].v = 0;
                            }
                        }
                        else
                        {
                            tiles[i].u = -1;
                            tiles[i].v = -1;
                        }
                    }
                    if (version <= 0x19)
                        b.ReadBoolean(); //skip obsolete hasLight
                    if (b.ReadBoolean())
                    {
                        tiles[i].wall = b.ReadByte();
                        tiles[i].wallu = -1;
                        tiles[i].wallv = -1;
                    }
                    else
                        tiles[i].wall = 0;
                    if (b.ReadBoolean())
                    {
                        tiles[i].liquid = b.ReadByte();
                        tiles[i].isLava = b.ReadBoolean();
                    }
                    else
                        tiles[i].liquid = 0;
                    if (version >= 0x21)
                        tiles[i].hasWire = b.ReadBoolean();
                    else
                        tiles[i].hasWire = false;
                    if (version >= 0x19) //RLE
                    {
                        int rle = b.ReadInt16();
                        for (int r = 0; r < rle; r++)
                            tiles[i + r + 1] = tiles[i];
                        i += rle;
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
                                chest.items[ii].name = b.ReadString();
                                if (version >= 0x24) //item prefixes
                                    b.ReadByte(); //toss prefix
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
                NPCs.Items.Clear();
                while (b.ReadBoolean())
                {
                    NPC npc = new NPC();
                    npc.name = b.ReadString();
                    npc.x = b.ReadSingle();
                    npc.y = b.ReadSingle();
                    npc.isHomeless = b.ReadBoolean();
                    npc.homeX = b.ReadInt32();
                    npc.homeY = b.ReadInt32();

                    npc.sprite = 0;
                    if (npc.name == "Merchant") npc.sprite = 17;
                    if (npc.name == "Nurse") npc.sprite = 18;
                    if (npc.name == "Arms Dealer") npc.sprite = 19;
                    if (npc.name == "Dryad") npc.sprite = 20;
                    if (npc.name == "Guide") npc.sprite = 22;
                    if (npc.name == "Old Man") npc.sprite = 37;
                    if (npc.name == "Demolitionist") npc.sprite = 38;
                    if (npc.name == "Clothier") npc.sprite = 54;
                    if (npc.name == "Goblin Tinkerer") npc.sprite = 107;
                    if (npc.name == "Wizard") npc.sprite = 108;
                    if (npc.name == "Mechanic") npc.sprite = 124;
                    
                    npcs.Add(npc);

                    if (!npc.isHomeless)
                    {
                        MenuItem item = new MenuItem();
                        item.Header = String.Format("Jump to {0}'s Home", npc.name);
                        item.Click += new RoutedEventHandler(jumpNPC);
                        item.Tag = npc;
                        NPCs.Items.Add(item);
                        NPCs.IsEnabled = true;
                    }
                    else
                    {
                        MenuItem item = new MenuItem();
                        item.Header = String.Format("Jump to {0}'s Location", npc.name);
                        item.Click += new RoutedEventHandler(jumpNPC);
                        item.Tag = npc;
                        NPCs.Items.Add(item);
                        NPCs.IsEnabled = true;
                    }
                }
                // if (version>=0x1f) read the names of the following npcs:
                // merchant, nurse, arms dealer, dryad, guide, clothier, demolitionist,
                // tinkerer and wizard
                // if (version>=0x23) read the name of the mechanic
            }

            calculateLight();

            render.SetWorld(tiles, tilesWide, tilesHigh, groundLevel, rockLevel,npcs);
            //load info
            loaded = true;
        }

        void jumpNPC(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            NPC npc = (NPC)item.Tag;
            if (npc.isHomeless)
            {
                curX = npc.x/16;
                curY = npc.y/16;
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
                render.Draw(curWidth, curHeight, startx, starty, curScale, bits,
                    isHilight, hilight, Lighting.IsChecked,
                    UseTextures.IsChecked && curScale > 2.0);
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
                if (sx >= 0 && sx < tilesWide && sy >= 0 && sy < tilesHigh)
                {
                    int offset = sy + sx * tilesHigh;
                    string label = "Nothing";
                    if (tiles[offset].wall > 0)
                        label = wallInfo[tiles[offset].wall].name;
                    if (tiles[offset].liquid > 0)
                        label = tiles[offset].isLava ? "Lava" : "Water";
                    if (tiles[offset].isActive)
                        label = tileInfo[tiles[offset].type].name;
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
        private SignPopup signPop=null;
        private ChestPopup chestPop=null;

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
                Loading load = new Loading();
                load.Show();
                Load(dlg.FileName);
                curX = spawnX;
                curY = spawnY;
                if (render.Textures.Valid)
                {
                    UseTextures.IsChecked = true;
                    curScale = 16.0;
                }
                RenderMap();
                load.Close();
            }
        }
        private void OpenWorld(object sender, ExecutedRoutedEventArgs e)
        {
            int id = (int)e.Parameter;
            Loading load = new Loading();
            load.Show();
            Load(worlds[id]);
            curX = spawnX;
            curY = spawnY;
            if (render.Textures.Valid)
            {
                UseTextures.IsChecked = true;
                curScale = 16.0;
            }
            RenderMap();
            load.Close();
        }
        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void OpenWorld_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
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
            RenderMap();
        }
        private void Texture_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!render.Textures.Valid)
                return;
            if (UseTextures.IsChecked)
                UseTextures.IsChecked = false;
            else
                UseTextures.IsChecked = true;
            RenderMap();
        }

        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Loading load = new Loading();
            load.Show();
            Load(currentWorld);
            RenderMap();
            load.Close();
        }

        private void Hilight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ArrayList items = new ArrayList();
            for (int i = 0; i < tileInfo.Length; i++)
                items.Add(tileInfo[i].name);
            HilightWin h = new HilightWin(items);
            if (h.ShowDialog() == true)
            {
                hilight = (byte)(h.SelectedItem);
                isHilight = true;
                RenderMap();
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
                saveOpts.CanUseTexture = render.Textures.Valid;
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
                        pixels, false, 0, Lighting.IsChecked,
                        saveOpts.UseTextures && curScale > 2.0);

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

        private void initWindow(object sender, EventArgs e)
        {
            checkVersion();

            HwndSource hwnd = HwndSource.FromVisual(Map) as HwndSource;

            render.Textures = new Textures(hwnd.Handle);

            if (!render.Textures.Valid) //couldn't find textures?
                UseTextures.IsEnabled = false;
        }

        private void calculateLight()
        {
            // turn off all light
            for (int i = 0; i < tilesWide * tilesHigh; i++)
                tiles[i].light = 0.0;
            // light up light sources
            for (int y = 0; y < tilesHigh; y++)
            {
                for (int x = 0; x < tilesWide; x++)
                {
                    int offset = x * tilesHigh + y;
                    if ((!tiles[offset].isActive || tileInfo[tiles[offset].type].transparent) &&
                        tiles[offset].wall == 0 && tiles[offset].liquid < 255 && y < groundLevel) //sunlight
                        tiles[offset].light = 1.0;
                    if (tiles[offset].liquid > 0 && tiles[offset].isLava) //lava
                        tiles[offset].light = Math.Max(tiles[offset].light, tiles[offset].liquid / 255);
                    if (tiles[offset].type == 61 && tiles[offset].u == 144) //special case jungle light ball
                        tiles[offset].light = Math.Max(tiles[offset].light, 0.75);
                    tiles[offset].light = Math.Max(tiles[offset].light, tileInfo[tiles[offset].type].light);
                }
            }
            // spread light
            for (int y = 0; y < tilesHigh; y++)
            {
                for (int x = 0; x < tilesWide; x++)
                {
                    int offset = x * tilesHigh + y;
                    double delta = 0.04;
                    if (tiles[offset].isActive && !tileInfo[tiles[offset].type].transparent) delta = 0.16;
                    if (y > 0 && tiles[offset - 1].light - delta > tiles[offset].light)
                        tiles[offset].light = tiles[offset - 1].light - delta;
                    if (x > 0 && tiles[offset - tilesHigh].light - delta > tiles[offset].light)
                        tiles[offset].light = tiles[offset - tilesHigh].light - delta;
                }
            }
            // spread light backwards
            for (int y = tilesHigh - 1; y >= 0; y--)
            {
                for (int x = tilesWide - 1; x >= 0; x--)
                {
                    int offset = x * tilesHigh + y;
                    double delta = 0.04;
                    if (tiles[offset].isActive && !tileInfo[tiles[offset].type].transparent) delta = 0.16;
                    if (y < tilesHigh - 1 && tiles[offset + 1].light - delta > tiles[offset].light)
                        tiles[offset].light = tiles[offset + 1].light - delta;
                    if (x < tilesWide - 1 && tiles[offset + tilesHigh].light - delta > tiles[offset].light)
                        tiles[offset].light = tiles[offset + tilesHigh].light - delta;
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
