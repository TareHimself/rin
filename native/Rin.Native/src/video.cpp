#include "video.hpp"

#include <atomic>
#include <cstring>
#include <memory.hpp>
#include <mutex>
#include <optional>
#include <vector>
#include <span>
#include <webmdx/SourceDecoder.h>
#include <webmdx/errors.h>
#include <memory>
#include <webmdx/IDecodedVideoFrame.h>

struct VideoPacket {
    double timestamp;
    std::shared_ptr<wdx::IDecodedVideoFrame> data{};
    std::shared_ptr<VideoPacket> next{};
    VideoPacket(const double& _timestamp,const std::shared_ptr<wdx::IDecodedVideoFrame>& _data) : data(_data), timestamp(_timestamp) {}
};

// What the headers said, copied out once so other threads can read it while the decoder works.
struct VideoTrackInfo {
    bool hasVideo{};
    Extent2D extent{};
    std::size_t lumaByteSize{};
    std::size_t chromaRowBytes{};
    std::size_t chromaRows{};
    std::size_t frameByteSize{};
    int audioTrackCount{};
    int audioSampleRate{};
    int audioChannels{};
    double duration{};
};

struct VideoDecodeContext {
    std::shared_ptr<wdx::SourceDecoder> decoder{};
    std::atomic<std::size_t> packetCount{};
    std::shared_ptr<VideoPacket> firstPacket{};
    std::shared_ptr<VideoPacket> lastPacket{};
    AudioCallback audioCallback{nullptr};
    void*audioCallbackUserData{};
    // Lets a repeat query for the same display time skip the conversion.
    std::optional<double> lastReturnedTimestamp{};
    // Written under decoderMutex before ready is set, read freely once it is.
    VideoTrackInfo info{};
    std::atomic<bool> ready{};
    // Held for a whole demux, seek or source change. Taken before queueMutex, never after it.
    std::mutex decoderMutex;
    // Guards the packet list, which the decode thread appends to while the render thread reads it.
    std::mutex queueMutex;
};


struct VideoSource final : public wdx::ISource {
    VideoSourceCallbacks callbacks{};
    explicit VideoSource(const VideoSourceCallbacks& inCallbacks) : callbacks(inCallbacks) {}

    void Read(const std::int64_t& pos, std::span<std::uint8_t> data) override {
        callbacks.read(pos, data.size(), data.data(), callbacks.userData);
    }

    [[nodiscard]] std::int64_t GetLength() const override {
        return callbacks.length(callbacks.userData);
    }

    [[nodiscard]] std::int64_t GetAvailable() const override {
        return callbacks.available(callbacks.userData);
    }

    [[nodiscard]] std::int64_t GetAvailableFrom(const std::int64_t& pos) const override {
        return callbacks.availableFrom(pos, callbacks.userData);
    }

    void MakeAvailable(const std::uint64_t& size) override {
        const auto available = GetAvailable();
        if (static_cast<std::int64_t>(size) > available) {
            MakeRangeAvailable(available, size - static_cast<std::uint64_t>(available));
        }
    }

    void MakeRangeAvailable(const std::int64_t& pos, const std::uint64_t& size) override {
        callbacks.makeRangeAvailable(pos, size, callbacks.userData);
    }

    void ReleaseBefore(const std::int64_t& pos) override {
        callbacks.releaseBefore(pos, callbacks.userData);
    }

    ~VideoSource() override = default;
};

struct VideoSourceWrapper {
    std::shared_ptr<VideoSource> source{};
};

static int toResult(const wdx::DemuxResult result) {
    switch (result) {
        case wdx::DemuxResult::IncompleteHeader: return VIDEO_DEMUX_INCOMPLETE_HEADER;
        case wdx::DemuxResult::IncompleteCluster: return VIDEO_DEMUX_INCOMPLETE_CLUSTER;
        case wdx::DemuxResult::Success: return VIDEO_DEMUX_SUCCESS;
        case wdx::DemuxResult::Finished: return VIDEO_DEMUX_FINISHED;
    }
    return VIDEO_DEMUX_ERROR;
}

// Runs a decoder step under decoderMutex. Bad data throws inside webmdx, which must not unwind into managed code.
template <typename Step>
static int guardedStep(VideoDecodeContext* ctx, Step&& step) {
    std::lock_guard lock(ctx->decoderMutex);
    try {
        return toResult(step());
    } catch (const std::exception&) {
        return VIDEO_DEMUX_ERROR;
    }
}

