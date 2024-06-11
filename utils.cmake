
function(Fetch REPOSITORY BRANCH DESTINATION)
  if(NOT EXISTS ${DESTINATION})
    execute_process(
      COMMAND git clone --depth 1 --branch ${BRANCH} ${REPOSITORY} ${DESTINATION}
    )
  endif()
endfunction()

function(GetBuildExt B_EXT)
  if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
    set(${B_EXT} "_debug" PARENT_SCOPE)
  else()
    set(${B_EXT} "" PARENT_SCOPE)
  endif()
endfunction()

function(BuildOnly CLONED_DIR BUILD_DEST LOCAL_BUILD_TYPE BUILD_FN)
  set(BUILD_DIR ${CLONED_DIR}/build/${LOCAL_BUILD_TYPE})
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
endfunction()

function(FetchAndBuild REPOSITORY BRANCH TARGET_NAME BUILD_DEST TEMP_DEST PRE_BUILD_FN BUILD_FN)
  set(TARGET_NAME_DEST ${BUILD_DEST}/${TARGET_NAME}_check)
  if(NOT EXISTS ${BUILD_DEST})
    set(CLONED_DIR ${TEMP_DEST})

    Fetch(${REPOSITORY} ${BRANCH} ${CLONED_DIR})

    if(NOT "${PRE_BUILD_FN}" STREQUAL "")
      cmake_language(CALL ${PRE_BUILD_FN} ${CLONED_DIR})
    endif()

    if(CMAKE_BUILD_TYPE)
      if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
        BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Debug" "${BUILD_FN}")
      else()
        BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Release" "${BUILD_FN}")
      endif()
    else()
      BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Release" "${BUILD_FN}")
    endif()

    #file(GENERATE OUTPUT ${TARGET_NAME_DEST} CONTENT "")

    #file(REMOVE_RECURSE ${CLONED_DIR})
  endif()
endfunction()

# Fetches and builds a dependency
function(BuildThirdPartyDep FOLDER_NAME REPOSITORY VERSION RESULT PRE_BUILD_FN BUILD_ARGS)
  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../ext)
  #set(RESULT_DIR ${THIRD_PARTY_DIR})
  set(RESULT_DIR ${THIRD_PARTY_DIR}/${FOLDER_NAME}_${VERSION})
  set(CLONE_DIR ${CMAKE_CURRENT_BINARY_DIR}/${FOLDER_NAME})

  FetchAndBuild(${REPOSITORY} ${VERSION} ${FOLDER_NAME} ${RESULT_DIR} ${CLONE_DIR} "${PRE_BUILD_FN}" "${BUILD_ARGS}")
  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

macro(AddTargetLibs EXTERNAL_TARGET)
  get_target_property(EXTERNAL_TARGET_LIBRARY_PATH ${EXTERNAL_TARGET} LOCATION)
  list(APPEND VENGINE_LIBS ${EXTERNAL_TARGET_LIBRARY_PATH})
  set(EXTERNAL_TARGET_LIBRARY_PATH)
