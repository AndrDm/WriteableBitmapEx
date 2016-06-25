#region Header
//
//   Project:           WriteableBitmapEx - WriteableBitmap extensions
//   Description:       Collection of transformation extension methods for the WriteableBitmap class.
//
//   Changed by:        $Author: unknown $
//   Changed on:        $Date: 2015-03-05 18:18:24 +0100 (Do, 05 Mrz 2015) $
//   Changed in:        $Revision: 113191 $
//   Project:           $URL: https://writeablebitmapex.svn.codeplex.com/svn/trunk/Source/WriteableBitmapEx/WriteableBitmapTransformationExtensions.cs $
//   Id:                $Id: WriteableBitmapTransformationExtensions.cs 113191 2015-03-05 17:18:24Z unknown $
//
//
//   Copyright � 2009-2015 Rene Schulte and WriteableBitmapEx Contributors
//
//   This code is open source. Please read the License.txt for details. No worries, we won't sue you! ;)
//
#endregion

using System;

#if NETFX_CORE
using Windows.Foundation;

namespace Windows.UI.Xaml.Media.Imaging
#else
namespace System.Windows.Media.Imaging
#endif
{
    /// <summary>
    /// Collection of transformation extension methods for the WriteableBitmap class.
    /// </summary>
    public
#if WPF
 unsafe
#endif
 static partial class WriteableBitmapExtensions
    {
        #region Enums

        /// <summary>
        /// The interpolation method.
        /// </summary>
        public enum Interpolation
        {
            /// <summary>
            /// The nearest neighbor algorithm simply selects the color of the nearest pixel.
            /// </summary>
            NearestNeighbor = 0,

            /// <summary>
            /// Linear interpolation in 2D using the average of 3 neighboring pixels.
            /// </summary>
            Bilinear,
        }

        /// <summary>
        /// The mode for flipping.
        /// </summary>
        public enum FlipMode
        {
            /// <summary>
            /// Flips the image vertical (around the center of the y-axis).
            /// </summary>
            Vertical,

            /// <summary>
            /// Flips the image horizontal (around the center of the x-axis).
            /// </summary>
            Horizontal
        }

        #endregion

        #region Methods

        #region Crop

        /// <summary>
        /// Creates a new cropped WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate of the rectangle that defines the crop region.</param>
        /// <param name="y">The y coordinate of the rectangle that defines the crop region.</param>
        /// <param name="width">The width of the rectangle that defines the crop region.</param>
        /// <param name="height">The height of the rectangle that defines the crop region.</param>
        /// <returns>A new WriteableBitmap that is a cropped version of the input.</returns>
        public static WriteableBitmap Crop(this WriteableBitmap bmp, int x, int y, int width, int height)
        {
            using (var srcContext = bmp.GetBitmapContext())
            {
                var srcWidth = srcContext.Width;
                var srcHeight = srcContext.Height;

                // If the rectangle is completely out of the bitmap
                if (x > srcWidth || y > srcHeight)
                {
                    return BitmapFactory.New(0, 0);
                }

                // Clamp to boundaries
                if (x < 0) x = 0;
                if (x + width > srcWidth) width = srcWidth - x;
                if (y < 0) y = 0;
                if (y + height > srcHeight) height = srcHeight - y;

                // Copy the pixels line by line using fast BlockCopy
                var result = BitmapFactory.New(width, height);
                using (var destContext = result.GetBitmapContext())
                {
                    for (var line = 0; line < height; line++)
                    {
                        var srcOff = ((y + line) * srcWidth + x) * SizeOfArgb;
                        var dstOff = line * width * SizeOfArgb;
                        BitmapContext.BlockCopy(srcContext, srcOff, destContext, dstOff, width * SizeOfArgb);
                    }

                    return result;
                }
            }
        }
        /// <summary>
        /// Creates a new cropped WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="region">The rectangle that defines the crop region.</param>
        /// <returns>A new WriteableBitmap that is a cropped version of the input.</returns>
        public static WriteableBitmap Crop(this WriteableBitmap bmp, Rect region)
        {
            return bmp.Crop((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height);
        }

        #endregion

        #region Resize

        /// <summary>
        /// Creates a new resized WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="width">The new desired width.</param>
        /// <param name="height">The new desired height.</param>
        /// <param name="interpolation">The interpolation method that should be used.</param>
        /// <returns>A new WriteableBitmap that is a resized version of the input.</returns>
        public static WriteableBitmap Resize(this WriteableBitmap bmp, int width, int height, Interpolation interpolation)
        {
            using (var srcContext = bmp.GetBitmapContext())
            {
                var pd = Resize(srcContext, srcContext.Width, srcContext.Height, width, height, interpolation);

                var result = BitmapFactory.New(width, height);
                using (var dstContext = result.GetBitmapContext())
                {
                    BitmapContext.BlockCopy(pd, 0, dstContext, 0, SizeOfArgb * pd.Length);
                }
                return result;
            }
        }

        /// <summary>
        /// Creates a new resized bitmap.
        /// </summary>
        /// <param name="srcContext">The source context.</param>
        /// <param name="widthSource">The width of the source pixels.</param>
        /// <param name="heightSource">The height of the source pixels.</param>
        /// <param name="width">The new desired width.</param>
        /// <param name="height">The new desired height.</param>
        /// <param name="interpolation">The interpolation method that should be used.</param>
        /// <returns>A new bitmap that is a resized version of the input.</returns>
        public static int[] Resize(BitmapContext srcContext, int widthSource, int heightSource, int width, int height, Interpolation interpolation)
        {
            return Resize(srcContext.Pixels, widthSource, heightSource, width, height, interpolation);
        }

        /// <summary>
        /// Creates a new resized bitmap.
        /// </summary>
        /// <param name="pixels">The source pixels.</param>
        /// <param name="widthSource">The width of the source pixels.</param>
        /// <param name="heightSource">The height of the source pixels.</param>
        /// <param name="width">The new desired width.</param>
        /// <param name="height">The new desired height.</param>
        /// <param name="interpolation">The interpolation method that should be used.</param>
        /// <returns>A new bitmap that is a resized version of the input.</returns>
#if WPF
        public static int[] Resize(int* pixels, int widthSource, int heightSource, int width, int height, Interpolation interpolation)
#else
      public static int[] Resize(int[] pixels, int widthSource, int heightSource, int width, int height, Interpolation interpolation)
#endif
        {
            var pd = new int[width * height];
            var xs = (float)widthSource / width;
            var ys = (float)heightSource / height;

            float fracx, fracy, ifracx, ifracy, sx, sy, l0, l1, rf, gf, bf;
            int c, x0, x1, y0, y1;
            byte c1a, c1r, c1g, c1b, c2a, c2r, c2g, c2b, c3a, c3r, c3g, c3b, c4a, c4r, c4g, c4b;
            byte a, r, g, b;

            // Nearest Neighbor
            if (interpolation == Interpolation.NearestNeighbor)
            {
                var srcIdx = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        sx = x * xs;
                        sy = y * ys;
                        x0 = (int)sx;
                        y0 = (int)sy;

                        pd[srcIdx++] = pixels[y0 * widthSource + x0];
                    }
                }
            }

               // Bilinear
            else if (interpolation == Interpolation.Bilinear)
            {
                var srcIdx = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        sx = x * xs;
                        sy = y * ys;
                        x0 = (int)sx;
                        y0 = (int)sy;

                        // Calculate coordinates of the 4 interpolation points
                        fracx = sx - x0;
                        fracy = sy - y0;
                        ifracx = 1f - fracx;
                        ifracy = 1f - fracy;
                        x1 = x0 + 1;
                        if (x1 >= widthSource)
                        {
                            x1 = x0;
                        }
                        y1 = y0 + 1;
                        if (y1 >= heightSource)
                        {
                            y1 = y0;
                        }


                        // Read source color
                        c = pixels[y0 * widthSource + x0];
                        c1a = (byte)(c >> 24);
                        c1r = (byte)(c >> 16);
                        c1g = (byte)(c >> 8);
                        c1b = (byte)(c);

                        c = pixels[y0 * widthSource + x1];
                        c2a = (byte)(c >> 24);
                        c2r = (byte)(c >> 16);
                        c2g = (byte)(c >> 8);
                        c2b = (byte)(c);

                        c = pixels[y1 * widthSource + x0];
                        c3a = (byte)(c >> 24);
                        c3r = (byte)(c >> 16);
                        c3g = (byte)(c >> 8);
                        c3b = (byte)(c);

                        c = pixels[y1 * widthSource + x1];
                        c4a = (byte)(c >> 24);
                        c4r = (byte)(c >> 16);
                        c4g = (byte)(c >> 8);
                        c4b = (byte)(c);


                        // Calculate colors
                        // Alpha
                        l0 = ifracx * c1a + fracx * c2a;
                        l1 = ifracx * c3a + fracx * c4a;
                        a = (byte)(ifracy * l0 + fracy * l1);

                        // Red
                        l0 = ifracx * c1r + fracx * c2r;
                        l1 = ifracx * c3r + fracx * c4r;
                        rf = ifracy * l0 + fracy * l1;

                        // Green
                        l0 = ifracx * c1g + fracx * c2g;
                        l1 = ifracx * c3g + fracx * c4g;
                        gf = ifracy * l0 + fracy * l1;

                        // Blue
                        l0 = ifracx * c1b + fracx * c2b;
                        l1 = ifracx * c3b + fracx * c4b;
                        bf = ifracy * l0 + fracy * l1;

                        // Cast to byte
                        r = (byte)rf;
                        g = (byte)gf;
                        b = (byte)bf;

                        // Write destination
                        pd[srcIdx++] = (a << 24) | (r << 16) | (g << 8) | b;
                    }
                }
            }
            return pd;
        }

        #endregion

        #region Rotate

        /// <summary>
        /// Rotates the bitmap in 90� steps clockwise and returns a new rotated WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="angle">The angle in degrees the bitmap should be rotated in 90� steps clockwise.</param>
        /// <returns>A new WriteableBitmap that is a rotated version of the input.</returns>
        public static WriteableBitmap Rotate(this WriteableBitmap bmp, int angle)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Use refs for faster access (really important!) speeds up a lot!
                var w = context.Width;
                var h = context.Height;
                var p = context.Pixels;
                var i = 0;
                WriteableBitmap result = null;
                angle %= 360;

                if (angle > 0 && angle <= 90)
                {
                    result = BitmapFactory.New(h, w);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var x = 0; x < w; x++)
                        {
                            for (var y = h - 1; y >= 0; y--)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }
                else if (angle > 90 && angle <= 180)
                {
                    result = BitmapFactory.New(w, h);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var y = h - 1; y >= 0; y--)
                        {
                            for (var x = w - 1; x >= 0; x--)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }
                else if (angle > 180 && angle <= 270)
                {
                    result = BitmapFactory.New(h, w);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var x = w - 1; x >= 0; x--)
                        {
                            for (var y = 0; y < h; y++)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }
                else
                {
                    result = bmp.Clone();
                }
                return result;
            }
        }

        /// <summary>
        /// Rotates the bitmap in any degree returns a new rotated WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="angle">Arbitrary angle in 360 Degrees (positive = clockwise).</param>
        /// <param name="crop">if true: keep the size, false: adjust canvas to new size</param>
        /// <returns>A new WriteableBitmap that is a rotated version of the input.</returns>
        public static WriteableBitmap RotateFree(this WriteableBitmap bmp, double angle, bool crop = true)
        {
            // rotating clockwise, so it's negative relative to Cartesian quadrants
            double cnAngle = -1.0 * (Math.PI / 180) * angle;

            // general iterators
            int i, j;
            // calculated indices in Cartesian coordinates
            int x, y;
            double fDistance, fPolarAngle;
            // for use in neighboring indices in Cartesian coordinates
            int iFloorX, iCeilingX, iFloorY, iCeilingY;
            // calculated indices in Cartesian coordinates with trailing decimals
            double fTrueX, fTrueY;
            // for interpolation
            double fDeltaX, fDeltaY;

            // interpolated "top" pixels
            double fTopRed, fTopGreen, fTopBlue, fTopAlpha;

            // interpolated "bottom" pixels
            double fBottomRed, fBottomGreen, fBottomBlue, fBottomAlpha;

            // final interpolated color components
            int iRed, iGreen, iBlue, iAlpha;

            int iCentreX, iCentreY;
            int iDestCentreX, iDestCentreY;
            int iWidth, iHeight, newWidth, newHeight;
            using (var bmpContext = bmp.GetBitmapContext())
            {

                iWidth = bmpContext.Width;
                iHeight = bmpContext.Height;

                if (crop)
                {
                    newWidth = iWidth;
                    newHeight = iHeight;
                }
                else
                {
                    var rad = angle / (180 / Math.PI);
                    newWidth = (int)Math.Ceiling(Math.Abs(Math.Sin(rad) * iHeight) + Math.Abs(Math.Cos(rad) * iWidth));
                    newHeight = (int)Math.Ceiling(Math.Abs(Math.Sin(rad) * iWidth) + Math.Abs(Math.Cos(rad) * iHeight));
                }


                iCentreX = iWidth / 2;
                iCentreY = iHeight / 2;

                iDestCentreX = newWidth / 2;
                iDestCentreY = newHeight / 2;

                var bmBilinearInterpolation = BitmapFactory.New(newWidth, newHeight);

                using (var bilinearContext = bmBilinearInterpolation.GetBitmapContext())
                {
                    var newp = bilinearContext.Pixels;
                    var oldp = bmpContext.Pixels;
                    var oldw = bmpContext.Width;

                    // assigning pixels of destination image from source image
                    // with bilinear interpolation
                    for (i = 0; i < newHeight; ++i)
                    {
                        for (j = 0; j < newWidth; ++j)
                        {
                            // convert raster to Cartesian
                            x = j - iDestCentreX;
                            y = iDestCentreY - i;

                            // convert Cartesian to polar
                            fDistance = Math.Sqrt(x * x + y * y);
                            if (x == 0)
                            {
                                if (y == 0)
                                {
                                    // center of image, no rotation needed
                                    newp[i * newWidth + j] = oldp[iCentreY * oldw + iCentreX];
                                    continue;
                                }
                                if (y < 0)
                                {
                                    fPolarAngle = 1.5 * Math.PI;
                                }
                                else
                                {
                                    fPolarAngle = 0.5 * Math.PI;
                                }
                            }
                            else
                            {
                                fPolarAngle = Math.Atan2(y, x);
                            }

                            // the crucial rotation part
                            // "reverse" rotate, so minus instead of plus
                            fPolarAngle -= cnAngle;

                            // convert polar to Cartesian
                            fTrueX = fDistance * Math.Cos(fPolarAngle);
                            fTrueY = fDistance * Math.Sin(fPolarAngle);

                            // convert Cartesian to raster
                            fTrueX = fTrueX + iCentreX;
                            fTrueY = iCentreY - fTrueY;

                            iFloorX = (int)(Math.Floor(fTrueX));
                            iFloorY = (int)(Math.Floor(fTrueY));
                            iCeilingX = (int)(Math.Ceiling(fTrueX));
                            iCeilingY = (int)(Math.Ceiling(fTrueY));

                            // check bounds
                            if (iFloorX < 0 || iCeilingX < 0 || iFloorX >= iWidth || iCeilingX >= iWidth || iFloorY < 0 ||
                                iCeilingY < 0 || iFloorY >= iHeight || iCeilingY >= iHeight) continue;

                            fDeltaX = fTrueX - iFloorX;
                            fDeltaY = fTrueY - iFloorY;

                            var clrTopLeft = oldp[iFloorY * oldw + iFloorX];
                            var clrTopRight = oldp[iFloorY * oldw + iCeilingX];
                            var clrBottomLeft = oldp[iCeilingY * oldw + iFloorX];
                            var clrBottomRight = oldp[iCeilingY * oldw + iCeilingX];

                            fTopAlpha = (1 - fDeltaX) * ((clrTopLeft >> 24) & 0xFF) + fDeltaX * ((clrTopRight >> 24) & 0xFF);
                            fTopRed = (1 - fDeltaX) * ((clrTopLeft >> 16) & 0xFF) + fDeltaX * ((clrTopRight >> 16) & 0xFF);
                            fTopGreen = (1 - fDeltaX) * ((clrTopLeft >> 8) & 0xFF) + fDeltaX * ((clrTopRight >> 8) & 0xFF);
                            fTopBlue = (1 - fDeltaX) * (clrTopLeft & 0xFF) + fDeltaX * (clrTopRight & 0xFF);

                            // linearly interpolate horizontally between bottom neighbors
                            fBottomAlpha = (1 - fDeltaX) * ((clrBottomLeft >> 24) & 0xFF) + fDeltaX * ((clrBottomRight >> 24) & 0xFF);
                            fBottomRed = (1 - fDeltaX) * ((clrBottomLeft >> 16) & 0xFF) + fDeltaX * ((clrBottomRight >> 16) & 0xFF);
                            fBottomGreen = (1 - fDeltaX) * ((clrBottomLeft >> 8) & 0xFF) + fDeltaX * ((clrBottomRight >> 8) & 0xFF);
                            fBottomBlue = (1 - fDeltaX) * (clrBottomLeft & 0xFF) + fDeltaX * (clrBottomRight & 0xFF);

                            // linearly interpolate vertically between top and bottom interpolated results
                            iRed = (int)(Math.Round((1 - fDeltaY) * fTopRed + fDeltaY * fBottomRed));
                            iGreen = (int)(Math.Round((1 - fDeltaY) * fTopGreen + fDeltaY * fBottomGreen));
                            iBlue = (int)(Math.Round((1 - fDeltaY) * fTopBlue + fDeltaY * fBottomBlue));
                            iAlpha = (int)(Math.Round((1 - fDeltaY) * fTopAlpha + fDeltaY * fBottomAlpha));

                            // make sure color values are valid
                            if (iRed < 0) iRed = 0;
                            if (iRed > 255) iRed = 255;
                            if (iGreen < 0) iGreen = 0;
                            if (iGreen > 255) iGreen = 255;
                            if (iBlue < 0) iBlue = 0;
                            if (iBlue > 255) iBlue = 255;
                            if (iAlpha < 0) iAlpha = 0;
                            if (iAlpha > 255) iAlpha = 255;

                            var a = iAlpha + 1;
                            newp[i * newWidth + j] = (iAlpha << 24)
                                                   | ((byte)((iRed * a) >> 8) << 16)
                                                   | ((byte)((iGreen * a) >> 8) << 8)
                                                   | ((byte)((iBlue * a) >> 8));
                        }
                    }
                    return bmBilinearInterpolation;
                }
            }
        }

        /// <summary>
        /// Renders the bitmap using Fantasia Painter's fast and quality rendering algorithm using any affine transformation and transparency into another bitmap
        /// Unlike Silverlight's Render() method, this one uses 2-3 times less memory, and is the same or better quality
        /// The algorithm is simple dx/dy (bresenham-like) step by step painting, optimized with fixed point and fast bilinear filtering
        /// It's used in Fantasia Painter for drawing stickers and 3D objects on screen
        /// </summary>
        /// <param name="bmp">Bitmap to render</param>
        /// <param name="destWillBeCleared">Destination bitmap. Note: the destination bitmap will be set to all clear (0) before rendering</param>
        /// <param name="opacityF">opacity of the source bitmap to render, between 0 and 1 inclusive</param>
        /// <param name="forward">Transformation to apply</param>
        /// <param name="inverse">Inverse of the transformation to apply, used for optimization</param>
        public static void RenderOnClear(this WriteableBitmap bmp, WriteableBitmap destWillBeCleared, float opacityF, GeneralTransform forward, GeneralTransform inverse)
        {
            const int PRECISION_SHIFT = 10;
            //const float PRECISION_VALUEf = (1 << PRECISION_SHIFT);
            //const float OneOverPRECISION_VALUEf = 1.0f / (1 << PRECISION_SHIFT);
            const int PRECISION_VALUE = (1 << PRECISION_SHIFT);
            const int PRECISION_MASK = PRECISION_VALUE - 1;

            using (BitmapContext destContext = destWillBeCleared.GetBitmapContext())
            {
                var destPixels = destContext.Pixels;
                int destWidth = destContext.Width;
                int destHeight = destContext.Height;
                destContext.Clear();

                using (BitmapContext sourceContext = bmp.GetBitmapContext())
                {
                    var sourcePixels = sourceContext.Pixels;
                    int sourceWidth = sourceContext.Width;
                    int sourceHeight = sourceContext.Height;

                    Rect sourceRect = new Rect(0, 0, sourceWidth, sourceHeight);
                    Rect destRect = new Rect(0, 0, destWidth, destHeight);
                    Rect bounds = forward.TransformBounds(sourceRect);
                    bounds.Intersect(destRect);

                    int startX = (int)bounds.Left;
                    int startY = (int)bounds.Top;
                    int endX = (int)bounds.Right;
                    int endY = (int)bounds.Bottom;

                    Point zeroZero = inverse.Transform(new Point(startX, startY));
                    Point oneZero = inverse.Transform(new Point(startX + 1, startY));
                    Point zeroOne = inverse.Transform(new Point(startX, startY + 1));

                    float sourceXf = ((float)zeroZero.X);
                    float sourceYf = ((float)zeroZero.Y);
                    int dxDx = (int)((((float)oneZero.X) - sourceXf) * PRECISION_VALUE); // for 1 unit in X coord, how much does X change in source texture?
                    int dxDy = (int)((((float)oneZero.Y) - sourceYf) * PRECISION_VALUE); // for 1 unit in X coord, how much does Y change in source texture?
                    int dyDx = (int)((((float)zeroOne.X) - sourceXf) * PRECISION_VALUE); // for 1 unit in Y coord, how much does X change in source texture?
                    int dyDy = (int)((((float)zeroOne.Y) - sourceYf) * PRECISION_VALUE); // for 1 unit in Y coord, how much does Y change in source texture?

                    int sourceX = (int)(((float)zeroZero.X) * PRECISION_VALUE);
                    int sourceY = (int)(((float)zeroZero.Y) * PRECISION_VALUE);
                    int sourceWidthFixed = sourceWidth << PRECISION_SHIFT;
                    int sourceHeightFixed = sourceHeight << PRECISION_SHIFT;

                    int opacity = (int)(opacityF * 255);

                    int index = 0;
                    for (int destY = startY; destY < endY; destY++)
                    {
                        index = destY * destWidth + startX;
                        int savedSourceX = sourceX;
                        int savedSourceY = sourceY;

                        for (int destX = startX; destX < endX; destX++)
                        {
                            if ((sourceX >= 0) && (sourceX < sourceWidthFixed) && (sourceY >= 0) && (sourceY < sourceHeightFixed))
                            {
                                // bilinear filtering
                                int xFloor = sourceX >> PRECISION_SHIFT;
                                int yFloor = sourceY >> PRECISION_SHIFT;

                                if (xFloor < 0) xFloor = 0;
                                if (yFloor < 0) yFloor = 0;

                                int xCeil = xFloor + 1;
                                int yCeil = yFloor + 1;

                                if (xCeil >= sourceWidth)
                                {
                                    xFloor = sourceWidth - 1;
                                    xCeil = 0;
                                }
                                else
                                {
                                    xCeil = 1;
                                }

                                if (yCeil >= sourceHeight)
                                {
                                    yFloor = sourceHeight - 1;
                                    yCeil = 0;
                                }
                                else
                                {
                                    yCeil = sourceWidth;
                                }

                                int i1 = yFloor * sourceWidth + xFloor;
                                int p1 = sourcePixels[i1];
                                int p2 = sourcePixels[i1 + xCeil];
                                int p3 = sourcePixels[i1 + yCeil];
                                int p4 = sourcePixels[i1 + yCeil + xCeil];

                                int xFrac = sourceX & PRECISION_MASK;
                                int yFrac = sourceY & PRECISION_MASK;

                                // alpha
                                byte a1 = (byte)(p1 >> 24);
                                byte a2 = (byte)(p2 >> 24);
                                byte a3 = (byte)(p3 >> 24);
                                byte a4 = (byte)(p4 >> 24);

                                int comp1, comp2;
                                byte a;

                                if ((a1 == a2) && (a1 == a3) && (a1 == a4))
                                {
                                    if (a1 == 0)
                                    {
                                        destPixels[index] = 0;

                                        sourceX += dxDx;
                                        sourceY += dxDy;
                                        index++;
                                        continue;
                                    }

                                    a = a1;
                                }
                                else
                                {
                                    comp1 = a1 + ((xFrac * (a2 - a1)) >> PRECISION_SHIFT);
                                    comp2 = a3 + ((xFrac * (a4 - a3)) >> PRECISION_SHIFT);
                                    a = (byte)(comp1 + ((yFrac * (comp2 - comp1)) >> PRECISION_SHIFT));
                                }

                                // red
                                comp1 = ((byte)(p1 >> 16)) + ((xFrac * (((byte)(p2 >> 16)) - ((byte)(p1 >> 16)))) >> PRECISION_SHIFT);
                                comp2 = ((byte)(p3 >> 16)) + ((xFrac * (((byte)(p4 >> 16)) - ((byte)(p3 >> 16)))) >> PRECISION_SHIFT);
                                byte r = (byte)(comp1 + ((yFrac * (comp2 - comp1)) >> PRECISION_SHIFT));

                                // green
                                comp1 = ((byte)(p1 >> 8)) + ((xFrac * (((byte)(p2 >> 8)) - ((byte)(p1 >> 8)))) >> PRECISION_SHIFT);
                                comp2 = ((byte)(p3 >> 8)) + ((xFrac * (((byte)(p4 >> 8)) - ((byte)(p3 >> 8)))) >> PRECISION_SHIFT);
                                byte g = (byte)(comp1 + ((yFrac * (comp2 - comp1)) >> PRECISION_SHIFT));

                                // blue
                                comp1 = ((byte)p1) + ((xFrac * (((byte)p2) - ((byte)p1))) >> PRECISION_SHIFT);
                                comp2 = ((byte)p3) + ((xFrac * (((byte)p4) - ((byte)p3))) >> PRECISION_SHIFT);
                                byte b = (byte)(comp1 + ((yFrac * (comp2 - comp1)) >> PRECISION_SHIFT));

                                // save updated pixel
                                if (opacity != 255)
                                {
                                    a = (byte)((a * opacity) >> 8);
                                    r = (byte)((r * opacity) >> 8);
                                    g = (byte)((g * opacity) >> 8);
                                    b = (byte)((b * opacity) >> 8);
                                }
                                destPixels[index] = (a << 24) | (r << 16) | (g << 8) | b;
                            }

                            sourceX += dxDx;
                            sourceY += dxDy;
                            index++;
                        }

                        sourceX = savedSourceX + dyDx;
                        sourceY = savedSourceY + dyDy;
                    }
                }
            }
        }
        #endregion

        #region Flip

        /// <summary>
        /// Flips (reflects the image) either vertical or horizontal.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="flipMode">The flip mode.</param>
        /// <returns>A new WriteableBitmap that is a flipped version of the input.</returns>
        public static WriteableBitmap Flip(this WriteableBitmap bmp, FlipMode flipMode)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Use refs for faster access (really important!) speeds up a lot!
                var w = context.Width;
                var h = context.Height;
                var p = context.Pixels;
                var i = 0;
                WriteableBitmap result = null;

                if (flipMode == FlipMode.Horizontal)
                {
                    result = BitmapFactory.New(w, h);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var y = h - 1; y >= 0; y--)
                        {
                            for (var x = 0; x < w; x++)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }
                else if (flipMode == FlipMode.Vertical)
                {
                    result = BitmapFactory.New(w, h);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var y = 0; y < h; y++)
                        {
                            for (var x = w - 1; x >= 0; x--)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }

                return result;
            }
        }

        #endregion

        #endregion
    }
}