#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Upvote.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.IO;
using System.Linq;
using LeagueSharp;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility
{
    internal class Upvote
    {
        private const int Padding = 5;
        private const int Margin = 8;
        private static string _text;
        private static Font _font;
        private static Line _line;
        private static Rectangle _measured;
        private static bool _started;

        public static void Initialize(string name, int maxGames)
        {
            try
            {
                if (ObjectManager.Get<Obj_AI_Hero>().Any())
                {
                    return;
                }
                var error = false;
                var count = 1;
                try
                {
                    var file = Path.Combine(Global.BaseDir, string.Format("{0}.upvote", name.ToLower()));
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
                            error = true;
                        }
                    }
                    else
                    {
                        File.WriteAllText(file, count.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                    error = true;
                }

                _text = string.Format("Please consider to upvote {0} in the Assembly Database.", name);

                var day = DateTime.Now.DayOfWeek;
                if ((error && day != DayOfWeek.Wednesday) || (!error && count < maxGames))
                {
                    return;
                }

                _font = new Font(
                    Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = 20,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });
                _measured = _font.MeasureText(null, _text, FontDrawFlags.Center);
                _line = new Line(Drawing.Direct3DDevice) { Width = _measured.Height + (Padding * 2) };

                Game.OnUpdate += OnGameUpdate;
                Drawing.OnEndScene += OnDrawingEndScene;
                Drawing.OnPreReset -= OnDrawingPreReset;
                Drawing.OnPostReset -= OnDrawingPostReset;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingPostReset(EventArgs args)
        {
            try
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingPreReset(EventArgs args)
        {
            try
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (_started || Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }
                _line.Begin();

                _line.Draw(
                    new[]
                    {
                        new Vector2(Drawing.Width / 2f - _measured.Width / 2f - Padding, _measured.Height / 2f + Margin),
                        new Vector2(Drawing.Width / 2f + _measured.Width / 2f + Padding, _measured.Height / 2f + Margin)
                    }, new Color(0, 0, 0, 175));

                _line.End();

                _font.DrawTextCentered(
                    _text, new Vector2(Drawing.Width / 2f, _measured.Height / 2f + Margin),
                    new Color(255, 255, 255, 175));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}