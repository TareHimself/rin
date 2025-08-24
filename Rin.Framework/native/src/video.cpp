#define PLM_MALLOC memoryAllocate
#define PLM_REALLOC memoryReAllocate
#define PLM_FREE memoryFree;
#define PL_MPEG_IMPLEMENTATION
#include "video.hpp"

#include <atomic>
#include <iostream>
#include <memory.hpp>
#include <vector>
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
};


struct VideoSource final : public wdx::ISource {
    SourceReadCallback readCallback{};
    SourceAvailableCallback availableCallback{};
    SourceLengthCallback lengthCallback{};
    VideoSource(SourceReadCallback read,SourceAvailableCallback available,SourceLengthCallback length) : readCallback(read), availableCallback(available), lengthCallback(length) {}
    void Read(const std::size_t pos, const std::size_t size, unsigned char *data) override {
        readCallback(pos, size, data);
    }

    [[nodiscard]] std::size_t GetLength() const override {
        return lengthCallback();
    }

    [[nodiscard]] std::size_t GetAvailable() const override {
        return availableCallback();
    }

    [[nodiscard]] bool IsWriting() const override {
        return false;
    }

    ~VideoSource() override = default;
};

struct VideoSourceWrapper {
    std::shared_ptr<VideoSource> source{};
};
void * videoContextCreate() {
    const auto ctx = new VideoDecodeContext{};
    ctx->decoder = std::make_shared<wdx::SourceDecoder>();
    ctx->decoder->SetVideoCallback([ctx](const double time,const std::shared_ptr<wdx::IDecodedVideoFrame>& data) {
        if (ctx->packetCount == 0) {
            auto packet = std::make_shared<VideoPacket>(time, data);
            ctx->firstPacket = packet;
            ctx->lastPacket = packet;
            ++ctx->packetCount;
        }
        else {
            const auto p = std::make_shared<VideoPacket>(time, data);
            ctx->lastPacket->next = p;
            ctx->lastPacket = p;
            ++ctx->packetCount;
        }
    });
    ctx->decoder->SetAudioCallback([ctx](const double time,const std::span<float>& data) {
        if (ctx->audioCallback != nullptr) {
            ctx->audioCallback(data.data(),data.size(),time);
        }
    });

    return ctx;
}

void * videoContextFromFile(const char *filename) {
    const auto ctx = new VideoDecodeContext{};
    ctx->decoder = std::make_shared<wdx::SourceDecoder>();
    ctx->decoder->SetSource(std::make_shared<wdx::FileSource>(filename));
    ctx->decoder->SetVideoCallback([ctx](const double time,const std::shared_ptr<wdx::IDecodedVideoFrame>& data) {
        if (ctx->packetCount == 0) {
            auto packet = std::make_shared<VideoPacket>(time, data);
            ctx->firstPacket = packet;
            ctx->lastPacket = packet;
            ++ctx->packetCount;
        }
        else {
            const auto p = std::make_shared<VideoPacket>(time, data);
            ctx->lastPacket->next = p;
            ctx->lastPacket = p;
            ++ctx->packetCount;
        }
    });
    ctx->decoder->SetAudioCallback([ctx](const double time,const std::span<float>& data) {
        if (ctx->audioCallback != nullptr) {
            ctx->audioCallback(data.data(),data.size(),time);
        }
    });

    return ctx;
}

void * videoContextFromSource(void *source) {
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    const auto ctx = new VideoDecodeContext{};
    ctx->decoder = std::make_shared<wdx::SourceDecoder>();
    ctx->decoder->SetSource(videoSource->source);
    ctx->decoder->SetVideoCallback([ctx](const double time,const std::shared_ptr<wdx::IDecodedVideoFrame>& data) {
        if (ctx->packetCount == 0) {
            auto packet = std::make_shared<VideoPacket>(time, data);
            ctx->firstPacket = packet;
            ctx->lastPacket = packet;
            ++ctx->packetCount;
        }
        else {
            const auto p = std::make_shared<VideoPacket>(time, data);
            ctx->lastPacket->next = p;
            ctx->lastPacket = p;
            ++ctx->packetCount;
        }
    });
    ctx->decoder->SetAudioCallback([ctx](const double time,const std::span<float>& data) {
        if (ctx->audioCallback != nullptr) {
            ctx->audioCallback(data.data(),data.size(),time);
        }
    });

    return ctx;
}

int videoContextHasVideo(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->HasVideo();
}

rwin::Extent2D videoContextGetVideoExtent(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    if (!videoContext->decoder->HasVideo()) {
        return {};
    }
    const auto videoTrack = videoContext->decoder->GetVideoTrack();
    return rwin::Extent2D{static_cast<uint32_t>(videoTrack.width),static_cast<uint32_t>(videoTrack.height)};
}

void videoContextSeek(void *context, double time) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    videoContext->packetCount = 0;
    videoContext->firstPacket = {};
    videoContext->lastPacket = {};
    videoContext->decoder->Seek(time);
}

int videoContextHasAudio(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetAudioTrackCount() > 0;
}

void videoContextSetAudioCallback(void *context, AudioCallback audioCallback) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
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

void videoContextDecode(void *context, double delta) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    const auto result = videoContext->decoder->Decode(delta);
}

int videoContextEnded(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    return videoContext->decoder->GetPosition() >= videoContext->decoder->GetDuration();
}

void * videoContextCopyRecentFrame(void *context, double timestamp) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    const auto track = videoContext->decoder->GetVideoTrack();
    const auto byteSize = track.width * track.height * 4;
    const auto data = new uint8_t[byteSize];

    if (videoContext->packetCount == 0) {
        return data;
    }

    auto packet = videoContext->firstPacket;

    auto skipped = 0;
    while (packet->timestamp <= timestamp && packet->next) {
        const auto next = packet->next;
        if (next->timestamp > timestamp) break;
        videoContext->firstPacket = next;
        --videoContext->packetCount;
        packet = next;
        skipped += 1;
    }

    packet->data->ToRgba(std::span(data,byteSize));
    return data;
}

void videoContextSetSource(void *context, void *source) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    videoContext->packetCount = 0;
    videoContext->firstPacket = {};
    videoContext->lastPacket = {};
    videoContext->decoder->SetSource(videoSource->source);
}

void videoContextFree(void *context) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    delete videoContext;
}

void * videoSourceCreate(SourceReadCallback readCallback, SourceAvailableCallback availableCallback,
    SourceLengthCallback lengthCallback) {
    return new VideoSourceWrapper{std::make_shared<VideoSource>(readCallback,availableCallback,lengthCallback)};
}

void videoSourceFree(void *source) {
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    delete videoSource;
}
