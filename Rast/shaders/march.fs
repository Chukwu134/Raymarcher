#version 330

uniform mat4 mPi;  // projection matrix inverse
uniform mat4 mVi;
uniform mat4 turnMat;

//uniform float bias;
uniform float theta;

//in vec3 vNormal;
in vec3 vPosition;
in vec2 geom_texCoord;

out vec4 fragColor;

const float PI = 3.14159265358979323846264;

const float threshold = 0.0005;

vec4 worldPos = vec4(1);
vec4 worldDir = vec4(0);

// mandlebulb
float distanceEstimatorMandlebulb(vec4 pos) {
  vec3 z = pos.xyz;
  float dr = 1.0;
  float r = 0.0;
  int Power = 6;

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
  return 0.8 - 40 * minDistance;
}

vec4 reflect(vec4 abcd, vec4 worldPt) {
  float val = dot(worldPt, abcd);
  vec4 testingPos = worldPt;
  if(val < 0) {
    testingPos -= 2 * val * vec4(abcd.xyz, 0);
  }
  return testingPos;
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

  vec4 testingPos = worldPos;
  vec3 planeDir = (turnMat * vec4(vec3(1, 0, 0), 1)).xyz;
  vec4 planeDirBias = vec4(planeDir, sin(theta) / 5 - 0.0);
  while(travelled < 50 && distance > threshold) {

    vec4 turnedPos = turnMat * worldPos;
    testingPos = reflect(planeDirBias, turnedPos);

    //testingPos = mod(worldPos + 1.5, 3) - 1.5;
    // testingPos.y = mod(worldPos.y + 1.5, 3) - 1.5;
    // testingPos.z = mod(worldPos.z + 1.5, 3) - 1.5;

    distance = distanceEstimatorMandlebulb(testingPos);
    travelled += distance;
    worldPos = worldPos + worldDir * distance;
    minDistance = min(minDistance, distance);
    steps += 1;
  }

  if(travelled < 50) {
    vec3 color = vec3(length(testingPos.xyz) / 2, 0.2, .5);
    fragColor = vec4(color - steps / 200, 1);
    //fragColor = vec4(color, 1);

  } else {
    float glow = get_glow(minDistance);
    vec3 glow_hue = vec3(0.9, 1, 0.7);

    //fragColor = vec4(glow_hue * glow, 0);
    fragColor = vec4(0, 0, 0, 1);
  }
}