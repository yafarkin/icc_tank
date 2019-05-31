#define COLORED_DEAD_0

namespace TankGuiObserver2
{
    using SharpDX;
    using SharpDX.Direct2D1;
    using SharpDX.DirectWrite;
    using SharpDX.DXGI;
    using SharpDX.Mathematics.Interop;
    using SharpDX.Windows;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Forms;
    using TankCommon.Enum;
    using TankCommon.Objects;
    using AlphaMode = SharpDX.Direct2D1.AlphaMode;

    struct ImmutableObject
    {
        public char BitmapIndex;
        public RawRectangleF Rectangle;
        public ImmutableObject(
            char bitmapIndex,
            RawRectangleF rectangle)
        {
            Rectangle = rectangle;
            BitmapIndex = bitmapIndex;
        }
    }

    struct DestuctiveWalls
    {
        public char ColorBrushIndex;
        public int RowIndex;
        public int ColumnIndex;
        public int BitmapIndex;
        public RawRectangleF Rectangle;
        public DestuctiveWalls(
            char colorBrushIndex,
            int row, int column,
            int bitmapIndex,
            RawRectangleF rectangle)
        {
            ColorBrushIndex = colorBrushIndex;
            RowIndex = row;
            ColumnIndex = column;
            BitmapIndex = bitmapIndex;
            Rectangle = rectangle;
        }
    }

    public class TextAnimation
    {
        private Stopwatch _textTimer;
        private string _cAnimatedString;
        private string _animatedString;

        public TextAnimation()
        {
            _textTimer = new Stopwatch();
            _textTimer.Start();
        }

        public void SetAnimatedString(string animatedString)
        {
            _animatedString = animatedString;
            _cAnimatedString = animatedString;
        }

        public string GetAnimatedString()
        {
            return _animatedString;
        }

        public void AnimationStart(int ms, string frame)
        {
            if (_textTimer.ElapsedMilliseconds > ms)
            {
                _animatedString += frame;
                if (_animatedString.Length > (_cAnimatedString.Length + 3))
                    _animatedString = _cAnimatedString;
                _textTimer.Reset();
                _textTimer.Start();
            }
        }
    }

    public class TextColorAnimation
    {
        private float _incriment;
        private RawColor4 _color;
        private Stopwatch _textTimer;

        public TextColorAnimation()
        {
            _incriment = -0.15f;
            _textTimer = new Stopwatch();
            _textTimer.Start();
        }

        public void AnimationStart(int ms, ref SolidColorBrush brush)
        {
            if (_textTimer.ElapsedMilliseconds > ms)
            {
                _color = brush.Color;

                if (_color.A > 1.0f)
                    _incriment = -0.05f;
                else if (_color.A < 0.2f)
                    _incriment = 0.05f;

                _color.A += _incriment;

                brush.Color = _color;
                _textTimer.Reset();
                _textTimer.Start();
            }
        }
    }

    class FpsCounter
    {
        private int fps;
        private int ms;
        public int FPSCounter { get; set; }
        public Stopwatch FPSTimer { get; set; }

        public FpsCounter()
        {
            FPSTimer = new Stopwatch();
            FPSTimer.Start();
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void CalculateFpsMs()
        {
            fps = (int)((1000.0f * FPSCounter) / FPSTimer.ElapsedMilliseconds);
            ms = (int)FPSTimer.ElapsedMilliseconds / FPSCounter;
            FPSTimer.Reset();
            FPSTimer.Stop();
            FPSTimer.Start();
            FPSCounter = 0;
        }

        public override string ToString()
        {
            return string.Format("{0}fps, {1}ms", fps, ms);
        }
    }

    public class CustomColorRenderer : SharpDX.DirectWrite.TextRendererBase
    {
        private RenderTarget _renderTarget;
        private SolidColorBrush _defaultBrush;

        public void AssignResources(RenderTarget renderTarget, SolidColorBrush defaultBrush)
        {
            _renderTarget = renderTarget;
            _defaultBrush = defaultBrush;
        }

        public override Result DrawGlyphRun(object clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, GlyphRun glyphRun, GlyphRunDescription glyphRunDescription, ComObject clientDrawingEffect)
        {
            SolidColorBrush sb = _defaultBrush;
            if (clientDrawingEffect != null && clientDrawingEffect is SolidColorBrush)
            {
                sb = (SolidColorBrush)clientDrawingEffect;
            }

            try
            {
                _renderTarget.DrawGlyphRun(new Vector2(baselineOriginX, baselineOriginY), glyphRun, sb, measuringMode);
                return Result.Ok;
            }
            catch
            {
                return Result.Fail;
            }
        }
    }

    class GameRender : System.IDisposable
    {
        //Game
        string _serverString;
        RenderForm _renderForm;
        RenderTarget _renderTarget2D;
        SharpDX.Direct2D1.Factory _factory2D;
        SharpDX.DirectWrite.Factory _directFactory;

        public Map Map { get; set; }
        public TankCommon.TankSettings Settings { get; set; }
        public int FPS => _fpsmsCounter.FPSCounter;

