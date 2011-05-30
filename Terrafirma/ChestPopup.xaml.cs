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
using System.Windows.Controls.Primitives;
using System.Collections;

namespace Terrafirma
{
    /// <summary>
    /// Interaction logic for ChestPopup.xaml
    /// </summary>
    public partial class ChestPopup : Popup
    {
        public ChestPopup(ArrayList names)
        {
            InitializeComponent();
            ChestList.ItemsSource = names;
        }

        private void Popup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsOpen = false;
        }

        private void Popup_MouseLeave(object sender, MouseEventArgs e)
        {
            IsOpen = false;
        }
    }
}
