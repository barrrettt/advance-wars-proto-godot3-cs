shader_type canvas_item;
uniform vec3 noiseSt = vec3(1.0,0.5,-0.9);
uniform vec4 times = vec4 (0.3,-0.4,0.1,-0.5);
uniform vec4 tint: hint_color;

// 2D Random
float random (in vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898,78.233)))* 43758.5453123);
}

// 2D Noise based on Morgan McGuire @morgan3d
// https://www.shadertoy.com/view/4dS3Wd
float noise (in vec2 st) {
    vec2 i = floor(st);
    vec2 f = fract(st);

    // Four corners in 2D of a tile
    float a = random(i);
    float b = random(i + vec2(1.0, 0.0));
    float c = random(i + vec2(0.0, 1.0));
    float d = random(i + vec2(1.0, 1.0));

    // Smooth Interpolation

    // Cubic Hermine Curve.  Same as SmoothStep()
    vec2 u = f*f*(3.0-2.0*f);
    // u = smoothstep(0.,1.,f);

    // Mix 4 coorners percentages
    return mix(a, b, u.x) +
            (c - a)* u.y * (1.0 - u.x) +
            (d - b) * u.x * u.y;
}

void fragment(){
	vec2 st = UV;

	//movement
	vec2 noiseCoord1 = st * noiseSt.x;
	vec2 noiseCoord2 = st *noiseSt.y + noiseSt.z;
	
	vec2 motion1 = vec2(TIME*times.x,TIME *times.y);
	vec2 motion2 = vec2(TIME*times.z,TIME *times.w);
	
	vec2 dist1 = vec2(noise(noiseCoord1 + motion1), noise (noiseCoord2 + motion1));
	vec2 dist2 = vec2(noise(noiseCoord1 + motion2), noise (noiseCoord2 + motion2));
	
	vec2 distorsion = (dist1+dist2) /200.0f;
	
	vec4 color = texture(TEXTURE,st + distorsion);//aplica la textura
	color *= tint; //pinta
	
	COLOR = color;
}