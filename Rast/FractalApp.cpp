#include "FractalApp.hpp"
#include <nanogui/window.h>
#include <nanogui/glcanvas.h>
#include <nanogui/layout.h>

#include <cpplocate/cpplocate.h>

// Fixed screen size is awfully convenient, but you can also
// call Screen::setSize to set the size after the Screen base
// class is constructed.
const int FractalApp::windowWidth = 800;
const int FractalApp::windowHeight = 600;

bool useDeferred = true;
int bufferNum = 0;

// Constructor runs after nanogui is initialized and the OpenGL context is current.
// Sets up camera, fsq, shading pass (potentially more later)
FractalApp::FractalApp()
    : nanogui::Screen(Eigen::Vector2i(windowWidth, windowHeight), "Fractal Time", false),
      backgroundColor(0.0f, 0.0f, 0.0f, 0.0f)
{

  const std::string resourcePath =
      cpplocate::locatePath("Rast/shaders", "", nullptr) + "Rast/shaders/";
  cout << resourcePath;

  // Set up a simple shader program by passing the shader filenames to the convenience constructor
  fractalShader.reset(new GLWrap::Program("Fractal Shader", {{GL_VERTEX_SHADER, resourcePath + "fsq.vert"},
                                                             {GL_FRAGMENT_SHADER, resourcePath + "march.fs"}}));

  // Create a camera in a default position, respecting the aspect ratio of the window.
  cam = shared_ptr<RTUtil::PerspectiveCamera>(new RTUtil::PerspectiveCamera(
      Eigen::Vector3f(0, 0, -3), // eye
      Eigen::Vector3f(0, 0, 0),
      Eigen::Vector3f(0, 1, 0), // up
      16.0 / 9.0,               // aspect
      1,
      50, // near, far
      3   // fov
      ));

  cc.reset(new RTUtil::DefaultCC(cam));
  Screen::setSize(Eigen::Vector2i(windowWidth, 9.0 / 16 * windowWidth));

  // Upload a two-triangle mesh for drawing a full screen quad
  Eigen::MatrixXf vertices(5, 4);
  vertices.col(0) << -1.0f, -1.0f, 0.0f, 0.0f, 0.0f;
  vertices.col(1) << 1.0f, -1.0f, 0.0f, 1.0f, 0.0f;
  vertices.col(2) << 1.0f, 1.0f, 0.0f, 1.0f, 1.0f;
  vertices.col(3) << -1.0f, 1.0f, 0.0f, 0.0f, 1.0f;

  Eigen::Matrix<float, 3, Eigen::Dynamic> positions = vertices.topRows<3>();
  Eigen::Matrix<float, 2, Eigen::Dynamic> texCoords = vertices.bottomRows<2>();

  fsqMesh.reset(new GLWrap::Mesh());
  fsqMesh->setAttribute(fractalShader->getAttribLocation("vert_position"), positions);
  fsqMesh->setAttribute(fractalShader->getAttribLocation("vert_texCoord"), texCoords);

  // Set viewport
  Eigen::Vector2i framebufferSize;
  glfwGetFramebufferSize(glfwWindow(), &framebufferSize.x(), &framebufferSize.y());
  glViewport(0, 0, framebufferSize.x(), framebufferSize.y());

  performLayout();
  setVisible(true);
}

bool FractalApp::keyboardEvent(int key, int scancode, int action, int modifiers)
{
  if (Screen::keyboardEvent(key, scancode, action, modifiers))
    return true;

  if (action == GLFW_PRESS)
  {
    switch (key)
    {
    case GLFW_KEY_ESCAPE:
      setVisible(false);
      return true;
    case GLFW_KEY_R:
      t = 0;
      return true;
    case GLFW_KEY_P:
      speed = 0;
      return true;
    case GLFW_KEY_LEFT:
      speed += 250;
      return true;
    case GLFW_KEY_RIGHT:
      speed -= 250;
      return true;
    default:
      return true;
    }
  }
  return cc->keyboardEvent(key, scancode, action, modifiers);
}

bool FractalApp::mouseButtonEvent(const Eigen::Vector2i &p, int button, bool down, int modifiers)
{
  return Screen::mouseButtonEvent(p, button, down, modifiers) ||
         cc->mouseButtonEvent(p, button, down, modifiers);
}

bool FractalApp::mouseMotionEvent(const Eigen::Vector2i &p, const Eigen::Vector2i &rel, int button, int modifiers)
{
  return Screen::mouseMotionEvent(p, rel, button, modifiers) ||
         cc->mouseMotionEvent(p, rel, button, modifiers);
}

bool FractalApp::scrollEvent(const Eigen::Vector2i &p, const Eigen::Vector2f &rel)
{
  return Screen::scrollEvent(p, rel) ||
         cc->scrollEvent(p, rel);
}

void FractalApp::drawContents()
{
  if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
  {
    std::cout << "ERROR::FRAMEBUFFER:: Framebuffer is not complete!" << std::endl;
  }

  GLWrap::checkGLError("drawContents start");
  glClearColor(backgroundColor.r(), backgroundColor.g(), backgroundColor.b(), backgroundColor.w());
  glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
  glEnable(GL_DEPTH_TEST);

  fractalShader->use();
  cout << cam->getProjectionMatrix().inverse().matrix();
  fractalShader->uniform("mPi", cam->getProjectionMatrix().inverse().matrix());
  fractalShader->uniform("mVi", cam->getViewMatrix().inverse().matrix());
  fsqMesh->drawArrays(GL_TRIANGLE_FAN, 0, 4);
  fractalShader->unuse();
}