static void clearQueuedFrames(VideoDecodeContext* ctx) {
    std::lock_guard lock(ctx->queueMutex);
    ctx->packetCount = 0;
    ctx->firstPacket = {};
    ctx->lastPacket = {};
    ctx->lastReturnedTimestamp = {};
}

// Leaves the frame that should be on screen at `timestamp` at the front of the queue.
static void dropFramesBefore(VideoDecodeContext* ctx, double timestamp) {
    while (ctx->firstPacket && ctx->firstPacket->next && ctx->firstPacket->next->timestamp <= timestamp) {
        ctx->firstPacket = ctx->firstPacket->next;
        --ctx->packetCount;
    }
}

// A decoder can hold frames back, so one packet yields zero, one or several frames.
static void queueDecodedFrames(VideoDecodeContext* ctx, wdx::IVideoDecoder* decoder) {
    while (const auto frame = decoder->GetFrame()) {
        const auto queued = std::make_shared<VideoPacket>(frame->GetTime(), frame);
        std::lock_guard lock(ctx->queueMutex);
        if (ctx->lastPacket) {
            ctx->lastPacket->next = queued;
        } else {
            ctx->firstPacket = queued;
        }
        ctx->lastPacket = queued;
        ++ctx->packetCount;
    }
}

// Called under decoderMutex once the decoder has read the headers.
static void captureTrackInfo(VideoDecodeContext* ctx) {
    if (ctx->ready.load(std::memory_order_acquire) || !ctx->decoder->IsReady()) {
        return;
    }
    VideoTrackInfo info{};
    info.hasVideo = ctx->decoder->HasVideo();
    if (info.hasVideo) {
        const auto track = ctx->decoder->GetVideoTrack();
        info.extent = Extent2D{static_cast<std::uint32_t>(track.width), static_cast<std::uint32_t>(track.height)};
        info.lumaByteSize = static_cast<std::size_t>(track.width) * track.height;
        info.chromaRowBytes = static_cast<std::size_t>((track.width + 1) / 2) * 2;
        info.chromaRows = static_cast<std::size_t>((track.height + 1) / 2);
        info.frameByteSize = info.lumaByteSize + info.chromaRowBytes * info.chromaRows;
    }
    if (ctx->decoder->HasAudio()) {
        const auto track = ctx->decoder->GetAudioTrack(0);
        info.audioTrackCount = ctx->decoder->GetAudioTrackCount();
        info.audioSampleRate = track.sampleRate;
        info.audioChannels = track.channels;
    }
    info.duration = ctx->decoder->GetDuration();
    ctx->info = info;
    ctx->ready.store(true, std::memory_order_release);
}

static const VideoTrackInfo& trackInfo(VideoDecodeContext* ctx) {
    static const VideoTrackInfo none{};
    return ctx->ready.load(std::memory_order_acquire) ? ctx->info : none;
}

void * videoContextCreate() {
    const auto ctx = new VideoDecodeContext{};
    ctx->decoder = std::make_shared<wdx::SourceDecoder>();
    ctx->decoder->SetVideoPacketCallback([ctx](const std::shared_ptr<wdx::Packet>& packet, wdx::IVideoDecoder* decoder) {
        decoder->Decode(packet);
        queueDecodedFrames(ctx, decoder);
    });
    ctx->decoder->SetAudioPacketCallback([ctx](const std::shared_ptr<wdx::Packet>& packet, wdx::IAudioDecoder* decoder) {
        if (ctx->audioCallback != nullptr) {
            std::vector<float> pcm;
            decoder->Decode(packet, pcm);
            ctx->audioCallback(pcm.data(), static_cast<int>(pcm.size()), packet->GetTime(), ctx->audioCallbackUserData);
        }
    });

    return ctx;
}

int videoContextEnsureHeader(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return guardedStep(videoContext, [videoContext] {
        const auto result = videoContext->decoder->EnsureHeader();
        captureTrackInfo(videoContext);
        return result;
    });
}

int videoContextIsReady(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->ready.load(std::memory_order_acquire);
}

int videoContextHasVideo(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).hasVideo;
}

Extent2D videoContextGetVideoExtent(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).extent;
}

int videoContextSeek(void *context, double time) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return guardedStep(videoContext, [videoContext, time] {
        const auto result = videoContext->decoder->Seek(time);
        captureTrackInfo(videoContext);
        if (result == wdx::DemuxResult::Success) {
            clearQueuedFrames(videoContext);
        }
        return result;
    });
}

