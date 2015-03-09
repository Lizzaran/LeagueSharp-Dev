#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Formatter.cs is part of SFXLibrary.

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

namespace SFXLibrary.JSON
{
    #region

    using System.Text;

    #endregion

    internal static class Formatter
    {
        public static string Indent = "   ";

        public static void AppendIndent(StringBuilder sb, int count)
        {
            for (; count > 0; --count) sb.Append(Indent);
        }

        public static string PrettyPrint(string input)
        {
            var output = new StringBuilder();
            var depth = 0;
            var len = input.Length;
            var chars = input.ToCharArray();
            for (var i = 0; i < len; ++i)
            {
                var ch = chars[i];

                if (ch == '\"')
                {
                    var str = true;
                    while (str)
                    {
                        output.Append(ch);
                        ch = chars[++i];
                        if (ch == '\\')
                        {
                            output.Append(ch);
                            ch = chars[++i];
                        }
                        else if (ch == '\"')
                            str = false;
                    }
                }

                switch (ch)
                {
                    case '{':
                    case '[':
                        output.Append(ch);
                        output.AppendLine();
                        AppendIndent(output, ++depth);
                        break;

                    case '}':
                    case ']':
                        output.AppendLine();
                        AppendIndent(output, --depth);
                        output.Append(ch);
                        break;

                    case ',':
                        output.Append(ch);
                        output.AppendLine();
                        AppendIndent(output, depth);
                        break;

                    case ':':
                        output.Append(" : ");
                        break;

                    default:
                        if (!char.IsWhiteSpace(ch))
                            output.Append(ch);
                        break;
                }
            }

            return output.ToString();
        }
    }
}