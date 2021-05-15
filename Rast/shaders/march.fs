#version 330

uniform mat4 mPi;  // projection matrix inverse
uniform mat4 mVi;

//in vec3 vNormal;
in vec3 vPosition;
in vec2 geom_texCoord;

out vec4 fragColor;

const float PI = 3.14159265358979323846264;

const float threshold = 0.00001;

vec4 worldPos = vec4(1);
vec4 worldDir = vec4(0);
float gStep = 0.001;
vec3 gradient = vec3(0);

float potential(vec3 pos) {
  vec3 z = pos;
  int Power = 8;
  for(int i = 1; i < 50; i++) {
    return log(length(z)) / pow(Power, float(i));
  }
  return 0.0;
}

float fastMandlebulbDE(vec3 p) {
  float pot = potential(p);
  if(pot == 0.0)
    return 0.0;
  gradient = (vec3(potential(p + worldDir.x * gStep), potential(p + worldDir.y * gStep), potential(p + worldDir.z * gStep)) - pot) / gStep;
  return (0.5 / exp(pot)) * sinh(pot) / length(gradient);
}

// mandlebulb
float distanceEstimatorMandlebulb(vec4 pos) {
  vec3 z = pos.xyz;
  float dr = 1.0;
  float r = 0.0;
  int Power = 5;

  for(int i = 0; i < 50; i++) {
    r = length(z);
    if(r > 50)
      break;

		// convert to polar coordinates
    float theta = acos(z.z / r);
    float phi = atan(z.y, z.x);
    dr = pow(r, Power - 1.0) * Power * dr + 1.0;

		// scale and rotate the point
    float zr = pow(r, Power);
    theta = theta * Power;
    phi = phi * Power;

		// convert back to cartesian coordinates
    z = zr * vec3(sin(theta) * cos(phi), sin(phi) * sin(theta), cos(theta));
    z += pos.xyz;
  }
  return 0.5 * log(r) * r / dr;
}

//sphere
float distanceEstimatorSphere(vec4 position) {
  return length(position.xyz) - 2;
}

//cube
float distanceEstimatorCube(vec4 position) {
  float size = 1;
  float x = position.x;
  float y = position.y;
  float z = position.z;
  float distance = 0;

  distance = sqrt(pow(max(0, abs(x) - size), 2) + pow(max(0, abs(y) - size), 2) + pow(max(0, abs(z) - size), 2));
  return distance;
}

float get_glow(float minDistance) {
  return 0.8 - 10 * minDistance;
}

void main() {
  vec4 NDC = vec4(geom_texCoord * 2 - 1, 0, 1);
  vec4 eyeSpace = mPi * NDC;
  eyeSpace = eyeSpace / eyeSpace.w;
  vec4 dir = normalize(vec4(eyeSpace.xyz, 0));

  vec4 pos = vec4(0, 0, 0, 1);
  worldPos = mVi * pos;
  worldDir = mVi * dir;
  float travelled = 0;
  float steps = 0;
  float distance = 100000;
  float minDistance = 100000;

  while(travelled < 50 && distance > threshold) {
    distance = fastMandlebulbDE(worldPos.xyz);
    travelled += distance;
    worldPos = worldPos + worldDir * distance;
    minDistance = min(minDistance, distance);
    steps += 1;
  }

  if(travelled < 50) {
    vec3 color = vec3(length(worldPos.xyz) / 2, 0.2, .5);
    fragColor = vec4(color - steps / 200, 1);

  } else {
    float glow = get_glow(minDistance);
    vec3 glow_hue = vec3(0.9, 1, 0.7);

    fragColor = vec4(glow_hue * glow, 1);
  }
}