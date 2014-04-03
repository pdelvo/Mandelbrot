using Cloo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Mandelbrot
{
    public class MandelbrotHelper(int maxRecursionCount = 30, int imageWidth = 1200, int imageHeight = 800, double centerX = -0.5, double centerY = 0, double width = 2, double height = 2)
    {
        public int ImageWidth { get; private set; } = imageWidth;
        public int ImageHeight { get; private set; } = imageHeight;
        public double CenterX { get; set; } = centerX;
        public double CenterY { get; set; } = centerY;
        public double Width { get; set; } = width;
        public double Height { get; set; } = height;
        public int MaxRecursionCount { get; private set; } = maxRecursionCount;

        ComputeContext _context;
        ComputeCommandQueue _commandQueue;

        ComputeProgram _program;
        ComputeKernel _toBitmap;
        ComputeKernel _mandelbrot;

        ComputeBuffer<int> _resultBuffer;
        ComputeBuffer<byte> _bitmapBuffer;

        [DllImport("opengl32.dll")]
        extern static IntPtr wglGetCurrentDC();

        public void Initialize(IntPtr contextHandle)
        {
            IntPtr wglHandle = wglGetCurrentDC();
            ComputePlatform platform = ComputePlatform.GetByName("NVIDIA CUDA");
            ComputeContextProperty p1 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platform.Handle.Value);
            ComputeContextProperty p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, contextHandle);
            ComputeContextProperty p3 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, wglHandle);
            List<ComputeContextProperty> props = new List<ComputeContextProperty>() { p1, p2, p2 };


            ComputeContextPropertyList Properties = new ComputeContextPropertyList(props);
            ComputeContext Ctx = new ComputeContext(ComputeDeviceTypes.Gpu, Properties, null, IntPtr.Zero);
            Initialize();
        }

        public void Initialize()
        {
            if (_context == null)
            {
                var devices = ComputePlatform.Platforms.SelectMany(a => a.Devices).Where(a => a.Extensions.Contains("cl_khr_fp64")).Take(1).ToArray();
                ComputeContextPropertyList list = new ComputeContextPropertyList(devices[0].Platform);
                _context = new ComputeContext(devices, list, null, IntPtr.Zero);
            }
            _program = new ComputeProgram(_context, File.ReadAllText("Mandelbrot.cl"));

            _program.Build(null, null, null, IntPtr.Zero);

            _mandelbrot = _program.CreateKernel("Mandelbrot");
            _toBitmap = _program.CreateKernel("ToBitmap");

            _resultBuffer = new ComputeBuffer<int>(_context, ComputeMemoryFlags.ReadWrite, ImageWidth * ImageHeight);
            _bitmapBuffer = new ComputeBuffer<byte>(_context, ComputeMemoryFlags.ReadWrite, ImageWidth * ImageHeight * 4);

            _mandelbrot.SetMemoryArgument(7, _resultBuffer);
            _toBitmap.SetMemoryArgument(1, _resultBuffer);
            _toBitmap.SetMemoryArgument(2, _bitmapBuffer);

            _commandQueue = new ComputeCommandQueue(_context, _context.Devices.OrderBy(a => a.Type).Where(a => a.Extensions.Contains("cl_khr_fp64")).First(), ComputeCommandQueueFlags.None);
        }

        public byte[] ReadResultBuffer()
        {
            var bitmap = new byte[ImageWidth * ImageHeight * 4];

            _commandQueue.ReadFromBuffer(_bitmapBuffer, ref bitmap, true, null);
            return bitmap;
        }
        public void ReadResultBuffer(byte[] bitmap)
        {
            _commandQueue.ReadFromBuffer(_bitmapBuffer, ref bitmap, true, null);
        }

        public void GetNextImageFrame()
        {
            _mandelbrot.SetValueArgument(0, MaxRecursionCount);
            _mandelbrot.SetValueArgument(1, CenterX);
            _mandelbrot.SetValueArgument(2, CenterY);
            _mandelbrot.SetValueArgument(3, Width);
            _mandelbrot.SetValueArgument(4, Height);
            _mandelbrot.SetValueArgument(5, ImageWidth);
            _mandelbrot.SetValueArgument(6, ImageHeight);

            _toBitmap.SetValueArgument(0, MaxRecursionCount);

            _commandQueue.Execute(_mandelbrot, null, new long[] { ImageWidth * ImageHeight }, null, null);
            _commandQueue.Execute(_toBitmap, null, new long[] { ImageWidth * ImageHeight }, null, null);

            //ComputeImage2D destination = new ComputeImage2D(_context, ComputeMemoryFlags.ReadWrite, new ComputeImageFormat(ComputeImageChannelOrder.Bgra, ComputeImageChannelType.UnsignedInt8), ImageWidth, ImageHeight, 0, IntPtr.Zero);

            //_commandQueue.CopyBufferToImage(_bitmapBuffer, destination, null);

        }
    }
}
