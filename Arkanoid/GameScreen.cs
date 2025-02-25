﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using rndomNamespace.Properties;
using Timer = System.Windows.Forms.Timer;

namespace rndomNamespace
{
    public class GameModel
    {
        public readonly int BallSpeed;
        public readonly int BallSize;
        public readonly int PlatformHeight;
        public readonly int PlatformWidth;
        public readonly string[,] LevelStruct;

        public readonly int Difficulty;
        public readonly int Level;
        public GameModel(int difficulty, int level)
        {
            Difficulty = difficulty;
            Level = level;
            
            Ball ball = new Ball(difficulty);
            Platform platform = new Platform(difficulty);
            Levels levels = new Levels(level);

            BallSpeed = ball.Speed;
            BallSize = ball.Size;
            
            PlatformHeight = platform.GetHeight;
            PlatformWidth = platform.GetWidth;

            LevelStruct = levels.Level;
        }
    }

    public class GameVisual
    {
        private BufferedGraphics ball;
        private Form _form;
        public GameVisual(Form form)
        {
            _form = form;
        }
        public BufferedGraphics InitializeGraphics()
        {
            BufferedGraphicsContext context = new BufferedGraphicsContext();
            ball = context.Allocate(_form.CreateGraphics(), _form.ClientRectangle);
            context.MaximumBuffer = _form.ClientRectangle.Size;
            return ball;
        }
        
        public void GameFailed(int width, int height, PictureBox mainMenu, PictureBox retry)
        {
            _form.BackColor = Color.Black;
            
            PictureBox youDead = new PictureBox();
            youDead.Size = new Size(884, 224);
            youDead.Location = new Point(width / 2 - youDead.Size.Width / 2, height / 8);
            youDead.Image = Resources.you_died;
            _form.Controls.Add(youDead);
            youDead.BringToFront();
                
            InitializeEndButtons(mainMenu, retry, width, height);
        }

        public void GameFinished(Label scoreLable, int score, int diff, int width, int height,  PictureBox mainMenu, PictureBox retry)
        {
            scoreLable.Visible = true;
            scoreLable.Text = "Ваш счёт: " + score * diff;
            scoreLable.Size = new Size(200, 50);
            scoreLable.Location = new Point(width / 2 - scoreLable.Size.Width / 2, height / 8);
            scoreLable.Show();
            scoreLable.BringToFront();
                
            InitializeEndButtons(mainMenu, retry, width, height);
        }
        
        private void InitializeEndButtons(PictureBox mainMenu, PictureBox retry, int width, int height)
        {
            mainMenu.Size = new Size(148, 55);
            retry.Size = new Size(182, 56);
            
            mainMenu.Location = new Point(width / 2 - mainMenu.Size.Width / 2, (height * 3) / 4 - 30);
            retry.Location = new Point(width / 2 - retry.Size.Width / 2, height / 2 - 30);
            
            mainMenu.Image = Resources.game_exit;
            retry.Image = Resources.game_restart;
            
            _form.Controls.Add(mainMenu);
            _form.Controls.Add(retry);
            
            mainMenu.BringToFront();
            retry.BringToFront();
        }

        public BufferedGraphics UpdateGraphics(int x, int y, int size)
        {
            ball.Graphics.Clear(_form.BackColor);
            ball.Graphics.DrawEllipse(new Pen(Brushes.Blue), x, y, size, size);
            ball.Render(); //выводим то, что отрисовано в буфере
            return ball;
        }
        
        public void TileBreak(string soundDir)
        {
            var random = new Random();
            var rndInt = random.Next(0, 5);
            var sound = "pong_gha.wav";
            if (rndInt == 1) sound = "pong_hoba.wav";
            else if (rndInt == 2) sound = "pong_hoba!.wav";
            else if (rndInt == 3) sound = "pong_hobaaa.wav";
            else if (rndInt == 4) sound = "pong_hop.wav";
            
            var player = new SoundPlayer(soundDir + sound);
            player.Play();
        }

        public void PlayGameOverSound(string soundDir)
        {
            var player = new SoundPlayer(soundDir + "fail_sound.wav");
            player.Play();
        }
    }

