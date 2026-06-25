#pragma once
#include "macro.hpp"
#include <cstdint>
#include <cstddef>

using TParams = void*;
using TState = void*;

struct AudioEffectContext
{
    float time;
    int sampleRate;
    int channels;
};

struct AudioEffect
{
    std::uint64_t effectId{0};
    void* descriptorHandle{nullptr};
    TParams parameters{};
    TState state{};
    void (*processCallback)(float*, float*,int,AudioEffectContext*,TParams,TState){};
    void (*cleanupCallback)(void*,TParams,TState){};
};
struct AudioEffectInstance
{
    AudioEffect effect{};
    ~AudioEffectInstance()
    {
        effect.cleanupCallback(effect.descriptorHandle,effect.parameters,effect.state);
    }
};

// Engine
RIN_NATIVE_API int   audioInit();
RIN_NATIVE_API void  audioShutdown();
RIN_NATIVE_API float audioGetVolume();
RIN_NATIVE_API void  audioSetVolume(float volume);

// IAudioSample factory — returns sample handle (0 on failure)
RIN_NATIVE_API std::uint64_t audioMakeSampleFromFile(const char* filePath);
RIN_NATIVE_API std::uint64_t audioMakeSampleFromMemory(const std::uint8_t* data, std::size_t size);
RIN_NATIVE_API std::uint64_t audioMakeStreamFromFile(const char* filePath);
RIN_NATIVE_API std::uint64_t audioMakeStreamFromMemory(const std::uint8_t* data, std::size_t size);
RIN_NATIVE_API void          audioSampleDispose(std::uint64_t sampleId);

// IAudioSample.MakeActive() — returns active audio handle (0 on failure)
RIN_NATIVE_API std::uint64_t audioSampleMakeActive(std::uint64_t sampleId);

// IActiveAudio / IChannel — same handle for both
RIN_NATIVE_API int    audioActiveIsPlaying(std::uint64_t id);
RIN_NATIVE_API double audioActiveGetPosition(std::uint64_t id);
RIN_NATIVE_API double audioActiveGetDuration(std::uint64_t id);
RIN_NATIVE_API int    audioActivePlay(std::uint64_t id, int restart);
RIN_NATIVE_API int    audioActivePause(std::uint64_t id);
RIN_NATIVE_API int    audioActiveSetVolume(std::uint64_t id, float volume);
RIN_NATIVE_API int    audioActiveSetPosition(std::uint64_t id, double positionSeconds);
RIN_NATIVE_API void   audioActiveDispose(std::uint64_t id);

// IPushStream — id also works with audioActive* functions
RIN_NATIVE_API std::size_t   audioPushStreamPush(std::uint64_t id, const std::uint8_t* data, std::size_t size);

struct Vec3 { float x, y, z; };

// IMixer
RIN_NATIVE_API int audioMixerAddEffect(std::uint64_t mixerId,const AudioEffect& effect);
RIN_NATIVE_API void audioMixerRemoveEffect(std::uint64_t mixerId,std::uint64_t effectId);
RIN_NATIVE_API std::uint64_t audioMixerCreate(std::uint64_t parentId);
RIN_NATIVE_API void          audioMixerDispose(std::uint64_t mixerId);
RIN_NATIVE_API float         audioMixerGetVolume(std::uint64_t mixerId);
RIN_NATIVE_API void          audioMixerSetVolume(std::uint64_t mixerId, float volume);
RIN_NATIVE_API std::uint64_t audioMixerPlay(std::uint64_t mixerId, std::uint64_t sampleId);
RIN_NATIVE_API std::uint64_t audioMixerCreatePushStream(std::uint64_t mixerId, int sampleRate, int channels);
RIN_NATIVE_API std::uint64_t audioGetMasterMixerId();

// IDirectionalBus (reuses audioScene* native names)
RIN_NATIVE_API std::uint64_t audioCreateScene(std::uint64_t parentId);
RIN_NATIVE_API void          audioSceneSetListenerPose(std::uint64_t sceneId,
                                 Vec3 position, Vec3 forward, Vec3 right);
RIN_NATIVE_API std::uint64_t audioScenePlayAtLocation(std::uint64_t sceneId, std::uint64_t sampleId,
                                 Vec3 location);
RIN_NATIVE_API std::uint64_t audioScenePlay2d(std::uint64_t sceneId, std::uint64_t sampleId);
RIN_NATIVE_API float         audioSceneGetVolume(std::uint64_t sceneId);
RIN_NATIVE_API void          audioSceneSetVolume(std::uint64_t sceneId, float volume);
RIN_NATIVE_API void          audioSceneDispose(std::uint64_t sceneId);
