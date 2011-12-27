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
    /// Interaction logic for ServerPassword.xaml
    /// </summary>
    public partial class ServerPassword : Window
    {
        public ServerPassword()
        {
            InitializeComponent();
            password.Focus();
        }
        public string Password
        {
            get { return password.Password; }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
