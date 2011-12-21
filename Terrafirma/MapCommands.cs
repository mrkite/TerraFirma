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
using System.Windows.Input;

namespace Terrafirma
{
    static class MapCommands
    {
        public static readonly RoutedUICommand OpenWorld = new RoutedUICommand(
            "Open World", "OpenWorld", typeof(MapCommands));
        public static readonly RoutedUICommand JumpToSpawn = new RoutedUICommand(
            "Jump To Spawn", "JumpToSpawn", typeof(MapCommands),
            new InputGestureCollection(new InputGesture[]{ new KeyGesture(Key.F6) }));
        public static readonly RoutedUICommand Lighting = new RoutedUICommand(
            "Lighting", "Lighting", typeof(MapCommands));
        public static readonly RoutedUICommand NoLight = new RoutedUICommand(
            "No Lighting", "NoLight", typeof(MapCommands));
        public static readonly RoutedUICommand NewLight = new RoutedUICommand(
            "Color Lighting", "NewLight", typeof(MapCommands));
        public static readonly RoutedUICommand Hilight = new RoutedUICommand(
            "Hilight Block...", "Hilight", typeof(MapCommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F2) }));
        public static readonly RoutedUICommand StopHilight = new RoutedUICommand(
            "Stop Hilighting", "StopHilight", typeof(MapCommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F3) }));
        public static readonly RoutedUICommand Textures = new RoutedUICommand(
            "Use Textures", "Textures", typeof(MapCommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F1) }));
        public static readonly RoutedUICommand Houses = new RoutedUICommand(
            "Show NPC Houses", "Houses", typeof(MapCommands));
        public static readonly RoutedUICommand Wires = new RoutedUICommand(
            "Show Wires", "Wires", typeof(MapCommands));
        public static readonly RoutedUICommand ConnectToServer = new RoutedUICommand(
            "Connect to Server...", "ConnectToServer", typeof(MapCommands));
        public static readonly RoutedUICommand ShowStats = new RoutedUICommand(
            "World Information...", "ShowStats", typeof(MapCommands));
        public static readonly RoutedUICommand JumpToDungeon = new RoutedUICommand(
            "Jump to Dungeon", "JumpToDungeon", typeof(MapCommands));
    }
}