    public partial class Arkanoid : Form
    {
        private GameVisual _gameVisual;
        private string soundDir = Path.GetDirectoryName(Application.ExecutablePath).Replace(@"bin\Debug", @"Sounds\");
        
        private bool isKeyLeftPressed;
        private bool isKeyRightPressed;

        private readonly int Level;
        private readonly int Difficulty;

        private readonly int _platformWidth;
        private readonly int _platformPosition;

        private readonly int _windowHeight;
        private readonly int _windowWidth;

        private readonly int _ballSize;
        private BufferedGraphics ball;

        private int _speedX, _speedY; // Скорость для шарика, путем ускорения отталкивания для sx,xy (по умолчанию 1.5, потом мб увеличим в зависимости от сложности)

        private int _coordinateX;
        private int _coordinateY; //

        private Timer timer1 = new Timer();
        private Timer timer2 = new Timer();
        
        private PictureBox mainMenu = new PictureBox();
        private PictureBox retry = new PictureBox();

        private PictureBox[,] _level;
        private int[] _levelSize;

        private int _score;

        public Arkanoid(GameModel gameModel)
        {
            InitializeComponent();

            _gameVisual = new GameVisual(this); 

            winScreen.Visible = false;
            winScreen.Size = new Size(800, 800);
            winScreen.Location = new Point(0, 0);
            scoreLable.Visible = false;

            Level = gameModel.Level;
            Difficulty = gameModel.Difficulty;

            StartPosition = FormStartPosition.CenterScreen;
            Text = "Arkanoid";
            
            _windowHeight = 800; // планируется добавить разный размер окон, а мб и нет
            _windowWidth = 800;
            
            Size = new Size(_windowWidth, _windowHeight);
            MinimumSize = new Size(_windowWidth, _windowHeight);
            MaximumSize = new Size(_windowWidth, _windowHeight);

            _platformPosition = _windowHeight - 100;
            int platformWidth = gameModel.PlatformWidth;
            int platformHeight = gameModel.PlatformHeight;
            platform1.Image = Resources.platform1;
            platform1.Size = new Size(platformWidth, platformHeight);
            platform1.Location = new Point(_windowWidth / 2 - platformWidth / 2, _platformPosition);
            _platformWidth = platformWidth;

            _ballSize = gameModel.BallSize;
            _speedX = gameModel.BallSpeed;
            _speedY = gameModel.BallSpeed;

            _coordinateX = _windowWidth / 2 - _ballSize / 2;
            _coordinateY = _windowHeight - 100 - gameModel.BallSpeed - _ballSize;

            var tile = new PictureBox();

            tile.Image = Resources.tile;
            tile.Size = new Size(50, 20);
            tile.Location = new Point(100, 100);

            string[,] levelStruct = gameModel.LevelStruct;
            LevelBuilder(levelStruct);
            
            gameOverScreen.Image = Resources.bg;
            gameOverScreen.Location = new Point(0, 0);
            gameOverScreen.Size = new Size(1920, 1080);
            gameOverScreen.Visible = false;

            GameModel game = gameModel;
            FormClosing += Form_Closing;

            timer1.Interval = 10;
            timer1.Tick += update;
            timer1.Start();

            timer2.Interval = 10;
            timer2.Tick += Elapsed;
            timer2.Start();
            
            ball = _gameVisual.InitializeGraphics();

            KeyDown += Arkanoid_KeyDown;
            KeyUp += Arkanoid_KeyUp;

        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            ball.Dispose();
            Dispose();
        }

        private void Elapsed(object sender, EventArgs e)
        {
            _coordinateX += _speedX; // Движение по х
            _coordinateY += _speedY; // Движение по y 
            
            if (_coordinateX <= 0 || _coordinateX + _ballSize >= _windowWidth - 19)
                _speedX = -_speedX;

            if (platform1.Location.X <= _coordinateX + _ballSize / 2 && platform1.Location.X + _platformWidth >= _coordinateX + _ballSize / 2 &&
                platform1.Location.Y == _coordinateY + _ballSize)
                _speedY = -_speedY;

            if (_coordinateY <= 0)
                _speedY = -_speedY;

            if (ball.Graphics != null)
                ball = _gameVisual.UpdateGraphics(_coordinateX, _coordinateY, _ballSize);
            
            var isLevelComplete = true;
            
            for (int i = 0; i < _level.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < _level.GetUpperBound(1) + 1; j++)
                {
                    if (_level[i, j].Visible)
                        isLevelComplete = false;
                    
                    if (!_level[i, j].Visible) continue;
                    
                    if (_coordinateX + _ballSize / 2 >= 120 + i * (100 + 5) &&
                            _coordinateX + _ballSize / 2 <= 220 + i * (100 + 5)) // ширина плитки
                    {
                        if (_coordinateY <= 130 + j * (30 + 5) &&
                            _coordinateY >= 100 + j * (30 + 5)) 
                            TileBreak(i, j, false); // отскок верхней стороной шарика от нижней границы
                        else if (_coordinateY + _ballSize >= 100 + j * (30 + 5) && 
                                 _coordinateY + _ballSize <= 130 + j * (30 + 5))
                            TileBreak(i, j, false); // отскок нижней стороной шарика от верхней границы
                    }
                    if (_coordinateY + _ballSize / 2 >= 100 + j * (30 + 5) &&
                             _coordinateY + _ballSize / 2 <= 130 + j * (30 + 5)) // высота плитки
                    {
                        if (_coordinateX <= 220 + i * (100 + 5) && _coordinateX >= 120 + i * (100 + 5))
                            TileBreak(i, j, true); // отскок левой стороной шарика от правой границы
                        else if (_coordinateX + _ballSize >= 120 + i * (100 + 5) &&
                                 _coordinateX + _ballSize <= 220 + i * (100 + 5))
                            TileBreak(i, j, true); // отскок правой стороной шарика от левой границы
                    }
                }
            }

            var isGameOver = false;
            
            if (_coordinateY + _ballSize >= _windowHeight - _ballSize)
            {
                platform1.Visible = false;
                _gameVisual.PlayGameOverSound(soundDir);
                
                _gameVisual.GameFailed(_windowWidth, _windowHeight, mainMenu, retry);
                isGameOver = true;
                // в планах запилить менюшку с Начать заново / Главное меню / Ваш рекорд
            }

            if (isLevelComplete)
            {
                winScreen.Visible = true;
                _gameVisual.GameFinished(scoreLable, _score, Difficulty, _windowWidth, _windowHeight, mainMenu, retry);
                isGameOver = true;
            }

            if (isGameOver)
            {
                timer1.Stop();
                timer2.Stop();
                retry.Click += retry_click;
                mainMenu.Click += mainMenu_click;
            }
        }

