#define MINIAUDIO_IMPLEMENTATION
#define NOMINMAX
#define MA_ENGINE_MAX_LISTENERS 30
#include <miniaudio.h>
#include "api.hpp"
#include <algorithm>
#include <atomic>
#include <cstdint>
#include <memory>
#include <mutex>
#include <ranges>
#include <set>
#include <string>
#include <unordered_map>
#include <vector>

static ma_engine g_engine;
static std::atomic<std::uint64_t> g_nextId{1};
static std::mutex g_mutex;

enum class AudioState : std::uint8_t { Idle, Playing, Paused };

struct PushStreamSource
{
    ma_data_source_base base{};
    std::mutex mutex{};
    std::vector<std::uint8_t> buffer{};
    std::size_t readPos{0};
    std::atomic<ma_uint64> framesConsumed{0};
    ma_format format{};
    ma_uint32 channels{};
    ma_uint32 sampleRate{};
};

struct ActiveAudio
{
    ma_sound sound{};
    std::vector<std::uint8_t> ownedData{};
    std::unique_ptr<ma_decoder> decoder{};
    std::unique_ptr<PushStreamSource> pushSource{};
    std::uint64_t ownerBusId{0}; // 0 = ungrouped (push streams only)
    bool ownerIsScene{false};
};

struct AudioSample
{
    bool isStream{false};
    std::string filePath{};
    std::vector<std::uint8_t> fileData{};
};

struct EffectManager
{
    using AudioEffectVectorPtr = std::vector<std::shared_ptr<AudioEffectInstance>>*;
    std::vector<std::shared_ptr<AudioEffectInstance>> effects{};
    std::atomic<AudioEffectVectorPtr> pendingEffects{};
    std::atomic<AudioEffectVectorPtr> activeEffects{};
    std::atomic<AudioEffectVectorPtr> staleEffects{};

    void UpdatePending()
    {
        delete staleEffects.exchange(nullptr);
        delete pendingEffects.exchange(new std::vector(effects));
    }

    void AddEffect(const std::shared_ptr<AudioEffectInstance>& effect)
    {
        effects.push_back(effect);
        UpdatePending();
    }

    void RemoveEffect(const std::uint64_t& effectId)
    {
        if (const auto it = std::ranges::find_if(effects, [effectId](const std::shared_ptr<AudioEffectInstance>& a)
        {
            return a->effect.effectId == effectId;
        }); it != effects.end())
        {
            effects.erase(it);
        }
        UpdatePending();
    }

    ~EffectManager()
    {
        delete pendingEffects.exchange(nullptr);
        delete staleEffects.exchange(nullptr);
    }
};

struct EffectNode
{
    ma_node_base base{};
    std::shared_ptr<EffectManager> effectManager{};
    double timeSeconds{0.0};
    std::vector<float> scratch{};
};

static void effectNode_process(ma_node* pNode, const float** ppFramesIn, ma_uint32* /*pFrameCountIn*/,
                               // ReSharper disable once CppParameterMayBeConstPtrOrRef
                               float** ppFramesOut, ma_uint32* pFrameCountOut)
{
    auto* node = static_cast<EffectNode*>(pNode);
    const ma_uint32 frameCount = *pFrameCountOut;
    const ma_uint32 channels = ma_node_get_output_channels(pNode, 0);

    const int sr = static_cast<int>(ma_engine_get_sample_rate(&g_engine));
    node->timeSeconds += static_cast<double>(frameCount) / sr;

    if (auto* pending = node->effectManager->pendingEffects.exchange(nullptr))
    {
        auto* stale = node->effectManager->activeEffects.exchange(pending);
        node->effectManager->staleEffects.exchange(stale);
    }
    const ma_uint32 sampleCount = frameCount * channels;
    memcpy(ppFramesOut[0], ppFramesIn[0], sampleCount * sizeof(float));

    if (const auto* active = node->effectManager->activeEffects.load(std::memory_order_acquire))
    {

        memcpy(node->scratch.data(), ppFramesIn[0], sampleCount * sizeof(float));


        AudioEffectContext ctx{static_cast<float>(node->timeSeconds), sr, static_cast<int>(channels)};


        for (const auto& inst : *active)
        {
            inst->effect.processCallback(node->scratch.data(), ppFramesOut[0],
                                         static_cast<int>(sampleCount), &ctx,
                                         inst->effect.parameters, inst->effect.state);

            memcpy(node->scratch.data(), ppFramesOut[0], sampleCount * sizeof(float));
        }
    }
}

