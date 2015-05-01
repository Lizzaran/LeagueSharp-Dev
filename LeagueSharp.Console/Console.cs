#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 console.cs is part of LeagueSharp.Console.

 LeagueSharp.Console is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 LeagueSharp.Console is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with LeagueSharp.Console. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace LeagueSharp.Console
{
    #region

    using System;
    using System.Globalization;
    using Common;
    using Parts;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    public static class Console
    {
        private static Vector2 _offset;
        private static string _output = string.Empty;
        private static int _height;
        private static int _width;
        internal static Menu Menu;

        static Console()
        {
            Menu = new Menu("Console", "Console", true);

            var headerMenu = Menu.AddSubMenu(new Menu("Header", Menu.Name + "Header"));
            headerMenu.AddItem(
                new MenuItem(headerMenu.Name + "BackgroundColor", "Background Color").SetShared().SetValue(SharpDX.Color.DarkSlateGray.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Header.BackgroundColor = args.GetNewValue<Color>().Convert(); };
            headerMenu.AddItem(
                new MenuItem(headerMenu.Name + "ForegroundColor", "Foreground Color").SetShared().SetValue(SharpDX.Color.White.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Header.ForegroundColor = args.GetNewValue<Color>().Convert(); };
            headerMenu.AddItem(new MenuItem(headerMenu.Name + "Height", "Height").SetShared().SetValue(new Slider(25, 15, 50))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Header.Height = args.GetNewValue<Slider>().Value; };
            headerMenu.AddItem(new MenuItem(headerMenu.Name + "FontSize", "Font Size").SetShared().SetValue(new Slider(18, 10, 30))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Header.FontHeight = args.GetNewValue<Slider>().Value; };

            var contentMenu = Menu.AddSubMenu(new Menu("Content", Menu.Name + "Content"));
            contentMenu.AddItem(
                new MenuItem(contentMenu.Name + "BackgroundColor", "Background Color").SetShared().SetValue(SharpDX.Color.Black.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Content.BackgroundColor = args.GetNewValue<Color>().Convert(); };
            contentMenu.AddItem(
                new MenuItem(contentMenu.Name + "ForegroundColor", "Foreground Color").SetShared().SetValue(SharpDX.Color.White.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Content.ForegroundColor = args.GetNewValue<Color>().Convert(); };
            contentMenu.AddItem(new MenuItem(contentMenu.Name + "Padding", "Padding").SetShared().SetValue(new Slider(10, 0, 30))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Content.Padding = args.GetNewValue<Slider>().Value; };
            contentMenu.AddItem(new MenuItem(contentMenu.Name + "FontSize", "Font Size").SetShared().SetValue(new Slider(18, 10, 30))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Content.FontHeight = args.GetNewValue<Slider>().Value; };

            var scrollbarMenu = Menu.AddSubMenu(new Menu("Scrollbar", Menu.Name + "Scrollbar"));
            scrollbarMenu.AddItem(
                new MenuItem(scrollbarMenu.Name + "BackgroundColor", "Background Color").SetShared().SetValue(SharpDX.Color.DarkGray.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Scrollbar.BackgroundColor = args.GetNewValue<Color>().Convert(); };
            scrollbarMenu.AddItem(new MenuItem(scrollbarMenu.Name + "Width", "Width").SetShared().SetValue(new Slider(15, 5, 50))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Scrollbar.Width = args.GetNewValue<Slider>().Value; };
            scrollbarMenu.AddItem(new MenuItem(scrollbarMenu.Name + "Interval", "Interval").SetShared().SetValue(new Slider(5, 1, 25))).ValueChanged
                += delegate(object sender, OnValueChangeEventArgs args) { Scrollbar.Interval = args.GetNewValue<Slider>().Value; };

            var minimizeMenu = Menu.AddSubMenu(new Menu("Minimize", Menu.Name + "Minimize"));
            minimizeMenu.AddItem(
                new MenuItem(minimizeMenu.Name + "BackgroundColor", "Background Color").SetShared().SetValue(SharpDX.Color.DarkSlateGray.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Parts.Minimize.BackgroundColor = args.GetNewValue<Color>().Convert(); };
            minimizeMenu.AddItem(
                new MenuItem(minimizeMenu.Name + "ForegroundColor", "Foreground Color").SetShared().SetValue(SharpDX.Color.White.Convert()))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Parts.Minimize.ForegroundColor = args.GetNewValue<Color>().Convert(); };

            Menu.AddItem(new MenuItem(Menu.Name + "Width", "Width").SetShared().SetValue(new Slider((int) (Drawing.Width/2.5), 100, Drawing.Width)))
                .ValueChanged += delegate(object sender, OnValueChangeEventArgs args) { Width = args.GetNewValue<Slider>().Value; };
            Menu.AddItem(new MenuItem(Menu.Name + "Height", "Height").SetShared()
                .SetValue(new Slider((int) (Drawing.Height/2.5), 100, Drawing.Height))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Height = args.GetNewValue<Slider>().Value; };
            Menu.AddItem(new MenuItem(Menu.Name + "OffsetLeft", "Offset Left").SetShared().SetValue(new Slider(0, 0, Drawing.Width))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    Offset = new Vector2(args.GetNewValue<Slider>().Value, Menu.Item(Menu.Name + "OffsetTop").GetValue<Slider>().Value);
                };
            Menu.AddItem(new MenuItem(Menu.Name + "OffsetTop", "Offset Top").SetShared().SetValue(new Slider(0, 0, Drawing.Height))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    Offset = new Vector2(Menu.Item(Menu.Name + "OffsetLeft").GetValue<Slider>().Value, args.GetNewValue<Slider>().Value);
                };
            Menu.AddItem(new MenuItem(Menu.Name + "Alpha", "Alpha").SetShared().SetValue(new Slider(200, 0, 255))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Alpha = args.GetNewValue<Slider>().Value; };

            Menu.AddItem(new MenuItem(Menu.Name + "Hidden", "Hidden").SetShared().SetValue(false)).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Hidden = args.GetNewValue<bool>(); };

            Menu.AddItem(new MenuItem(Menu.Name + "Minimized", "Minimized").SetShared().SetValue(false)).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Minimized = args.GetNewValue<bool>(); };

            Menu.AddToMainMenu();

            Width = Menu.Item(Menu.Name + "Width").GetValue<Slider>().Value;
            Height = Menu.Item(Menu.Name + "Height").GetValue<Slider>().Value;

            Offset = new Vector2(Menu.Item(Menu.Name + "OffsetLeft").GetValue<Slider>().Value,
                Menu.Item(Menu.Name + "OffsetTop").GetValue<Slider>().Value);

            Alpha = Menu.Item(Menu.Name + "Alpha").GetValue<Slider>().Value;

            Hidden = Menu.Item(Menu.Name + "Hidden").GetValue<bool>();
            Minimized = Menu.Item(Menu.Name + "Minimized").GetValue<bool>();

            Header.BackgroundColor = Menu.Item(headerMenu.Name + "BackgroundColor").GetValue<Color>().Convert();
            Header.ForegroundColor = Menu.Item(headerMenu.Name + "ForegroundColor").GetValue<Color>().Convert();
            Header.Height = Menu.Item(headerMenu.Name + "Height").GetValue<Slider>().Value;
            Header.FontHeight = Menu.Item(headerMenu.Name + "FontSize").GetValue<Slider>().Value;
            Header.Content = "LeagueSharp.Console";

            Content.BackgroundColor = Menu.Item(contentMenu.Name + "BackgroundColor").GetValue<Color>().Convert();
            Content.ForegroundColor = Menu.Item(contentMenu.Name + "ForegroundColor").GetValue<Color>().Convert();
            Content.Padding = Menu.Item(contentMenu.Name + "Padding").GetValue<Slider>().Value;
            Content.FontHeight = Menu.Item(contentMenu.Name + "FontSize").GetValue<Slider>().Value;

            Scrollbar.BackgroundColor = Menu.Item(scrollbarMenu.Name + "BackgroundColor").GetValue<Color>().Convert();
            Scrollbar.Width = Menu.Item(scrollbarMenu.Name + "Width").GetValue<Slider>().Value;
            Scrollbar.Interval = Menu.Item(scrollbarMenu.Name + "Interval").GetValue<Slider>().Value;

            Parts.Minimize.BackgroundColor = Menu.Item(minimizeMenu.Name + "BackgroundColor").GetValue<Color>().Convert();
            Parts.Minimize.ForegroundColor = Menu.Item(minimizeMenu.Name + "ForegroundColor").GetValue<Color>().Convert();

            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            WriteLine("LeagueSharp.Console successfully loaded." + Environment.NewLine);
        }

        internal static bool Minimized { get; set; }
        internal static bool Hidden { get; set; }

        public static Vector2 Offset
        {
            get { return _offset; }
            set
            {
                var x = value.X;
                var y = value.Y;

                if (x + Width/2f > Drawing.Width)
                {
                    x = Drawing.Width - Width/2f;
                }
                else if (x - Width/2f < 0)
                {
                    x = 0 + Width/2f;
                }

                if (y + Height > Drawing.Height)
                {
                    y = Drawing.Height - Height;
                }
                else if (y < 0)
                {
                    y = 0;
                }
                _offset = new Vector2(x, y);
            }
        }

        public static int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                RaiseEvent(OnChange);
            }
        }

        public static int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                RaiseEvent(OnChange);
            }
        }

        internal static string Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public static int Alpha
        {
            set
            {
                Header.Alpha = value;
                Content.Alpha = value;
                Scrollbar.Alpha = value;
                Parts.Minimize.Alpha = value;
            }
        }

        internal static bool IsMoving { get; set; }

        internal static Color Convert(this SharpDX.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        internal static SharpDX.Color Convert(this Color color)
        {
            return new SharpDX.Color(color.R, color.G, color.B, color.A);
        }

        internal static event EventHandler OnChange;
        internal static event EventHandler OnWrite;

        private static void OnDrawingEndScene(EventArgs args)
        {
            if (Hidden)
            {
                return;
            }

            if (Minimized)
            {
                Parts.Minimize.EndScene();
            }
            else
            {
                Header.EndScene();
                Content.EndScene();
                Scrollbar.EndScene();
                Parts.Minimize.EndScene();
            }
        }

        private static void OnDrawingPostReset(EventArgs args)
        {
            Header.PostReset();
            Content.PostReset();
            Scrollbar.PostReset();
            Parts.Minimize.PostReset();
        }

        private static void OnDrawingPreReset(EventArgs args)
        {
            Header.PreReset();
            Content.PreReset();
            Scrollbar.PreReset();
            Parts.Minimize.PreReset();
        }

        public static void Write(string value)
        {
            try
            {
                var scrollDown = Scrollbar.DragTop == Content.Height - Scrollbar.Height;
                Output += value;
                Scrollbar.DragTop = scrollDown ? Content.Height - Scrollbar.Height : Scrollbar.DragTop;
                RaiseEvent(OnWrite);
            }
            catch
            {
            }
        }

        private static void RaiseEvent(EventHandler evt)
        {
            try
            {
                if (evt != null)
                    evt(null, null);
            }
            catch
            {
            }
        }

        public static void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }

        public static void Clear()
        {
            Output = string.Empty;
        }

        public static void Show()
        {
            Hidden = false;
        }

        public static void Hide()
        {
            Hidden = true;
        }

        public static void Minimize()
        {
            Minimized = true;
        }

        public static void Maximize()
        {
            Minimized = false;
        }

        #region Write

        public static void WriteLine()
        {
            WriteLine(string.Empty);
        }

        public static void WriteLine(bool value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(char value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(decimal value)
        {
            WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteLine(double value)
        {
            WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteLine(float value)
        {
            WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(uint value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(long value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(ulong value)
        {
            WriteLine(value.ToString());
        }

        public static void Write(bool value)
        {
            Write(value.ToString());
        }

        public static void Write(char value)
        {
            Write(value.ToString());
        }

        public static void Write(double value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void Write(decimal value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void Write(float value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void Write(int value)
        {
            Write(value.ToString());
        }

        public static void Write(uint value)
        {
            Write(value.ToString());
        }

        public static void Write(long value)
        {
            Write(value.ToString());
        }

        public static void Write(ulong value)
        {
            Write(value.ToString());
        }

        #endregion
    }
}