        private void TileBreak(int i, int j, bool isX)
        {
            _level[i, j].Visible = false;
            _score += 50;
            if (isX)
                _speedX = -_speedX;
            else
                _speedY = -_speedY;
            
            _gameVisual.TileBreak(soundDir);
        }

        private void retry_click(object sender, EventArgs e)
        {
            GameModel game = new GameModel(Difficulty, Level);
            var gameForm = new Arkanoid(game);
            gameForm.Show();
            Close();
        }

        private void mainMenu_click(object sender, EventArgs e)
        {
            Close();
            var startScreen = new Form2();
            Close();
            startScreen.Show();
        }

        private void Arkanoid_KeyUp(object sender, KeyEventArgs e)
        {
            isKeyLeftPressed = false;
            isKeyRightPressed = false;
        }

        private void update(object obj, EventArgs e)
        {
            if (isKeyLeftPressed)
            {
                if (platform1.Left < 3) return;
                platform1.Location = new Point(platform1.Location.X - 10, platform1.Location.Y);
            }

            if (isKeyRightPressed)
            {
                if (platform1.Right > _windowWidth - 19) return;
                platform1.Location = new Point(platform1.Location.X + 10, platform1.Location.Y);
            }
        }

        private void Arkanoid_KeyDown(object obj, KeyEventArgs e)
        {
            int currentKey = e.KeyValue;
            switch (currentKey)
            {
                case 37:
                    if (platform1.Left < 3) return;
                    isKeyLeftPressed = true;
                    break;
                case 39:
                    if (platform1.Right > _windowWidth - 19) return;
                    isKeyRightPressed = true;
                    break;
            }
        }

        private void LevelBuilder(string[,] levelStruct)
        {
            Tile tileBlock = new Tile(levelStruct.Length);
            int height = tileBlock.GetHeight;
            int width = tileBlock.GetWidth;
            _level = new PictureBox[levelStruct.GetUpperBound(0) + 1, levelStruct.GetUpperBound(1) + 1];
            _levelSize = new int[] {0, 0};

            for (int i = 0; i < levelStruct.GetLength(0); i++)
            {
                for (int j = 0; j < levelStruct.GetLength(1); j++)
                {
                    if (levelStruct[i, j] == "*")
                    {
                        PictureBox tile = new PictureBox();
                        tile.Image = Resources.tile;
                        tile.Size = new Size(width, height);
                        tile.Location = new Point(120 + i * (width + 5), 100 + j * (height + 5));

                        Controls.Add(tile);
                        _level[i, j] = tile;
                    }
                    else
                    {
                        _level[i, j] = null;
                    }

                    _levelSize[0] += 105;
                }

                _levelSize[1] += 55;
            }

            _levelSize[0] -= 5;
            _levelSize[1] -= 5;
        }
    }
}