#include "video.hpp"

#include <atomic>
#include <iostream>
#include <memory.hpp>
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
};


struct VideoSource final : public wdx::ISource {
    SourceReadCallback readCallback{};
    SourceAvailableCallback availableCallback{};
    SourceLengthCallback lengthCallback{};
    void*userData{};
    VideoSource(SourceReadCallback read,SourceAvailableCallback available,SourceLengthCallback length,void* inUserData) : readCallback(read), availableCallback(available), lengthCallback(length), userData(inUserData) {}
    void Read(const std::int64_t& pos, std::span<std::uint8_t> data) override {
        readCallback(static_cast<unsigned long>(pos), static_cast<unsigned long>(data.size()), data.data(), userData);
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
void * videoContextCreate() {
    const auto ctx = new VideoDecodeContext{};
    ctx->decoder = std::make_shared<wdx::SourceDecoder>();
    ctx->decoder->SetVideoPacketCallback([ctx](const std::shared_ptr<wdx::Packet>& packet, wdx::IVideoDecoder* decoder) {
        decoder->Decode(packet);
        const auto frame = decoder->GetFrame();
        const double time = packet->GetTime();
        if (ctx->packetCount == 0) {
            auto vpacket = std::make_shared<VideoPacket>(time, frame);
            ctx->firstPacket = vpacket;
            ctx->lastPacket = vpacket;
            ++ctx->packetCount;
        }
        else {
            const auto p = std::make_shared<VideoPacket>(time, frame);
            ctx->lastPacket->next = p;
            ctx->lastPacket = p;
            ++ctx->packetCount;
        }
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
    videoContext->packetCount = 0;
    videoContext->firstPacket = {};
    videoContext->lastPacket = {};
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

void videoContextDecode(void *context, double delta) {
    const auto videoContext = static_cast<VideoDecodeContext *>(context);
    videoContext->decoder->Demux(delta);
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
    SourceLengthCallback lengthCallback,void*userData) {
    return new VideoSourceWrapper{std::make_shared<VideoSource>(readCallback,availableCallback,lengthCallback,userData)};
}

void videoSourceFree(void *source) {
    const auto videoSource = static_cast<VideoSourceWrapper *>(source);
    delete videoSource;
}
