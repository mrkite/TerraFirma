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

namespace Terrafirma
{
    /// <summary>
    /// Interaction logic for SignPopup.xaml
    /// </summary>
    public partial class SignPopup : Popup
    {
        public SignPopup(string text)
        {
            InitializeComponent();
            SignText.Text = text;
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
