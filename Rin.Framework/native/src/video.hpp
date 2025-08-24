#pragma once
#include <rwin/types.h>

#include "macro.hpp"

using AudioCallback = void(RIN_CALLBACK_CONVENTION *)(float* data,int count,double time);
using SourceReadCallback = void(RIN_CALLBACK_CONVENTION *)(unsigned long pos,unsigned long size, unsigned char *data);
using SourceAvailableCallback = unsigned long(RIN_CALLBACK_CONVENTION *)();
using SourceLengthCallback = unsigned long(RIN_CALLBACK_CONVENTION *)();
RIN_NATIVE_API void * videoContextCreate();
RIN_NATIVE_API void * videoContextFromFile(const char * filename);
RIN_NATIVE_API void * videoContextFromSource(void * source);
RIN_NATIVE_API int videoContextHasVideo(void *context);
RIN_NATIVE_API rwin::Extent2D videoContextGetVideoExtent(void *context);
RIN_NATIVE_API void videoContextSeek(void *context, double time);
RIN_NATIVE_API int videoContextHasAudio(void *context);
RIN_NATIVE_API void videoContextSetAudioCallback(void *context,AudioCallback audioCallback);
RIN_NATIVE_API int videoContextGetAudioSampleRate(void *context);
RIN_NATIVE_API int videoContextGetAudioChannels(void *context);
RIN_NATIVE_API int videoContextGetAudioTrackCount(void *context);
RIN_NATIVE_API void videoContextSetAudioTrack(void *context,int track);
RIN_NATIVE_API double videoContextGetDuration(void *context);
RIN_NATIVE_API double videoContextGetPosition(void *context);
RIN_NATIVE_API void videoContextDecode(void *context, double delta);
RIN_NATIVE_API int videoContextEnded(void *context);
RIN_NATIVE_API void * videoContextCopyRecentFrame(void *context, double timestamp);
RIN_NATIVE_API void videoContextSetSource(void *context,void * source);
RIN_NATIVE_API void videoContextFree(void* context);
RIN_NATIVE_API void * videoSourceCreate(SourceReadCallback readCallback,SourceAvailableCallback availableCallback,SourceLengthCallback lengthCallback);
RIN_NATIVE_API void videoSourceFree(void * source);