static ma_node_vtable g_effectNodeVtable = {effectNode_process, nullptr, 1, 1, 0};

struct AudioMixer
{
    ma_sound_group group{};
    EffectNode effectNode{};
    std::vector<std::uint64_t> activeSounds{};
};

struct AudioScene
{
    ma_uint32 listenerIndex{0};
    ma_sound_group group{};
    std::vector<std::uint64_t> activeSounds;
};


static std::unordered_map<std::uint64_t, AudioSample> g_samples;
static std::unordered_map<std::uint64_t, std::unique_ptr<ActiveAudio>> g_activeAudio;
static std::unordered_map<std::uint64_t, AudioMixer> g_mixers;
static std::unordered_map<std::uint64_t, AudioScene> g_scenes;
static std::uint64_t g_masterBusId{0};
static ma_uint32 g_nextListenerIndex{0};
static std::set<ma_uint32> g_freeListenerIndices;
static ma_uint64 g_deviceLatencyFrames{0};

static ma_result pushSource_read(ma_data_source* ds, void* out, const ma_uint64 frameCount, ma_uint64* framesRead)
{
    auto* src = static_cast<PushStreamSource*>(ds);
    const ma_uint32 bpf = ma_get_bytes_per_frame(src->format, src->channels);
    const auto need = static_cast<std::size_t>(frameCount * bpf);

    std::lock_guard<std::mutex> lock(src->mutex);
    const std::size_t avail = src->buffer.size() - src->readPos;
    const std::size_t toCopy = avail < need ? avail : need;

    if (toCopy > 0)
    {
        memcpy(out, src->buffer.data() + src->readPos, toCopy);
        src->readPos += toCopy;
        src->framesConsumed.fetch_add(toCopy / bpf, std::memory_order_relaxed);
    }
    if (toCopy < need)
        memset(static_cast<std::uint8_t*>(out) + toCopy, 0, need - toCopy);

    *framesRead = frameCount;
    return MA_SUCCESS;
}

static ma_result pushSource_seek(ma_data_source*, ma_uint64)
{
    return MA_NOT_IMPLEMENTED;
}

static ma_result pushSource_getDataFormat(ma_data_source* ds, ma_format* format,
                                          ma_uint32* channels, ma_uint32* sampleRate, ma_channel*, std::size_t)
{
    const auto* src = static_cast<PushStreamSource*>(ds);
    if (format) *format = src->format;
    if (channels) *channels = src->channels;
    if (sampleRate) *sampleRate = src->sampleRate;
    return MA_SUCCESS;
}

static ma_result pushSource_getCursor(ma_data_source*, ma_uint64* cursor)
{
    if (cursor) *cursor = 0;
    return MA_NOT_IMPLEMENTED;
}

static ma_result pushSource_getLength(ma_data_source*, ma_uint64* length)
{
    if (length) *length = 0;
    return MA_NOT_IMPLEMENTED;
}

static ma_data_source_vtable g_pushSourceVtable = {
    pushSource_read,
    pushSource_seek,
    pushSource_getDataFormat,
    pushSource_getCursor,
    pushSource_getLength,
    nullptr,
    0
};

static ma_result initSoundFromSample(const AudioSample& sample, ActiveAudio& active,
                                     const ma_uint32 soundFlags, ma_sound_group* pGroup)
{
    if (!sample.filePath.empty())
    {
        const ma_uint32 flags = sample.isStream
                                    ? (soundFlags | MA_SOUND_FLAG_STREAM)
                                    : (soundFlags | MA_SOUND_FLAG_DECODE);
        return ma_sound_init_from_file(&g_engine, sample.filePath.c_str(), flags, pGroup, nullptr, &active.sound);
    }
    // Memory-backed audio always decodes — MA_SOUND_FLAG_STREAM requires a file path
    active.ownedData = sample.fileData;
    active.decoder = std::make_unique<ma_decoder>();
    const ma_result r = ma_decoder_init_memory(active.ownedData.data(), active.ownedData.size(),
                                               nullptr, active.decoder.get());
    if (r != MA_SUCCESS) return r;
    return ma_sound_init_from_data_source(&g_engine, active.decoder.get(), soundFlags, pGroup, &active.sound);
}

