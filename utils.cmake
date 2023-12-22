
function(Fetch REPOSITORY BRANCH DESTINATION)
  if(NOT EXISTS ${DESTINATION})
    execute_process(
      COMMAND git clone --depth 1 --branch ${BRANCH} ${REPOSITORY} ${DESTINATION}
    )
  endif()
endfunction()

function(GetBuildExt B_EXT)
  if("${CMAKE_BUILD_TYPE}" STREQUAL "Release")
    set(${B_EXT} "" PARENT_SCOPE)
  else()
    set(${B_EXT} "_debug" PARENT_SCOPE)
  endif()
endfunction()

function(FetchAndBuild REPOSITORY BRANCH BUILD_DEST TEMP_DEST PRE_BUILD_FN BUILD_FN)
  if(NOT EXISTS ${BUILD_DEST})
    set(CLONED_DIR ${TEMP_DEST})
    
    if("${CMAKE_BUILD_TYPE}" STREQUAL "Release")
      set(LOCAL_BUILD_TYPE "Release")
    else()
      set(LOCAL_BUILD_TYPE "Debug")
    endif()

    set(BUILD_DIR ${CLONED_DIR}/build/${LOCAL_BUILD_TYPE})

    Fetch(${REPOSITORY} ${BRANCH} ${CLONED_DIR})

    if(NOT "${PRE_BUILD_FN}" STREQUAL "")
      cmake_language(CALL ${PRE_BUILD_FN} ${CLONED_DIR})
    endif()

    if("${BUILD_FN}" STREQUAL "")
      execute_process(
        COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${LOCAL_BUILD_TYPE} -S ${CLONED_DIR} -B ${BUILD_DIR}
      )
    else()
      cmake_language(CALL ${BUILD_FN} ${LOCAL_BUILD_TYPE} ${CLONED_DIR} ${BUILD_DIR})
    endif()

    execute_process(
      COMMAND ${CMAKE_COMMAND} --build ${BUILD_DIR} --config ${LOCAL_BUILD_TYPE}
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} --install ${BUILD_DIR} --prefix ${BUILD_DEST} --config ${LOCAL_BUILD_TYPE}
    )
  endif()
endfunction()

function(BuildThirdPartyDep FOLDER_NAME REPOSITORY VERSION RESULT PRE_BUILD_FN BUILD_ARGS)
  GetBuildExt(BUILD_EXT)
  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(RESULT_DIR ${THIRD_PARTY_DIR}/${FOLDER_NAME}${BUILD_EXT})
  set(CLONE_DIR ${CMAKE_CURRENT_BINARY_DIR}/${FOLDER_NAME})

  FetchAndBuild(${REPOSITORY} ${VERSION} ${RESULT_DIR} ${CLONE_DIR} "${PRE_BUILD_FN}" "${BUILD_ARGS}")
  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

function(GetVulkanMemoryAllocator VERSION RESULT)

  BuildThirdPartyDep(vkm https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator ${VERSION} RESULT_DIR "" "")

  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

function(GetGLSL VERSION RESULT)
  function(UpdateGlslDeps CLONED_PATH)
    execute_process(
      COMMAND python update_glslang_sources.py
      WORKING_DIRECTORY ${CLONED_PATH}
    )
  endfunction()

  function(BuildGlsl B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DSKIP_SPIRV_TOOLS_INSTALL=ON -DENABLE_GLSLANG_BINARIES=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()
  BuildThirdPartyDep(glslang https://github.com/KhronosGroup/glslang ${VERSION} RESULT_DIR "UpdateGlslDeps" "BuildGlsl")

  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

function(GetVkBootstrap VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(VK_BOOTSTRAP_DIR ${THIRD_PARTY_DIR}/vkb)

  Fetch(https://github.com/charles-lunarg/vk-bootstrap ${VERSION} ${VK_BOOTSTRAP_DIR})

  set(${RESULT} ${VK_BOOTSTRAP_DIR} PARENT_SCOPE)
endfunction()

function(GetSDL VERSION RESULT)

  BuildThirdPartyDep(sdl https://github.com/libsdl-org/SDL ${VERSION} RESULT_DIR "" "")
  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

function(GetGlm VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(GLM_DIR ${THIRD_PARTY_DIR}/glm)

  if(NOT EXISTS ${GLM_DIR})
    set(DOWNLOADED_FILE ${CMAKE_CURRENT_BINARY_DIR}/sdl.zip)

    file(DOWNLOAD https://github.com/g-truc/glm/releases/download/${VERSION}/glm-${VERSION}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)

    file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${THIRD_PARTY_DIR})
  endif()

  set(${RESULT} ${GLM_DIR} PARENT_SCOPE)
endfunction()

function(GetReactPhys VERSION RESULT)

  BuildThirdPartyDep(rp3d https://github.com/DanielChappuis/reactphysics3d ${VERSION} RESULT_DIR "" "")

  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()