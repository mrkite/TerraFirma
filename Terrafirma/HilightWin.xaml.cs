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
            private int num;
            public HTile(string name, int num)
            {
                this.name = name;
                this.num = num;
            }
            public int Num
            {
                get { return num; }
            }
            public override string ToString()
            {
                return name;
            }
            int IComparable.CompareTo(object obj)
            {
                HTile h = (HTile)obj;
                int r = String.Compare(this.name, h.name);
                if (r == 0)
                {
                    if (this.num < h.num) r = -1;
                    else if (this.num > h.num) r = 1;
                }
                return r;
            }
        }
        ArrayList theTiles;
        public HilightWin(ArrayList tiles)
        {
            InitializeComponent();

            theTiles = new ArrayList();
            int i=0;
            foreach (string name in tiles)
            {
                theTiles.Add(new HTile(name,i++));
            }
            theTiles.Sort();
            tileList.ItemsSource = theTiles;
        }
        public int SelectedItem {
            get {
                return (tileList.SelectedItem as HTile).Num;
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
