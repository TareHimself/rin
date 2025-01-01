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

macro(FindSlang TARGET_PROJECT)
  set(SLANG_BINARIES_OUTPUT_DIR ${CMAKE_CURRENT_LIST_DIR}/bin/${CMAKE_BUILD_TYPE})
  set(DOWNLOAD_DIR ${CMAKE_CURRENT_BINARY_DIR})
  set(SLANG_OUTPUT_DIR ${DOWNLOAD_DIR}/slang)
  
  if(NOT EXISTS ${SLANG_OUTPUT_DIR})
        set(DOWNLOADED_FILE ${DOWNLOAD_DIR}/slang.zip)
        set(SLANG_VERSION 2024.17)
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
  file(COPY ${SLANG_BINARIES} DESTINATION ${SLANG_BINARIES_OUTPUT_DIR})
  message(STATUS "SLANG BINARIES ${SLANG_BINARIES}")
  find_library(SLANG_LIBRARY
      NAMES slang gfx slang-rt
      HINTS "${SLANG_OUTPUT_DIR}/lib"
  )
  target_include_directories(${TARGET_PROJECT} PRIVATE ${SLANG_OUTPUT_DIR}/include)
  target_link_libraries(${TARGET_PROJECT} ${SLANG_LIBRARY})
endmacro()