static void destroyActive(ActiveAudio& active)
{
    ma_sound_uninit(&active.sound); // must be first — detaches data source from engine before we free it
    if (active.pushSource)
        ma_data_source_uninit(&active.pushSource->base);
    if (active.decoder)
    {
        ma_decoder_uninit(active.decoder.get());
    }
}

// Caller must hold g_mutex. Returns the group for a bus or scene ID, plus which registry it came from.
static ma_sound_group* getGroupForId(std::uint64_t id, bool* outIsScene = nullptr)
{
    const auto busIt = g_mixers.find(id);
    if (busIt != g_mixers.end())
    {
        if (outIsScene) *outIsScene = false;
        return &busIt->second.group;
    }
    const auto sceneIt = g_scenes.find(id);
    if (sceneIt != g_scenes.end())
    {
        if (outIsScene) *outIsScene = true;
        return &sceneIt->second.group;
    }
    return nullptr;
}

std::uint64_t audioMixerCreate(std::uint64_t parentId)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    ma_sound_group* pParent = parentId != 0 ? getGroupForId(parentId) : nullptr;
    if (parentId != 0 && !pParent) return 0;
    const std::uint64_t id = g_nextId++;
    g_mixers[id] = AudioMixer{};
    auto& mixer = g_mixers[id];

    const ma_uint32 ch = ma_engine_get_channels(&g_engine);
    ma_uint32 chArr[1] = {ch};
    ma_node_config nodeCfg = ma_node_config_init();
    nodeCfg.vtable = &g_effectNodeVtable;
    nodeCfg.pInputChannels = chArr;
    nodeCfg.pOutputChannels = chArr;
    if (ma_node_init(ma_engine_get_node_graph(&g_engine), &nodeCfg, nullptr, &mixer.effectNode.base) != MA_SUCCESS)
    {
        g_mixers.erase(id);
        return 0;
    }
    mixer.effectNode.effectManager = std::make_shared<EffectManager>();
    mixer.effectNode.scratch.resize(
        ma_engine_get_device(&g_engine)->playback.internalPeriodSizeInFrames * ch * 2);

    ma_node* parentNode = pParent
                              ? reinterpret_cast<ma_node*>(pParent)
                              : ma_node_graph_get_endpoint(ma_engine_get_node_graph(&g_engine));
    ma_node_attach_output_bus(&mixer.effectNode.base, 0, parentNode, 0);

    if (ma_sound_group_init(&g_engine, 0, nullptr, &mixer.group) != MA_SUCCESS)
    {
        ma_node_uninit(&mixer.effectNode.base, nullptr);
        g_mixers.erase(id);
        return 0;
    }
    ma_node_attach_output_bus(&mixer.group, 0, &mixer.effectNode.base, 0);

    return id;
}

void audioMixerDispose(std::uint64_t mixerId)
{
    if (mixerId == 0 || mixerId == g_masterBusId) return;

    std::vector<std::uint64_t> sounds;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto it = g_mixers.find(mixerId);
        if (it == g_mixers.end()) return;
        sounds = std::move(it->second.activeSounds);
        // bus stays in g_buses until after sounds are disposed
    }

    // dispose owned sounds; each call briefly locks to unlink from activeSounds (now empty) — no-op
    for (const auto soundId : sounds) audioActiveDispose(soundId);

    // extract and uninit the group outside the lock (same pattern as destroyActive)
    decltype(g_mixers)::node_type node;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        node = g_mixers.extract(mixerId);
    }
    if (!node.empty())
    {
        ma_sound_group_uninit(&node.mapped().group);
        ma_node_uninit(&node.mapped().effectNode.base, nullptr);
        // EffectManager shared_ptr destructs here — AudioEffectInstance destructors fire → cleanupCallback
    }
}

float audioMixerGetVolume(const std::uint64_t mixerId)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_mixers.find(mixerId);
    if (it == g_mixers.end()) return 1.0f;
    return ma_sound_group_get_volume(&it->second.group);
}

void audioMixerSetVolume(const std::uint64_t mixerId, const float volume)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_mixers.find(mixerId);
    if (it == g_mixers.end()) return;
    ma_sound_group_set_volume(&it->second.group, volume);
}

