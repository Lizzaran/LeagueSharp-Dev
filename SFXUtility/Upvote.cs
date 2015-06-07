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
using SFXUtility.Classes;
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

        public static void Initialize()
        {
            _font = MDrawing.GetFont(20);
            _measured = _font.MeasureText(null, Text, FontDrawFlags.Center);
            _line = MDrawing.GetLine(_measured.Height + (Padding * 2));
            Drawing.OnEndScene += OnDrawingEndScene;
            CustomEvents.Game.OnGameLoad += delegate
            {
                Drawing.OnEndScene -= OnDrawingEndScene;
                _line.Dispose();
                _font.Dispose();
            };
        }

        private static void OnDrawingEndScene(EventArgs args)
        {
            _line.Begin();

            _line.Draw(
                new[]
                {
                    new Vector2(Drawing.Width / 2f - _measured.Width / 2f - Padding, _measured.Height / 2f + Margin),
                    new Vector2(Drawing.Width / 2f + _measured.Width / 2f + Padding, _measured.Height / 2f + Margin)
                },
                new Color(0, 0, 0, 175));

            _line.End();

            _font.DrawTextCentered(
                Text, new Vector2(Drawing.Width / 2f, _measured.Height / 2f + Margin), new Color(255, 255, 255, 175));
        }
    }
}