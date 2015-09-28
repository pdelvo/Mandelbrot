// <copyright file="MandelbrotCalculator.cs" company="Dennis Fischer">
// Copyright (c) Dennis Fischer. All rights reserved.
// </copyright>

namespace Mandelbrot
{
    using System;
    using System.IO;
    using System.Linq;
    using Cloo;

    /// <summary>
    /// A class that computes an image of the mandelbrot set on the gpu.
    /// </summary>
    public class MandelbrotCalculator
    {
        private ComputeContext context;
        private ComputeCommandQueue commandQueue;

        private ComputeProgram program;
        private ComputeKernel toBitmap;
        private ComputeKernel mandelbrot;

        private ComputeBuffer<int> resultBuffer;
        private ComputeBuffer<byte> bitmapBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MandelbrotCalculator"/> class.
        /// </summary>
        /// <param name="maxRecursionCount">The maximum number of recursion.</param>
        /// <param name="imageWidth">The width of the image in pixels.</param>
        /// <param name="imageHeight">The height of the image in pixels.</param>
        /// <param name="centerX">The real part of the center of the image.</param>
        /// <param name="centerY">The imaginary part of the center of the image.</param>
        /// <param name="width">The visible real interval.</param>
        /// <param name="height">The visible imaginary interval</param>
        public MandelbrotCalculator(int maxRecursionCount = 30, int imageWidth = 1200, int imageHeight = 800, double centerX = -0.5, double centerY = 0, double width = 2, double height = 2)
        {
            this.MaxRecursionCount = maxRecursionCount;
            this.ImageWidth = imageWidth;
            this.ImageHeight = imageHeight;
            this.CenterX = centerX;
            this.CenterY = centerY;
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        public int ImageWidth { get; private set; }

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        public int ImageHeight { get; private set; }

        /// <summary>
        /// The real part of the center of the image.
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// The imaginary part of the center of the image.
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// The visible real interval.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// The visible imaginary interval.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// The maximum number of recursions.
        /// </summary>
        public int MaxRecursionCount { get; private set; }

        /// <summary>
        /// Initializes local fields and the underlying compute context.
        /// </summary>
        public void Initialize()
        {
            if (this.context == null)
            {
                var devices = ComputePlatform.Platforms.SelectMany(a => a.Devices).Where(a => a.Extensions.Contains("cl_khr_fp64")).Take(1).ToArray();
                ComputeContextPropertyList list = new ComputeContextPropertyList(devices[0].Platform);
                this.context = new ComputeContext(devices, list, null, IntPtr.Zero);
            }

            this.program = new ComputeProgram(this.context, File.ReadAllText("Mandelbrot.cl"));

            this.program.Build(null, null, null, IntPtr.Zero);

            this.mandelbrot = this.program.CreateKernel("Mandelbrot");
            this.toBitmap = this.program.CreateKernel("ToBitmap");

            this.resultBuffer = new ComputeBuffer<int>(this.context, ComputeMemoryFlags.ReadWrite, this.ImageWidth * this.ImageHeight);
            this.bitmapBuffer = new ComputeBuffer<byte>(this.context, ComputeMemoryFlags.ReadWrite, this.ImageWidth * this.ImageHeight * 4);

            this.mandelbrot.SetMemoryArgument(7, this.resultBuffer);
            this.toBitmap.SetMemoryArgument(1, this.resultBuffer);
            this.toBitmap.SetMemoryArgument(2, this.bitmapBuffer);

            this.commandQueue = new ComputeCommandQueue(this.context, this.context.Devices.OrderBy(a => a.Type).Where(a => a.Extensions.Contains("cl_khr_fp64")).First(), ComputeCommandQueueFlags.None);
        }

        /// <summary>
        /// Reads the result bitmap from the gpu.
        /// </summary>
        /// <returns>A <see cref="byte"/> array with the raw bitmap data.</returns>
        public byte[] ReadResultBuffer()
        {
            var bitmap = new byte[this.ImageWidth * this.ImageHeight * 4];

            this.commandQueue.ReadFromBuffer(this.bitmapBuffer, ref bitmap, true, null);
            return bitmap;
        }

        /// <summary>
        /// Reads the result bitmap from the gpu.
        /// </summary>
        /// <param name="bitmap">An array to hold the result bitmap data.</param>
        public void ReadResultBuffer(byte[] bitmap)
        {
            this.commandQueue.ReadFromBuffer(this.bitmapBuffer, ref bitmap, true, null);
        }

        /// <summary>
        /// Computes the next image frame on the gpu.
        /// </summary>
        public void GetNextImageFrame()
        {
            this.mandelbrot.SetValueArgument(0, this.MaxRecursionCount);
            this.mandelbrot.SetValueArgument(1, this.CenterX);
            this.mandelbrot.SetValueArgument(2, this.CenterY);
            this.mandelbrot.SetValueArgument(3, this.Width);
            this.mandelbrot.SetValueArgument(4, this.Height);
            this.mandelbrot.SetValueArgument(5, this.ImageWidth);
            this.mandelbrot.SetValueArgument(6, this.ImageHeight);

            this.toBitmap.SetValueArgument(0, this.MaxRecursionCount);

            this.commandQueue.Execute(this.mandelbrot, null, new long[] { this.ImageWidth * this.ImageHeight }, null, null);
            this.commandQueue.Execute(this.toBitmap, null, new long[] { this.ImageWidth * this.ImageHeight }, null, null);
        }
    }
}
