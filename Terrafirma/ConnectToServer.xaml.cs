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
    /// Interaction logic for ConnectToServer.xaml
    /// </summary>
    public partial class ConnectToServer : Window
    {
        public ConnectToServer()
        {
            InitializeComponent();
            serverip.Focus();
        }
        public string ServerIP
        {
            get { return serverip.Text; }
        }
        public int ServerPort
        {
            get { return Convert.ToInt16(serverport.Text); }
        }

        private void button1_Click(object sender, RoutedEventArgs e) //connect
        {
            DialogResult = true;
            this.Close();
        }

        private void button2_Click(object sender, RoutedEventArgs e) //cancel
        {
            DialogResult = false;
            this.Close();
        }
    }
}
