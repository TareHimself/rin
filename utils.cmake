function(Fetch REPOSITORY BRANCH DESTINATION)
  if(NOT EXISTS ${DESTINATION})
    execute_process(
      COMMAND git clone --depth 1 --branch ${BRANCH} ${REPOSITORY} ${DESTINATION}
    )
  endif()
endfunction()

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


function(GetBass)
    set(OUTPUT_DIR ${CMAKE_CURRENT_LIST_DIR}/bin/${CMAKE_BUILD_TYPE})
    set(DOWNLOAD_DIR ${CMAKE_CURRENT_BINARY_DIR}/_dl)
    set(CHECKPOINT_FILE ${CMAKE_CURRENT_BINARY_DIR}/_bass_checkpoint)
    
    if(NOT EXISTS ${CHECKPOINT_FILE})
        if(NOT EXISTS ${OUTPUT_DIR})
            file(MAKE_DIRECTORY ${OUTPUT_DIR})
        endif()
        set(DOWNLOADED_FILE ${DOWNLOAD_DIR}/bass.zip)
        set(EXTRACTED_FOLDER ${DOWNLOAD_DIR}/bass)
        if(WIN32)
          set(OUTPUT_FILE ${OUTPUT_DIR}/bass.dll)
          if(NOT EXISTS OUTPUT_FILE)
              file(DOWNLOAD https://www.un4seen.com/files/bass24.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${EXTRACTED_FOLDER})
              file(COPY ${EXTRACTED_FOLDER}/x64/bass.dll DESTINATION ${OUTPUT_DIR})
          endif()
        elseif(LINUX)
            set(OUTPUT_FILE ${OUTPUT_DIR}/libbass.so)
          if(NOT EXISTS OUTPUT_FILE)
              file(DOWNLOAD https://www.un4seen.com/files/bass24-linux.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${EXTRACTED_FOLDER})
              file(COPY ${EXTRACTED_FOLDER}/libs/x86_64/libbass.so DESTINATION ${OUTPUT_DIR})
          endif()
        elseif(APPLE)
            set(OUTPUT_FILE ${OUTPUT_DIR}/libbass.dylib)
          if(NOT EXISTS OUTPUT_FILE)
              file(DOWNLOAD https://www.un4seen.com/files/bass24-osx.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${EXTRACTED_FOLDER})
              file(COPY ${EXTRACTED_FOLDER}/libbass.dylib DESTINATION ${OUTPUT_DIR})
          endif()
        endif()
        file(REMOVE_RECURSE ${DOWNLOAD_DIR})
        file(TOUCH ${CHECKPOINT_FILE}) 
    endif()
endfunction()

macro(GetSlang TARGET_PROJECT SLANG_VERSION)
  set(SLANG_BINARIES_OUTPUT_DIR ${CMAKE_CURRENT_LIST_DIR}/bin/${CMAKE_BUILD_TYPE})
  set(DOWNLOAD_DIR ${CMAKE_CURRENT_BINARY_DIR})
  set(SLANG_OUTPUT_DIR ${DOWNLOAD_DIR}/slang_${SLANG_VERSION})
  
  if(NOT EXISTS ${SLANG_OUTPUT_DIR})
        set(DOWNLOADED_FILE ${DOWNLOAD_DIR}/slang.zip)
        if(WIN32)
            file(DOWNLOAD https://github.com/shader-slang/slang/releases/download/v${SLANG_VERSION}/slang-${SLANG_VERSION}-windows-x86_64.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
            file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
        elseif(LINUX)
            message( FATAL_ERROR "Slang support not added for Linux." )
        elseif(APPLE)
            message( FATAL_ERROR "Slang support not added for Apple." )
        endif()
  endif()
  file(GLOB SLANG_BINARIES
    "${SLANG_OUTPUT_DIR}/bin/*.*"
  )
  add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND python ${CMAKE_CURRENT_LIST_DIR}/copy_s.py "${SLANG_BINARIES}" "$<TARGET_FILE_DIR:${PROJECT_NAME}>"
  )
  find_library(SLANG_LIBRARY
      NAMES slang gfx slang-rt
      HINTS "${SLANG_OUTPUT_DIR}/lib"
  )
  target_include_directories(${TARGET_PROJECT} PRIVATE ${SLANG_OUTPUT_DIR}/include)
  target_link_libraries(${TARGET_PROJECT} PUBLIC ${SLANG_LIBRARY})
endmacro()

# STB
macro(GetStb TARGET_PROJECT VERSION)
  set(RESULT_DIR ${CMAKE_CURRENT_BINARY_DIR}/stb_${VERSION})
  set(OUTPUT_FILES "")
  if(NOT EXISTS ${RESULT_DIR})
    set(REPO_DIR ${CMAKE_CURRENT_BINARY_DIR}/stb)
    Fetch(https://github.com/nothings/stb ${VERSION} ${REPO_DIR})

    file(MAKE_DIRECTORY ${RESULT_DIR})

    set(FILES_TO_COPY "stb_image.h" "stb_image_write.h")

    foreach(TO_COPY ${FILES_TO_COPY})
      file(COPY ${REPO_DIR}/${TO_COPY} DESTINATION ${RESULT_DIR}/include/stb)
      list(APPEND OUTPUT_FILES ${RESULT_DIR}/include/stb/${TO_COPY})
    endforeach()

    file(REMOVE_RECURSE ${REPO_DIR})
    unset(FILES_TO_COPY)
    unset(REPO_DIR)
  endif()

  target_include_directories(
    ${TARGET_PROJECT}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include>
  )

  target_sources(${TARGET_PROJECT} PUBLIC ${OUTPUT_FILES})
  unset(OUTPUT_FILES)
  unset(RESULT_DIR)
endmacro()
