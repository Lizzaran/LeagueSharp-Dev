#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Upvote.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.SharpDX;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXLibrary
{
    public class Upvote
    {
        private const int Padding = 5;
        private const int Margin = 8;
        private static readonly List<UpvoteItem> UpvoteItems = new List<UpvoteItem>();
        private static bool _started;

        static Upvote()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnEndScene += OnDrawingEndScene;
            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
        }

        public static MenuItem Initialize(string name, int maxGames)
        {
            var menuItem = new MenuItem(name + "Upvoted", "Upvoted?").SetValue(false);
            try
            {
                if (ObjectManager.Get<Obj_AI_Hero>().Any())
                {
                    return menuItem;
                }
                var firstRun = false;
                if (!menuItem.GetValue<bool>())
                {
                    var count = 1;
                    try
                    {
                        var file = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.upvote", name.ToLower()));
                        if (File.Exists(file))
                        {
                            var content = File.ReadAllText(file);
                            if (int.TryParse(content, out count))
                            {
                                count++;
                                File.WriteAllText(file, count >= maxGames ? "0" : count.ToString());
                            }
                            else
                            {
                                File.WriteAllText(file, count.ToString());
                            }
                        }
                        else
                        {
                            firstRun = true;
                            File.WriteAllText(file, count.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    if (count >= maxGames || firstRun)
                    {
                        UpvoteItems.Add(new UpvoteItem(name));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return menuItem;
        }

        private static void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                foreach (var item in UpvoteItems)
                {
                    item.OnPostReset();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                foreach (var item in UpvoteItems)
                {
                    item.OnPreReset();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (!UpvoteItems.Any())
                {
                    return;
                }
                var offset = 0f;
                var maxWidth = UpvoteItems.Max(u => u.Width);
                foreach (var item in UpvoteItems)
                {
                    item.OnEndScene(offset, maxWidth);
                    offset += item.Height - Margin / 2f;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (!_started && Game.Time > 0)
                {
                    _started = true;
                    Game.OnUpdate -= OnGameUpdate;
                    Drawing.OnEndScene -= OnDrawingEndScene;
                    Drawing.OnPreReset -= OnDrawingPreReset;
                    Drawing.OnPostReset -= OnDrawingPostReset;

                    foreach (var item in UpvoteItems)
                    {
                        item.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal class UpvoteItem
        {
            private readonly Font _font;
            private readonly Line _line;
            private readonly string _text;
            private Rectangle _measured;

            public UpvoteItem(string name)
            {
                _text = string.Format("Please consider to upvote {0} in the Assembly Database.", name);
                _font = new Font(
                    Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = "Calibri",
                        Height = 20,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });
                _measured = _font.MeasureText(null, _text, FontDrawFlags.Center);
                _line = new Line(Drawing.Direct3DDevice) { Width = _measured.Height + (Padding * 2) };
            }

            public float Height
            {
                get { return _measured.Height + Margin + (Padding * 2); }
            }

            public float Width
            {
                get { return _measured.Width + (Padding * 2); }
            }

            public void OnEndScene(float offet, float width = -1)
            {
                if (_started || Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }
                _line.Begin();

                _line.Draw(
                    new[]
                    {
                        new Vector2(Margin, _measured.Height / 2f + Margin + offet),
                        new Vector2(
                            Margin + (width > 0 ? width : (_measured.Width + (Padding * 2))),
                            _measured.Height / 2f + Margin + offet)
                    }, new Color(0, 0, 0, 175));

                _line.End();

                _font.DrawTextCentered(
                    _text, new Vector2(Margin + _measured.Width / 2f + Padding, _measured.Height / 2f + Margin + offet),
                    new Color(255, 255, 255, 175));
            }

            public void OnPreReset()
            {
                if (_font != null && !_font.IsDisposed)
                {
                    _font.OnLostDevice();
                }
                if (_line != null && !_line.IsDisposed)
                {
                    _line.OnLostDevice();
                }
            }

            public void OnPostReset()
            {
                if (_font != null && !_font.IsDisposed)
                {
                    _font.OnResetDevice();
                }
                if (_line != null && !_line.IsDisposed)
                {
                    _line.OnResetDevice();
                }
            }

            public void Dispose()
            {
                if (_font != null && !_font.IsDisposed)
                {
                    _font.Dispose();
                }
                if (_line != null && !_line.IsDisposed)
                {
                    _line.Dispose();
                }
            }
        }
    }
}