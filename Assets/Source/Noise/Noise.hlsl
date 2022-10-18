#ifndef UTOPIA_NOISE_INCLUDED
#define UTOPIA_NOISE_INCLUDED

/*
	Designed around the book of shaders:
	*	https://thebookofshaders.com/10/
	*	https://thebookofshaders.com/11/
	*	https://thebookofshaders.com/12/
*/

float Random(float x)
{
	// Very basic, but it works efficiently.
	return frac(sin(x) * 100000.0);
}

float Noise(float x)
{
	float integer = floor(x);
	float fraction = frac(x);
	
	// Interpolate between the two samples by the fractional component with smoothing.
	return lerp(Random(integer), Random(integer + 1.0), smoothstep(0.0, 1.0, fraction));
}

float NoiseFractal(float x, uint octaves, float lacunarity, float gain)
{
	float value = 0.0;
	float amplitude = 0.5;
	float frequency = 1.0;
	
	for(uint i = 0; i < octaves; ++i)
	{
		value += amplitude * Noise(frequency * x);
		frequency *= lacunarity;
		amplitude *= gain;
	}

	return value;
}

#endif
