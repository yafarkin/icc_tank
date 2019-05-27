namespace TankGuiObserver2
{
    using System;
    using System.Windows.Forms;
    using System.Collections.Generic;

    using SharpDX;
    using SharpDX.Windows;
    using SharpDX.Direct2D1;
    using SharpDX.DirectWrite;
    using SharpDX.Mathematics.Interop;

    using TankCommon.Enum;
    using TankCommon.Objects;
    using System.Diagnostics;
    using System.Linq;
    using SharpDX.DXGI;

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
        public RawRectangleF Rectangle;
        public DestuctiveWalls(
            char colorBrushIndex,
            int row, int column,
            RawRectangleF rectangle)
        {
            ColorBrushIndex = colorBrushIndex;
            RowIndex = row;
            ColumnIndex = column;
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

    class GameRender : System.IDisposable
    {
        RenderForm RenderForm;
        RenderTarget RenderTarget2D;
        SharpDX.Direct2D1.Factory _factory2D;

        //DrawMap
        bool _isMapSet;
        float _zoomWidth;
        float _zoomHeight;
        bool _isImmutableObjectsInitialized;
        bool _isDestructiveObjectsInitialized;

        int _mapWidth;
        int _mapHeight;
        public Map Map { get; set; }
        public TankCommon.TankSettings Settings { get; set; }
        RawColor4 _blackScreen;
        List<ImmutableObject> _immutableMapObjects;
        List<ImmutableObject> _immutableGrass;
        List<DestuctiveWalls> _destuctiveWallsObjects;
        SolidColorBrush[] _mapObjectsColors;

        //ClientInfo
        public bool UIIsVisible
        {
            get => (_sessionTime.Visible && _clientInfoLabel.Visible && _dgv.Visible); 
            set
            {
                _sessionTime.Visible = value;
                _clientInfoLabel.Visible = value;
                _dgv.Visible = value;
            }
        }
        Label _clientInfoLabel;
        Label _sessionTime;
        DataGridView _dgv;
        List<TankObject> _clientInfoTanks;
        RawVector2 _clientInfoLeftPoint;
        RawVector2 _clientInfoRightPoint;
        RectangleF _clientInfoAreaRect;
        RectangleF _clientInfoTextRect;

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
        RawRectangleF dstinationRectangle;
        List<float> _tanksIncriments;
        List<float> _tanksOpacities;
        List<float> _upgradesIncriments;
        List<float> _upgradesOpacities;
        BitmapInterpolationMode interpolationMode;
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
        Bitmap[] _bitmaps;

        public GameRender(
            string server,
            RenderForm renderForm,
            SharpDX.Direct2D1.Factory factory2D,
            RenderTarget renderTarget)
        {
            #region Itialization

            RenderForm = renderForm;
            _factory2D = factory2D;
            RenderTarget2D = renderTarget;

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
            /*0*/ new SolidColorBrush(RenderTarget2D, Color.White),
            /*1*/ new SolidColorBrush(RenderTarget2D, Color.DarkRed),
            /*2*/ new SolidColorBrush(RenderTarget2D, Color.DarkBlue),
            /*3*/ new SolidColorBrush(RenderTarget2D, Color.GreenYellow),
            /*4*/ new SolidColorBrush(RenderTarget2D, Color.SandyBrown),
            /*5*/ new SolidColorBrush(RenderTarget2D, Color.Yellow), //bullet speed
            /*6*/ new SolidColorBrush(RenderTarget2D, Color.Red), //Damage
            /*7*/ new SolidColorBrush(RenderTarget2D, Color.Aquamarine), //Health
            /*8*/ new SolidColorBrush(RenderTarget2D, Color.Blue), //MaxHP
            /*9*/ new SolidColorBrush(RenderTarget2D, Color.CornflowerBlue),//Speed
            /*10*/ new SolidColorBrush(RenderTarget2D, Color.LightYellow), //Bullet
            /*11*/ new SolidColorBrush(RenderTarget2D, Color.White), //_defaultBrush
            /*12*/ new SolidColorBrush(RenderTarget2D, Color.Green), //_greenBrush
            /*13*/ new SolidColorBrush(RenderTarget2D, new RawColor4(0.3f, 0.3f, 0.3f, 0.9f)), //_backgroundBrush
            /*14*/ new SolidColorBrush(RenderTarget2D, new RawColor4(1.0f, 1.0f, 1.0f, 1.0f)), //_logoBrush
            /*15*/ new SolidColorBrush(RenderTarget2D, new RawColor4(0.28f, 0.88f, 0.23f, 1.0f)) //nickname
            };

            #endregion

            #region DirectUI
            _fpsmsTextRect = new RectangleF(25, 5, 150, 30);
            _fpsmsTextBackground = new RectangleF(
                _fpsmsTextRect.Left, _fpsmsTextRect.Top,
                _fpsmsTextRect.Width, _fpsmsTextRect.Height);
            _logoTextRect = new RectangleF((float)RenderForm.Width / 5, (float)RenderForm.Height / 3, 1500, 100);
            _enterTextRect = new RectangleF(
                _logoTextRect.X + 8*_logoTextRect.X/7,
                RenderForm.Height - (RenderForm.Height - _logoTextRect.Bottom - 200), 800, 30);
            _statusTextRect = new RectangleF(
                _logoTextRect.X + 5*_logoTextRect.X/6,
                RenderForm.Height - (RenderForm.Height - _logoTextRect.Bottom - 200), 800, 30);
            _clientInfoAreaRect = new RectangleF(1080, 0, 1920 - 1080, 1080);
            _clientInfoTextRect = new RectangleF(
                _clientInfoAreaRect.X + 0.39f * _clientInfoAreaRect.X,
                _clientInfoAreaRect.Y + 0.05f * _clientInfoAreaRect.Height, 300, 100);
            _clientInfoLeftPoint = new RawVector2(_clientInfoAreaRect.X,
                _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);
            _clientInfoRightPoint = new RawVector2(
                _clientInfoAreaRect.X + _clientInfoAreaRect.Width,
                _clientInfoTextRect.Y + 0.6f * _clientInfoTextRect.Height);

            _tankRectangle = new RawRectangleF();
            _nickRectangle = new RawRectangleF();
            _nickBackRectangle = new RawRectangleF();
            _rawRectangleTemp = new RawRectangleF();

            SharpDX.DirectWrite.Factory directFactory =
                new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared);
            _statusTextFormat = new TextFormat(directFactory, "Arial", FontWeight.Regular, FontStyle.Normal, 30.0f);
            _fpsmsTextFormat = new TextFormat(directFactory, "Arial", FontWeight.Regular, FontStyle.Normal, 24.0f);
            _logoBrushTextFormat =
                new TextFormat(directFactory, "Arial", FontWeight.Normal, FontStyle.Italic, 180.0f);
            _nicknameTextFormat =
                new TextFormat(directFactory, "Times New Roman", FontWeight.Normal, FontStyle.Italic, 16.0f);
            #endregion

            #region Animation
            _textAnimation = new TextAnimation();
            _textAnimation.SetAnimatedString($"Waiting for connection to {server}");
            _textColorAnimation = new TextColorAnimation();
            _fpsmsCounter = new FpsCounter();
            _fpsmsCounter.FPSCounter = 1000;
            #endregion
            
            #region UI
            _clientInfoLabel = new Label();
            _clientInfoLabel.Text = "Client info";
            _clientInfoLabel.Font = new System.Drawing.Font("Cambria", 30);
            _clientInfoLabel.BackColor = System.Drawing.Color.Green;
            _clientInfoLabel.ForeColor = System.Drawing.Color.White;
            _clientInfoLabel.Location = new System.Drawing.Point(1380, 30);
            _clientInfoLabel.AutoSize = true;
            _clientInfoLabel.Visible = false;

            _sessionTime = new Label();
            _sessionTime.Text = server;
            _sessionTime.Font = new System.Drawing.Font("Cambria", 16);
            _sessionTime.BackColor = System.Drawing.Color.Green;
            _sessionTime.ForeColor = System.Drawing.Color.White;
            _sessionTime.Location = new System.Drawing.Point(1100, 40);
            _sessionTime.AutoSize = true;
            _sessionTime.Visible = false;
            
            _dgv = new DataGridView();
            _dgv.Width = 800; //840 (1920)
            _dgv.Height = 350; //1080
            _dgv.AutoSize = true;
            _dgv.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, 
                System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            _dgv.Location = new System.Drawing.Point(1100, 150);
            _dgv.Name = "dataTab";
            _dgv.Text = "Статус:";
            _dgv.Visible = false;
            _dgv.Columns.Add("id", "Id");
            _dgv.Columns.Add("nick", "Nickname");
            _dgv.Columns.Add("score", "Score");
            _dgv.Columns.Add("hp", "Hp");
            _dgv.Columns.Add("lives", "Lives");
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.Rows.Add(); _dgv.Rows.Add();
            _dgv.AutoSize = false;
            _dgv.ReadOnly = false;
            _dgv.AllowUserToOrderColumns = false;
            _dgv.AllowDrop = false;
            _dgv.AllowUserToResizeColumns = false;
            _dgv.AllowUserToResizeRows = false;
            _dgv.AllowUserToDeleteRows = false;
            _dgv.AllowUserToDeleteRows = false;
            _dgv.AllowUserToOrderColumns = false;
            _dgv.IsAccessible = false;
            _dgv.ReadOnly = true;
            _dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _dgv.MultiSelect = false;
            
            for (int i = 0; i < _dgv.ColumnCount; i++)
            {
                _dgv.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            
            RenderForm.Controls.Add(_sessionTime);
            RenderForm.Controls.Add(_clientInfoLabel);
            RenderForm.Controls.Add(_dgv);
            #endregion

            //Loading resources
            LoadResources();

        }
        
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public long GetElapsedMs()
        {
            return _fpsmsCounter.FPSTimer.ElapsedMilliseconds;
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        protected void FillBlock(RawRectangleF rectangle, SolidColorBrush brush)
        {
            RenderTarget2D.FillRectangle(rectangle, brush);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawFPS()
        {
            ++_fpsmsCounter.FPSCounter;
            if (_fpsmsCounter.FPSTimer.ElapsedMilliseconds > 1000)
            {
                _fpsmsCounter.CalculateFpsMs();
            }
            RenderTarget2D.FillRectangle(_fpsmsTextBackground, _mapObjectsColors[13]);
            RenderTarget2D.DrawText(_fpsmsCounter.ToString(), _fpsmsTextFormat, _fpsmsTextRect, _mapObjectsColors[12]);
        }
        
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawLogo()
        {
            RenderTarget2D.Clear(_blackScreen);
            _textColorAnimation.AnimationStart(100, ref _mapObjectsColors[11]);
            RenderTarget2D.DrawText("Battle City v0.1",
                _logoBrushTextFormat, _logoTextRect, _mapObjectsColors[14]);
            RenderTarget2D.DrawText("Press Enter to start a game",
                _statusTextFormat, _enterTextRect, _mapObjectsColors[11]);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawWaitingLogo()
        {
            RenderTarget2D.Clear(_blackScreen);
            RenderTarget2D.DrawText("Battle City v0.1",
                _logoBrushTextFormat, _logoTextRect, _mapObjectsColors[14]);
            _textAnimation.AnimationStart(300, ".");
            RenderTarget2D.DrawText(_textAnimation.GetAnimatedString(),
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
                _zoomWidth = (float)1080 / _mapWidth;
                _zoomHeight = RenderTarget2D.Size.Height / _mapHeight;
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
                    RenderTarget2D.DrawBitmap(_bitmaps[obj.BitmapIndex], obj.Rectangle, 
                        1.0f, BitmapInterpolationMode.Linear);
                }
            }

            if (!_isDestructiveObjectsInitialized)
            {
                _isDestructiveObjectsInitialized = true;
                for (var i = 5; i < (_mapHeight - 5); i++)
                {
                    for (var j = 5; j < (_mapWidth - 5); j++)
                    {
                        var c = Map[i, j];
                        if (c == CellMapType.DestructiveWall)
                        {
                            _rawRectangleTemp.Left = j * _zoomWidth;
                            _rawRectangleTemp.Top = i * _zoomHeight;
                            _rawRectangleTemp.Right = j * _zoomWidth + _zoomWidth;
                            _rawRectangleTemp.Bottom = i * _zoomHeight + _zoomHeight;
                            _destuctiveWallsObjects.Add(new DestuctiveWalls((char)1, i, j, _rawRectangleTemp));
                            RenderTarget2D.DrawBitmap(_bitmaps[3], _rawRectangleTemp, 
                                1.0f, BitmapInterpolationMode.Linear);
                        }
                    }
                }
            }
            else
            {
                foreach (var obj in _destuctiveWallsObjects)
                {
                    if (Map[obj.RowIndex, obj.ColumnIndex] == CellMapType.DestructiveWall)
                    {
                        RenderTarget2D.DrawBitmap(_bitmaps[3], obj.Rectangle, 
                            1.0f, BitmapInterpolationMode.Linear);
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawGrass()
        {
            foreach (var obj in _immutableGrass)
            {
                RenderTarget2D.DrawBitmap(
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
                    //if (DateTime.Now > upgradeObject.SpawnTime.AddSeconds(27))
                    //{
                    //    if (_upgradesOpacities[upgradeIndex] >= 1.0f)
                    //        _upgradesIncriments[upgradeIndex] = -0.01f;
                    //    else if (_upgradesOpacities[upgradeIndex] < 0.0f)
                    //        _upgradesIncriments[upgradeIndex] = 0.01f;
                    //    _upgradesOpacities[upgradeIndex] += _upgradesIncriments[upgradeIndex];
                    //}

                    switch (upgradeObject.Type)
                    {
                        case UpgradeType.BulletSpeed:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                RenderTarget2D.DrawBitmap(_bulletSpeedUpgradeBitmap, _rawRectangleTemp, 
                                    _upgradesOpacities[upgradeIndex], interpolationMode);
                            }
                            break;
                        case UpgradeType.Damage:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                RenderTarget2D.DrawBitmap(_damageUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], interpolationMode);
                            }
                            break;
                        case UpgradeType.Health:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                RenderTarget2D.DrawBitmap(_healthUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], interpolationMode);
                            }
                            break;
                        case UpgradeType.MaxHp:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                RenderTarget2D.DrawBitmap(_maxHpUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], interpolationMode);
                            }
                            break;
                        case UpgradeType.Speed:
                            {
                                _rawRectangleTemp.Left = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left) * _zoomWidth;
                                _rawRectangleTemp.Top = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top) * _zoomHeight;
                                _rawRectangleTemp.Right = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Left + upgradeObject.Rectangle.Width) * _zoomWidth;
                                _rawRectangleTemp.Bottom = Convert.ToSingle(upgradeObject.Rectangle.LeftCorner.Top + upgradeObject.Rectangle.Height) * _zoomHeight;
                                RenderTarget2D.DrawBitmap(_speedUpgradeBitmap, _rawRectangleTemp,
                                    _upgradesOpacities[upgradeIndex], interpolationMode);
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
                    FillBlock(_rawRectangleTemp, _mapObjectsColors[10]);
                    //RenderTarget2D.DrawBitmap(_bulletUpBitmap, rawRectangleTemp, 1.0f, interpolationMode);
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

                        RenderTarget2D.FillRectangle(_nickBackRectangle, _mapObjectsColors[13]);
                        RenderTarget2D.DrawText(tankObject.Nickname,
                            _nicknameTextFormat, _nickRectangle, _mapObjectsColors[15]);

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
                            RenderTarget2D.DrawBitmap(_tankUpBitmap, _tankRectangle, _tanksOpacities[tankIndex], interpolationMode);
                        }
                        else if (tankObject.Direction == DirectionType.Down)
                        {
                            RenderTarget2D.DrawBitmap(_tankDownBitmap, _tankRectangle, _tanksOpacities[tankIndex], interpolationMode);
                        }
                        else if (tankObject.Direction == DirectionType.Left)
                        {
                            RenderTarget2D.DrawBitmap(_tankLeftBitmap, _tankRectangle, _tanksOpacities[tankIndex], interpolationMode);
                        }
                        else if (tankObject.Direction == DirectionType.Right)
                        {
                            RenderTarget2D.DrawBitmap(_tankRightBitmap, _tankRectangle, _tanksOpacities[tankIndex], interpolationMode);
                        }
                    }
                    ++tankIndex;
                }
            }
        }
        
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void DrawClientInfo()
        {
            RenderTarget2D.Clear(_blackScreen);
            RenderTarget2D.FillRectangle(_clientInfoAreaRect, _mapObjectsColors[13]);
            RenderTarget2D.DrawLine(_clientInfoLeftPoint, _clientInfoRightPoint, _mapObjectsColors[12], 10);
            _clientInfoTanks.AddRange(
                Map.InteractObjects.OfType<TankObject>().OrderByDescending(t => t.Score).ToList());
            
            int index = 0;
            if (_dgv.Rows.Count > 0)
            {
                foreach (var tank in _clientInfoTanks)
                {
                    _dgv.Rows[index].SetValues(index, tank.Nickname, tank.Score, tank.Hp, tank.Lives);
                    ++index;
                }
            }
            _clientInfoTanks.Clear();
        }

        public void LoadResources()
        {
            dstinationRectangle = new RawRectangleF(0, 0, 100, 100);
            interpolationMode = BitmapInterpolationMode.Linear;
            _bitmaps = new Bitmap[4];

            _bitmaps[0] = LoadFromFile(RenderTarget2D, @"img\wall.png");
            _bitmaps[1] = LoadFromFile(RenderTarget2D, @"img\water2.png");
            _bitmaps[2] = LoadFromFile(RenderTarget2D, @"img\Grass_T.png");
            _bitmaps[3] = LoadFromFile(RenderTarget2D, @"img\brick4k.png");

            _tankUpBitmap = LoadFromFile(RenderTarget2D, @"img\tank\tankUp.png");
            _tankDownBitmap = LoadFromFile(RenderTarget2D, @"img\tank\tankDown.png");
            _tankLeftBitmap = LoadFromFile(RenderTarget2D, @"img\tank\tankLeft.png");
            _tankRightBitmap = LoadFromFile(RenderTarget2D, @"img\tank\tankRight.png");

            _bulletSpeedUpgradeBitmap = LoadFromFile(RenderTarget2D, @"img\upgrade\BulletSpeed.png");
            _damageUpgradeBitmap = LoadFromFile(RenderTarget2D, @"img\upgrade\Damage.png");
            _healthUpgradeBitmap = LoadFromFile(RenderTarget2D, @"img\upgrade\Health.png");
            _maxHpUpgradeBitmap = LoadFromFile(RenderTarget2D, @"img\upgrade\MaxHp.png");
            _speedUpgradeBitmap = LoadFromFile(RenderTarget2D, @"img\upgrade\Speed.png");
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
            for (int mapIndex = 0; mapIndex < _mapObjectsColors.Length; mapIndex++)
            {
                _mapObjectsColors[mapIndex].Dispose();
            }

            for (int i = 0; i < _bitmaps.Length; i++)
            {
                _bitmaps[i].Dispose();
            }

            _tankUpBitmap.Dispose();
            _tankDownBitmap.Dispose();
            _tankLeftBitmap.Dispose();
            _tankRightBitmap.Dispose();

            _bulletSpeedUpgradeBitmap.Dispose();
            _damageUpgradeBitmap.Dispose();
            _healthUpgradeBitmap.Dispose();
            _maxHpUpgradeBitmap.Dispose();
            _speedUpgradeBitmap.Dispose();
        }

    }
}