std::uint64_t audioMixerPlay(const std::uint64_t mixerId, const std::uint64_t sampleId)
{
    AudioSample sample;
    ma_sound_group* pGroup = nullptr;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto busIt = g_mixers.find(mixerId);
        if (busIt == g_mixers.end()) return 0;
        const auto sampleIt = g_samples.find(sampleId);
        if (sampleIt == g_samples.end()) return 0;
        sample = sampleIt->second;
        pGroup = &busIt->second.group;
    }

    auto active = std::make_unique<ActiveAudio>();
    if (initSoundFromSample(sample, *active, 0, pGroup) != MA_SUCCESS) return 0;
    active->ownerBusId = mixerId;
    active->ownerIsScene = false;
    ma_sound_start(&active->sound);

    const std::uint64_t id = g_nextId++;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto busIt = g_mixers.find(mixerId);
        if (busIt != g_mixers.end())
            busIt->second.activeSounds.push_back(id);
        g_activeAudio[id] = std::move(active);
    }
    return id;
}

std::uint64_t audioGetMasterMixerId()
{
    return g_masterBusId;
}

int audioInit()
{
    ma_engine_config config = ma_engine_config_init();
    config.listenerCount = MA_ENGINE_MAX_LISTENERS;
    if (ma_engine_init(&config, &g_engine) != MA_SUCCESS) return 0;
    const ma_device* dev = ma_engine_get_device(&g_engine);
    g_deviceLatencyFrames = static_cast<ma_uint64>(dev->playback.internalPeriodSizeInFrames)
        * dev->playback.internalPeriods;
    g_masterBusId = audioMixerCreate(0);
    if (g_masterBusId == 0)
    {
        ma_engine_uninit(&g_engine);
        return 0;
    }
    return 1;
}

void audioShutdown()
{
    std::unordered_map<std::uint64_t, std::unique_ptr<ActiveAudio>> activeAudio;
    std::unordered_map<std::uint64_t, AudioMixer> buses;
    std::unordered_map<std::uint64_t, AudioScene> scenes;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        activeAudio = std::move(g_activeAudio);
        buses = std::move(g_mixers);
        scenes = std::move(g_scenes);
        g_samples.clear();
        g_nextListenerIndex = 0;
        g_freeListenerIndices.clear();
        g_masterBusId = 0;
    }
    // uninit sounds first — they reference groups
    for (auto& active : activeAudio | std::views::values) destroyActive(*active);
    for (auto& bus : buses | std::views::values)
    {
        ma_sound_group_uninit(&bus.group);
        ma_node_uninit(&bus.effectNode.base, nullptr);
    }
    for (auto& scene : scenes | std::views::values) ma_sound_group_uninit(&scene.group);
    ma_engine_uninit(&g_engine);
}

float audioGetVolume()
{
    return ma_engine_get_volume(&g_engine);
}

void audioSetVolume(float volume)
{
    ma_engine_set_volume(&g_engine, volume);
}

std::uint64_t audioMakeSampleFromFile(const char* filePath)
{
    if (!filePath) return 0;
    std::lock_guard<std::mutex> lock(g_mutex);
    const std::uint64_t id = g_nextId++;
    g_samples[id] = AudioSample{false, filePath, {}};
    return id;
}

std::uint64_t audioMakeSampleFromMemory(const std::uint8_t* data, const std::size_t size)
{
    if (!data || size == 0) return 0;
    std::lock_guard<std::mutex> lock(g_mutex);
    const std::uint64_t id = g_nextId++;
    AudioSample s;
    s.fileData.assign(data, data + size);
    g_samples[id] = std::move(s);
    return id;
}

std::uint64_t audioMakeStreamFromFile(const char* filePath)
{
    if (!filePath) return 0;
    std::lock_guard<std::mutex> lock(g_mutex);
    const std::uint64_t id = g_nextId++;
    g_samples[id] = AudioSample{true, filePath, {}};
    return id;
}

std::uint64_t audioMakeStreamFromMemory(const std::uint8_t* data, const std::size_t size)
{
    if (!data || size == 0) return 0;
    // Streaming from memory is not meaningful — treated as a decoded sample
    return audioMakeSampleFromMemory(data, size);
}