        //DrawMap
        bool _isMapSet;
        bool _isImmutableObjectsInitialized;
        bool _isDestructiveObjectsInitialized;
        int _mapWidth;
        int _mapHeight;
        float _zoomWidth;
        float _zoomHeight;
        RawColor4 _blackScreen;
        SolidColorBrush[] _mapObjectsColors;
        List<ImmutableObject> _immutableMapObjects;
        List<ImmutableObject> _immutableGrass;
        List<DestuctiveWalls> _destuctiveWallsObjects;

        //ClientInfo
        bool _setted;
        bool _centeredCI;
        bool _isStringResseted;
        int _index;
        int _nickDifLen;
        int _scoreDifLen;
        int _hpDifLen;
        RawVector2 _clientInfoLeftPoint;
        RawVector2 _clientInfoRightPoint;
        TextRange _tempRange;
        RectangleF _clientInfoAreaRect;
        RectangleF _cleintInfo;
        RectangleF _clientInfoTextRect;
        System.Text.StringBuilder _clientInfoStringBuilder;
        Label _clientInfoLabel;
        Label _clientInfoSessionTime;
        Label _clientInfoSessionServer;
        TextBox _ipResetIpTextBox;
        Button _resetIpBtn;
        TextFormat _clientInfoTextFormat;
        TextLayout _textLayout;
        CustomColorRenderer _textRenderer;
        List<TextRange> _clientInfoTableEffects;
        List<string> _paddingStrings;
        List<TankObject> _clientInfoTanks;

        //Entry screen
        RectangleF _logoTextRect;
        RectangleF _enterTextRect;
        RectangleF _statusTextRect;
        TextFormat _statusTextFormat;
        TextFormat _logoBrushTextFormat;
        TextAnimation _textAnimation;
        TextColorAnimation _textColorAnimation;

        //Fps
        RectangleF _fpsmsTextRect;
        RectangleF _fpsmsTextBackground;
        FpsCounter _fpsmsCounter;
        TextFormat _fpsmsTextFormat;

        //DrawTank()
        bool _isNickRendered;
        int _nickLength;
        float _width;
        float _difference;
        RawRectangleF _tankRectangle;
        RawRectangleF _nickRectangle;
        RawRectangleF _nickBackRectangle;
        TextFormat _nicknameTextFormat;

        //Interactive objects
        RawRectangleF _rawRectangleTemp;

        //Bitmap
        RawRectangleF _destinationRectangle;
        List<float> _tanksIncriments;
        List<float> _tanksOpacities;
        List<float> _upgradesIncriments;
        List<float> _upgradesOpacities;
        Bitmap _tankUpBitmap;
        Bitmap _tankDownBitmap;
        Bitmap _tankLeftBitmap;
        Bitmap _tankRightBitmap;
        Bitmap _bulletSpeedUpgradeBitmap;
        Bitmap _damageUpgradeBitmap;
        Bitmap _healthUpgradeBitmap;
        Bitmap _maxHpUpgradeBitmap;
        Bitmap _speedUpgradeBitmap;
        Bitmap _bulletUpBitmap;
        Bitmap _bulletDownBitmap;
        Bitmap _bulletLeftBitmap;
        Bitmap _bulletRightBitmap;
        Bitmap[] _bitmaps;
        Bitmap[] _bricksBitmaps;

        public bool UIIsVisible
        {
            get => (_clientInfoSessionServer.Visible && _clientInfoLabel.Visible && _clientInfoSessionTime.Visible);
            set
            {
                _clientInfoSessionServer.Visible = value;
                _clientInfoLabel.Visible = value;
                //we are not using it (now)
                _clientInfoSessionTime.Visible = false;
            }
        }

        public bool IpReconnectUIVisible
        {
            get => _ipResetIpTextBox.Visible && _resetIpBtn.Visible;
            set
            {
                _ipResetIpTextBox.Visible = value;
                _resetIpBtn.Visible = value;
                _resetIpBtn.IsAccessible = value;
                if (value == false)
                {
                    _resetIpBtn.Hide();
                }
            }
        }

        public void GameSetDefault()
        {
            _isMapSet = false;
            _isImmutableObjectsInitialized = false;
            _isDestructiveObjectsInitialized = false;
            Map = null;

            _immutableMapObjects.Clear();
            _immutableGrass.Clear();
            _destuctiveWallsObjects.Clear();
        }

        public bool IsNickRendered
        {
            get => _isNickRendered;
            set
            {
                _isNickRendered = value;
            }
        }

