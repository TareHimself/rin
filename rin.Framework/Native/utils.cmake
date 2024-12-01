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
endfunction()