void audioSampleDispose(std::uint64_t sampleId)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    g_samples.erase(sampleId);
}

std::uint64_t audioSampleMakeActive(const std::uint64_t sampleId)
{
    AudioSample sample;
    ma_sound_group* masterGroup = nullptr;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto it = g_samples.find(sampleId);
        if (it == g_samples.end()) return 0;
        sample = it->second;
        const auto busIt = g_mixers.find(g_masterBusId);
        if (busIt != g_mixers.end())
            masterGroup = &busIt->second.group;
    }

    auto active = std::make_unique<ActiveAudio>();
    if (initSoundFromSample(sample, *active, 0, masterGroup) != MA_SUCCESS) return 0;
    active->ownerBusId = g_masterBusId;
    active->ownerIsScene = false;

    const std::uint64_t id = g_nextId++;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto busIt = g_mixers.find(g_masterBusId);
        if (busIt != g_mixers.end())
            busIt->second.activeSounds.push_back(id);
        g_activeAudio[id] = std::move(active);
    }
    return id;
}

int audioActiveIsPlaying(const std::uint64_t id)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0;
    return ma_sound_is_playing(&it->second->sound) ? 1 : 0;
}

double audioActiveGetPosition(const std::uint64_t id)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0.0;
    const ActiveAudio& active = *it->second;

    if (active.pushSource)
    {
        const ma_uint32 sr = active.pushSource->sampleRate;
        if (sr == 0) return 0.0;
        const ma_uint64 consumed = active.pushSource->framesConsumed.load(std::memory_order_acquire);
        const ma_uint64 adjusted = consumed > g_deviceLatencyFrames ? consumed - g_deviceLatencyFrames : 0;
        return static_cast<double>(adjusted) / sr;
    }

    ma_uint64 cursorFrames = 0;
    if (ma_sound_get_cursor_in_pcm_frames(&it->second->sound, &cursorFrames) != MA_SUCCESS) return 0.0;

    ma_uint32 sampleRate = 0;
    ma_sound_get_data_format(&it->second->sound, nullptr, nullptr, &sampleRate, nullptr, 0);
    if (sampleRate == 0) return 0.0;

    return static_cast<double>(cursorFrames) / sampleRate;
}

double audioActiveGetDuration(const std::uint64_t id)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0.0;
    float len = 0.0f;
    ma_sound_get_length_in_seconds(&it->second->sound, &len);
    return static_cast<double>(len);
}

int audioActivePlay(const std::uint64_t id, const int restart)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0;
    ma_sound* s = &it->second->sound;
    if (restart) ma_sound_seek_to_pcm_frame(s, 0);
    const ma_result r = ma_sound_start(s);
    return r == MA_SUCCESS ? 1 : 0;
}

int audioActivePause(const std::uint64_t id)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0;
    const ma_result r = ma_sound_stop(&it->second->sound);
    return r == MA_SUCCESS ? 1 : 0;
}

int audioActiveSetVolume(const std::uint64_t id, const float volume)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0;
    ma_sound_set_volume(&it->second->sound, volume);
    return 1;
}

int audioActiveSetPosition(std::uint64_t id, double positionSeconds)
{
    if (positionSeconds < 0.0) return 0;
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end()) return 0;
    ma_sound* s = &it->second->sound;
    ma_uint32 sampleRate = 0;
    ma_sound_get_data_format(s, nullptr, nullptr, &sampleRate, nullptr, 0);
    if (sampleRate == 0) return 0;
    const auto frame = static_cast<ma_uint64>(positionSeconds * sampleRate);
    return ma_sound_seek_to_pcm_frame(s, frame) == MA_SUCCESS ? 1 : 0;
}

void audioActiveDispose(const std::uint64_t id)
{
    std::unique_ptr<ActiveAudio> active;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto it = g_activeAudio.find(id);
        if (it == g_activeAudio.end()) return;
        active = std::move(it->second);
        g_activeAudio.erase(it);

        if (active->ownerBusId != 0)
        {
            if (active->ownerIsScene)
            {
                const auto sceneIt = g_scenes.find(active->ownerBusId);
                if (sceneIt != g_scenes.end())
                {
                    auto& v = sceneIt->second.activeSounds;
                    std::erase(v, id);
                }
            }
            else
            {
                const auto busIt = g_mixers.find(active->ownerBusId);
                if (busIt != g_mixers.end())
                {
                    auto& v = busIt->second.activeSounds;
                    std::erase(v, id);
                }
            }
        }
    }
    destroyActive(*active); // ma_sound_uninit outside the lock; audio thread doesn't use g_mutex
}

