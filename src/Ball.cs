using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;

namespace Balls
{
    public class Ball : IRenderable
    {
        public Ball(Vector2 location, double radius)
            : this(location, radius, Vector2.Zero, Colour3.White)
        {
            
        }
        public Ball(Vector2 location, double radius, Colour3 colour)
            : this(location, radius, Vector2.Zero, colour)
        {
            
        }
        public Ball(Vector2 location, double radius, Vector2 velocity)
            : this(location, radius, velocity, Colour3.White)
        {
            
        }
        public Ball(Vector2 location, double radius, Vector2 velocity, Colour3 colour)
        {
            Colour = colour;
            Location = location;
            _oldPos = location - velocity;
            Radius = radius;
        }
        
        public Vector2 Location;// { get; set; }
        private Vector2 _oldPos;
        public Vector2 Velocity => Location - _oldPos;
        // public Vector2 Acceleration { get; set; }
        public double Radius;// { get; set; }
        public Colour3 Colour;// { get; set; }
        
        internal Vector2 OldLocation => _oldPos;
        
        public void OnRender(IDrawingContext context)
        {
            context.DrawEllipse(new Box(Location, Radius * 2d), (ColourF)Colour);
        }
        
        public void ApplyVerlet(double dt)
        {
            Vector2 vel = Velocity;
            _oldPos = Location;
            // Location += vel + (Acceleration * dt * dt);
            Location += vel - (0d, PhysicsManager.Gravity * dt * dt);
            // Location += vel - (0d, 1000d * dt * dt);
        }
    }
}
