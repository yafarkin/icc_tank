#define Log_Game_1

namespace TankGuiObserver2
{
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

        int _serverStartupIndex;
        string _serverString;
        GuiObserverCore _guiObserverCore;

        bool _isFPressed;
        bool _isEnterPressed;
        bool _isTabPressed;
        bool _onlyTankRendering;
        bool _isRenderNeaded;
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
            _guiObserverCore = new GuiObserverCore(_serverString, string.Empty);
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
            _logger.Debug("Ctor is working fine. [Game]");
            #endregion

            _renderForm.Activated += ActivatedMethod;
            _renderForm.Deactivate += DeactivateMethod;
            _gameRender = new GameRender(_serverString, _renderForm, _factory2D, _renderTarget2D);
            _gameRender.Map = new TankCommon.Objects.Map();
        }
        public void ActivatedMethod(object sender, System.EventArgs e)
        {
            _isRenderNeaded = true;
        }

        public void DeactivateMethod(object sender, System.EventArgs e)
        {
            _isRenderNeaded = false;
        }

        public void RunGame()
        {
            RenderLoop.Run(_renderForm, Draw);
        }

        public void Draw()
        {
            _renderTarget2D.BeginDraw();
            _logger.Debug("Frame draw: begin");

            if (_isRenderNeaded)
            {
                System.Collections.Generic.List<Key> pressedKeys =
                    _keyboard.GetCurrentState().PressedKeys;

                if (pressedKeys.Count > 0)
                {
                    foreach (Key pressedKey in pressedKeys)
                    {
                        switch (pressedKey)
                        {
                            case Key.F1:
                                {
                                    _renderForm.TopMost = true;
                                    _renderForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                                    _renderForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                                }
                                break;
                            case Key.F2:
                                {
                                    _renderForm.TopMost = false;
                                    _renderForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
                                    _renderForm.WindowState = System.Windows.Forms.FormWindowState.Normal;
                                }
                                break;
                            case Key.F3:
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
                                break;
                            case Key.F4:
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
                                break;
                            case Key.F:
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
                                break;
                            case Key.Return:
                                {
                                    if (_guiObserverCore?.Map != null) _isEnterPressed = true;
                                }
                                break;
                            case Key.Escape:
                                {
                                    _renderForm.Close();
                                }
                                break;
                            case Key.Tab:
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
                                break;
                            case Key.V:
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
                                break;
                            case Key.T:
                                {
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
                                break;
                            case Key.H:
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
                                break;
                        };

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
            if (!_guiObserverCore.IsWebSocketOpen)
            {
                Reset();
            }

            if (_guiObserverCore.WasMapUpdated)
            {
                if (_guiObserverCore.Map.Cells != null)
                {
                    _gameRender.Map = _guiObserverCore.Map;
                }
                else if (_guiObserverCore.Map != null)
                {
                    _gameRender.Map.InteractObjects = _guiObserverCore.Map.InteractObjects;
                }
            }

            //Draw a game
            if (_gameRender.Map != null &&
                _guiObserverCore.IsWebSocketOpen)
            {
                if (_isEnterPressed)
                {
                    _logger.Debug("Drawing a game");

                    if (!_gameRender.UIIsVisible && !_isTabPressed)
                    {
                        _logger.Debug("Drawing ui");
                        _gameRender.UIIsVisible = true;
                    }

                    _logger.Debug("call: DrawClientInfo()");
                    _gameRender.DrawClientInfo();

                    if (!_isTabPressed)
                    {
                        if (!_onlyTankRendering)
                        {
                            _gameRender.DrawMap();
                            _logger.Debug("call: DrawMap()");
                            _gameRender.DrawTanks(_gameRender.Map.InteractObjects);
                            _logger.Debug("call: DrawTanks()");
                            _gameRender.DrawGrass();
                            _logger.Debug("call: DrawGrass()");
                            _gameRender.DrawInteractiveObjects(_gameRender.Map.InteractObjects);
                            _logger.Debug("call: DrawInteractiveObjects()");
                        }
                        else
                        {
                            _gameRender.DrawTanks(_guiObserverCore.Map.InteractObjects);
                            _logger.Debug("call: DrawTanks()");
                        }

                    }
                }
                else if (_guiObserverCore.WasMapUpdated)
                {
                    _gameRender.DrawLogo();
                    _logger.Debug("call: DrawLogo()");
                }
            }

            if (_isFPressed)
            {
                _gameRender.DrawFPS();
                _logger.Debug("Press F to pay respect.");
            }

            try
            {
                _renderTarget2D.EndDraw();
            }
            catch
            {
                _logger.Debug("Catch exception from: _renderTarget2D.EndDraw()");
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
            _gameRender.GameSetDefault();
            _guiObserverCore.Restart(_serverString);
            _gameRender.Settings = _guiObserverCore.Settings;
            _gameRender.DrawWaitingLogo();
            ++_serverStartupIndex;
        }

        public void Dispose()
        {
            _notifyVerticalSyncOn.Dispose();
            _notifyVerticalSyncOff.Dispose();
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