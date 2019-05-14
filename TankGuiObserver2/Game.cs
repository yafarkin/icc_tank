namespace TankGuiObserver2
{
    using SharpDX;
    using SharpDX.DXGI;
    using SharpDX.Windows;
    using SharpDX.Direct2D1;
    using SharpDX.Direct3D;
    using SharpDX.Direct3D11;
    using SharpDX.DirectInput;
    using AlphaMode = SharpDX.Direct2D1.AlphaMode;
    using Device = SharpDX.Direct3D11.Device;
    using Factory = SharpDX.DXGI.Factory;

    class Game : System.IDisposable
    {
        RenderForm RenderForm;
        RenderTarget RenderTarget2D;
        SharpDX.Direct2D1.Factory Factory2D;
        Surface Surface;
        SwapChain SwapChain;
        Device Device;

        GuiSpectator _spectatorClass;
        System.Threading.CancellationTokenSource tokenSource;
        TankClient.ClientCore clientCore;
        System.Threading.Thread _clientThread;

        bool _isEnterPressed;
        bool _isTabPressed;
        bool _isFPressed;
        DirectInput _directInput;
        Keyboard _keyboard;
        GameRender _gameRender;

        float _bitmapOpacity;
        SharpDX.Mathematics.Interop.RawRectangleF _destinationRectangle;
        BitmapInterpolationMode _interpolationMode;
        Bitmap _bitmap;

        public Game(string windowName,
            int windowWidth, int windowHeight,
            bool isWindowed = true)
        {
            RenderForm = new RenderForm(windowName);
            RenderForm.Width = windowWidth;
            RenderForm.Height = windowHeight;
            RenderForm.AllowUserResizing = false;

            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(
                        (int)(RenderForm.Width),
                        (int)(RenderForm.Height),
                        new Rational(60, 1),
                        Format.R8G8B8A8_UNorm),
                IsWindowed = isWindowed,
                OutputHandle = RenderForm.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            Device.CreateWithSwapChain(DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 },
                desc, out Device, out SwapChain);

            Factory2D = new SharpDX.Direct2D1.Factory();
            Factory factory = SwapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(RenderForm.Handle,
                    WindowAssociationFlags.IgnoreAll);

            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
            Surface = backBuffer.QueryInterface<Surface>();

            RenderTarget2D = new RenderTarget(Factory2D, Surface, new RenderTargetProperties(
                                 new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)));

            //WEB_SOCKET
            Connect();
            _spectatorClass = new GuiSpectator();

            System.Threading.Tasks.Task.Delay(1000);

            _gameRender = new GameRender(RenderForm, Factory2D, RenderTarget2D);

            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();

            //Loadng resources
            LoadResources();

        }

        public void RunGame()
        {
            RenderLoop.Run(RenderForm, Draw);
        }

        public void Draw()
        {
            RenderTarget2D.BeginDraw();
            KeyboardState kbs = _keyboard.GetCurrentState();//_keyboard.Poll();
            foreach (var key in kbs.PressedKeys)
            {
                if (key == Key.F)
                {
                    bool canChange = _gameRender.GetElapsedMs() > 500;
                    if (canChange)
                    {
                        if (!_isFPressed)
                        {
                            _isFPressed = true;
                        }
                        else
                        {
                            _isFPressed = false;
                        }
                    }
                }
                else if (key == Key.Tab)
                {
                    _isTabPressed = true;
                }
                else if (key == Key.Return && _spectatorClass.Map != null)
                {
                    _isEnterPressed = true;
                }
                else if (key == Key.Escape)
                {
                    RenderForm.Close();
                }
            }

            //Drawing a gama
            if (_isEnterPressed)
            {
                _gameRender.DrawMap(_spectatorClass.Map);
                _gameRender.DrawInteractiveObjects(_spectatorClass.Map.InteractObjects);
                _gameRender.DrawClientInfo();
            }
            else
            {
                _gameRender.DrawLogo();
                _gameRender.DrawWaitingLogo();
            }
            
            if (_isFPressed)
            {
                _gameRender.DrawFPS();
            }

            try
            {
                RenderTarget2D.EndDraw();
            }
            catch 
            {
            }

            SwapChain.Present(0, PresentFlags.None);
        }

        public void Connect()
        {
            var tokenSource = new System.Threading.CancellationTokenSource();
            var clientCore = new TankClient.ClientCore("ws://127.0.0.1:2000", string.Empty);
            _spectatorClass = new GuiSpectator(tokenSource.Token);

            _clientThread = new System.Threading.Thread(() => {
                clientCore.Run(false, _spectatorClass.Client, tokenSource.Token);
            });
            _clientThread.Start();
        }

        public void LoadResources()
        {
            //System.IO.File.WriteAllText("FILE_FILE.txt", "SOME TEXT");

            _destinationRectangle = new
                SharpDX.Mathematics.Interop.RawRectangleF(0, 0, 100, 100);
            _bitmapOpacity = 1.0f;
            _interpolationMode = BitmapInterpolationMode.Linear;
            _bitmap = LoadFromFile(RenderTarget2D, @"..\..\img\tank.png");
            
        }

        public static Bitmap LoadFromFile(RenderTarget renderTarget, string file)
        {
            // Loads from file using System.Drawing.Image
            using (var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(file))
            {
                var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapProperties = new BitmapProperties(
                    new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied));
                var size = new Size2(bitmap.Width, bitmap.Height);

                // Transform pixels from BGRA to RGBA
                int stride = bitmap.Width * sizeof(int);
                using (var tempStream = new DataStream(bitmap.Height * stride, true, true))
                {
                    // Lock System.Drawing.Bitmap
                    var bitmapData = bitmap.LockBits(sourceArea,
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                    // Convert all pixels 
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int offset = bitmapData.Stride * y;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            // Not optimized 
                            byte B = System.Runtime.InteropServices.Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte G = System.Runtime.InteropServices.Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte R = System.Runtime.InteropServices.Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte A = System.Runtime.InteropServices.Marshal.ReadByte(bitmapData.Scan0, offset++);
                            int rgba = R | (G << 8) | (B << 16) | (A << 24);
                            tempStream.Write(rgba);
                        }

                    }
                    bitmap.UnlockBits(bitmapData);
                    tempStream.Position = 0;

                    return new Bitmap(renderTarget, size, tempStream, stride, bitmapProperties);
                }
            }
        }

        public void Dispose()
        {
            RenderForm.Dispose();
            RenderTarget2D.Dispose();
            Factory2D.Dispose();
            Surface.Dispose();
            SwapChain.Dispose();
            Device.ImmediateContext.ClearState();
            Device.ImmediateContext.Flush();
            Device.Dispose();
            _gameRender.Dispose();
        }

    }
}