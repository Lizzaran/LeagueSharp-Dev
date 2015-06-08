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
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility
{
    internal class Upvote
    {
        private const string Text =
            "                                     SFXUtility\r\nPlease consider to upvote it in the Assembly Database.";

        private const int Padding = 5;
        private const int Margin = 8;
        private static Font _font;
        private static Line _line;
        private static Rectangle _measured;
        private static bool _started;

        public static void Initialize()
        {
            try
            {
                _font = new Font(
                    Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = 20,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });
                _measured = _font.MeasureText(null, Text, FontDrawFlags.Center);
                _line = new Line(Drawing.Direct3DDevice) { Width = _measured.Height + (Padding * 2) };

                CustomEvents.Game.OnGameLoad += OnGameLoad;
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

        private static void OnGameLoad(EventArgs args)
        {
            try
            {
                OnInGame();
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
                    OnInGame();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnInGame()
        {
            try
            {
                if (!_started)
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
                    Text, new Vector2(Drawing.Width / 2f, _measured.Height / 2f + Margin), new Color(255, 255, 255, 175));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}