std::uint64_t audioMixerCreatePushStream(std::uint64_t mixerId, int sampleRate, int channels)
{
    if (sampleRate <= 0 || channels <= 0) return 0;

    auto active = std::make_unique<ActiveAudio>();
    auto pushSrc = std::make_unique<PushStreamSource>();

    pushSrc->format = ma_format_f32;
    pushSrc->channels = static_cast<ma_uint32>(channels);
    pushSrc->sampleRate = static_cast<ma_uint32>(sampleRate);

    ma_data_source_config dsConfig = ma_data_source_config_init();
    dsConfig.vtable = &g_pushSourceVtable;
    if (ma_data_source_init(&dsConfig, &pushSrc->base) != MA_SUCCESS)
        return 0;

    ma_sound_group* pGroup = nullptr;
    bool ownerIsScene = false;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        pGroup = getGroupForId(mixerId, &ownerIsScene);
    }
    if (!pGroup)
    {
        ma_data_source_uninit(&pushSrc->base);
        return 0;
    }

    if (ma_sound_init_from_data_source(&g_engine, pushSrc.get(), 0, pGroup, &active->sound) != MA_SUCCESS)
    {
        ma_data_source_uninit(&pushSrc->base);
        return 0;
    }

    active->pushSource = std::move(pushSrc);
    active->ownerBusId = mixerId;
    active->ownerIsScene = ownerIsScene;

    const std::uint64_t id = g_nextId++;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        if (ownerIsScene)
        {
            const auto it = g_scenes.find(mixerId);
            if (it != g_scenes.end()) it->second.activeSounds.push_back(id);
        }
        else
        {
            const auto it = g_mixers.find(mixerId);
            if (it != g_mixers.end()) it->second.activeSounds.push_back(id);
        }
        g_activeAudio[id] = std::move(active);
    }
    return id;
}

std::size_t audioPushStreamPush(std::uint64_t id, const std::uint8_t* data, std::size_t size)
{
    if (!data || size == 0) return 0;
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_activeAudio.find(id);
    if (it == g_activeAudio.end() || !it->second->pushSource) return 0;

    PushStreamSource* src = it->second->pushSource.get();
    std::lock_guard<std::mutex> srcLock(src->mutex);

    // Compact: shift unconsumed bytes to front so the buffer doesn't grow unboundedly
    if (src->readPos > 0)
    {
        const std::size_t remaining = src->buffer.size() - src->readPos;
        if (remaining > 0)
            memmove(src->buffer.data(), src->buffer.data() + src->readPos, remaining);
        src->buffer.resize(remaining);
        src->readPos = 0;
    }

    src->buffer.insert(src->buffer.end(), data, data + size);
    return size;
}

int audioMixerAddEffect(std::uint64_t mixerId, const AudioEffect& effect)
{
    const auto effectInstance = std::make_shared<AudioEffectInstance>();
    effectInstance->effect = effect;
    {
        std::lock_guard lock(g_mutex);
        if (const auto it = g_mixers.find(mixerId); it != g_mixers.end())
        {
            it->second.effectNode.effectManager->AddEffect(effectInstance);
            return 1;
        }
    }
    return 0;
}

void audioMixerRemoveEffect(std::uint64_t mixerId, std::uint64_t effectId)
{
    {
        std::lock_guard lock(g_mutex);
        if (const auto it = g_mixers.find(mixerId); it != g_mixers.end())
        {
            it->second.effectNode.effectManager->RemoveEffect(effectId);
        }
    }
}

