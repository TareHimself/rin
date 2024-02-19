include(ExternalProject)

ExternalProject_Add(
  reflect

  GIT_REPOSITORY "https://github.com/TareHimself/reflect.git"
  GIT_TAG "master"
  
  UPDATE_COMMAND ""
  PATCH_COMMAND ""
  
  SOURCE_DIR ${CMAKE_CURRENT_BINARY_DIR}/reflect
  CMAKE_ARGS -DCMAKE_INSTALL_PREFIX=${DCMAKE_CURRENT_LIST_DIR}/ThirdParty/reflect
  
  TEST_COMMAND ""
)