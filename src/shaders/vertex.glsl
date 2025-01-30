#version 330 core

layout(location = locations.vertex) in vec3 vPosition;
layout(location = locations.texture) in vec2 texCoord;
// Instance data
layout(location = 3) in vec2 location;
layout(location = 5) in float radius;
layout(location = 6) in vec3 colour;

out vec4 pos_Colour;
out vec2 tex_Coords;
out vec2 pos;

uniform mat4 matrix;

const float convert = 1.0 / 255.0;

void main()
{
	pos = texCoord - vec2(0.5);
    // pos_Colour = vec4(
    //     float(colour.r) / 255.0,
    //     float(colour.g) / 255.0,
    //     float(colour.b) / 255.0, 1.0);
    pos_Colour = vec4(colour, 1.0);
    
    vec2 p = vPosition.xy * vec2(radius * 2.0);
    p += location;
    
	gl_Position = matrix * vec4(p, vPosition.z, 1);
}