std::uint64_t audioCreateScene(std::uint64_t parentId)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    ma_sound_group* pParent = parentId != 0 ? getGroupForId(parentId) : nullptr;
    if (parentId != 0 && !pParent) return 0;
    ma_uint32 listenerIndex;
    if (!g_freeListenerIndices.empty())
    {
        listenerIndex = *g_freeListenerIndices.begin();
        g_freeListenerIndices.erase(g_freeListenerIndices.begin());
    }
    else
    {
        if (g_nextListenerIndex >= MA_ENGINE_MAX_LISTENERS) return 0;
        listenerIndex = g_nextListenerIndex++;
    }
    const std::uint64_t id = g_nextId++;
    g_scenes[id] = AudioScene{listenerIndex};
    if (ma_sound_group_init(&g_engine, 0, pParent, &g_scenes[id].group) != MA_SUCCESS)
    {
        g_freeListenerIndices.insert(listenerIndex);
        g_scenes.erase(id);
        return 0;
    }
    return id;
}

void audioSceneSetListenerPose(std::uint64_t sceneId, Vec3 position, Vec3 forward, Vec3 right)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_scenes.find(sceneId);
    if (it == g_scenes.end()) return;
    const ma_uint32 li = it->second.listenerIndex;
    ma_engine_listener_set_position(&g_engine, li, position.x, position.y, position.z);
    ma_engine_listener_set_direction(&g_engine, li, forward.x, forward.y, forward.z);
    const float ux = right.y * forward.z - right.z * forward.y;
    const float uy = right.z * forward.x - right.x * forward.z;
    const float uz = right.x * forward.y - right.y * forward.x;
    ma_engine_listener_set_world_up(&g_engine, li, ux, uy, uz);
}

static std::uint64_t scenePlay(std::uint64_t sceneId, std::uint64_t sampleId, bool spatial, Vec3 location)
{
    AudioSample sample;
    ma_sound_group* pGroup = nullptr;
    ma_uint32 listenerIndex = 0;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto sceneIt = g_scenes.find(sceneId);
        if (sceneIt == g_scenes.end()) return 0;
        const auto sampleIt = g_samples.find(sampleId);
        if (sampleIt == g_samples.end()) return 0;
        sample = sampleIt->second;
        pGroup = &sceneIt->second.group;
        listenerIndex = sceneIt->second.listenerIndex;
    }

    auto active = std::make_unique<ActiveAudio>();
    if (initSoundFromSample(sample, *active, 0, pGroup) != MA_SUCCESS) return 0;
    active->ownerBusId = sceneId;
    active->ownerIsScene = true;

    ma_sound_set_spatialization_enabled(&active->sound, spatial ? MA_TRUE : MA_FALSE);
    if (spatial)
    {
        ma_sound_set_position(&active->sound, location.x, location.y, location.z);
        ma_sound_set_pinned_listener_index(&active->sound, listenerIndex);
    }
    ma_sound_start(&active->sound);

    const std::uint64_t id = g_nextId++;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto sceneIt = g_scenes.find(sceneId);
        if (sceneIt != g_scenes.end())
            sceneIt->second.activeSounds.push_back(id);
        g_activeAudio[id] = std::move(active);
    }
    return id;
}

std::uint64_t audioScenePlayAtLocation(std::uint64_t sceneId, std::uint64_t sampleId, Vec3 location)
{
    return scenePlay(sceneId, sampleId, true, location);
}

std::uint64_t audioScenePlay2d(std::uint64_t sceneId, std::uint64_t sampleId)
{
    return scenePlay(sceneId, sampleId, false, {});
}

float audioSceneGetVolume(std::uint64_t sceneId)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_scenes.find(sceneId);
    if (it == g_scenes.end()) return 1.0f;
    return ma_sound_group_get_volume(&it->second.group);
}

void audioSceneSetVolume(std::uint64_t sceneId, float volume)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    const auto it = g_scenes.find(sceneId);
    if (it == g_scenes.end()) return;
    ma_sound_group_set_volume(&it->second.group, volume);
}

void audioSceneDispose(std::uint64_t sceneId)
{
    std::vector<std::uint64_t> sounds;
    ma_uint32 listenerIndex = 0;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        const auto it = g_scenes.find(sceneId);
        if (it == g_scenes.end()) return;
        sounds = std::move(it->second.activeSounds);
        listenerIndex = it->second.listenerIndex;
    }

    for (auto soundId : sounds) audioActiveDispose(soundId);

    decltype(g_scenes)::node_type node;
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        node = g_scenes.extract(sceneId);
        if (!node.empty()) g_freeListenerIndices.insert(listenerIndex);
    }
    if (!node.empty()) ma_sound_group_uninit(&node.mapped().group);
}
