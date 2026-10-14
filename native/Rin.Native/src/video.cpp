#include "video.hpp"

#include <atomic>
#include <iostream>
#include <memory.hpp>
#include <mutex>
#include <optional>
#include <vector>
#include <span>
#include <webmdx/SourceDecoder.h>
#include <webmdx/FileSource.h>
#include <list>
#include <memory>
#include <webmdx/IDecodedVideoFrame.h>

struct VideoPacket {
    double timestamp;
    std::shared_ptr<wdx::IDecodedVideoFrame> data{};
    std::shared_ptr<VideoPacket> next{};
    VideoPacket(const double& _timestamp,const std::shared_ptr<wdx::IDecodedVideoFrame>& _data) : data(_data), timestamp(_timestamp) {}
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
    std::size_t frameByteSize{};
    // Held for a whole demux, seek or source change. Taken before queueMutex, never after it.
    std::mutex decoderMutex;
    // Guards the packet list, which the decode thread appends to while the render thread reads it.
    std::mutex queueMutex;
};


struct VideoSource final : public wdx::ISource {
    SourceReadCallback readCallback{};
    SourceAvailableCallback availableCallback{};
    SourceLengthCallback lengthCallback{};
    void*userData{};
    VideoSource(SourceReadCallback read,SourceAvailableCallback available,SourceLengthCallback length,void* inUserData) : readCallback(read), availableCallback(available), lengthCallback(length), userData(inUserData) {}
    void Read(const std::int64_t& pos, std::span<std::uint8_t> data) override {
        readCallback(static_cast<std::uint64_t>(pos), data.size(), data.data(), userData);
    }

    [[nodiscard]] std::int64_t GetLength() const override {
        return static_cast<std::int64_t>(lengthCallback(userData));
    }

    [[nodiscard]] std::int64_t GetAvailable() const override {
        return static_cast<std::int64_t>(availableCallback(userData));
    }

    void MakeAvailable(const std::uint64_t&) override {}

    ~VideoSource() override = default;
};

struct VideoSourceWrapper {
    std::shared_ptr<VideoSource> source{};
};
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

int videoContextHasVideo(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->HasVideo();
}

Extent2D videoContextGetVideoExtent(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    if (!videoContext->decoder->HasVideo()) {
        return {};
    }
    const auto videoTrack = videoContext->decoder->GetVideoTrack();
    return Extent2D{static_cast<uint32_t>(videoTrack.width),static_cast<uint32_t>(videoTrack.height)};
}

void videoContextSeek(void *context, double time) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    std::lock_guard lock(videoContext->decoderMutex);
    clearQueuedFrames(videoContext);
    videoContext->decoder->Seek(time);
}

int videoContextHasAudio(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetAudioTrackCount() > 0;
}

void videoContextSetAudioCallback(void *context, AudioCallback audioCallback,void*userData) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    videoContext->audioCallbackUserData = userData;
    videoContext->audioCallback = audioCallback;
}


int videoContextGetAudioSampleRate(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    if (!videoContext->decoder->HasAudio()) {
        return 0;
    }
    auto track = videoContext->decoder->GetAudioTrack(0);
    return track.sampleRate;
}

int videoContextGetAudioChannels(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    if (!videoContext->decoder->HasAudio()) {
        return 0;
    }
    auto track = videoContext->decoder->GetAudioTrack(0);
    return track.channels;
}
int videoContextGetAudioTrackCount(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    if (!videoContext->decoder->HasAudio()) {
        return 0;
    }
    return videoContext->decoder->GetAudioTrackCount();
}

void videoContextSetAudioTrack(void *context, int track) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    if (!videoContext->decoder->HasAudio()) {
        return;
    }

}

double videoContextGetDuration(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetDuration();
}

double videoContextGetPosition(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetPosition();
}

int videoContextDecode(void *context, double delta) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    std::lock_guard lock(videoContext->decoderMutex);
    const auto result = videoContext->decoder->Demux(delta);
    // The decoder is flushed at the end of the stream, which releases the frames it held back.
    if (result == wdx::DemuxResult::Finished && videoContext->decoder->HasVideo()) {
        queueDecodedFrames(videoContext, videoContext->decoder->GetVideoDecoder());
    }
    return result == wdx::DemuxResult::Success;
}

int videoContextEnded(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetPosition() >= videoContext->decoder->GetDuration();
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

int videoContextCopyRecentFrame(void *context, double timestamp, std::uint8_t *destination, std::uint64_t size) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    std::shared_ptr<VideoPacket> packet;
    {
        std::lock_guard lock(videoContext->queueMutex);
        dropFramesBefore(videoContext, timestamp);
        packet = videoContext->firstPacket;
        if (!packet || packet->timestamp == videoContext->lastReturnedTimestamp || size < videoContext->frameByteSize) {
            return 0;
        }
        videoContext->lastReturnedTimestamp = packet->timestamp;
    }

    packet->data->ToRgba(std::span(destination, size));
    return 1;
}

void videoContextSetSource(void *context, void *source) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    std::lock_guard lock(videoContext->decoderMutex);
    clearQueuedFrames(videoContext);
    videoContext->decoder->SetSource(videoSource->source);
    std::size_t frameByteSize = 0;
    if (videoContext->decoder->HasVideo()) {
        const auto track = videoContext->decoder->GetVideoTrack();
        frameByteSize = static_cast<std::size_t>(track.width) * track.height * 4;
    }
    std::lock_guard queueLock(videoContext->queueMutex);
    videoContext->frameByteSize = frameByteSize;
}

void videoContextFree(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    delete videoContext;
}

void * videoSourceCreate(SourceReadCallback readCallback, SourceAvailableCallback availableCallback,
    SourceLengthCallback lengthCallback,void*userData) {
    return new VideoSourceWrapper{std::make_shared<VideoSource>(readCallback,availableCallback,lengthCallback,userData)};
}

void videoSourceFree(void *source) {
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    delete videoSource;
}
