using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCL.Net.Extensions;
using OpenCL.Net;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Mandelbrot
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupViewport();
        }

        private unsafe static void SetupViewport()
        {

            GameWindow window = new GameWindow(DisplayDevice.Default.Width, DisplayDevice.Default.Height, OpenTK.Graphics.GraphicsMode.Default, "Mandelbrot", GameWindowFlags.Fullscreen);
            int tex;

            OpenTK.Graphics.IGraphicsContextInternal ctx = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;

            IntPtr contextHandle = ctx.Context.Handle;



            var mandelbrot = new MandelbrotHelper(maxRecursionCount: 200);
            mandelbrot.Initialize();


            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.GenTextures(1, out tex);
            GL.BindTexture(TextureTarget.Texture2D, tex);

            mandelbrot.GetNextImageFrame();
            fixed (byte* p =  mandelbrot.ReadResultBuffer())
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1200, 800, 0, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)p);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            bool isButtonPressed = false;
            bool changed = true;
            window.Mouse.ButtonDown += (s, e) => isButtonPressed = true;
            window.Keyboard.KeyDown += (s, e) =>
            {
                if (e.Key == OpenTK.Input.Key.Escape) window.Close();
            };
            window.Mouse.ButtonUp += (s, e) => isButtonPressed = false;

            window.Mouse.Move += (s, e) =>
            {
                if (isButtonPressed)
                {
                    mandelbrot.CenterX -= (e.XDelta / (double)mandelbrot.ImageWidth * mandelbrot.Width);
                    mandelbrot.CenterY += (e.YDelta / (double)mandelbrot.ImageHeight * mandelbrot.Height);
                    changed = true;
                }
            };

            window.Mouse.WheelChanged += (s, e) =>
            { 
                mandelbrot.Width /= Math.Pow(2,  e.DeltaPrecise);
                mandelbrot.Height /= Math.Pow(2, e.DeltaPrecise);
                changed = true;
            };

            window.UpdateFrame += (s, e) =>
            {
                if (changed)
                {
                    mandelbrot.GetNextImageFrame();
                    fixed (byte* p = mandelbrot.ReadResultBuffer())
                    {
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1200, 800, 0, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)p);
                    }
                }
                changed = false;
            };
            window.RenderFrame += (sender, e) =>
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                DrawImage(tex, mandelbrot.ImageWidth, mandelbrot.ImageHeight);

                window.SwapBuffers();


            };
            window.VSync = VSyncMode.Off;
            window.Run(0);
        }
        public static void DrawImage(int image, int width, int height)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Ortho(0, width, 0, height, -1, 1);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Disable(EnableCap.Lighting);

            GL.Enable(EnableCap.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, image);

            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex3(0, 0, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex3(width, 0, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex3(width, height, 0);

            GL.TexCoord2(0, 1);
            GL.Vertex3(0, height, 0);

            GL.End();

            GL.Disable(EnableCap.Texture2D);
            GL.PopMatrix();

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();

            GL.MatrixMode(MatrixMode.Modelview);
        } 


    }
}
