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
        RenderForm _renderForm;
        RenderTarget _renderTarget2D;
        SharpDX.Direct2D1.Factory _factory2D;
        Surface _surface;
        SwapChain _swapChain;
        Device _device;

        bool _isClientThreadRunning;
        string _serverString;
        System.Threading.Thread _clientThread;
        GuiSpectator _spectatorClass;
        System.Threading.CancellationTokenSource _tokenSource;
        GuiObserverCore _guiObserverCore;
        Connector _connector;

        bool _isFPressed;
        bool _isEnterPressed;
        bool _isWebSocketOpen;
        DirectInput _directInput;
        Keyboard _keyboard;
        GameRender _gameRender;

        //UI
        System.Windows.Forms.NotifyIcon _notifyIcon;

        public Game(string windowName,
            int windowWidth, int windowHeight,
            bool isFullscreen = false)
        {
            _renderForm = new RenderForm(windowName);
            _renderForm.Width = windowWidth;
            _renderForm.Height = windowHeight;
            _renderForm.AllowUserResizing = false;
            if (isFullscreen)
            {
                _renderForm.TopMost = true;
                _renderForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                _renderForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            }

            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(
                        (int)(_renderForm.Width),
                        (int)(_renderForm.Height),
                        new Rational(60, 1),
                        Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = _renderForm.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            Device.CreateWithSwapChain(DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 },
                desc, out _device, out _swapChain);

            _factory2D = new SharpDX.Direct2D1.Factory();
            Factory factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(_renderForm.Handle,
                    WindowAssociationFlags.IgnoreAll);

            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
            _surface = backBuffer.QueryInterface<Surface>();

            _renderTarget2D = new RenderTarget(_factory2D, _surface, new RenderTargetProperties(
                                 new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)));

            //WEB_SOCKET
            _serverString = string.Empty;
            _serverString = System.Configuration.ConfigurationManager.AppSettings["server"];
            if (_serverString == null)
            {
                _serverString = "ws://127.0.0.1:2000";
            }
            
            _guiObserverCore = new GuiObserverCore(_serverString, string.Empty);
            _tokenSource = new System.Threading.CancellationTokenSource();
            _spectatorClass = new GuiSpectator(_tokenSource.Token);
            _connector = new Connector(_serverString);

            _gameRender = new GameRender(_serverString, _renderForm, _factory2D, _renderTarget2D);

            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Exclamation;
            _notifyIcon.BalloonTipTitle = "Подсказка";
            _notifyIcon.BalloonTipText = "Чтобы узнать горячие клавиши GuiObserver, нажмите кнопку H";
            _notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(500);
        }

        public void RunGame()
        {
            RenderLoop.Run(_renderForm, Draw);
        }

        public void Draw()
        {
            _renderTarget2D.BeginDraw();
            KeyboardState kbs = _keyboard.GetCurrentState();//_keyboard.Poll();
            foreach (var key in kbs.PressedKeys)
            {
                if (key == Key.F)
                {
                    if (_gameRender.GetElapsedMs() > 500)
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
                else if (key == Key.Return && _spectatorClass.Map != null)
                {
                    _isEnterPressed = true;
                }
                else if (key == Key.Escape)
                {
                    try
                    {
                        _clientThread.Interrupt();
                    }
                    catch
                    {
                    }
                    _renderForm.Close();
                }
                else if (key == Key.F1)
                {
                    _renderForm.TopMost = true;
                    _renderForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    _renderForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                }
                else if (key == Key.F2)
                {
                    _renderForm.TopMost = false;
                    _renderForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
                    _renderForm.WindowState = System.Windows.Forms.FormWindowState.Normal;
                }
                else if (key == Key.H)
                {
                    System.Windows.Forms.MessageBox.Show("F1 - fullscreen\nF2 - windowed\nF - show fps\nH - help\nEsc - exit", "Help(me)");
                }
            }
           
            //Drawing a gama
            _isWebSocketOpen = (_guiObserverCore.WebSocketProxy.State == WebSocket4Net.WebSocketState.Open);
            if (!_isWebSocketOpen)
            {
                _isEnterPressed = false;
                _gameRender.UIIsVisible = false;
                _isClientThreadRunning = true;
                _gameRender.DrawWaitingLogo();

                _connector.IsServerRunning();
                if (_connector.ServerRunning)
                {
                    _isClientThreadRunning = false;
                }

                if (!_isClientThreadRunning)
                {
                    //_gameRender.Settings = _connector.Settings;
                    _isClientThreadRunning = true;
                    //_clientThread = new System.Threading.Thread(() => {
                    //    _guiObserverCore.Run(_spectatorClass.Client, _tokenSource.Token);
                    //});
                    //_clientThread.Start();
                    new System.Threading.Thread(() => {
                        _guiObserverCore.Run(_spectatorClass.Client, _tokenSource.Token);
                    }).Start();

                }
            }

            if (_isEnterPressed && _isWebSocketOpen)
            {
                if (!_gameRender.UIIsVisible) { _gameRender.UIIsVisible = true; }
                _gameRender.Map = _spectatorClass.Map;
                //_gameRender.Settings = _spectatorClass.Settings;
                _gameRender.DrawClientInfo();
                _gameRender.DrawMap();
                _gameRender.DrawTanks(_spectatorClass.Map.InteractObjects);
                _gameRender.DrawGrass();
                _gameRender.DrawInteractiveObjects(_spectatorClass.Map.InteractObjects);
            }
            else if (_spectatorClass.Map != null && _isWebSocketOpen)
            {
                _gameRender.DrawLogo();
            }
                

            if (_isFPressed)
            {
                _gameRender.DrawFPS();
            }

            try
            {
                _renderTarget2D.EndDraw();
            }
            catch
            {
            }

            _swapChain.Present(0, PresentFlags.None);
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
            _notifyIcon.Dispose();
            _connector.Dispose();
            _renderTarget2D.Dispose();
            _factory2D.Dispose();
            _surface.Dispose();
            _swapChain.Dispose();
            _device.ImmediateContext.ClearState();
            _device.ImmediateContext.Flush();
            _device.Dispose();
            _gameRender.Dispose();
            _renderForm.Dispose();
        }

    }
}