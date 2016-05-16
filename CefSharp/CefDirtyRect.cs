// Copyright © 2010-2016 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

namespace CefSharp {
    public struct CefDirtyRect {
        readonly int x;
        readonly int y;
        readonly int width;
        readonly int height;

        public CefDirtyRect(int x, int y, int width, int height) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public int X { get { return x; } }
        public int Y { get { return y; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        public CefDirtyRect Combine(CefDirtyRect other) {
            if ((width == 0)||(height==0))
                return other;
            if ((other.width == 0)||(other.height == 0))
                return this;
            var lx = (x < other.x) ? x : other.X;
            var hx = ((x + width) > (other.x + other.width)) ? (x + width) : (other.x + other.width);

            var ly = (y < other.y) ? y : other.y;
            var hy = ((y + height) > (other.y + other.height)) ? (y + height) : (other.y + other.height);

            return new CefDirtyRect(lx, ly, hx - lx, hy - ly);
        }
    }
}
