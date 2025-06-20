#pragma once
#include "macro.hpp"
#include "structs.hpp"

using AudioCallback = void(RIN_CALLBACK_CONVENTION *)(float* data,int count,double time);
using SourceReadCallback = void(RIN_CALLBACK_CONVENTION *)(unsigned long pos,unsigned long size, unsigned char *data);
using SourceAvailableCallback = unsigned long(RIN_CALLBACK_CONVENTION *)();
using SourceLengthCallback = unsigned long(RIN_CALLBACK_CONVENTION *)();
EXPORT_DECL void * videoContextCreate();
EXPORT_DECL void * videoContextFromFile(char * filename);
EXPORT_DECL void * videoContextFromSource(void * source);
EXPORT_DECL int videoContextHasVideo(void *context);
EXPORT_DECL Extent2D videoContextGetVideoExtent(void *context);
EXPORT_DECL void videoContextSeek(void *context, double time);
EXPORT_DECL int videoContextHasAudio(void *context);
EXPORT_DECL void videoContextSetAudioCallback(void *context,AudioCallback audioCallback);
EXPORT_DECL int videoContextGetAudioSampleRate(void *context);
EXPORT_DECL int videoContextGetAudioChannels(void *context);
EXPORT_DECL int videoContextGetAudioTrackCount(void *context);
EXPORT_DECL void videoContextSetAudioTrack(void *context,int track);
EXPORT_DECL double videoContextGetDuration(void *context);
EXPORT_DECL double videoContextGetPosition(void *context);
EXPORT_DECL void videoContextDecode(void *context, double delta);
EXPORT_DECL int videoContextEnded(void *context);
EXPORT_DECL void * videoContextCopyRecentFrame(void *context, double timestamp);
EXPORT_DECL void videoContextSetSource(void *context,void * source);
EXPORT_DECL void videoContextFree(void* context);
EXPORT_DECL void * videoSourceCreate(SourceReadCallback readCallback,SourceAvailableCallback availableCallback,SourceLengthCallback lengthCallback);
EXPORT_DECL void videoSourceFree(void * source);