        public GameRender(
            string server,
            RenderForm renderForm,
            SharpDX.Direct2D1.Factory factory2D,
            RenderTarget renderTarget)
        {
            #region Itialization

            _serverString = server;
            _renderForm = renderForm;
            _factory2D = factory2D;
            _renderTarget2D = renderTarget;

            _immutableMapObjects = new List<ImmutableObject>();
            _immutableGrass = new List<ImmutableObject>();
            _destuctiveWallsObjects = new List<DestuctiveWalls>();
            _clientInfoTanks = new List<TankObject>(10);

            _tanksOpacities = new List<float>(100);
            _tanksIncriments = new List<float>(100);
            _upgradesIncriments = new List<float>(100);
            _upgradesOpacities = new List<float>(100);
            for (int i = 0; i < 100; i ++)
            {
                _tanksOpacities.Add(1.0f);
                _tanksIncriments.Add(1.0f);
                _upgradesIncriments.Add(1.0f);
                _upgradesOpacities.Add(1.0f);
            }
            //textRenderer
            _blackScreen = new RawColor4(0.0f, 0.0f, 0.0f, 1.0f);

            _mapObjectsColors = new SolidColorBrush[] {
            /*0*/ new SolidColorBrush(_renderTarget2D, Color.White),
            /*1*/ new SolidColorBrush(_renderTarget2D, Color.DarkRed),
            /*2*/ new SolidColorBrush(_renderTarget2D, Color.DarkBlue),
            /*3*/ new SolidColorBrush(_renderTarget2D, Color.GreenYellow),
            /*4*/ new SolidColorBrush(_renderTarget2D, Color.SandyBrown),
            /*5*/ new SolidColorBrush(_renderTarget2D, Color.Yellow), //bullet speed
            /*6*/ new SolidColorBrush(_renderTarget2D, Color.Red), //Damage
            /*7*/ new SolidColorBrush(_renderTarget2D, Color.Aquamarine), //Health
            /*8*/ new SolidColorBrush(_renderTarget2D, Color.Blue), //MaxHP
            /*9*/ new SolidColorBrush(_renderTarget2D, Color.CornflowerBlue),//Speed
            /*10*/ new SolidColorBrush(_renderTarget2D, Color.LightYellow), //Bullet
            /*11*/ new SolidColorBrush(_renderTarget2D, Color.White), //_defaultBrush
            /*12*/ new SolidColorBrush(_renderTarget2D, Color.Green), //_greenBrush
            /*13*/ new SolidColorBrush(_renderTarget2D, new RawColor4(0.3f, 0.3f, 0.3f, 0.9f)), //_backgroundBrush
            /*14*/ new SolidColorBrush(_renderTarget2D, new RawColor4(1.0f, 1.0f, 1.0f, 1.0f)), //_logoBrush
            /*15*/ new SolidColorBrush(_renderTarget2D, new RawColor4(0.28f, 0.88f, 0.23f, 1.0f)), //nickname
            /*16*/ new SolidColorBrush(_renderTarget2D, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f)) //nickname
            };

            #endregion

