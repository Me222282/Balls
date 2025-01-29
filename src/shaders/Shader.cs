using System.IO;
using Zene.Graphics;
using Zene.Structs;

namespace Balls
{
    public sealed class Shader : BaseShaderProgram
    {
        public Shader()
        {
            Create(File.ReadAllText("./shaders/vertex.glsl"), ShaderPresets.CircleFrag, 1,
                "colourType", "matrix", "radius", "minRadius");

            SetUniform(Uniforms[1], Matrix4.Identity);
            SetUniform(Uniforms[0], (int)ColourSource.AttributeColour);
            
            SetUniform(Uniforms[2], 0.25f);
            SetUniform(Uniforms[3], 0.25f);
        }
    }
}