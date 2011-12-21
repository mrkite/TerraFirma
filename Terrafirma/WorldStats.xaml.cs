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
using System.Collections.ObjectModel;

namespace Terrafirma
{
    /// <summary>
    /// Interaction logic for WorldStats.xaml
    /// </summary>
    public partial class WorldStats : Window
    {
        private ObservableCollection<Stat> stats = new ObservableCollection<Stat>();
        public WorldStats()
        {
            InitializeComponent();
        }
        public ObservableCollection<Stat> Stats
        {
            get { return stats; }
        }
        public void Add(string label, string value)
        {
            stats.Add(new Stat { Label = label, Value = value });
        }
    }
    public class Stat
    {
        public string Label { get; set; }
        public string Value { get; set; }
    };
}