int videoContextHasAudio(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).audioTrackCount > 0;
}

void videoContextSetAudioCallback(void *context, AudioCallback audioCallback,void*userData) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    videoContext->audioCallbackUserData = userData;
    videoContext->audioCallback = audioCallback;
}


int videoContextGetAudioSampleRate(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).audioSampleRate;
}

int videoContextGetAudioChannels(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).audioChannels;
}

int videoContextGetAudioTrackCount(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).audioTrackCount;
}

void videoContextSetAudioTrack(void *context, int track) {
}

double videoContextGetDuration(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).duration;
}

double videoContextGetPosition(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetPosition();
}

int videoContextDecode(void *context, double delta) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return guardedStep(videoContext, [videoContext, delta] {
        const auto result = videoContext->decoder->Demux(delta);
        captureTrackInfo(videoContext);
        // The decoder is flushed at the end of the stream, which releases the frames it held back.
        if (result == wdx::DemuxResult::Finished && videoContext->decoder->HasVideo()) {
            queueDecodedFrames(videoContext, videoContext->decoder->GetVideoDecoder());
        }
        return result;
    });
}

VideoByteRange videoContextGetNeededRange(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    std::lock_guard lock(videoContext->decoderMutex);
    const auto range = videoContext->decoder->GetNeededRange();
    return VideoByteRange{range.start, range.end};
}

int videoContextEnded(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetPosition() >= trackInfo(videoContext).duration;
}

int videoContextGetBufferedFrameCount(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return static_cast<int>(videoContext->packetCount.load());
}

void videoContextDropFramesBefore(void *context, double timestamp) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    std::lock_guard lock(videoContext->queueMutex);
    dropFramesBefore(videoContext, timestamp);
}

std::uint64_t videoContextGetFrameByteSize(void *context) {
    return trackInfo(static_cast<VideoDecodeContext *>(context)).frameByteSize;
}

// webmdx writes the UV plane with a row stride of `width` bytes, which is one byte short of a row of
// chroma pairs when the width is odd, so those rows are spread out again after the fact.
static void writeNv12(const VideoTrackInfo& info, wdx::IDecodedVideoFrame& frame, std::uint8_t* destination) {
    const auto luma = std::span(destination, info.lumaByteSize);
    const auto chroma = std::span(destination + info.lumaByteSize, info.chromaRowBytes * info.chromaRows);
    const std::size_t writtenRowBytes = info.extent.width;
    if (writtenRowBytes == info.chromaRowBytes) {
        frame.ToNV12(luma, chroma);
        return;
    }

    std::vector<std::uint8_t> packed(writtenRowBytes * info.chromaRows + 1);
    frame.ToNV12(luma, packed);
    for (std::size_t row = info.chromaRows; row-- > 0;) {
        std::memcpy(chroma.data() + row * info.chromaRowBytes, packed.data() + row * writtenRowBytes, writtenRowBytes);
        chroma[row * info.chromaRowBytes + writtenRowBytes] = chroma[row * info.chromaRowBytes + writtenRowBytes - 2];
    }
}

int videoContextCopyRecentFrame(void *context, double timestamp, std::uint8_t *destination, std::uint64_t size) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    const auto& info = trackInfo(videoContext);
    const auto frameByteSize = info.frameByteSize;
    std::shared_ptr<VideoPacket> packet;
    {
        std::lock_guard lock(videoContext->queueMutex);
        dropFramesBefore(videoContext, timestamp);
        packet = videoContext->firstPacket;
        if (!packet || packet->timestamp == videoContext->lastReturnedTimestamp || size < frameByteSize) {
            return 0;
        }
        videoContext->lastReturnedTimestamp = packet->timestamp;
    }

    writeNv12(info, *packet->data, destination);
    return 1;
}

void videoContextSetSource(void *context, void *source) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    std::lock_guard lock(videoContext->decoderMutex);
    videoContext->ready.store(false, std::memory_order_release);
    videoContext->info = {};
    clearQueuedFrames(videoContext);
    videoContext->decoder->SetSource(videoSource->source);
}

void videoContextFree(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    delete videoContext;
}

void * videoSourceCreate(const VideoSourceCallbacks* callbacks) {
    return new VideoSourceWrapper{std::make_shared<VideoSource>(*callbacks)};
}

void videoSourceFree(void *source) {
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    delete videoSource;
}