endmacro()
# SimdJson
macro(GetSimdJson SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(simdjson https://github.com/simdjson/simdjson ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/simdjson)

  find_package(simdjson CONFIG REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC simdjson::simdjson)
  AddTargetLibs("simdjson::simdjson")
endmacro()

# Json
macro(GetJson SPECIFIC_PROJECT VERSION)

function(BuildJson B_TYPE B_SRC B_DEST)
execute_process(
  COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DJSON_BuildTests=OFF -S ${B_SRC} -B ${B_DEST}
)
endfunction()

  BuildThirdPartyDep(nlohmann_json https://github.com/nlohmann/json ${VERSION} RESULT_DIR "" "BuildJson")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/share/cmake/nlohmann_json)

  find_package(nlohmann_json REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC nlohmann_json::nlohmann_json)

endmacro()



# FastGLTF
macro(GetFastGLTF SPECIFIC_PROJECT VERSION)
  

  function(BuildFastGLTF B_TYPE B_SRC B_DEST)
    # execute_process(
    #   COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DFASTGLTF_DOWNLOAD_SIMDJSON=OFF -DCMAKE_PREFIX_PATH=${CMAKE_PREFIX_PATH} -S ${B_SRC} -B ${B_DEST}
    # )
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(fastgltf https://github.com/TareHimself/fastgltf ${VERSION} RESULT_DIR "" "BuildFastGLTF")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/fastgltf)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/fastgltf/simdjson)

  find_package(fastgltf_simdjson REQUIRED)
  find_package(fastgltf CONFIG REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC fastgltf::fastgltf)
  AddTargetLibs("fastgltf::fastgltf")
endmacro()

# Miniz
macro(GetMiniz SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(miniz https://github.com/richgel999/miniz ${VERSION} MINIZ_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${MINIZ_DIR}/lib/cmake/miniz)

  find_package(miniz CONFIG REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${MINIZ_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC miniz::miniz)
  AddTargetLibs("miniz::miniz")
endmacro()

# uuid
macro(GetStdUUID SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(stduuid https://github.com/mariusbancila/stduuid ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/stduuid)
  find_package(stduuid REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include)
endmacro()



# XXHash
macro(GetXXHash SPECIFIC_PROJECT VERSION)

  function(BuildXXHash B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DXXHASH_BUILD_XXHSUM=OFF -S ${B_SRC}/cmake_unofficial -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(xxhash https://github.com/Cyan4973/xxHash ${VERSION} RESULT_DIR "" "BuildXXHash")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(xxHash REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC xxHash::xxhash)
endmacro()

# FMT
macro(GetFmt SPECIFIC_PROJECT VERSION)

  function(BuildFmt B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DFMT_DOC=OFF -DFMT_TEST=OFF -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(fmt https://github.com/fmtlib/fmt ${VERSION} RESULT_DIR "" "BuildFmt")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(fmt REQUIRED)
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC fmt::fmt)

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

# spdlog
macro(GetSpdLog SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(spdlog https://github.com/gabime/spdlog ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(spdlog REQUIRED)
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC spdlog::spdlog)

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


# Bass
macro(GetBass SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(bass https://github.com/TareHimself/bass-cpp ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(basscpp REQUIRED PATHS ${RESULT_DIR}/lib/cmake)
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC basscpp::basscpp)

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

# GLM
macro(GetGlm SPECIFIC_PROJECT VERSION)



  function(BuildGlm B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DGLM_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()
  BuildThirdPartyDep(glm https://github.com/g-truc/glm ${VERSION} RESULT_DIR "" "BuildGlm")

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

# vscript
macro(GetVScript SPECIFIC_PROJECT VERSION)



  BuildThirdPartyDep(vscript https://github.com/TareHimself/vscript ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/share)

  find_package(vscript REQUIRED)

  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC vscript::vscript)

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

# ReactPhysics3D
macro(GetReactPhys SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(rp3d https://github.com/DanielChappuis/reactphysics3d ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(ReactPhysics3D REQUIRED PATHS ${RESULT_DIR}/lib/cmake)

  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC ReactPhysics3D::ReactPhysics3D)
  
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


# Argparse
macro(GetArgparse SPECIFIC_PROJECT VERSION)

  function(BuildArgparse B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DARGPARSE_BUILD_TESTS=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(argparse https://github.com/p-ranav/argparse ${VERSION} RESULT_DIR "" "BuildArgparse")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(argparse REQUIRED)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC argparse::argparse)
  
endmacro()

# # LibJpegTurbo
# macro(GetLibJpegTurbo SPECIFIC_PROJECT VERSION)

#   function(BuildLibJpegTurbo B_TYPE B_SRC B_DEST)
#     execute_process(
#       COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DENABLE_SHARED=OFF -DENABLE_STATIC=ON -S ${B_SRC} -B ${B_DEST}
#     )
#   endfunction()

#   BuildThirdPartyDep(libjpegturbo https://github.com/libjpeg-turbo/libjpeg-turbo ${VERSION} RESULT_DIR "" "BuildLibJpegTurbo")

#   # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

#   find_package(libjpeg-turbo REQUIRED)
#   target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
#   target_link_libraries(${SPECIFIC_PROJECT} PUBLIC libjpeg-turbo::jpeg-static)
  
# endmacro()

# STB
macro(GetStb SPECIFIC_PROJECT VERSION)

  set(RESULT_DIR ${CMAKE_CURRENT_LIST_DIR}/ext/stb)

  if(NOT EXISTS ${RESULT_DIR})
    set(REPO_DIR ${CMAKE_CURRENT_BINARY_DIR}/stb)
    Fetch(https://github.com/nothings/stb ${VERSION} ${REPO_DIR})
    file(MAKE_DIRECTORY ${RESULT_DIR})
    file(COPY ${REPO_DIR}/stb_image.h DESTINATION ${RESULT_DIR})
    file(COPY ${REPO_DIR}/stb_image_write.h DESTINATION ${RESULT_DIR})
  endif()

  target_include_directories(
    ${SPECIFIC_PROJECT}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}>
    $<INSTALL_INTERFACE:include> 
  )

  target_sources(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/stb_image.h ${RESULT_DIR}/stb_image_write.h)
  #target_compile_definitions(${SPECIFIC_PROJECT} PUBLIC SPNG_USE_MINIZ)
endmacro()

# FreeType
macro(GetFreeType SPECIFIC_PROJECT VERSION)

  BuildThirdPartyDep(freetype https://gitlab.freedesktop.org/freetype/freetype ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(freetype REQUIRED PATHS ${RESULT_DIR}/lib/cmake)

  target_include_directories(
    ${SPECIFIC_PROJECT}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include/freetype2>
    $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC freetype)
endmacro()

# Tinyxml2
macro(GetTinyxml2 SPECIFIC_PROJECT VERSION)
  BuildThirdPartyDep(tinyxml2 https://github.com/leethomason/tinyxml2 ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(tinyxml2 REQUIRED)

  target_include_directories(
          ${SPECIFIC_PROJECT}
          PUBLIC
          $<BUILD_INTERFACE:${RESULT_DIR}/include>
          $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC tinyxml2::tinyxml2)
endmacro()

# MsdfGen
macro(GetMsdfGen SPECIFIC_PROJECT VERSION)


  function(BuildMsdfGen B_TYPE B_SRC B_DEST)
    execute_process(#
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DMSDFGEN_DYNAMIC_RUNTIME=ON -DBUILD_SHARED_LIBS=ON -DMSDFGEN_USE_SKIA=OFF -DMSDFGEN_INSTALL=ON -DMSDFGEN_CORE_ONLY=ON -DMSDFGEN_BUILD_STANDALONE=OFF -DMSDFGEN_USE_VCPKG=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(msdfgen https://github.com/Chlumsky/msdfgen ${VERSION} RESULT_DIR "" "BuildMsdfGen")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(msdfgen REQUIRED PATHS ${RESULT_DIR}/lib/cmake)

  target_include_directories(
    ${SPECIFIC_PROJECT}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include>
  )
  
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC msdfgen::msdfgen)
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


