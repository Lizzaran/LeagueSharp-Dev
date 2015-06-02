#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Images.cs is part of SFXUtility.

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

///*
// Copyright 2014 - 2015 Nikita Bernthaler
// Images.cs is part of SFXUtility.

// SFXUtility is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SFXUtility is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
//*/

//#endregion License

//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using LeagueSharp;
//using SFXUtility.Properties;

//namespace SFXUtility.Classes
//{
//    class Images
//    {
//        public static Bitmap Load(string uniqueId, string name)
//        {
//            string cachedPath = GetCachePath(uniqueId, name);
//            if (File.Exists(cachedPath))
//            {
//                return new Bitmap(cachedPath);
//            }
//            var bitmap = Resources.ResourceManager.GetObject(name + "_Square_0") as Bitmap;
//            if (bitmap == null)
//            {
//                switch (uniqueId)
//                {
//                    case "lp":
//                        return CreateLastPositionImage(Resources.LP_Default);
//                    case "sb":
//                        return CreateLastPositionImage(Resources.SB_Default);
//                }
//            }

//            Bitmap finalBitmap = null;


//            switch (uniqueId)
//            {
//                case "lp":
//                    finalBitmap = CreateLastPositionImage(bitmap);
//                    break;
//                case "sb":
//                    finalBitmap = CreateLastPositionImage(bitmap);
//                    break;
//            }
//            if (finalBitmap != null)
//            {
//                finalBitmap.Save(cachedPath);
//            }
//            return finalBitmap;
//        }

//        private static string GetCachePath(string uniqueId, string name)
//        {
//            string path = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0} - Cache", Global.Name)));
//            if (!Directory.Exists(path))
//            {
//                Directory.CreateDirectory(path);
//            }
//            path = Path.Combine(path, Game.Version);
//            if (!Directory.Exists(path))
//            {
//                Directory.CreateDirectory(path);
//            }
//            return Path.Combine(path, string.Format("{0}_{1}.png", uniqueId, name));
//        }

//        private static Bitmap CreateLastPositionImage(Bitmap source)
//        {
//            var img = new Bitmap(source.Width, source.Width);
//            var cropRect = new Rectangle(0, 0, source.Width, source.Width);

//            using (Bitmap sourceImage = source)
//            {
//                using (Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat))
//                {
//                    using (var tb = new TextureBrush(croppedImage))
//                    {
//                        using (Graphics g = Graphics.FromImage(img))
//                        {
//                            g.FillEllipse(tb, 0, 0, source.Width, source.Width);
//                            var p = new Pen(Color.DimGray, 10) { Alignment = PenAlignment.Inset };
//                            g.DrawEllipse(p, 0, 0, source.Width, source.Width);
//                        }
//                    }
//                }
//            }
//            source.Dispose();
//            return ResizeImage(img, 24, 24);
//        }

//        private static Bitmap CreateSidebarImage(Bitmap source)
//        {
//            return ResizeImage(source, 62, 62);
//        }

//        private static Bitmap ResizeImage(Image image, int width, int height)
//        {
//            var destRect = new Rectangle(0, 0, width, height);
//            var destImage = new Bitmap(width, height);
//            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
//            using (var graphics = Graphics.FromImage(destImage))
//            {
//                graphics.CompositingMode = CompositingMode.SourceCopy;
//                graphics.CompositingQuality = CompositingQuality.HighQuality;
//                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
//                graphics.SmoothingMode = SmoothingMode.HighQuality;
//                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

//                using (var wrapMode = new ImageAttributes())
//                {
//                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
//                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
//                }
//            }
//            return destImage;
//        }
//    }
//}
