include(${CMAKE_CURRENT_LIST_DIR}/../../build.cmake)

# GLM
macro(GetGlm SPECIFIC_PROJECT VERSION)



  function(BuildGlm B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DGLM_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()
  BuildThirdPartyDep(glm ${CMAKE_CURRENT_LIST_DIR}/../../ext https://github.com/g-truc/glm ${VERSION} RESULT_DIR "" "BuildGlm")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/share)

  find_package(glm REQUIRED)

  target_include_directories(
    ${SPECIFIC_PROJECT}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include/
  )
endmacro()

macro(SetDynamicLibraryDir SPECIFIC_TARGET)
  set(TARGET_DIR ${CMAKE_CURRENT_LIST_DIR}/bin/${CMAKE_BUILD_TYPE})
  set_target_properties(${SPECIFIC_TARGET}
      PROPERTIES
      LIBRARY_OUTPUT_DIRECTORY  ${TARGET_DIR}
      RUNTIME_OUTPUT_DIRECTORY ${TARGET_DIR}
      LIBRARY_OUTPUT_DIRECTORY_DEBUG  ${TARGET_DIR}
      RUNTIME_OUTPUT_DIRECTORY_DEBUG ${TARGET_DIR}
      LIBRARY_OUTPUT_DIRECTORY_RELEASE  ${TARGET_DIR}
      RUNTIME_OUTPUT_DIRECTORY_RELEASE ${TARGET_DIR}
  )
  message(STATUS ${TARGET_DIR})
endmacro()


