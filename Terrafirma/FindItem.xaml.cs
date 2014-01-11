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
using System.Timers;

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

        // Event handler when find item button is clicked
        public event FindItemClickedHandler Clicked;

        private Dictionary<string, List<ChestLeaf>> source;
        private bool filtered = false;

        // Typing delay timer for textbox
        private Timer delay;
        private int delayTime = 1000;
        private bool elapsed = false;
        private bool pressed = false;

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
            source.OrderBy(k => k.Key);
            ItemTree.ItemsSource = source.OrderBy(k => k.Key);

            delay = new Timer(delayTime);
            delay.Elapsed += new ElapsedEventHandler(delay_Elapsed);
            
            ItemFilter.Focus();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Don't close this window
            if (Clicked != null) {
                Clicked(SelectedChest);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Clicked = null;
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

        // Filter items
        private void Filter() {
            if (elapsed || !pressed) {
                elapsed = false;
                pressed = false;

                if (String.IsNullOrWhiteSpace(ItemFilter.Text)) {
                    ItemTree.ItemsSource = source.OrderBy(k => k.Key);
                    filtered = false;
                } else {
                    ItemTree.ItemsSource = source.Where((t) => { return t.Key.ToLower().Contains(ItemFilter.Text.ToLower()); });
                    filtered = true;
                }
            }
        }

        private void ItemFilter_TextChanged(object sender, TextChangedEventArgs e) {
            // We need to handle backspace or delete pressed event in this method
            if (Keyboard.IsKeyDown(Key.Back)) {
                OnKeyPressed();
            }
            // Always call filter even when it will not fired actually
            Filter();
            e.Handled = true;
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e) {
            ItemFilter.Text = "";
        }

        private void delay_Elapsed(object sender, ElapsedEventArgs e) {
            delay.Stop();
            elapsed = true;

            // Have to call filter method with invoke call
            this.Dispatcher.Invoke((Action)(() => {
                Filter();
            }));
        }

        private void ItemFilter_KeyDown(object sender, KeyEventArgs e) {
            OnKeyPressed();
        }

        private void OnKeyPressed() {
            if (!delay.Enabled) {
                // Start when not starting
                delay.Start();
            } else {
                // Reset when already started
                delay.Stop();
                delay.Start();
            }

            pressed = true;
        }
    }

    public delegate void FindItemClickedHandler(int selectedChest);
}
