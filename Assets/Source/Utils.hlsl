#ifndef UTOPIA_UTILS_INCLUDED
#define UTOPIA_UTILS_INCLUDED

float cmax(float4 vec)
{
	return max(vec.x, max(vec.y, max(vec.z, vec.w)));
}

float unlerp(float4 a, float4 b, float4 x)
{
	return (x - a) / (b - a);
}

#endif