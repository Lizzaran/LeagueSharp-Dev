#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Key.cs is part of SFXSnake.

 SFXSnake is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXSnake is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXSnake. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SFXSnake
{
    internal enum Key
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        VK_LEFT = 0x25,
        VK_RIGHT = 0x27,
        VK_UP = 0x26,
        VK_DOWN = 0x28,
        VK_KEY_W = 0x57,
        VK_KEY_A = 0x41,
        VK_KEY_S = 0x53,
        VK_KEY_D = 0x44
    }
}