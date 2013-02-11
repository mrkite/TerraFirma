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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections;

namespace Terrafirma
{
    /// <summary>
    /// Interaction logic for HilightWin.xaml
    /// </summary>
    public partial class HilightWin : Window
    {
        class HTile : IComparable
        {
            private string name;
            private TileInfo info;
            public HTile(string name, TileInfo info)
            {
                this.name = name;
                this.info = info;
            }
            public TileInfo Info
            {
                get { return info; }
            }
            public override string ToString()
            {
                return name;
            }
            int IComparable.CompareTo(object obj)
            {
                HTile h = (HTile)obj;
                int r = String.Compare(this.name, h.name);
                return r;
            }
        }
        ArrayList theTiles;
        public HilightWin(ArrayList tiles)
        {
            InitializeComponent();

            theTiles = new ArrayList();
            foreach (TileInfo info in tiles)
            {
                info.isHilighting = false;
                theTiles.Add(new HTile(info.name,info));
                AddVariants(theTiles, info);
            }
            theTiles.Sort();
            tileList.ItemsSource = theTiles;
        }
        private void AddVariants(ArrayList tiles, TileInfo info)
        {
            foreach (TileInfo v in info.variants)
            {
                if (v.name != info.name)
                {
                    v.isHilighting = false;
                    tiles.Add(new HTile(v.name, v));
                }
                AddVariants(tiles, v);
            }
        }
        public TileInfo SelectedItem {
            get {
                return (tileList.SelectedItem as HTile).Info;
            }
        }

        public List<TileInfo> SelectedItems
        {
            get
            {
                List<TileInfo> tinfo = new List<TileInfo>();
                foreach (var item in tileList.SelectedItems)
                {
                    tinfo.Add((item as HTile).Info);
                }
                return tinfo;
            }
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
        private void hilight_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void tileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            hilightbutton.IsEnabled = true;
        }
    }
}
