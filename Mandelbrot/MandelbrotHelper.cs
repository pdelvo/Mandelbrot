using mandelbrot;
using OpenCL.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Environment = OpenCL.Net.Environment;
using OpenCL.Net.Extensions;

namespace Mandelbrot
{
    public class MandelbrotHelper
    {
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int MaxRecursionCount { get; private set; }

        Environment _environment;
        CommandQueue _commandQueue;
        Context _context;

        Kernel _toBitmap;
        MandelBrotTest _mandelbrot;

        IMem<int> _resultBuffer;
        IMem<byte> _bitmapBuffer;

        public MandelbrotHelper(int maxRecursionCount = 30, int imageWidth = 1200, int imageHeight = 800, double centerX = -0.5, double centerY = 0, double width = 2, double height  = 2)
        {
            MaxRecursionCount = maxRecursionCount;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            CenterX = centerX;
            CenterY = centerY;
            Width = width;
            Height = height;
        }

        public void Initialize()
        {
            _environment = "*".CreateCLEnvironment();
            _commandQueue = _environment.CommandQueues[0];
            _context = _environment.Context;
            _mandelbrot = new mandelbrot.MandelBrotTest(_context);
            _mandelbrot.Compile();
            var toBitmap = new mandelbrot.toBitmap(_context);
            toBitmap.Compile();
            _toBitmap = toBitmap.Kernel;
            ErrorCode errorCode;

            _resultBuffer = Cl.CreateBuffer<int>(_context, MemFlags.ReadWrite, ImageWidth * ImageHeight, out errorCode);
            _bitmapBuffer = Cl.CreateBuffer<byte>(_context, MemFlags.ReadWrite, ImageWidth * ImageHeight * 4, out errorCode);

            _toBitmap.SetKernelArg(MaxRecursionCount)
                         .SetKernelArg((IMem)_resultBuffer)
                         .SetKernelArg((IMem)_bitmapBuffer);
        }

        public byte[] GetNextImageFrame()
        {
            _mandelbrot.Run(_commandQueue, MaxRecursionCount, CenterX, CenterY, Width, Height, ImageWidth, ImageHeight, _resultBuffer, (uint)(ImageWidth * ImageHeight));
            _commandQueue.EnqueueKernel(_toBitmap, (uint)(ImageWidth * ImageHeight));

            var bitmap = new byte[ImageWidth * ImageHeight * 4];
            _commandQueue.EnqueueReadFromBuffer(_bitmapBuffer, bitmap).Wait();
            return bitmap;
        }
    }
}
