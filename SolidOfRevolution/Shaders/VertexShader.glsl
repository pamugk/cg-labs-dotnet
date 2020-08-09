#version 330

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_color;

uniform mat4 u_mvp;
uniform mat3 u_n;
uniform float u_mph;
uniform float u_mc;

out vec3 v_color;
out vec3 v_pos;
out vec3 v_normal;

void main()
{
    v_color = a_color;
    v_pos = a_position;
    v_normal = vec3(0.0,0.0,0.0);
    gl_Position = u_mvp * vec4(a_position, 1.0);
}