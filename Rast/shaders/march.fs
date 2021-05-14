#version 330

uniform mat4 mPi;  // projection matrix inverse
uniform mat4 mVi;

//in vec3 vNormal;
in vec3 vPosition;
in vec2 geom_texCoord;

out vec4 fragColor;

const float PI = 3.14159265358979323846264;

const float threshold = 0.001;

//sphere
float distanceEstimatorSphere(vec4 position) {
  return length(position.xyz) - 2;
}

float distanceEstimatorCube(vec4 position) {
  float size = 2;
  float x = position.x;
  float y = position.y;
  float z = position.z;
  float distance = 0;
  // if(abs(x) <= size) {
  //   if(abs(y) <= 1) {
  //     distance = abs(z) - size;
  //   } else {
  //     if(abs(z) <= size) {
  //       distance = abs(y) - size;
  //     } else {
  //       distance = sqrt(pow((abs(y) - size), 2) + pow((abs(z) - size), 2));
  //     }
  //   }
  // } else {
  //   if(abs(y) <= size) {
  //     if(abs(z) <= size) {
  //       distance = abs(x) - size;
  //     } else {
  //       distance = sqrt(pow((abs(x) - size), 2) + pow((abs(z) - size), 2));
  //     }
  //   } else {
  //     if(abs(z) <= size) {
  //       distance = sqrt(pow((abs(x) - size), 2) + pow((abs(y) - size), 2));
  //     } else {
  //       distance = sqrt(pow((abs(x) - size), 2) + pow((abs(y) - size), 2) + pow((abs(z) - size), 2));
  //     }
  //   }
  // }

  distance = sqrt(pow(max(0, abs(x) - size), 2) + pow(max(0, abs(y) - size), 2) + pow(max(0, abs(z) - size), 2));
  return distance;
}

float march(vec4 direction) {
  vec4 pos = vec4(0, 0, 0, 1);
  vec4 worldPos = mVi * pos;
  vec4 worldDir = mVi * direction;
  float travelled = 0;
  float distance = 100000;

  while(travelled < 50 && distance > threshold) {
    distance = distanceEstimatorSphere(worldPos);
    travelled += distance;
    worldPos = worldPos + worldDir * distance;
  }
  return travelled;
}

void main() {
  vec4 NDC = vec4(geom_texCoord * 2 - 1, 0, 1);
  vec4 eyeSpace = mPi * NDC;
  eyeSpace = eyeSpace / eyeSpace.w;
  vec4 dir = normalize(vec4(eyeSpace.xyz, 0));

  float travelled = march(dir);
  if(travelled < 50) {
    fragColor = vec4(1, 1, 1, 1);
  } else {
    fragColor = vec4(0, 0, 0, 1);
  }
}