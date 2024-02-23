

//push constants block
layout( push_constant ) uniform constants
{
    vec4 clip;
	vec4 extent;
    mat4 transform;
} pRect;
