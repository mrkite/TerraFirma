/*
Copyright (c) 2013, Sean Kasun
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

namespace Terrafirma
{
    /// <summary>
    /// Interaction logic for FindItem.xaml
    /// </summary>
    public partial class FindItem : Window
    {
        class ChestLeaf
        {
            private string name;
            private int id;
            public ChestLeaf(string name,int id)
            {
                this.name = name;
                this.id = id;
            }
            public override string ToString()
            {
                return name;
            }
            public int Id
            {
                get { return id; }
            }
        }

        Dictionary<string, List<ChestLeaf>> source;
        public FindItem(Dictionary<string,List<int>> items)
        {
            InitializeComponent();
            source = new Dictionary<string, List<ChestLeaf>>();
            foreach (string key in items.Keys)
            {
                source[key] = new List<ChestLeaf>();
                int num=1;
                foreach (int id in items[key])
                {
                    source[key].Add(new ChestLeaf("Chest #" + num, id));
                    num++;
                }
            }
            ItemTree.ItemsSource = source.OrderBy(k => k.Key);
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void ItemTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            FindButton.IsEnabled = true;
        }
        public int SelectedChest
        {
            get
            {
                if (ItemTree.SelectedItem.GetType().Equals(typeof(ChestLeaf)))
                {
                    ChestLeaf c = (ChestLeaf)ItemTree.SelectedItem;
                    return c.Id;
                }
                return ((KeyValuePair<string, List<ChestLeaf>>)ItemTree.SelectedItem).Value.First().Id;
            }
        }
    }
}