            #region DirectUI
            _fpsmsTextRect = new RectangleF(25, 5, 150, 30);
            _fpsmsTextBackground = new RectangleF(
                _fpsmsTextRect.Left, _fpsmsTextRect.Top,
                _fpsmsTextRect.Width, _fpsmsTextRect.Height);
            _logoTextRect = new RectangleF((float)_renderForm.Width / 5, (float)_renderForm.Height / 3,
                _renderForm.Width-400, 100);
            _enterTextRect = new RectangleF(
                _logoTextRect.X + 8*_logoTextRect.X/7,
                _renderForm.Height - (_renderForm.Height - _logoTextRect.Bottom - 200), 
                _renderForm.Width - _renderForm.Height, 30);
            _statusTextRect = new RectangleF(
                _logoTextRect.X + 5*_logoTextRect.X/6,
                _renderForm.Height - (_renderForm.Height - _logoTextRect.Bottom - 200),
                _renderForm.Width - _renderForm.Height, 30);
            _clientInfoAreaRect = new RectangleF(_renderForm.Height, 0, _renderForm.Width - _renderForm.Height, _renderForm.Height);
            _cleintInfo = new RectangleF(_renderForm.Height + 50, 100 + 50, 
                 (_renderForm.Width > _renderForm.Height) ? _renderForm.Width-_renderForm.Height-100 : 250
                 , 500);
            _clientInfoTextRect = new RectangleF(
                _clientInfoAreaRect.X + 0.39f * _clientInfoAreaRect.X,
                _clientInfoAreaRect.Y + 0.05f * _clientInfoAreaRect.Height, 300, 100);
            _clientInfoLeftPoint = new RawVector2(_clientInfoAreaRect.X,
                _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
            _clientInfoRightPoint = new RawVector2(
                _clientInfoAreaRect.X + _clientInfoAreaRect.Width,
                _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
            _tempRange = new TextRange();

            _tankRectangle = new RawRectangleF();
            _nickRectangle = new RawRectangleF();
            _nickBackRectangle = new RawRectangleF();
            _rawRectangleTemp = new RawRectangleF();

            _directFactory =
                new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared);
            _statusTextFormat = new TextFormat(_directFactory, "Arial", FontWeight.Regular, FontStyle.Normal, 30.0f);
            _fpsmsTextFormat = new TextFormat(_directFactory, "Arial", FontWeight.Regular, FontStyle.Normal, 24.0f);
            _logoBrushTextFormat =
                new TextFormat(_directFactory, "Arial", FontWeight.Normal, FontStyle.Italic, 180.0f);
            _nicknameTextFormat =
                new TextFormat(_directFactory, "Times New Roman", FontWeight.Normal, FontStyle.Italic, 16.0f);
            _clientInfoTextFormat = new TextFormat(_directFactory, "Consolas", 
                FontWeight.Normal, FontStyle.Normal, 24.0f);

            //advanced text renderer
            _textRenderer = new CustomColorRenderer();
            _textRenderer.AssignResources(_renderTarget2D, _mapObjectsColors[14]);
            
            _centeredCI = true;

            #endregion

            #region Animation
            _textAnimation = new TextAnimation();
            _textAnimation.SetAnimatedString($"Waiting for connection to {server}");
            _textColorAnimation = new TextColorAnimation();
            _fpsmsCounter = new FpsCounter();
            _fpsmsCounter.FPSCounter = 1000;
            #endregion

            #region UI
            _clientInfoStringBuilder = new System.Text.StringBuilder();
            _paddingStrings = new List<string>();
            {
                for (int i = 1; i < 30; i++)
                {
                    _paddingStrings.Add(new string('_', i));
                }
            }
            _clientInfoTableEffects = new List<TextRange>();

            _clientInfoLabel = new Label();
            _clientInfoLabel.Text = "Client info";
            _clientInfoLabel.Font = new System.Drawing.Font("Cambria", 30);
            _clientInfoLabel.BackColor = System.Drawing.Color.Green;
            _clientInfoLabel.ForeColor = System.Drawing.Color.White;
            _clientInfoLabel.Location = new System.Drawing.Point(_renderForm.Height + 250, 30);
            _clientInfoLabel.AutoSize = true;
            _clientInfoLabel.Visible = false;

            _clientInfoSessionTime = new Label();
            _clientInfoSessionTime.Width = 300;
            _clientInfoSessionTime.Height = 30;
            _clientInfoSessionTime.Text = "Session time: ";
            _clientInfoSessionTime.Font = new System.Drawing.Font("Cambria", 16);
            _clientInfoSessionTime.BackColor = System.Drawing.Color.Green;
            _clientInfoSessionTime.ForeColor = System.Drawing.Color.White;
            _clientInfoSessionTime.Location = new System.Drawing.Point(_renderForm.Height + 450, 40);
            _clientInfoSessionTime.AutoSize = false;
            _clientInfoSessionTime.Visible = false;

            _clientInfoSessionServer = new Label();
            _clientInfoSessionServer.Text = _serverString;
            _clientInfoSessionServer.Font = new System.Drawing.Font("Cambria", 16);
            _clientInfoSessionServer.BackColor = System.Drawing.Color.Green;
            _clientInfoSessionServer.ForeColor = System.Drawing.Color.White;
            _clientInfoSessionServer.Location = new System.Drawing.Point(_renderForm.Height + 20, 40);
            _clientInfoSessionServer.AutoSize = true;

            _ipResetIpTextBox = new TextBox();
            _ipResetIpTextBox.Font = new System.Drawing.Font("Cambria", 16);
            _ipResetIpTextBox.Text = _serverString;
            _ipResetIpTextBox.Width = 250;
            _ipResetIpTextBox.Location = new System.Drawing.Point(300, 0);
            _ipResetIpTextBox.Visible = true;
            _resetIpBtn = new Button();
            _resetIpBtn.Font = new System.Drawing.Font("Cambria", 16);
            _resetIpBtn.Name = "btnReset";
            _resetIpBtn.Text = "Reset ip";
            _resetIpBtn.Width = 250;
            _resetIpBtn.Location = new System.Drawing.Point(300, 35);
            _resetIpBtn.Click += ResetIPButton_Click;
            
            IpReconnectUIVisible = false;


            _renderForm.Controls.Add(_clientInfoSessionServer);
            _renderForm.Controls.Add(_clientInfoSessionTime);
            _renderForm.Controls.Add(_clientInfoLabel);
            _renderForm.Controls.Add(_ipResetIpTextBox);
            _renderForm.Controls.Add(_resetIpBtn);
            #endregion

            #region BitmapLoading

            _destinationRectangle = new RawRectangleF(0, 0, 100, 100);
            _bitmaps = new Bitmap[4];

            _bitmaps[0] = LoadFromFile(_renderTarget2D, @"img\wall.png");
            _bitmaps[1] = LoadFromFile(_renderTarget2D, @"img\water2.png");
            _bitmaps[2] = LoadFromFile(_renderTarget2D, @"img\Grass_T.png");
            _bitmaps[3] = LoadFromFile(_renderTarget2D, @"img\brick4k.png");

            _bricksBitmaps = new Bitmap[25];
            for (int i = 0; i < 25; i++)
            {
                _bricksBitmaps[i] = LoadFromFile(_renderTarget2D, @"img\bricks\"+i+".png");
            }

            _tankUpBitmap = LoadFromFile(_renderTarget2D, @"img\tank\tankUp.png");
            _tankDownBitmap = LoadFromFile(_renderTarget2D, @"img\tank\tankDown.png");
            _tankLeftBitmap = LoadFromFile(_renderTarget2D, @"img\tank\tankLeft.png");
            _tankRightBitmap = LoadFromFile(_renderTarget2D, @"img\tank\tankRight.png");

            _bulletSpeedUpgradeBitmap = LoadFromFile(_renderTarget2D, @"img\upgrade\BulletSpeed.png");
            _damageUpgradeBitmap = LoadFromFile(_renderTarget2D, @"img\upgrade\Damage.png");
            _healthUpgradeBitmap = LoadFromFile(_renderTarget2D, @"img\upgrade\Health.png");
            _maxHpUpgradeBitmap = LoadFromFile(_renderTarget2D, @"img\upgrade\MaxHp.png");
            _speedUpgradeBitmap = LoadFromFile(_renderTarget2D, @"img\upgrade\Speed.png");

            _bulletUpBitmap    = LoadFromFile(_renderTarget2D, @"img\bullets\Bullet_Up.png");
            _bulletDownBitmap  = LoadFromFile(_renderTarget2D, @"img\bullets\Bullet_Down.png");
            _bulletLeftBitmap  = LoadFromFile(_renderTarget2D, @"img\bullets\Bullet_Left.png");
            _bulletRightBitmap = LoadFromFile(_renderTarget2D, @"img\bullets\Bullet_Right.png");

            #endregion

        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public long GetElapsedMs()
        {
            return _fpsmsCounter.FPSTimer.ElapsedMilliseconds;
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        protected void FillBlock(RawRectangleF rectangle, SolidColorBrush brush)
        {
            _renderTarget2D.FillRectangle(rectangle, brush);
        }
        
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawFPS()
        {
            ++_fpsmsCounter.FPSCounter;
            if (_fpsmsCounter.FPSTimer.ElapsedMilliseconds > 1000)
            {
                _fpsmsCounter.CalculateFpsMs();
            }
            _renderTarget2D.FillRectangle(_fpsmsTextBackground, _mapObjectsColors[13]);
            _renderTarget2D.DrawText(_fpsmsCounter.ToString(), _fpsmsTextFormat, _fpsmsTextRect, _mapObjectsColors[12]);
        }
        
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawLogo()
        {
            _renderTarget2D.Clear(_blackScreen);
            _renderTarget2D.DrawText("Battle City v0.1",
                _logoBrushTextFormat, _logoTextRect, _mapObjectsColors[14]);
            _textColorAnimation.AnimationStart(100, ref _mapObjectsColors[11]);
            _renderTarget2D.DrawText("Press Enter to start a game",
                _statusTextFormat, _enterTextRect, _mapObjectsColors[11]);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawWaitingLogo()
        {
            _renderTarget2D.Clear(_blackScreen);
            _renderTarget2D.DrawText("Battle City v0.1",
                _logoBrushTextFormat, _logoTextRect, _mapObjectsColors[14]);
            _textAnimation.AnimationStart(300, ".");
            _renderTarget2D.DrawText(_textAnimation.GetAnimatedString(),
                _statusTextFormat, _statusTextRect, _mapObjectsColors[14]);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawMap()
        {
            // рисуем всю карту
            if (!_isMapSet && 
                Map != null &&
                Map.MapWidth > 0 &&
                Map.MapHeight > 0)
            {
                _isMapSet = true;
                
                _mapWidth  = Map.MapWidth /*Map.Cells.GetLength(0)*/;
                _mapHeight = Map.MapHeight/*Map.Cells.GetLength(1)*/;
                _zoomWidth = _renderTarget2D.Size.Height / _mapWidth;
                _zoomHeight = _renderTarget2D.Size.Height / _mapHeight;
            }

            //неизменяемые
            if (!_isImmutableObjectsInitialized)
            {
                _isImmutableObjectsInitialized = true;

                int i, j;

                //#### ################ ###########
                //#### текстуры блоками 5 на 5 ####
                //#### ################ ###########
                int blocksInARow = _mapWidth / 5;
                int blocksInACol = _mapHeight / 5;
                List<SharpDX.Point> walls = new List<SharpDX.Point>();
                List<SharpDX.Point> water = new List<SharpDX.Point>();
                List<SharpDX.Point> grass = new List<SharpDX.Point>();
                for (int r = 0; r < blocksInACol; r++)
                {
                    for (int c = 0; c < blocksInARow; c++)
                    {
                        for (i = (5 * r); i < (5 * r + 5); i++)
                        {
                            for (j = (5 * c); j < (5 * c + 5); j++)
                            {
                                if (Map[i, j] == CellMapType.Wall)
                                {
                                    walls.Add(new SharpDX.Point(i, j));
                                }
                                else if (Map[i, j] == CellMapType.Water)
                                {
                                    water.Add(new SharpDX.Point(i, j));
                                }
                                if (Map[i, j] == CellMapType.Grass)
                                {
                                    grass.Add(new SharpDX.Point(i, j));
                                }
                            }
                        }
                        if (walls.Count == 25)
                        {
                            _rawRectangleTemp.Left = 5 * c * _zoomWidth;
                            _rawRectangleTemp.Top = 5 * r * _zoomHeight;
                            _rawRectangleTemp.Right = (5 * c + 5) * _zoomWidth;
                            _rawRectangleTemp.Bottom = (5 * r + 5) * _zoomHeight;
                            _immutableMapObjects.Add(new ImmutableObject((char)0, _rawRectangleTemp));
                            walls.Clear();
                        }
                        if (water.Count == 25)
                        {
                            _rawRectangleTemp.Left = 5 * c * _zoomWidth;
                            _rawRectangleTemp.Top = 5 * r * _zoomHeight;
                            _rawRectangleTemp.Right = (5 * c + 5) * _zoomWidth;
                            _rawRectangleTemp.Bottom = (5 * r + 5) * _zoomHeight;
                            _immutableMapObjects.Add(new ImmutableObject((char)1, _rawRectangleTemp));
                            water.Clear();
                        }
                        if (grass.Count == 25)
                        {
                            _rawRectangleTemp.Left = 5 * c * _zoomWidth;
                            _rawRectangleTemp.Top = 5 * r * _zoomHeight;
                            _rawRectangleTemp.Right = (5 * c + 5) * _zoomWidth;
                            _rawRectangleTemp.Bottom = (5 * r + 5) * _zoomHeight;
                            _immutableGrass.Add(new ImmutableObject((char)2, _rawRectangleTemp));
                            grass.Clear();
                        }
                    }
                }

                walls = null;
                water = null;
                grass = null;
            }
            else
            {
                foreach (var obj in _immutableMapObjects)
                {
                    _renderTarget2D.DrawBitmap(_bitmaps[obj.BitmapIndex], obj.Rectangle, 
                        1.0f, BitmapInterpolationMode.Linear);
                }
            }

            if (!_isDestructiveObjectsInitialized)
            {
                _isDestructiveObjectsInitialized = true;
                int i, j, index = 0,
                    blocksInARow = _mapWidth / 5,
                    blocksInACol = _mapHeight / 5;
                for (int r = 0; r < blocksInACol; r++)
                {
                    for (int c = 0; c < blocksInARow; c++)
                    {
                        for (i = (5 * r); i < (5 * r + 5); i++)
                        {
                            for (j = (5 * c); j < (5 * c + 5); j++)
                            {
                                if (Map[i, j] == CellMapType.DestructiveWall)
                                {
                                    _rawRectangleTemp.Left = j * _zoomWidth;
                                    _rawRectangleTemp.Top = i * _zoomHeight;
                                    _rawRectangleTemp.Right = j * _zoomWidth + _zoomWidth;
                                    _rawRectangleTemp.Bottom = i * _zoomHeight + _zoomHeight;
                                    _destuctiveWallsObjects.Add(new DestuctiveWalls((char)1, i, j, 
                                        (index % 25), _rawRectangleTemp));
                                    _renderTarget2D.DrawBitmap(_bricksBitmaps[index % 25], _rawRectangleTemp,
                                        1.0f, BitmapInterpolationMode.Linear);
                                    ++index;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                int index = 0;
                foreach (var obj in _destuctiveWallsObjects)
                {
                    if (Map[obj.RowIndex, obj.ColumnIndex] == CellMapType.DestructiveWall)
                    {
                        _renderTarget2D.DrawBitmap(_bricksBitmaps[obj.BitmapIndex], obj.Rectangle, 
                            1.0f, BitmapInterpolationMode.Linear);
                        ++index;
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawGrass()
        {
            foreach (var obj in _immutableGrass)
            {
                _renderTarget2D.DrawBitmap(
                    _bitmaps[obj.BitmapIndex], 
                    obj.Rectangle, 
                    1.0f, BitmapInterpolationMode.Linear);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawInteractiveObjects(List<BaseInteractObject> baseInteractObjects)
        {
            int upgradeIndex = 0;
            foreach (var obj in baseInteractObjects)
            {
                if (obj is UpgradeInteractObject upgradeObject)
                {
                    if (DateTime.Now >= upgradeObject.SpawnTime.AddSeconds(27))
                    {
                        if (_upgradesOpacities[upgradeIndex] >= 1.0f)
                            _upgradesIncriments[upgradeIndex] = -0.01f;
                        else if (_upgradesOpacities[upgradeIndex] < 0.0f)
                            _upgradesIncriments[upgradeIndex] = 0.01f;
                        _upgradesOpacities[upgradeIndex] += _upgradesIncriments[upgradeIndex];
                    }

                    switch (upgradeObject.Type)
                    {
                        case UpgradeType.BulletSpeed:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                _renderTarget2D.DrawBitmap(_bulletSpeedUpgradeBitmap, _rawRectangleTemp, 
                                    _upgradesOpacities[upgradeIndex], BitmapInterpolationMode.Linear);
                            }
                            break;
                        case UpgradeType.Damage:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                _renderTarget2D.DrawBitmap(_damageUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], BitmapInterpolationMode.Linear);
                            }
                            break;
                        case UpgradeType.Health:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                _renderTarget2D.DrawBitmap(_healthUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], BitmapInterpolationMode.Linear);
                            }
                            break;
                        case UpgradeType.MaxHp:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                _renderTarget2D.DrawBitmap(_maxHpUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], BitmapInterpolationMode.Linear);
                            }
                            break;
                        case UpgradeType.Speed:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                _renderTarget2D.DrawBitmap(_speedUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], BitmapInterpolationMode.Linear);
                            }
                            break;
                    }
                    ++upgradeIndex;
                }
                else if (obj is BulletObject bulletObject)
                {
                    _rawRectangleTemp.Left = Convert.ToSingle(bulletObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                    _rawRectangleTemp.Top = Convert.ToSingle(bulletObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                    _rawRectangleTemp.Right = Convert.ToSingle(bulletObject.Rectangle.LeftCorner.Left + bulletObject.Rectangle.Width) * _zoomWidth;
                    _rawRectangleTemp.Bottom = Convert.ToSingle(bulletObject.Rectangle.LeftCorner.Top + bulletObject.Rectangle.Height) * _zoomHeight;

                    if (bulletObject.Direction == DirectionType.Down)
                    {
                        _renderTarget2D.DrawBitmap(
                            _bulletDownBitmap,
                            _rawRectangleTemp,
                            1.0f, BitmapInterpolationMode.Linear);
                    }
                    else if (bulletObject.Direction == DirectionType.Up)
                    {
                        _renderTarget2D.DrawBitmap(
                            _bulletUpBitmap,
                            _rawRectangleTemp,
                            1.0f, BitmapInterpolationMode.Linear);
                    }
                    else if (bulletObject.Direction == DirectionType.Left)
                    {
                        _renderTarget2D.DrawBitmap(
                            _bulletLeftBitmap,
                            _rawRectangleTemp,
                            1.0f, BitmapInterpolationMode.Linear);
                    }
                    else if (bulletObject.Direction == DirectionType.Right)
                    {
                        _renderTarget2D.DrawBitmap(
                            _bulletRightBitmap,
                            _rawRectangleTemp,
                            1.0f, BitmapInterpolationMode.Linear);
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawTanks(List<BaseInteractObject> baseInteractObjects)
        {
            int tankIndex = 0;
            foreach (var obj in baseInteractObjects)
            {
                if (obj is TankObject tankObject)
                {
                    if (!tankObject.IsDead)
                    {
                        _tankRectangle.Left = Convert.ToSingle(tankObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                        _tankRectangle.Top = Convert.ToSingle(tankObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                        _tankRectangle.Right = Convert.ToSingle(tankObject.Rectangle.LeftCorner.Left + tankObject.Rectangle.Width) * _zoomWidth;
                        _tankRectangle.Bottom = Convert.ToSingle(tankObject.Rectangle.LeftCorner.Top + tankObject.Rectangle.Height) * _zoomHeight;

                        if (_isNickRendered)
                        {
                            _nickLength = tankObject.Nickname.Length;
                            _width = _tankRectangle.Right - _tankRectangle.Left;
                            _difference = (_width - _nickLength * _zoomWidth) / 2;
                            _nickRectangle.Left = _tankRectangle.Left + _difference;
                            _nickRectangle.Right = _tankRectangle.Right - _difference + 3 * _zoomWidth;
                            _nickRectangle.Top = _tankRectangle.Top - 3 * _zoomHeight;
                            _nickRectangle.Bottom = _tankRectangle.Top - _zoomHeight;

                            _nickBackRectangle.Left = _nickRectangle.Left - 5;
                            _nickBackRectangle.Right = _nickRectangle.Right;
                            _nickBackRectangle.Top = _nickRectangle.Top - 3;
                            _nickBackRectangle.Bottom = _nickRectangle.Bottom + 5;

                            _renderTarget2D.FillRectangle(_nickBackRectangle, _mapObjectsColors[13]);
                            _renderTarget2D.DrawText(tankObject.Nickname,
                                _nicknameTextFormat, _nickRectangle, _mapObjectsColors[15]);
                        }

                        if (tankObject.IsInvulnerable)
                        {
                            if (_tanksOpacities[tankIndex] >= 1.0f)
                                _tanksIncriments[tankIndex] = -0.01f;
                            else if (_tanksOpacities[tankIndex] < 0.0f)
                                _tanksIncriments[tankIndex] = 0.01f;
                            _tanksOpacities[tankIndex] += _tanksIncriments[tankIndex];
                        }
                        else
                        {
                            _tanksOpacities[tankIndex] = 1.0f;
                        }
                        
                        if (tankObject.Direction == DirectionType.Up)
                        {
                            _renderTarget2D.DrawBitmap(_tankUpBitmap, _tankRectangle, _tanksOpacities[tankIndex], BitmapInterpolationMode.Linear);
                        }
                        else if (tankObject.Direction == DirectionType.Down)
                        {
                            _renderTarget2D.DrawBitmap(_tankDownBitmap, _tankRectangle, _tanksOpacities[tankIndex], BitmapInterpolationMode.Linear);
                        }
                        else if (tankObject.Direction == DirectionType.Left)
                        {
                            _renderTarget2D.DrawBitmap(_tankLeftBitmap, _tankRectangle, _tanksOpacities[tankIndex], BitmapInterpolationMode.Linear);
                        }
                        else if (tankObject.Direction == DirectionType.Right)
                        {
                            _renderTarget2D.DrawBitmap(_tankRightBitmap, _tankRectangle, _tanksOpacities[tankIndex], BitmapInterpolationMode.Linear);
                        }
                    }
                    ++tankIndex;
                }
            }
        }
        
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void CenterClientInfo()
        {
            if (_centeredCI)
            {
                _centeredCI = false;
                _clientInfoSessionServer.Location = new System.Drawing.Point(50, 40);
                _clientInfoLabel.Location = new System.Drawing.Point(
                    _renderForm.Width / 2-150, 30);
                _clientInfoAreaRect = new RectangleF(0, 0, 
                    _renderForm.Width, _renderForm.Height);
                _clientInfoLeftPoint =
                    new RawVector2(0,
                                   _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
                _clientInfoRightPoint = new RawVector2(
                                    _renderForm.Width,
                                    _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
                _cleintInfo = new RectangleF(
                    (float)_renderForm.Width / 3,
                    100 + 50, 
                    _renderForm.Width - _renderForm.Height - 100, 500);
            }
            else
            {
                _centeredCI = true;
                _clientInfoSessionServer.Location = new System.Drawing.Point(_renderForm.Height + 20, 40);
                _clientInfoLabel.Location = new System.Drawing.Point(_renderForm.Height + 250, 30);
                _clientInfoAreaRect = new RectangleF(_renderForm.Height, 0, 
                    _renderForm.Width - _renderForm.Height, _renderForm.Height);
                _clientInfoLeftPoint = 
                    new RawVector2(_clientInfoAreaRect.X,
                                    _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
                _clientInfoRightPoint = new RawVector2(
                                    _clientInfoAreaRect.X + _clientInfoAreaRect.Width,
                                    _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
                _cleintInfo = new RectangleF(_renderForm.Height + 50, 
                    100 + 50, 
                    _renderForm.Width - _renderForm.Height - 100, 500);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawClientInfo()
        {
            _renderTarget2D.Clear(_blackScreen);
            _renderTarget2D.FillRectangle(_clientInfoAreaRect, _mapObjectsColors[13]);
            _renderTarget2D.DrawLine(_clientInfoLeftPoint, _clientInfoRightPoint, _mapObjectsColors[12], 10);
            _clientInfoSessionTime.Text = $"Session time: {Settings?.SessionTime.ToString()}";
            _clientInfoTanks.AddRange(
                Map?.InteractObjects.OfType<TankObject>().OrderByDescending(t => t.Score).ToList());
            
            _index = 1;
            _nickDifLen = 0;
            _scoreDifLen = 0;
            _hpDifLen = 0;
            _clientInfoStringBuilder.Append("Id  Nickname        Score      Hp    Lives\n");
            foreach (var tank in _clientInfoTanks)
            {
                int cisbLength = _clientInfoStringBuilder.Length;
                string score = tank.Score.ToString();
                string hp = tank.Hp.ToString();
                string index = _index.ToString();
                _nickDifLen = 15 - tank.Nickname.Length;
                _scoreDifLen = 7 - score.Length;
                _hpDifLen = 7 - hp.Length;
                _clientInfoStringBuilder.AppendFormat("{0} {1}   {2} {3} {4}\n",
                    _index < 10 ? index + " " : index, 
                    (_nickDifLen <= 0) ? tank.Nickname : tank.Nickname + _paddingStrings[_nickDifLen],
                    (_scoreDifLen <= 0) ? score : score + _paddingStrings[_scoreDifLen],
                    (_hpDifLen <= 0) ? hp : hp + _paddingStrings[_hpDifLen],
                    tank.Lives.ToString());
#if COLORED_DEAD_1
                if (tank.IsDead)
                {
                    _clientInfoTableEffects.Add(new TextRange(cisbLength, _clientInfoStringBuilder.Length));
                }
#endif
                ++_index;
            }
            
            //Deleting '_' (made them invisible)
            _setted = false;
            _index = 0;
            _textLayout = new TextLayout(
                _directFactory, 
                _clientInfoStringBuilder.ToString(), 
                _clientInfoTextFormat, 
                _cleintInfo.Width, _cleintInfo.Height);
            for (int i = 0; i < (_clientInfoStringBuilder.Length-1); i++)
            {
                if (_clientInfoStringBuilder[i] == '_')
                {
                    if (!_setted)
                    {
                        _index = i;
                    }
                    _setted = true;

                    if (_setted && _clientInfoStringBuilder[i + 1] != '_')
                    {
                        _tempRange.StartPosition = _index;
                        _tempRange.Length = i;
                        _textLayout?.SetDrawingEffect(_mapObjectsColors[16], _tempRange);
                        _tempRange.StartPosition = i + 1;
                        _tempRange.Length = i + 1;
                        _textLayout?.SetDrawingEffect(_mapObjectsColors[14], _tempRange);
                        _setted = false;
                    }
                }
            }

#if COLORED_DEAD_1
            for (int i = 0; i < _clientInfoTableEffects.Count; i++)
            {
                _textLayout.SetDrawingEffect(_mapObjectsColors[6], _clientInfoTableEffects[i]);
            }
#endif

            //Draw
            _textLayout.Draw(_textRenderer, _cleintInfo.X, _cleintInfo.Y);
            _textLayout.Dispose();

            _clientInfoStringBuilder.Clear();
            _clientInfoTanks.Clear();
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

        public void ResetIPButton_Click(
            object sender, EventArgs e)
        {
            _isStringResseted = true;
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public bool ResetIp(ref string serverString)
        {
            if (_isStringResseted)
            {
                _isStringResseted = false;
                serverString = _ipResetIpTextBox.Text;
                _serverString = serverString;
                _textAnimation.SetAnimatedString($"Waiting for connection to {_serverString}");
                _clientInfoSessionServer.Text = _serverString;
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            for (int mapIndex = 0; mapIndex < _mapObjectsColors.Length; mapIndex++)
            {
                _mapObjectsColors[mapIndex].Dispose();
            }

            for (int i = 0; i < _bitmaps.Length; i++)
            {
                _bitmaps[i]?.Dispose();
            }

            for (int i = 0; i < _bricksBitmaps.Length; i++)
            {
                _bricksBitmaps[i]?.Dispose();
            }

            _textLayout?.Dispose();
            _tankUpBitmap?.Dispose();
            _tankDownBitmap?.Dispose();
            _tankLeftBitmap?.Dispose();
            _tankRightBitmap?.Dispose();

            _bulletSpeedUpgradeBitmap?.Dispose();
            _damageUpgradeBitmap?.Dispose();
            _healthUpgradeBitmap?.Dispose();
            _maxHpUpgradeBitmap?.Dispose();
            _speedUpgradeBitmap?.Dispose();
        }

    }
}