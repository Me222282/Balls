using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;

namespace Balls
{
    public class Ball : IRenderable
    {
        public Ball(Vector2 location, double radius)
            : this(location, radius, Vector2.Zero, ColourF.White)
        {
            
        }
        public Ball(Vector2 location, double radius, ColourF colour)
            : this(location, radius, Vector2.Zero, colour)
        {
            
        }
        public Ball(Vector2 location, double radius, Vector2 velocity)
            : this(location, radius, velocity, ColourF.White)
        {
            
        }
        public Ball(Vector2 location, double radius, Vector2 velocity, ColourF colour)
        {
            Colour = colour;
            Location = location;
            _oldPos = location - velocity;
            Radius = radius;
        }
        
        public ColourF Colour { get; set; }
        public Vector2 Location { get; internal set; }
        private Vector2 _oldPos;
        public Vector2 Velocity => Location - _oldPos;
        public Vector2 Acceleration { get; set; }
        public double Radius { get; set; }
        
        internal Vector2 OldLocation => _oldPos;
        
        public void OnRender(IDrawingContext context)
        {
            context.DrawEllipse(new Box(Location, Radius * 2d), Colour);
        }
        
        public void ApplyVerlet(double dt)
        {
            Vector2 vel = Velocity;
            _oldPos = Location;
            Location += vel + (Acceleration * dt * dt);
        }
    }
}
