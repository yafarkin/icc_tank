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
        int _serverStartupIndex;
        string _serverString;
        System.Threading.Thread _clientThread;
        GuiSpectator _spectatorClass;
        System.Threading.CancellationTokenSource _tokenSource;
        GuiObserverCore _guiObserverCore;
        Connector _connector;

        bool _isFPressed;
        bool _isEnterPressed;
        bool _isTabPressed;
        bool _isClientInfoWasCentered;
        bool _isWebSocketOpen;
        bool _onlyTankRendering;
        int _verticalSyncOn;
        System.Diagnostics.Stopwatch _keyboardDelay;
        DirectInput _directInput;
        Keyboard _keyboard;
        GameRender _gameRender;

        //UI
        System.Windows.Forms.NotifyIcon _notifyHelp;
        System.Windows.Forms.NotifyIcon _notifyVerticalSyncOn;
        System.Windows.Forms.NotifyIcon _notifyVerticalSyncOff;

        //Logger
        static NLog.Logger _logger;

        public Game(string windowName,
            int windowWidth, int windowHeight,
            bool isFullscreen = false)
        {
            #region Initialization
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
            _serverString = System.Configuration.ConfigurationManager.AppSettings["server"];
            _isTabPressed = false;
            _serverStartupIndex = 0;

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
            #endregion

            #region UI
            _notifyHelp = new System.Windows.Forms.NotifyIcon();
            _notifyHelp.Icon = System.Drawing.SystemIcons.Exclamation;
            _notifyHelp.BalloonTipTitle = "Подсказка";
            _notifyHelp.BalloonTipText = "Чтобы узнать горячие клавиши GuiObserver, нажмите кнопку H";
            _notifyHelp.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            _notifyHelp.Visible = true;
            _notifyHelp.ShowBalloonTip(500);
            _notifyHelp.Dispose();

            _verticalSyncOn = 1;
            _onlyTankRendering = false;
            _notifyVerticalSyncOn = new System.Windows.Forms.NotifyIcon();
            _notifyVerticalSyncOn.Icon = System.Drawing.SystemIcons.Information;
            _notifyVerticalSyncOn.BalloonTipTitle = "";
            _notifyVerticalSyncOn.BalloonTipText = "Вертикальная синхронизация включена";
            _notifyVerticalSyncOn.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;

            _notifyVerticalSyncOff = new System.Windows.Forms.NotifyIcon();
            _notifyVerticalSyncOff.Icon = System.Drawing.SystemIcons.Information;
            _notifyVerticalSyncOff.BalloonTipTitle = "";
            _notifyVerticalSyncOff.BalloonTipText = "Вертикальная синхронизация отключена";
            _notifyVerticalSyncOff.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            #endregion

            #region Connection
            _tokenSource = new System.Threading.CancellationTokenSource();
            _connector = new Connector(_serverString);
            _guiObserverCore = new GuiObserverCore(_serverString, string.Empty);
            _spectatorClass = new GuiSpectator();
            #endregion

            #region Keyboard
            _keyboardDelay = new System.Diagnostics.Stopwatch();
            _keyboardDelay.Start();
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();
            #endregion

            #region Logger
            _logger = NLog.LogManager.GetCurrentClassLogger();
            _logger.Info("Ctor is working fine. [Game]");
            #endregion

            _gameRender = new GameRender(_serverString, _renderForm, _factory2D, _renderTarget2D);

        }

        public void RunGame()
        {
            RenderLoop.Run(_renderForm, Draw);
        }

        public void Draw()
        {
            _renderTarget2D.BeginDraw();
            _logger.Debug("Frame draw: begin");
            
            KeyboardState kbs = _keyboard.GetCurrentState();//_keyboard.Poll();
            foreach (var key in kbs.PressedKeys)
            {
                if (key == Key.F)
                {
                    if (_keyboardDelay.ElapsedMilliseconds > 100)
                    {
                        _keyboardDelay.Stop();
                        if (!_isFPressed)
                        {
                            _isFPressed = true;
                        }
                        else
                        {
                            _isFPressed = false;
                        }
                        _keyboardDelay.Reset();
                        _keyboardDelay.Start();
                    }
                }
                else if (key == Key.Return && _spectatorClass?.Map != null)
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
                else if (key == Key.F3)
                {
                    if (_keyboardDelay.ElapsedMilliseconds > 100)
                    {
                        _keyboardDelay.Stop();
                        if (_gameRender.IsNickRendered)
                        {
                            _gameRender.IsNickRendered = false;
                        }
                        else
                        {
                            _gameRender.IsNickRendered = true;
                        }
                        _keyboardDelay.Reset();
                        _keyboardDelay.Start();
                    }
                }
                else if (key == Key.F4)
                {
                    if (_keyboardDelay.ElapsedMilliseconds > 300)
                    {
                        _keyboardDelay.Stop();
                        if (_gameRender.IpReconnectUIVisible)
                        {
                            _gameRender.IpReconnectUIVisible = false;
                        }
                        else
                        {
                            _gameRender.IpReconnectUIVisible = true;
                        }
                        _keyboardDelay.Reset();
                        _keyboardDelay.Start();
                    }
                }
                else if (key == Key.H)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "F1 - fullscreen\n" +
                        "F2 - windowed\n" +
                        "F3 - make nickname [visible/unvisible]\n" +
                        "F4 - reconnect\n" +
                        "F - show fps\n" +
                        "H - help\n" +
                        "V - vertical sync\n" +
                        "T - render only tank mode [on/off]\n" +
                        "Tab - show players\n" +
                        "Esc - exit\n", "Help(me)");
                }
                else if (key == Key.V)
                {
                    if (_keyboardDelay.ElapsedMilliseconds > 100)
                    {
                        _keyboardDelay.Stop();
                        if (_verticalSyncOn == 0)
                        {
                            _verticalSyncOn = 1;
                            _notifyVerticalSyncOn.Visible = true;
                            _notifyVerticalSyncOff.Visible = false;
                            _notifyVerticalSyncOn.ShowBalloonTip(200);
                        }
                        else
                        {
                            _verticalSyncOn = 0;
                            _notifyVerticalSyncOn.Visible = false;
                            _notifyVerticalSyncOff.Visible = true;
                            _notifyVerticalSyncOff.ShowBalloonTip(500);
                        }
                        _keyboardDelay.Reset();
                        _keyboardDelay.Start();
                    }
                }
                else if (key == Key.T)
                {
                    //рендерить только танки
                    if (_keyboardDelay.ElapsedMilliseconds > 100)
                    {
                        _keyboardDelay.Stop();
                        if (_onlyTankRendering)
                        {
                            _onlyTankRendering = false;
                        }
                        else
                        {
                            _onlyTankRendering = true;
                        }
                        _keyboardDelay.Reset();
                        _keyboardDelay.Start();
                    }
                }
                else if (key == Key.Tab)
                {
                    if (_keyboardDelay.ElapsedMilliseconds > 200)
                    {
                        _keyboardDelay.Stop();
                        if (_isTabPressed)
                        {
                            _isTabPressed = false;
                        }
                        else
                        {
                            _isTabPressed = true;
                        }
                        _gameRender.CenterClientInfo();
                        _keyboardDelay.Reset();
                        _keyboardDelay.Start();
                    }
                }

            }
            
            /*
              NOTE:
              Если данный ресет поставить после if (!_isWebSocketOpen),
              то реконект пройдет по старому _serverString, в случае, 
              когда нам действительно нужен реконект, а не конект.
             */
            if (_gameRender.ResetIp(ref _serverString))
            {
                Reset();
            }

            //Drawing a game
            _isWebSocketOpen = (_guiObserverCore?.WebSocketProxy.State ==  WebSocket4Net.WebSocketState.Open);
            if (!_isWebSocketOpen)
            {
                Reset();
            }

            if (_isEnterPressed && _isWebSocketOpen)
            {
                _logger.Debug("flag: _isEnterPressed");
                _logger.Debug("flag: _isWebSocketOpen");

                if (!_gameRender.UIIsVisible && !_isTabPressed)
                {
                    _gameRender.UIIsVisible = true;
                    _logger.Debug("flag: !UIIsVisible");
                    _logger.Debug("flag: !_isTabPressed");
                }
                
                _gameRender.Map = _spectatorClass?.Map;
                _gameRender.Settings = _spectatorClass.Settings;
                _gameRender.DrawClientInfo();
                _logger.Debug("call: DrawClientInfo()");

                if (!_isTabPressed)
                {
                    _logger.Debug("flag: _isTabPressed");
                    if (!_onlyTankRendering)
                    {
                        _gameRender.DrawMap();
                        _logger.Debug("call: DrawMap()");
                        _gameRender.DrawTanks(_spectatorClass.Map.InteractObjects);
                        _logger.Debug("call: DrawTanks()");
                        _gameRender.DrawGrass();
                        _logger.Debug("call: DrawGrass()");
                        _gameRender.DrawInteractiveObjects(_spectatorClass.Map.InteractObjects);
                        _logger.Debug("call: DrawInteractiveObjects()");
                    }
                    else
                    {
                        _gameRender.DrawTanks(_spectatorClass.Map.InteractObjects);
                        _logger.Debug("call: DrawTanks() [only/lonely]");
                    }

                }
                else
                {
                    //_gameRender.DrawClientInfo();
                }
            }
            else if (_spectatorClass?.Map != null && _isWebSocketOpen)
            {
                _gameRender.DrawLogo();
                _logger.Debug("call: DrawLogo()");
            }
            else if (_spectatorClass?.Map == null)
            {

            }

            if (_isFPressed)
            {
                _gameRender.DrawFPS();
                _logger.Info("Press F to pay respect.");
            }

            try
            {
                _renderTarget2D.EndDraw();
            }
            catch
            {
                _logger.Info("Catch exception from: _renderTarget2D.EndDraw()");
            }

            _swapChain.Present(_verticalSyncOn, PresentFlags.None);
            _logger.Debug("Frame draw: end");
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        private void Reset()
        {
            _logger.Debug("flag: !_isWebSocketOpen()");
            _isEnterPressed = false;
            _gameRender.UIIsVisible = false;

            if (!_connector.Server.Equals(_serverString))
            {
                _tokenSource.Cancel();
                _tokenSource?.Dispose();
                _tokenSource = new System.Threading.CancellationTokenSource();
                _connector?.Dispose();
                _connector = new Connector(_serverString);
                _guiObserverCore.Restart(_serverString);
            }

            _isClientThreadRunning = true;
            _connector.IsServerRunning();
            _logger.Debug("call: _connector.IsServerRunning()");
            if (_connector.ServerRunning)
            {
                _isClientThreadRunning = false;
            }
            else
            {
                _isClientThreadRunning = true;
                try
                {
                    _clientThread?.Interrupt();
                    _clientThread = null;
                }
                catch (System.Security.SecurityException ex)
                {
                    _logger.Error("exception: _clientThread.Interrupt()");
                    _logger.Error($"exception: {ex.Message}");
                }
            }

            _gameRender.DrawWaitingLogo();

            _logger.Debug("call: DrawWaitingLogo()");
            if (!_isClientThreadRunning)
            {
                _logger.Debug("flag: _isClientThreadRunning()");
                _isClientThreadRunning = true;
                ++_serverStartupIndex;
                _gameRender.GameSetDefault();

                _gameRender.Settings = _spectatorClass.Settings;
                
                try
                {
                    _clientThread?.Interrupt();
                    _clientThread = null;
                }
                catch (System.Security.SecurityException ex)
                {
                    _logger.Error("exception: _clientThread.Interrupt()");
                    _logger.Error($"exception: {ex.Message}");
                }
                _clientThread = new System.Threading.Thread(() => {
                    _guiObserverCore.Run(_spectatorClass.Client, _tokenSource.Token);
                });
                _clientThread.Start();
            }
        }

        public void Dispose()
        {
            try
            {
                _clientThread?.Interrupt();
            }
            catch (System.Exception ex)
            {
                _logger.Error($"exception: catched in Game.Dispose() [{ex.Message}]");
            }

            _notifyVerticalSyncOn.Dispose();
            _notifyVerticalSyncOff.Dispose();
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