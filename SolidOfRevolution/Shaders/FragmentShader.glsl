#version 330

uniform vec3 u_olpos;
uniform vec3 u_olcol;
uniform vec3 u_oeye;
uniform float u_odmin;
uniform float u_osfoc;
uniform bool u_lie;

in vec3 v_color;
in vec3 v_pos;
in vec3 v_normal;

layout(location = 0) out vec4 o_color;

void main()
{
   vec3 l = normalize(v_pos - u_olpos);
   float cosa = dot(l, v_normal);
   float d = max(cosa, u_odmin);
   vec3 r = reflect(l, v_normal);
   vec3 e = normalize(u_oeye - v_pos);
   float s = max(pow(dot(r, e), u_osfoc), 0.0) * (int(cosa >= 0.0));
   o_color = vec4(v_color, 1.0);
}