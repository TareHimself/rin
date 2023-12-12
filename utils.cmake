
function(GetGlfw VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(GLFW_DIR ${THIRD_PARTY_DIR}/glfw)

  if(NOT EXISTS ${GLFW_DIR})
    set(CLONED_DIR ${CMAKE_CURRENT_BINARY_DIR}/glfw)

    execute_process(
      COMMAND git clone --depth 1 --branch ${VERSION} https://github.com/glfw/glfw ${CLONED_DIR}
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=Release -S ${CLONED_DIR} -B ${CLONED_DIR}/build/
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} --build ${CLONED_DIR}/build --config Release
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} --install ${CLONED_DIR}/build --prefix ${GLFW_DIR}
    )
  endif()

  set(${RESULT} ${GLFW_DIR} PARENT_SCOPE)
endfunction()

function(GetGlm VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(GLM_DIR ${THIRD_PARTY_DIR}/glm)

  if(NOT EXISTS ${GLM_DIR})
    set(DOWNLOADED_FILE ${CMAKE_CURRENT_BINARY_DIR}/glfw.zip)

    file(DOWNLOAD https://github.com/g-truc/glm/releases/download/${VERSION}/glm-${VERSION}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)

    file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${THIRD_PARTY_DIR})
  endif()

  set(${RESULT} ${GLM_DIR} PARENT_SCOPE)
endfunction()

function(GetReactPhys VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(REACT_PHYS_DIR ${THIRD_PARTY_DIR}/reactphysics3d)

  if(NOT EXISTS ${REACT_PHYS_DIR})
    set(CLONED_DIR ${CMAKE_CURRENT_BINARY_DIR}/reactphysics3d)

    execute_process(
      COMMAND git clone --depth 1 --branch ${VERSION} https://github.com/DanielChappuis/reactphysics3d ${CLONED_DIR}
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=Release -S ${CLONED_DIR} -B ${CLONED_DIR}/build/
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} --build ${CLONED_DIR}/build --config Release
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} --install ${CLONED_DIR}/build --prefix ${REACT_PHYS_DIR}
    )
  endif()

  set(${RESULT} ${REACT_PHYS_DIR} PARENT_SCOPE)
endfunction()