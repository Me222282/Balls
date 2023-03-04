using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Balls
{
    class Program : Window
    {
        static void Main(string[] args)
        {
            Core.Init();
            
            Window w = new Program(800, 500, "WORK");
            w.RunMultithread();
            
            Core.Terminate();
        }
        
        public Program(int width, int height, string title)
            : base(width, height, title, 4.3, true)
        {
            _random = new Random();
            
            double hw = width * 0.5;
            double hh = height * 0.5;
            
            for (int i = 0; i < 100; i++)
            {
                AddBall(_random.NextVector2(-hw, hw, -hh, hh));
            }
            
            TR = new TextRenderer();
        }
        
        private Random _random;
        private Box _bounds;
        private List<Ball> _balls = new List<Ball>();
        public double Gravity { get; set; } = 30d;
        public double Friction { get; set; } = 0.95;
        
        public static TextRenderer TR;
        private bool _paused = false;
        
        private double _frameTime;
        private int _frameCounter = 0;
        protected override void OnUpdate(FrameEventArgs e)
        {
            _frameCounter++;
            _frameTime = Timer;
            Timer = 0d;
            
            if (this[MouseButton.Right])
            {
                RemoveBall(MouseLocation - (Size * 0.5));
            }
            else if (_frameCounter >= 5 && this[MouseButton.Left])
            {
                _frameCounter = 0;
                AddBall(MouseLocation - (Size * 0.5));
            }
            
            base.OnUpdate(e);
            
            if (!_paused)
            {
                CalculatePhysics();
            }
            
            e.Context.Framebuffer.Clear(BufferBit.Colour);
            DrawManager.View = Matrix.Identity;
            DrawManager.Model = Matrix.Identity;
            
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                e.Context.Render(_balls[i]);
            }
        }
        
        private Vector2 Reflect(Vector2 dir, Radian lineAngle)
        {
            Radian dirA = Math.Atan2(dir.Y, dir.X);
            Radian newA = (lineAngle * 2d) - dirA;
            
            return (
                Math.Cos(newA),
                Math.Sin(newA)
            );
        }
        
        private void CalculatePhysics()
        {
            ApplyGravity();
            
            int l = _balls.Count;
            Parallel.For(0, l, (i) =>
            {
                Ball a = _balls[i];
                
                for (int c = 0; c < l; c++)
                {
                    if (c == i) { continue; }
                    
                    Ball b = _balls[c];
                    
                    if (DetermineColision(a, b))
                    {
                        ResolveCollision(a, b);
                    }
                }
            });
            /*
            for (int i = 0; i < l; i++)
            {
                Ball a = _balls[i];
                
                for (int c = 0; c < l; c++)
                {
                    if (c == i) { continue; }
                    
                    Ball b = _balls[c];
                    
                    if (DetermineColision(a, b))
                    {
                        ResolveCollision(a, b);
                    }
                }
            }*/
        }
        private void ApplyGravity()
        {
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                
                b.Velocity -= (0d, Gravity);
                b.Location += b.Velocity * _frameTime;
                
                ClampToBounds(b);
            }
        }
        private void ClampToBounds(Ball b)
        {
            double r = b.Radius;
            Vector2 l = b.Location;
            
            if (l.X + r > _bounds.Right)
            {
                b.Velocity = (-b.Velocity.X * Friction, b.Velocity.Y);
                //l.X = Fold(l.X, _bounds.Right);
                l.X = _bounds.Right - r;
            }
            if (l.X - r < _bounds.Left)
            {
                b.Velocity = (-b.Velocity.X * Friction, b.Velocity.Y);
                //l.X = Fold(l.X, _bounds.Left);
                l.X = _bounds.Left + r;
            }
            if (l.Y + r > _bounds.Top)
            {
                b.Velocity = (b.Velocity.X, -b.Velocity.Y * Friction);
                //l.Y = Fold(l.Y, _bounds.Top);
                l.Y = _bounds.Top - r;
            }
            if (l.Y - r < _bounds.Bottom)
            {
                b.Velocity = (b.Velocity.X, -b.Velocity.Y * Friction);
                //l.Y = Fold(l.Y, _bounds.Bottom);
                l.Y = _bounds.Bottom + r;
            }
            
            b.Location = l;
        }
        private bool DetermineColision(Ball a, Ball b)
        {
            double min = a.Radius + b.Radius;
            return a.Location.SquaredDistance(b.Location) < (min * min);
        }
        
        private void ResolveCollision(Ball a, Ball b)
        {
            double dist = a.Location.Distance(b.Location);
            double diff = dist - (a.Radius + b.Radius);
            double scale = (diff * 0.5) / dist;
            scale -= 0.01;
            
            Vector2 between = a.Location - b.Location;
            Vector2 offset = between * scale;
            
            Radian lineAngle = Math.Atan2(between.Y, between.X);
            // Perpendicular angle
            lineAngle += 0.5 * Math.PI;
            
            Vector2 aDir = Reflect(a.Velocity, lineAngle);
            Vector2 bDir = Reflect(b.Velocity, lineAngle);
            
            //double momentum = (a.Velocity.Length * a.Mass) + (b.Velocity.Length * b.Mass);
            //double magnitude = momentum * Friction * 0.5;
            
            a.Location -= offset;
            //a.Velocity = -a.Velocity * Friction;
            //a.Velocity = aDir * (magnitude / a.Mass);
            a.Velocity = aDir * a.Velocity.Length * Friction;
            b.Location += offset;
            //b.Velocity = -b.Velocity * Friction;
            //b.Velocity = bDir * (magnitude / b.Mass);
            b.Velocity = bDir * b.Velocity.Length * Friction;
        }
        
        private static double Fold(double value, double fold) => fold - (value - fold);
        
        protected override void OnSizeChange(VectorIEventArgs e)
        {
            base.OnSizeChange(e);
            
            _bounds = new Box(Vector2.Zero, e.Value);
            DrawManager.Projection = Matrix4.CreateOrthographic(e.X, e.Y, 0d, 1d);
        }
        
        public void AddBall(Vector2 location)
        {
            _balls.Add(new Ball(
                location,
                _random.NextDouble(2d, 20d),
                _random.NextVector2(-1000d, 1000d),
                _random.NextColourF()
            ));
        }
        public void RemoveBall(Vector2 location)
        {   
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                
                // Inside circle
                if (b.Location.SquaredDistance(location) < (b.Radius * b.Radius))
                {
                    Actions.Push(() =>
                    {
                        _balls.RemoveAt(i);
                    });
                    return;
                }
            }
        }
        
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            Vector2 location = e.Location - (Size * 0.5);
            
            if (e.Button == MouseButton.Left)
            {
                AddBall(location);
                return;
            }
            if (e.Button == MouseButton.Right)
            {
                RemoveBall(location);
                return;
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e[Keys.Space])
            {
                _paused = !_paused;
                return;
            }
        }
    }
}
