using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Terrafirma
{
    static class MapCommands
    {
        public static readonly RoutedUICommand OpenWorld = new RoutedUICommand(
            "Open World", "OpenWorld", typeof(MapCommands));
        public static readonly RoutedUICommand JumpToSpawn = new RoutedUICommand(
            "Jump To Spawn", "JumpToSpawn", typeof(MapCommands),
            new InputGestureCollection(new InputGesture[]{ new KeyGesture(Key.F5) }));
        public static readonly RoutedUICommand Lighting = new RoutedUICommand(
            "Lighting", "Lighting", typeof(MapCommands));
    }
}
